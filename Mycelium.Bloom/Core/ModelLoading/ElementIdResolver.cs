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
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Resolves stable element identifiers from the cached Quantities model object graph.
    /// </summary>
    public sealed class ElementIdResolver : IElementIdResolver
    {
        /// <summary>
        /// Lazily builds the immutable lookup without coupling URL resolution to a rendered tree.
        /// </summary>
        private readonly Lazy<Task<IReadOnlyDictionary<string, IElement>>> elementIndex;

        /// <summary>
        /// The model loader that owns the cached model root.
        /// </summary>
        private readonly IModelLoaderService modelLoaderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementIdResolver" /> class.
        /// </summary>
        /// <param name="modelLoaderService">The service that provides the cached model root.</param>
        public ElementIdResolver(IModelLoaderService modelLoaderService)
        {
            ArgumentNullException.ThrowIfNull(modelLoaderService);

            this.modelLoaderService = modelLoaderService;
            this.elementIndex =
                new Lazy<Task<IReadOnlyDictionary<string, IElement>>>(() => Task.Run(this.BuildElementIndex));
        }

        /// <inheritdoc />
        public async ValueTask<IElement> ResolveAsync(
            string elementId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(elementId))
            {
                return null;
            }

            var index = await this.elementIndex.Value.WaitAsync(cancellationToken);

            return index.TryGetValue(elementId, out var element) ? element : null;
        }

        /// <summary>
        /// Builds an exact identifier lookup while rejecting ambiguous duplicate identifiers.
        /// </summary>
        /// <returns>The canonical elements indexed by stable identifier.</returns>
        private IReadOnlyDictionary<string, IElement> BuildElementIndex()
        {
            var elements = new Dictionary<string, IElement>(StringComparer.Ordinal);
            var duplicateIds = new HashSet<string>(StringComparer.Ordinal);
            var visitedElements = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            var pendingElements = new Stack<IElement>();
            var root = this.modelLoaderService.LoadQuantitiesModel();

            if (root is not null)
            {
                pendingElements.Push(root);
            }

            while (pendingElements.TryPop(out var element))
            {
                if (!visitedElements.Add(element))
                {
                    continue;
                }

                var elementId = element.ElementId;

                if (!string.IsNullOrWhiteSpace(elementId) && !duplicateIds.Contains(elementId))
                {
                    if (!elements.TryAdd(elementId, element))
                    {
                        elements.Remove(elementId);
                        duplicateIds.Add(elementId);
                    }
                }

                if (element.ownedElement is null)
                {
                    continue;
                }

                foreach (var ownedElement in element.ownedElement)
                {
                    if (ownedElement is not null)
                    {
                        pendingElements.Push(ownedElement);
                    }
                }
            }

            return elements;
        }
    }
}
