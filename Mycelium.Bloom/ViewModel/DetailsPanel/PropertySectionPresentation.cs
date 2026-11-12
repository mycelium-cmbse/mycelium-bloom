// ------------------------------------------------------------------------------------------------
// <copyright file="PropertySectionPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.DetailsPanel
{
    /// <summary>Groups read-only semantic fields and canonical references in a Properties tab.</summary>
    /// <param name="Title">The section heading.</param>
    /// <param name="Properties">Available labelled model values.</param>
    /// <param name="Paragraphs">Plain-text documentation or explanatory content.</param>
    public sealed record PropertySectionPresentation(string Title, IReadOnlyList<KeyValuePair<string, string>> Properties, IReadOnlyList<string> Paragraphs)
    {
        /// <summary>Gets canonical references belonging to this semantic section.</summary>
        public IReadOnlyList<ElementLinkPresentation> Links { get; init; } = [];

        /// <summary>Gets the destination tab.</summary>
        public string Tab { get; init; } = "Properties";

        /// <summary>Gets the visual treatment without requiring domain interpretation in Razor.</summary>
        public string Layout { get; init; } = "fields";

        /// <summary>Gets an optional count of actual items in this section.</summary>
        public int? Count { get; init; }

        /// <summary>Gets structured state or transition cards.</summary>
        public IReadOnlyList<PropertyCardPresentation> Cards { get; init; } = [];
    }

    /// <summary>Groups a canonical heading and its available action or transition references.</summary>
    /// <param name="Heading">The canonical card subject.</param>
    /// <param name="Links">The available action or transition roles.</param>
    /// <param name="Badge">An optional model-derived status.</param>
    public sealed record PropertyCardPresentation(ElementLinkPresentation Heading, IReadOnlyList<ElementLinkPresentation> Links, string Badge = null);
}
