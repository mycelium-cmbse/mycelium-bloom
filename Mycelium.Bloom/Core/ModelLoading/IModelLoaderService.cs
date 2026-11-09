// ------------------------------------------------------------------------------------------------
// <copyright file="IModelLoaderService.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Defines operations to load SysML model-server payloads into canonical SDK objects.
    /// </summary>
    public interface IModelLoaderService
    {
        /// <summary>
        /// Loads a SysML v2 API-shaped JSON model from the provided file URI.
        /// </summary>
        /// <param name="modelUri">The URI of the model file to load.</param>
        /// <returns>The canonical root namespace assembled from the model DTOs.</returns>
        INamespace LoadModel(Uri modelUri);

        /// <summary>Opens validated session-only JSON without updating previously loaded identities.</summary>
        /// <param name="stream">The bounded, seekable uploaded stream.</param>
        /// <param name="cancellationToken">Cancels validation before assembly.</param>
        /// <param name="accept">Publishes the completed candidate only when the owning session accepts the switch.</param>
        /// <returns>The canonical root of the complete uploaded model.</returns>
        Task<INamespace> OpenUploadedModelAsync(Stream stream, CancellationToken cancellationToken, Func<Func<INamespace>, Task<INamespace>> accept);

        /// <summary>Opens an independently owned model without merging identities with other contexts.</summary>
        /// <param name="stream">The bounded uploaded content.</param>
        /// <param name="cancellationToken">Cancels candidate acceptance.</param>
        /// <param name="accept">Publishes only a candidate accepted by its tab owner.</param>
        /// <returns>The canonical root for this content.</returns>
        Task<INamespace> OpenIndependentModelAsync(Stream stream, CancellationToken cancellationToken, Func<Func<INamespace>, Task<INamespace>> accept);

        /// <summary>Releases an uploaded identity owner after its final consumer closes.</summary>
        /// <param name="root">The canonical context being released.</param>
        void ReleaseUploadedModel(INamespace root);

        /// <summary>Resolves an accepted upload's canonical element without loading another model.</summary>
        /// <param name="elementId">The canonical element identifier.</param>
        /// <returns>The accepted canonical object, or null when absent.</returns>
        SysML2.NET.Core.POCO.Root.Elements.IElement FindUploadedElement(string elementId);

        /// <summary>Checks whether unscoped lookup must account for uploaded model owners.</summary>
        /// <param name="elementId">The canonical identifier.</param>
        /// <returns>Whether any uploaded context contains the identity.</returns>
        bool HasUploadedIdentity(string elementId);

        /// <summary>Resolves an identity in one accepted canonical model only.</summary>
        /// <param name="root">The exact owning model root.</param>
        /// <param name="elementId">The model-local canonical identity.</param>
        /// <returns>The matching object or null.</returns>
        SysML2.NET.Core.POCO.Root.Elements.IElement FindUploadedElement(INamespace root, string elementId);

        /// <summary>
        /// Loads the SysML Quantities standard library model.
        /// </summary>
        /// <returns>The loaded SysML Quantities model.</returns>
        INamespace LoadQuantitiesModel();
    }
}
