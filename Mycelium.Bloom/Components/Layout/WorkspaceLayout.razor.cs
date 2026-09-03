// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceLayout.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Layout
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    /// <summary>
    /// Composes the shared application frame for routed engineering-workspace pages.
    /// </summary>
    public sealed partial class WorkspaceLayout : LayoutComponentBase
    {
        /// <summary>
        /// A value indicating whether the shell currently reserves the collapsed navigation width.
        /// </summary>
        private bool isNavigationCollapsed = true;

        /// <summary>
        /// A value indicating whether the workspace inspector is visible.
        /// </summary>
        private bool isDetailsPanelOpen = true;

        /// <summary>
        /// Gets or sets the navigation service used to reconcile the current route and presentation state.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Gets or sets the navigation state owned by this workspace-layout instance.
        /// </summary>
        [Inject]
        private INavigationRailViewModel NavigationViewModel { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ArgumentNullException.ThrowIfNull(this.NavigationManager);
            ArgumentNullException.ThrowIfNull(this.NavigationViewModel);

            this.isNavigationCollapsed = this.NavigationViewModel.PresentationMode switch
            {
                NavigationRailPresentationMode.Expanded => false,
                NavigationRailPresentationMode.Collapsed => true,
                NavigationRailPresentationMode.ExpandOnHover => true,
                _ => throw CreateInvalidPresentationModeException(this.NavigationViewModel.PresentationMode)
            };
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            this.NavigationViewModel.ReconcileSelection(this.GetNormalizedCurrentRoute());
        }

        /// <summary>
        /// Updates the shell's persistent navigation-width reservation.
        /// </summary>
        /// <param name="isCollapsed">Whether the rail reserves its collapsed width.</param>
        private void HandleNavigationLayoutCollapsedChanged(bool isCollapsed)
        {
            this.isNavigationCollapsed = isCollapsed;
        }

        /// <summary>
        /// Closes the workspace inspector.
        /// </summary>
        private void CloseDetailsPanel()
        {
            this.isDetailsPanelOpen = false;
        }

        /// <summary>
        /// Toggles the workspace inspector visibility.
        /// </summary>
        private void ToggleDetailsPanel()
        {
            this.isDetailsPanelOpen = !this.isDetailsPanelOpen;
        }

        /// <summary>
        /// Gets the accessible action label for the workspace inspector toggle.
        /// </summary>
        /// <returns>The action performed by the toggle.</returns>
        private string GetDetailsPanelToggleLabel()
        {
            return this.isDetailsPanelOpen ? "Close details panel" : "Open details panel";
        }

        /// <summary>
        /// Gets the icon representing the workspace inspector toggle action.
        /// </summary>
        /// <returns>The Lucide icon name.</returns>
        private string GetDetailsPanelToggleIconName()
        {
            return this.isDetailsPanelOpen ? "panel-right-close" : "panel-right-open";
        }

        /// <summary>
        /// Gets the current application-relative route without query, fragment, or trailing separators.
        /// </summary>
        /// <returns>The normalized route used by the navigation ViewModel.</returns>
        private string GetNormalizedCurrentRoute()
        {
            var absoluteUri = this.NavigationManager.ToAbsoluteUri(this.NavigationManager.Uri);
            var relativePath = this.NavigationManager.ToBaseRelativePath(absoluteUri.GetLeftPart(UriPartial.Path));
            var trimmedPath = relativePath.Trim('/');

            return string.IsNullOrEmpty(trimmedPath) ? "/workspace/modeling" : $"/{trimmedPath}";
        }

        /// <summary>
        /// Creates the exception used when a presentation mode is unsupported.
        /// </summary>
        /// <param name="presentationMode">The unsupported presentation mode.</param>
        /// <returns>The exception describing the unsupported presentation mode.</returns>
        private static ArgumentOutOfRangeException CreateInvalidPresentationModeException(
            NavigationRailPresentationMode presentationMode)
        {
            return new ArgumentOutOfRangeException(nameof(presentationMode), presentationMode, null);
        }
    }
}
