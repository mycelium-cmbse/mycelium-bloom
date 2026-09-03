// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserSearchAssistant.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.UI.Atoms.SearchInput;
    /// <summary>
    /// Composes Blueprint input, popover, and command components into Project Browser search refinements.
    /// </summary>
    public sealed partial class ProjectBrowserSearchAssistant : ComponentBase
    {
        /// <summary>
        /// The stable identifier relating the search field to its Blueprint command listbox.
        /// </summary>
        private readonly string suggestionListId = $"mb-project-browser-search-suggestions-{Guid.NewGuid():N}";

        /// <summary>
        /// The rendered search input used to restore focus after accepting a refinement.
        /// </summary>
        private SearchInput searchInputReference;

        /// <summary>
        /// The Project Browser-specific list hosted by Blueprint's command component.
        /// </summary>
        private ProjectBrowserSearchSuggestionList suggestionListReference;

        /// <summary>
        /// The navigation direction deferred until Blueprint has mounted the popover items.
        /// </summary>
        private int pendingNavigationDirection;

        /// <summary>
        /// The transient text currently being edited in this rendered search surface.
        /// </summary>
        private string draftSearchText = string.Empty;

        /// <summary>
        /// A value indicating whether an input mutation occurred in the current focus session.
        /// </summary>
        private bool hasEditedDraftInCurrentFocusSession;

        /// <summary>
        /// A value indicating whether keyboard navigation explicitly highlighted a suggestion.
        /// </summary>
        private bool hasNavigatedSuggestions;

        /// <summary>
        /// A value indicating whether focus should move to a newly reset search input after rendering.
        /// </summary>
        private bool shouldRestoreSearchFocusAfterRender;

        /// <summary>
        /// A value indicating whether the stable native search input must reflect a cleared draft after rendering.
        /// </summary>
        private bool shouldClearSearchInputAfterRender;

        /// <summary>
        /// The latest parent-owned draft reset version applied to this rendered component.
        /// </summary>
        private int appliedDraftResetVersion;

        /// <summary>
        /// Gets or sets the committed per-tab Contains criterion.
        /// </summary>
        [Parameter]
        public string CommittedFilterText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the selected per-tab Type criteria.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public IReadOnlyCollection<Type> SelectedElementTypes { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// Gets or sets the concrete element types discovered in the loaded model.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public IReadOnlyList<Type> ElementTypeOptions { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether the transient assistant popover is open.
        /// </summary>
        [Parameter]
        public bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets the presentation-only signal used to clear transient draft state.
        /// </summary>
        [Parameter]
        public int DraftResetVersion { get; set; }

        /// <summary>
        /// Gets or sets the controlled open-state callback.
        /// </summary>
        [Parameter]
        public EventCallback<bool> IsOpenChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback that commits the current draft as the ViewModel-owned Contains criterion.
        /// </summary>
        [Parameter]
        public EventCallback<string> ContainsCommitted { get; set; }

        /// <summary>
        /// Gets or sets the callback that removes the ViewModel-owned Contains criterion.
        /// </summary>
        [Parameter]
        public EventCallback ContainsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the callback that toggles a ViewModel-owned Type criterion.
        /// </summary>
        [Parameter]
        public EventCallback<Type> ElementTypeToggled { get; set; }

        /// <summary>
        /// Gets the normalized draft used for display and suggestion matching.
        /// </summary>
        private string Query => this.draftSearchText.Trim();

        /// <summary>
        /// Applies a parent-requested reset to the transient draft state.
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (this.appliedDraftResetVersion == this.DraftResetVersion)
            {
                return;
            }

            this.appliedDraftResetVersion = this.DraftResetVersion;
            this.ResetDraftInteraction();
        }

        /// <summary>
        /// Completes deferred command navigation and search-input focus work.
        /// </summary>
        /// <param name="firstRender">Whether this is the component's first render.</param>
        /// <returns>A task representing the deferred UI work.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (this.IsOpen && this.pendingNavigationDirection != 0 && this.suggestionListReference is not null)
            {
                var direction = this.pendingNavigationDirection;
                this.pendingNavigationDirection = 0;
                await this.suggestionListReference.MoveFocusAsync(direction);
            }

            if (this.shouldClearSearchInputAfterRender && this.searchInputReference is not null)
            {
                this.shouldClearSearchInputAfterRender = false;
                await this.searchInputReference.ClearAsync();
            }

            if (this.shouldRestoreSearchFocusAfterRender)
            {
                this.shouldRestoreSearchFocusAfterRender = false;
                await this.FocusSearchAsync();
            }
        }

        /// <summary>
        /// Forwards controlled state changes emitted by Blueprint's popover.
        /// </summary>
        /// <param name="isOpen">Whether Blueprint requests the assistant to be open.</param>
        /// <returns>A task representing the callback.</returns>
        private Task HandleOpenChanged(bool isOpen)
        {
            if (isOpen
                && (!this.hasEditedDraftInCurrentFocusSession
                    || string.IsNullOrWhiteSpace(this.Query)))
            {
                return Task.CompletedTask;
            }

            if (!isOpen)
            {
                this.ResetSuggestionNavigation();
                this.hasEditedDraftInCurrentFocusSession = false;
            }

            return this.IsOpenChanged.InvokeAsync(isOpen);
        }

        /// <summary>
        /// Updates the transient draft and opens only after a real edit produces useful content.
        /// </summary>
        /// <param name="updatedDraftText">The updated draft text.</param>
        /// <returns>A task representing the callbacks.</returns>
        private async Task HandleDraftSearchTextChangedAsync(string updatedDraftText)
        {
            this.draftSearchText = updatedDraftText ?? string.Empty;
            this.hasEditedDraftInCurrentFocusSession = !string.IsNullOrWhiteSpace(this.Query);
            this.ResetSuggestionNavigation();

            await this.SetOpenAsync(!string.IsNullOrWhiteSpace(this.Query));
        }

        /// <summary>
        /// Tracks focus loss while leaving outside dismissal to Blueprint's popover.
        /// </summary>
        /// <param name="_">The native focus event arguments.</param>
        /// <returns>A completed task.</returns>
        private Task HandleSearchBlurAsync(FocusEventArgs _)
        {
            this.hasEditedDraftInCurrentFocusSession = false;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Routes search-field navigation through Blueprint's command context.
        /// </summary>
        /// <param name="args">The native keyboard event arguments.</param>
        /// <returns>A task representing the keyboard operation.</returns>
        private async Task HandleSearchKeyDownAsync(KeyboardEventArgs args)
        {
            if (args.IsComposing)
            {
                return;
            }

            switch (args.Key)
            {
                case " " when IsUnmodifiedSpace(args) && this.draftSearchText.Length == 0:
                    this.ResetSuggestionNavigation();
                    await this.SetOpenAsync(true);
                    break;
                case "ArrowDown":
                    await this.MoveSuggestionFocusAsync(1);
                    break;
                case "ArrowUp":
                    await this.MoveSuggestionFocusAsync(-1);
                    break;
                case "Enter" when this.IsOpen
                                       && this.hasNavigatedSuggestions
                                       && this.suggestionListReference?.HasFocusedSuggestion == true:
                    await this.suggestionListReference.SelectFocusedItemAsync();
                    break;
                case "Enter" when !string.IsNullOrWhiteSpace(this.Query):
                    await this.CommitContainsAsync();
                    break;
                case "Escape" when this.IsOpen:
                case "Tab" when this.IsOpen:
                    this.hasEditedDraftInCurrentFocusSession = false;
                    await this.SetOpenAsync(false);
                    break;
            }
        }

        /// <summary>
        /// Moves command focus immediately or after the controlled popover has mounted its items.
        /// </summary>
        /// <param name="direction">One for the next option, negative one for the previous option.</param>
        /// <returns>A task representing the navigation operation.</returns>
        private async Task MoveSuggestionFocusAsync(int direction)
        {
            if (string.IsNullOrWhiteSpace(this.Query) && !this.IsOpen)
            {
                return;
            }

            this.hasNavigatedSuggestions = true;

            if (this.IsOpen)
            {
                if (this.suggestionListReference is not null)
                {
                    await this.suggestionListReference.MoveFocusAsync(direction);
                }

                return;
            }

            this.pendingNavigationDirection = direction;
            await this.SetOpenAsync(true);
        }

        /// <summary>
        /// Gets whether the keyboard event represents an unmodified Space key.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <returns>True for an unmodified Space key; otherwise, false.</returns>
        private static bool IsUnmodifiedSpace(KeyboardEventArgs args)
        {
            return !args.CtrlKey
                   && !args.MetaKey
                   && !args.AltKey
                   && !args.ShiftKey;
        }

        /// <summary>
        /// Commits the current draft through the ViewModel owner.
        /// </summary>
        /// <returns>A task representing commitment, dismissal, and focus restoration.</returns>
        private Task HandleContainsAcceptedAsync()
        {
            return this.CommitContainsAsync();
        }

        /// <summary>
        /// Consumes the current text into the selected Type criterion.
        /// </summary>
        /// <param name="elementType">The accepted concrete element type.</param>
        /// <returns>A task representing the selection and focus restoration.</returns>
        private async Task HandleElementTypeAcceptedAsync(Type elementType)
        {
            await this.ResetDraftInteractionAsync();
            this.shouldRestoreSearchFocusAfterRender = true;
            await this.ElementTypeToggled.InvokeAsync(elementType);
            await this.SetOpenAsync(false);
        }

        /// <summary>
        /// Removes the committed Contains criterion and restores input focus.
        /// </summary>
        /// <returns>A task representing removal and focus restoration.</returns>
        private async Task HandleContainsRemovedAsync()
        {
            await this.ContainsRemoved.InvokeAsync();
            await this.CompleteCriterionRemovalAsync();
        }

        /// <summary>
        /// Removes one committed Type criterion through the shared ViewModel toggle operation.
        /// </summary>
        /// <param name="elementType">The selected concrete element type.</param>
        /// <returns>A task representing removal and focus restoration.</returns>
        private async Task HandleElementTypeRemovedAsync(Type elementType)
        {
            await this.ElementTypeToggled.InvokeAsync(elementType);
            await this.CompleteCriterionRemovalAsync();
        }

        /// <summary>
        /// Dismisses the transient assistant after a token action while preserving its current draft.
        /// </summary>
        /// <returns>A task representing dismissal and focus restoration.</returns>
        private async Task CompleteCriterionRemovalAsync()
        {
            this.hasEditedDraftInCurrentFocusSession = false;
            this.ResetSuggestionNavigation();
            await this.SetOpenAsync(false);
            await this.FocusSearchAsync();
        }

        /// <summary>
        /// Handles Escape emitted by Blueprint when its portalled content owns focus.
        /// </summary>
        /// <returns>A task representing focus restoration.</returns>
        private async Task HandleEscapeAsync()
        {
            this.hasEditedDraftInCurrentFocusSession = false;
            await this.SetOpenAsync(false);
            await this.FocusSearchAsync();
        }

        /// <summary>
        /// Commits the default Contains interpretation of the current draft.
        /// </summary>
        /// <returns>A task representing commitment, dismissal, and focus restoration.</returns>
        private async Task CommitContainsAsync()
        {
            var committedText = this.Query;

            if (committedText.Length == 0)
            {
                return;
            }

            await this.ResetDraftInteractionAsync();
            this.shouldRestoreSearchFocusAfterRender = true;
            await this.ContainsCommitted.InvokeAsync(committedText);
            await this.SetOpenAsync(false);
        }

        /// <summary>
        /// Requests controlled open state only when it differs from the current state.
        /// </summary>
        /// <param name="isOpen">The requested state.</param>
        /// <returns>A task representing the callback.</returns>
        private Task SetOpenAsync(bool isOpen)
        {
            return this.IsOpen == isOpen
                ? Task.CompletedTask
                : this.IsOpenChanged.InvokeAsync(isOpen);
        }

        /// <summary>
        /// Returns keyboard focus to the persistent search input.
        /// </summary>
        /// <returns>A task representing the focus operation.</returns>
        private async Task FocusSearchAsync()
        {
            if (this.searchInputReference is not null)
            {
                await this.searchInputReference.FocusAsync();
            }
        }

        /// <summary>
        /// Gets a value indicating whether at least one durable criterion should be rendered as a token.
        /// </summary>
        /// <returns>Whether a committed text or Type criterion exists.</returns>
        private bool HasCommittedCriteria()
        {
            return !string.IsNullOrWhiteSpace(this.CommittedFilterText)
                   || this.SelectedElementTypes.Count > 0;
        }

        /// <summary>
        /// Enumerates selected element types in the stable drawer order.
        /// </summary>
        /// <returns>The selected element types.</returns>
        private IEnumerable<Type> GetSelectedElementTypes()
        {
            return this.ElementTypeOptions.Where(this.SelectedElementTypes.Contains);
        }

        /// <summary>
        /// Gets the accessible removal label for the committed Contains criterion.
        /// </summary>
        /// <returns>The removal label.</returns>
        private string GetContainsRemovalLabel()
        {
            return $"Remove Contains {this.CommittedFilterText.Trim()} search criterion";
        }

        /// <summary>
        /// Gets the accessible removal label for one committed Type criterion.
        /// </summary>
        /// <param name="elementType">The selected concrete element type.</param>
        /// <returns>The removal label.</returns>
        private static string GetElementTypeRemovalLabel(Type elementType)
        {
            return $"Remove Type {ProjectBrowser.GetElementTypeLabel(elementType)} search criterion";
        }

        /// <summary>
        /// Clears draft text and interaction state while preserving durable ViewModel criteria.
        /// </summary>
        private void ResetDraftInteraction()
        {
            this.draftSearchText = string.Empty;
            this.shouldClearSearchInputAfterRender = true;
            this.hasEditedDraftInCurrentFocusSession = false;
            this.ResetSuggestionNavigation();
        }

        /// <summary>
        /// Clears the managed draft and synchronizes the stable Blueprint input before committing a criterion.
        /// </summary>
        /// <returns>A task representing native draft synchronization.</returns>
        private async Task ResetDraftInteractionAsync()
        {
            this.ResetDraftInteraction();

            if (this.searchInputReference is null)
            {
                return;
            }

            this.shouldClearSearchInputAfterRender = false;
            await this.searchInputReference.ClearAsync();
        }

        /// <summary>
        /// Clears deferred and active keyboard suggestion navigation.
        /// </summary>
        private void ResetSuggestionNavigation()
        {
            this.pendingNavigationDirection = 0;
            this.hasNavigatedSuggestions = false;
        }
    }
}
