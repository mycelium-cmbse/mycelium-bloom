// ------------------------------------------------------------------------------------------------
// <copyright file="ModelLoaderService.Upload.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using System.Security.Cryptography;
    using System.Text.Json;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Serializer.Json;
    using DtoElement = SysML2.NET.Core.DTO.Root.Elements.IElement;
    using DtoNamespace = SysML2.NET.Core.DTO.Root.Namespaces.INamespace;

    public sealed partial class ModelLoaderService
    {
        /// <summary>Bounds uploaded JSON nesting independently of SDK ownership depth.</summary>
        private static readonly JsonSerializerOptions UploadJsonOptions = new() { MaxDepth = 64 };

        /// <summary>Retains canonical upload contexts by content fingerprint until their final lease is released.</summary>
        private readonly
            System.Collections.Concurrent.ConcurrentDictionary<string, (INamespace Root, SysML2.NET.Dal.IAssembler Owner
                )> uploadedModels = new(StringComparer.Ordinal);

        /// <summary>Resolves a model-local identity through its accepted SDK owner.</summary>
        /// <param name="root">The exact accepted canonical root.</param>
        /// <param name="elementId">The validated model-local identity.</param>
        /// <returns>The canonical object, or null when absent.</returns>
        public SysML2.NET.Core.POCO.Root.Elements.IElement FindUploadedElement(INamespace root, string elementId)
        {
            if (!Guid.TryParse(elementId, out var id))
            {
                return null;
            }

            var owner = this.uploadedModels.Values.FirstOrDefault(value => ReferenceEquals(value.Root, root)).Owner;
            return owner is not null && owner.Cache.TryGetValue(id, out var element) ? element.Value : null;
        }

        /// <summary>Resolves an unscoped upload identity only when one canonical owner matches.</summary>
        /// <param name="elementId">The validated model-local identity.</param>
        /// <returns>The unambiguous canonical object, or null.</returns>
        public SysML2.NET.Core.POCO.Root.Elements.IElement FindUploadedElement(string elementId)
        {
            if (!Guid.TryParse(elementId, out var id))
            {
                return null;
            }

            var matches = this.uploadedModels.Values.Where(model => model.Owner.Cache.ContainsKey(id)).Take(2)
                .ToArray();
            return matches.Length == 1 ? matches[0].Owner.Cache[id].Value : null;
        }

        /// <summary>Checks whether any accepted SDK owner contains this identity.</summary>
        /// <param name="elementId">The model-local identity.</param>
        /// <returns>Whether unscoped resolution must consider uploaded models.</returns>
        public bool HasUploadedIdentity(string elementId) => Guid.TryParse(elementId, out var id)
                                                             && this.uploadedModels.Values.Any(model =>
                                                                 model.Owner.Cache.ContainsKey(id));

        /// <summary>Validates and assembles a candidate before synchronized session acceptance.</summary>
        /// <param name="stream">The bounded seekable upload.</param>
        /// <param name="accept">The owning session's atomic publication boundary.</param>
        /// <param name="cancellationToken">Cancels validation and acceptance.</param>
        /// <returns>The accepted canonical root.</returns>
        public Task<INamespace> OpenUploadedModelAsync(Stream stream,
            Func<Func<INamespace>, Task<INamespace>> accept, CancellationToken cancellationToken) =>
            this.OpenCandidateAsync(stream, accept, false, cancellationToken);

        /// <summary>Validates and assembles a candidate before synchronized session acceptance.</summary>
        /// <param name="stream">The bounded seekable upload.</param>
        /// <param name="accept">The owning session's atomic publication boundary.</param>
        /// <param name="cancellationToken">Cancels validation and acceptance.</param>
        /// <returns>The accepted canonical root.</returns>
        public Task<INamespace> OpenIndependentModelAsync(Stream stream,
            Func<Func<INamespace>, Task<INamespace>> accept, CancellationToken cancellationToken) =>
            this.OpenCandidateAsync(stream, accept, true, cancellationToken);

        /// <summary>Releases the accepted SDK owner after its final model consumer closes.</summary>
        /// <param name="root">The released canonical root.</param>
        public void ReleaseUploadedModel(INamespace root)
        {
            foreach (var entry in this.uploadedModels.Where(entry => ReferenceEquals(entry.Value.Root, root)))
            {
                this.uploadedModels.TryRemove(entry.Key, out _);
            }
        }

        /// <summary>Validates and assembles an isolated candidate before the caller publishes its context.</summary>
        /// <param name="stream">The bounded, seekable session-upload stream.</param>
        /// <param name="cancellationToken">Cancels validation or acceptance.</param>
        /// <param name="accept">The owning session’s synchronized acceptance boundary.</param>
        /// <param name="independent">Whether canonical IDs may overlap other isolated model contexts.</param>
        /// <returns>The accepted canonical root.</returns>
        private async Task<INamespace> OpenCandidateAsync(Stream stream,
            Func<Func<INamespace>, Task<INamespace>> accept, bool independent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(accept);
            if (!stream.CanSeek || stream.Length is <= 0 or > 67_108_864)
            {
                throw new InvalidDataException("Select a non-empty SysML v2 JSON file up to 64 MiB.");
            }

            stream.Position = 0;
            var digest = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
            if (this.uploadedModels.TryGetValue(digest, out var existing))
            {
                return await accept(() =>
                {
                    this.uploadedModels.TryAdd(digest, existing);
                    return this.uploadedModels[digest].Root;
                });
            }

            var ids = await ValidateUploadAsync(stream, cancellationToken);
            if (!independent && ids.Any(id =>
                    this.assembler.Cache.ContainsKey(id) ||
                    this.uploadedModels.Values.Any(accepted => accepted.Owner.Cache.ContainsKey(id))))
            {
                throw new InvalidDataException(
                    "This file conflicts with elements already loaded in this session. Open it in a new browser session to preserve existing work.");
            }

            stream.Position = 0;
            var data = (await this.deSerializer.DeSerializeAsync(stream, SerializationModeKind.JSON,
                SerializationTargetKind.PSM, false, cancellationToken)).ToList();
            var elements = data.OfType<DtoElement>().ToList();
            var roots = elements.OfType<DtoNamespace>().Where(element => !element.OwningRelationship.HasValue).ToList();
            if (elements.Count != data.Count || roots.Count != 1)
            {
                throw new InvalidDataException("The file must contain SysML elements with exactly one root namespace.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var candidate = this.createUploadAssembler();
            if (ReferenceEquals(candidate, this.assembler))
            {
                throw new InvalidOperationException("Upload assembly requires an isolated identity owner.");
            }

            candidate.Synchronize(elements);
            if (!candidate.Cache.TryGetValue(roots[0].Id, out var root) || root.Value is not INamespace model)
            {
                throw new InvalidDataException("The SDK could not assemble the model root.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await accept(() =>
            {
                if (!this.uploadedModels.TryAdd(digest, (model, candidate)))
                {
                    throw new InvalidOperationException("The upload was already accepted.");
                }

                return model;
            });
        }

        /// <summary>Checks bounded, self-contained input before the SDK creates canonical objects.</summary>
        /// <param name="stream">The bounded seekable upload.</param>
        /// <param name="cancellationToken">Cancels input validation.</param>
        /// <returns>The validated SDK identities.</returns>
        private static async Task<HashSet<Guid>> ValidateUploadAsync(Stream stream, CancellationToken cancellationToken)
        {
            stream.Position = 0;
            var ids = new HashSet<Guid>();
            var references = new HashSet<Guid>();
            var owners = new Dictionary<Guid, Guid>();
            await foreach (var element in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream,
                               UploadJsonOptions, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (element.ValueKind != JsonValueKind.Object
                    || !element.TryGetProperty("@id", out var identity) || !identity.TryGetGuid(out var id) ||
                    id == Guid.Empty
                    || !element.TryGetProperty("elementId", out var semantic) ||
                    !semantic.TryGetGuid(out var semanticId) || semanticId != id
                    || !element.TryGetProperty("@type", out var type) || type.ValueKind != JsonValueKind.String
                    || !ids.Add(id) || ids.Count > 1_000_000)
                {
                    throw new InvalidDataException("The file must contain uniquely identified SysML v2 JSON elements.");
                }

                CollectReferences(element, references);
                if ((element.TryGetProperty("owningRelationship", out var owner) ||
                     element.TryGetProperty("owningRelatedElement", out owner))
                    && owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty("@id", out var ownerId))
                {
                    owners.Add(id, ownerId.GetGuid());
                }
            }

            if (!references.IsSubsetOf(ids))
            {
                throw new InvalidDataException(
                    "This session opener requires a self-contained model; an element reference is missing from the file.");
            }

            ValidateOwnership(owners, cancellationToken);
            return ids;
        }

        /// <summary>Collects SDK identities without resolving external resources.</summary>
        /// <param name="element">The JSON value being validated.</param>
        /// <param name="references">The complete set of encountered references.</param>
        private static void CollectReferences(JsonElement element, HashSet<Guid> references)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in element.EnumerateArray())
                {
                    CollectReferences(child, references);
                }
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("@id"))
                    {
                        if (!property.Value.TryGetGuid(out var id))
                        {
                            throw new InvalidDataException("An element reference is invalid.");
                        }

                        references.Add(id);
                    }
                    else
                    {
                        CollectReferences(property.Value, references);
                    }
                }
            }
        }

        /// <summary>Rejects cycles and chains exceeding 1,024 ownership links regardless of input ordering.</summary>
        /// <param name="owners">The complete child-to-owner identity index.</param>
        /// <param name="cancellationToken">Cancels traversal before candidate assembly.</param>
        private static void ValidateOwnership(Dictionary<Guid, Guid> owners, CancellationToken cancellationToken)
        {
            var depths = new Dictionary<Guid, int>();
            foreach (var id in owners.Keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = new HashSet<Guid>();
                var ancestors = new List<Guid>();
                var current = id;
                while (!depths.ContainsKey(current) && owners.TryGetValue(current, out var parent))
                {
                    if (!path.Add(current) || path.Count > 1024)
                    {
                        throw new InvalidDataException("The file contains cyclic or excessively deep ownership.");
                    }

                    ancestors.Add(current);
                    current = parent;
                }

                var depth = depths.GetValueOrDefault(current);
                if (depth + ancestors.Count > 1024)
                {
                    throw new InvalidDataException("The file contains cyclic or excessively deep ownership.");
                }

                for (var index = ancestors.Count - 1; index >= 0; index--)
                {
                    depths.Add(ancestors[index], ++depth);
                }
            }
        }
    }
}