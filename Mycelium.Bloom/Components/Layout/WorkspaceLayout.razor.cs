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
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    /// <summary>
    /// Composes the shared application frame for routed engineering-workspace pages.
    /// </summary>
    public sealed partial class WorkspaceLayout : LayoutComponentBase, IDisposable
    {
        /// <summary>
        /// Owns renderer-dispatched URL-context subscriptions for this layout instance.
        /// </summary>
        private readonly CompositeDisposable urlContextSubscriptions = new();

        /// <summary>
        /// A value indicating whether the shell currently reserves the collapsed navigation width.
        /// </summary>
        private bool isNavigationCollapsed = true;

        /// <summary>
        /// A value indicating whether the workspace inspector is visible.
        /// </summary>
        private bool isDetailsPanelOpen = true;

        /// <summary>
        /// A value indicating whether final layout disposal has occurred.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Gets or sets the navigation service used to reconcile the current route and presentation state.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Gets or sets the shared selected-element authority reconciled from browser locations.
        /// </summary>
        [Inject]
        private IElementSelectionService ElementSelectionService { get; set; }

        /// <summary>
        /// Gets or sets the logger used for renderer-bound URL coordination failures.
        /// </summary>
        [Inject]
        private ILogger<WorkspaceLayout> Logger { get; set; }

        /// <summary>
        /// Gets or sets the factory that creates navigation state owned by this workspace-layout instance.
        /// </summary>
        [Inject]
        private Func<INavigationRailViewModel> NavigationViewModelFactory { get; set; }

        /// <summary>
        /// Gets or sets the factory that creates URL context owned by this workspace-layout instance.
        /// </summary>
        [Inject]
        private Func<IWorkspaceUrlContextService> UrlContextServiceFactory { get; set; }

        /// <summary>
        /// Gets the navigation state owned by this workspace-layout instance.
        /// </summary>
        private INavigationRailViewModel NavigationViewModel { get; set; }

        /// <summary>
        /// Gets the URL context owned and cascaded by this workspace-layout instance.
        /// </summary>
        private IWorkspaceUrlContextService UrlContextService { get; set; }

        /// <summary>
        /// Creates the layout-owned navigation state and initializes its width reservation.
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ArgumentNullException.ThrowIfNull(this.NavigationManager);
            ArgumentNullException.ThrowIfNull(this.ElementSelectionService);
            ArgumentNullException.ThrowIfNull(this.Logger);
            ArgumentNullException.ThrowIfNull(this.NavigationViewModelFactory);
            ArgumentNullException.ThrowIfNull(this.UrlContextServiceFactory);

            var navigationViewModel = this.NavigationViewModelFactory()
                ?? throw new InvalidOperationException("The navigation ViewModel factory returned null.");
            IWorkspaceUrlContextService urlContextService = null;

            try
            {
                urlContextService = this.UrlContextServiceFactory()
                                    ?? throw new InvalidOperationException(
                                        "The URL context service factory returned null.");
                this.isNavigationCollapsed = navigationViewModel.PresentationMode switch
                {
                    NavigationRailPresentationMode.Expanded => false,
                    NavigationRailPresentationMode.Collapsed => true,
                    NavigationRailPresentationMode.ExpandOnHover => true,
                    _ => throw CreateInvalidPresentationModeException(navigationViewModel.PresentationMode)
                };
                this.NavigationViewModel = navigationViewModel;
                this.UrlContextService = urlContextService;
                this.ObserveUrlContext();
            }
            catch
            {
                this.urlContextSubscriptions.Dispose();
                urlContextService?.Dispose();
                navigationViewModel.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Reconciles navigation selection whenever routed body content changes.
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            this.NavigationViewModel.ReconcileSelection(this.GetNormalizedCurrentRoute());
        }

        /// <summary>
        /// Releases the navigation state owned by this layout instance.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.urlContextSubscriptions.Dispose();
            this.UrlContextService?.Dispose();
            this.NavigationViewModel?.Dispose();
        }

        /// <summary>
        /// Connects URL-derived state and navigation requests to the Blazor renderer boundary.
        /// </summary>
        private void ObserveUrlContext()
        {
            this.urlContextSubscriptions.Add(
                this.UrlContextService.Restorations
                    .Select(restoration => Observable.FromAsync(cancellationToken =>
                        this.ApplyRestorationAsync(restoration, cancellationToken)))
                    .Concat()
                    .Subscribe(
                        _ => { },
                        exception => this.Logger.LogError(
                            exception,
                            "Workspace URL restoration stopped unexpectedly.")));

            this.urlContextSubscriptions.Add(
                this.UrlContextService.NavigationRequests
                    .Select(uri =>
                        Observable.FromAsync(cancellationToken =>
                            this.ApplyNavigationRequestAsync(uri, cancellationToken)))
                    .Concat()
                    .Subscribe(
                        _ => { },
                        exception => this.Logger.LogError(
                            exception,
                            "Workspace URL navigation stopped unexpectedly.")));
        }

        /// <summary>
        /// Applies one resolved browser selection through the renderer dispatcher.
        /// </summary>
        /// <param name="restoration">The resolved URL context.</param>
        /// <param name="cancellationToken">Cancels work after subscription disposal.</param>
        /// <returns>A task representing renderer dispatch.</returns>
        private Task ApplyRestorationAsync(
            WorkspaceUrlContextRestoration restoration,
            CancellationToken cancellationToken)
        {
            return this.InvokeAsync(() =>
            {
                if (!this.isDisposed && !cancellationToken.IsCancellationRequested)
                {
                    this.ElementSelectionService.SelectedElement = restoration.SelectedElement;
                }
            });
        }

        /// <summary>
        /// Applies one selected-element URI update without leaving Blazor client routing.
        /// </summary>
        /// <param name="uri">The absolute replacement URI.</param>
        /// <param name="cancellationToken">Cancels work after subscription disposal.</param>
        /// <returns>A task representing renderer dispatch.</returns>
        private Task ApplyNavigationRequestAsync(string uri, CancellationToken cancellationToken)
        {
            return this.InvokeAsync(() =>
            {
                if (!this.isDisposed
                    && !cancellationToken.IsCancellationRequested
                    && !string.Equals(uri, this.NavigationManager.Uri, StringComparison.Ordinal))
                {
                    this.NavigationManager.NavigateTo(uri, forceLoad: false, replace: true);
                }
            });
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
