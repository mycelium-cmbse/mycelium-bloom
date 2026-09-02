// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserSearchSuggestionList.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser
{
    using BlazorBlueprint.Components;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Renders Project Browser-specific refinements inside Blueprint's command context.
    /// </summary>
    public sealed partial class ProjectBrowserSearchSuggestionList : ComponentBase
    {
        /// <summary>
        /// The stable command value representing the current free-text interpretation.
        /// </summary>
        internal const string ContainsSuggestionValue = "project-browser-search-contains";

        /// <summary>
        /// The stable prefix used by structured Type suggestions.
        /// </summary>
        private const string TypeSuggestionValuePrefix = "project-browser-search-type:";

        /// <summary>
        /// Gets Blueprint's command context for option filtering and keyboard navigation.
        /// </summary>
        [CascadingParameter]
        public CommandContext CommandContext { get; set; }

        /// <summary>
        /// Gets or sets the selected per-tab Type criteria.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public IReadOnlySet<SysmlModelElementKind> SelectedElementKinds { get; set; } = new HashSet<SysmlModelElementKind>();

        /// <summary>
        /// Gets or sets the normalized draft text query.
        /// </summary>
        [Parameter]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the real element-kind choices supported by Project Browser filtering.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public IReadOnlyList<SysmlModelElementKind> ElementKindOptions { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback that accepts the current free-text interpretation.
        /// </summary>
        [Parameter]
        public EventCallback ContainsAccepted { get; set; }

        /// <summary>
        /// Gets or sets the callback that consumes text into a ViewModel-owned Type criterion.
        /// </summary>
        [Parameter]
        public EventCallback<SysmlModelElementKind> ElementKindAccepted { get; set; }

        /// <summary>
        /// Gets a value indicating whether Blueprint keyboard navigation has an active suggestion.
        /// </summary>
        internal bool HasFocusedSuggestion => this.CommandContext?.FocusedIndex >= 0;

        /// <summary>
        /// Filters Blueprint command items without maintaining a second suggestion collection.
        /// </summary>
        /// <param name="item">The registered Blueprint command item.</param>
        /// <param name="query">The normalized free-text query.</param>
        /// <returns>Whether the item is useful for the current query.</returns>
        internal static bool ShouldIncludeSuggestion(CommandItemMetadata item, string query)
        {
            if (string.Equals(item.Value, ContainsSuggestionValue, StringComparison.Ordinal))
            {
                return !string.IsNullOrWhiteSpace(query);
            }

            return item.Value?.StartsWith(TypeSuggestionValuePrefix, StringComparison.Ordinal) == true
                   && item.SearchText?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Moves Blueprint's active suggestion in the requested direction.
        /// </summary>
        /// <param name="direction">One for next, negative one for previous.</param>
        /// <returns>A task representing command navigation.</returns>
        internal Task MoveFocusAsync(int direction)
        {
            return this.CommandContext?.MoveFocusAsync(direction) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Accepts Blueprint's currently active suggestion.
        /// </summary>
        /// <returns>A task representing command selection.</returns>
        internal Task SelectFocusedItemAsync()
        {
            return this.CommandContext?.SelectFocusedItemAsync() ?? Task.CompletedTask;
        }

        /// <summary>
        /// Creates a stable command value for one real element kind.
        /// </summary>
        /// <param name="elementKind">The real element kind.</param>
        /// <returns>The stable command value.</returns>
        private static string GetTypeSuggestionValue(SysmlModelElementKind elementKind)
        {
            return $"{TypeSuggestionValuePrefix}{elementKind}";
        }

        /// <summary>
        /// Creates searchable Type text from the shared real-kind presentation mapping.
        /// </summary>
        /// <param name="elementKind">The real element kind.</param>
        /// <returns>The searchable element-kind text.</returns>
        private static string GetElementKindSearchText(SysmlModelElementKind elementKind)
        {
            return $"{elementKind} {ProjectBrowser.GetElementKindLabel(elementKind)}";
        }

        /// <summary>
        /// Gets the command-item classes that expose the ViewModel-owned selected Type state.
        /// </summary>
        /// <param name="isSelected">Whether the Type criterion is active.</param>
        /// <returns>The suggestion CSS classes.</returns>
        private static string GetTypeSuggestionCssClass(bool isSelected)
        {
            const string baseClass = "mb-project-browser-search-suggestion-list__item";

            return isSelected
                ? $"{baseClass} {baseClass}--selected"
                : baseClass;
        }
    }
}
