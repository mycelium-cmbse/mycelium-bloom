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
        public IReadOnlyCollection<Type> SelectedElementTypes { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// Gets or sets the normalized draft text query.
        /// </summary>
        [Parameter]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the concrete element types discovered in the loaded model.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public IReadOnlyList<Type> ElementTypeOptions { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback that accepts the current free-text interpretation.
        /// </summary>
        [Parameter]
        public EventCallback ContainsAccepted { get; set; }

        /// <summary>
        /// Gets or sets the callback that consumes text into a ViewModel-owned Type criterion.
        /// </summary>
        [Parameter]
        public EventCallback<Type> ElementTypeAccepted { get; set; }

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
        /// Creates a stable command value for one concrete element type.
        /// </summary>
        /// <param name="elementType">The concrete element type.</param>
        /// <returns>The stable command value.</returns>
        private static string GetTypeSuggestionValue(Type elementType)
        {
            return $"{TypeSuggestionValuePrefix}{elementType.FullName ?? elementType.Name}";
        }

        /// <summary>
        /// Creates searchable text from the concrete type and its shared presentation label.
        /// </summary>
        /// <param name="elementType">The concrete element type.</param>
        /// <returns>The searchable element-type text.</returns>
        private static string GetElementTypeSearchText(Type elementType)
        {
            return $"{elementType.Name} {ProjectBrowser.GetElementTypeLabel(elementType)}";
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
