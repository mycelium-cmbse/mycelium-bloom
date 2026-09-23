// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserRenderState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using System.Collections.Immutable;

    /// <summary>Captures one coherent browser presentation independently of concurrent model notifications.</summary>
    /// <param name="Roots">The visible root presentations.</param>
    /// <param name="SelectedNode">The retained logical selection.</param>
    /// <param name="FilterText">The committed text criterion.</param>
    /// <param name="AvailableElementTypes">The model's available filter types.</param>
    /// <param name="SelectedElementTypes">The active type criteria.</param>
    /// <param name="FilterPresentation">The effective visibility projection.</param>
    /// <param name="IsLoading">Whether initialization is active.</param>
    /// <param name="IsLoaded">Whether a model is available.</param>
    /// <param name="ErrorMessage">The current loading or update failure.</param>
    public sealed record ProjectBrowserRenderState(
        ImmutableArray<ProjectBrowserNodeRenderState> Roots,
        ProjectBrowserNodeViewModel SelectedNode,
        string FilterText,
        ImmutableArray<Type> AvailableElementTypes,
        ImmutableArray<Type> SelectedElementTypes,
        ProjectBrowserFilterPresentation FilterPresentation,
        bool IsLoading,
        bool IsLoaded,
        string ErrorMessage)
    {
        /// <summary>Gets the presentation before initialization.</summary>
        public static ProjectBrowserRenderState Empty { get; } =
            new([], null, string.Empty, [], [], ProjectBrowserFilterPresentation.Inactive, false, false, string.Empty);
    }

    /// <summary>Captures one row and its visible children without reading mutable SDK objects during rendering.</summary>
    /// <param name="Node">The stable browser identity used by user actions.</param>
    /// <param name="DisplayName">The captured display name.</param>
    /// <param name="QualifiedName">The captured qualified name.</param>
    /// <param name="ElementType">The captured concrete metaclass.</param>
    /// <param name="HasChildren">Whether expansion is available.</param>
    /// <param name="IsExpanded">Whether child rows are visible.</param>
    /// <param name="Children">The ordered visible children.</param>
    public sealed record ProjectBrowserNodeRenderState(
        ProjectBrowserNodeViewModel Node,
        string DisplayName,
        string QualifiedName,
        Type ElementType,
        bool HasChildren,
        bool IsExpanded,
        ImmutableArray<ProjectBrowserNodeRenderState> Children);
}
