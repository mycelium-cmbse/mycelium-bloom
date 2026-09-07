// ------------------------------------------------------------------------------------------------
// <copyright file="ElementIdResolver.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using SysML2.NET.Dal;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Resolves stable element identifiers against the canonical SDK assembler cache.
    /// </summary>
    public sealed class ElementIdResolver : IElementIdResolver
    {
        /// <summary>
        /// The SDK assembler that owns canonical model identity.
        /// </summary>
        private readonly IAssembler assembler;

        /// <summary>
        /// The model loader that owns the cached model root.
        /// </summary>
        private readonly IModelLoaderService modelLoaderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementIdResolver" /> class.
        /// </summary>
        /// <param name="modelLoaderService">The service that ensures the model session has been loaded.</param>
        /// <param name="assembler">The SDK assembler that owns the canonical model cache.</param>
        public ElementIdResolver(
            IModelLoaderService modelLoaderService,
            IAssembler assembler)
        {
            ArgumentNullException.ThrowIfNull(modelLoaderService);
            ArgumentNullException.ThrowIfNull(assembler);

            this.modelLoaderService = modelLoaderService;
            this.assembler = assembler;
        }

        /// <summary>
        /// Resolves an exact stable identifier to the canonical POCO currently owned by the SDK assembler.
        /// </summary>
        /// <param name="elementId">The stable SysML element identifier.</param>
        /// <param name="cancellationToken">Cancels model-cache inspection.</param>
        /// <returns>The unique canonical element, or <see langword="null" /> when the identifier is absent or ambiguous.</returns>
        public ValueTask<IElement> ResolveAsync(
            string elementId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(elementId))
            {
                return ValueTask.FromResult<IElement>(null);
            }

            cancellationToken.ThrowIfCancellationRequested();
            this.modelLoaderService.LoadQuantitiesModel();

            IElement resolvedElement = null;

            foreach (var cachedElement in this.assembler.Cache.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = cachedElement.Value;

                if (!string.Equals(candidate.ElementId, elementId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (resolvedElement is not null)
                {
                    return ValueTask.FromResult<IElement>(null);
                }

                resolvedElement = candidate;
            }

            return ValueTask.FromResult(resolvedElement);
        }
    }
}
