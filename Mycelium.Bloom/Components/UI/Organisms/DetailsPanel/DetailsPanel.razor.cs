// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanel.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DetailsPanel
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Displays read-only SDK properties and documentation for the selected element.
    /// </summary>
    public sealed partial class DetailsPanel : BloomReactiveComponentBase<IDetailsPanelViewModel>
    {
        /// <summary>
        /// The stable identifier of the panel heading.
        /// </summary>
        private readonly string headingId = $"mb-details-panel-title-{Guid.NewGuid():N}";

        /// <summary>The ViewModel constructed and disposed by this panel.</summary>
        private IDetailsPanelViewModel ownedViewModel;

        /// <summary>Gets or sets the layout-owned URI context.</summary>
        [CascadingParameter]
        public Mycelium.Bloom.Core.Context.IWorkspaceUrlContextService UrlContext { get; set; }

        /// <summary>Gets or sets the layout callback for the inspection header's measured minimum width.</summary>
        [Parameter]
        public EventCallback<double> MinimumWidthChanged { get; set; }

        /// <summary>The design's fixed semantic tabs.</summary>
        private static readonly string[] Tabs = ["Properties", "Relations", "Comments", "Attachments"];

        /// <summary>The tab selected in this component instance.</summary>
        private string activeTab = "Properties";

        /// <summary>Accessible feedback for the most recent clipboard request.</summary>
        private string copyStatus;

        /// <summary>Prevents overlapping clipboard requests.</summary>
        private bool isCopying;

        /// <summary>Suppresses completion updates after disposal.</summary>
        private bool isDisposed;

        /// <summary>Gets or sets the browser clipboard interop boundary.</summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>The leading mode, keyed independently of the panel lifetime.</summary>
        private bool? leadingBack;

        /// <summary>Enables a one-shot entrance after the first leading-mode change.</summary>
        private bool animateLeadingControl;

        /// <summary>The effective identity seen by the preceding render, including elements without stable identifiers.</summary>
        private IElement displayedElement;

        /// <summary>The stable identity captured before any subsequent model changes.</summary>
        private string displayedElementId;

        /// <summary>Copies only the current element's canonical URL; unavailable/rejected APIs report failure.</summary>
        /// <returns>The clipboard operation with accessible success or failure feedback.</returns>
        private async Task CopyElementLink()
        {
            var element = this.ViewModel.Presentation?.Element;
            if (this.isDisposed || this.isCopying || this.UrlContext is null || string.IsNullOrWhiteSpace(element?.ElementId))
            {
                return;
            }

            var uri = this.UrlContext.GetElementUri(element);
            if (string.IsNullOrWhiteSpace(uri))
            {
                this.copyStatus = "Unable to copy link";
                return;
            }

            this.isCopying = true;
            this.copyStatus = null;
            string status;
            try
            {
                await this.JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", uri);
                status = "Link copied";
            }
            catch (Exception exception) when (exception is JSException or InvalidOperationException)
            {
                status = "Unable to copy link";
            }
            finally
            {
                this.isCopying = false;
            }

            if (!this.isDisposed && this.ViewModel.Presentation?.Element.ElementId == element.ElementId)
            {
                this.copyStatus = status;
            }
        }

        /// <summary>Resets local tabs only when the effective Properties element changes.</summary>
        private void SynchronizeActiveTab()
        {
            var showBack = this.ViewModel.IsInspecting && this.UrlContext is not null;
            if (this.leadingBack != showBack)
            {
                this.animateLeadingControl = this.leadingBack.HasValue;
                this.leadingBack = showBack;
            }

            var element = this.ViewModel.Presentation?.Element;
            var elementId = element?.ElementId;
            var isSameElement = string.IsNullOrWhiteSpace(elementId)
                ? ReferenceEquals(element, this.displayedElement)
                : string.Equals(elementId, this.displayedElementId, StringComparison.Ordinal);

            if (!isSameElement)
            {
                this.activeTab = "Properties";
                this.copyStatus = null;
            }

            this.displayedElement = element;
            this.displayedElementId = elementId;
        }

        /// <summary>
        /// Gets or sets the factory for panel-owned presentation state.
        /// </summary>
        [Inject]
        private Func<IDetailsPanelViewModel> ViewModelFactory { get; set; }

        /// <summary>Gets or sets the composition-owned Browser reveal action.</summary>
        [Parameter]
        public EventCallback<IElement> ShowInBrowserRequested { get; set; }

        /// <summary>Gets or sets the composition-owned close action, which preserves canonical selection.</summary>
        [Parameter]
        public EventCallback CloseRequested { get; set; }

        /// <summary>Constructs panel state once while preserving a caller-supplied ViewModel's ownership.</summary>
        protected override void OnInitialized()
        {
            if (this.ViewModel is null)
            {
                this.ownedViewModel = this.ViewModelFactory()
                                      ?? throw new InvalidOperationException(
                                          "The Details Panel ViewModel factory returned null.");
                this.ViewModel = this.ownedViewModel;
            }

            base.OnInitialized();
        }

        /// <summary>Releases internally created state when a caller replaces it.</summary>
        protected override void OnParametersSet()
        {
            if (this.ownedViewModel is not null && !ReferenceEquals(this.ownedViewModel, this.ViewModel))
            {
                this.ownedViewModel.Dispose();
                this.ownedViewModel = null;
            }

            base.OnParametersSet();
        }

        /// <summary>Detaches rendering and releases only the state constructed by this panel.</summary>
        /// <param name="disposing">Whether managed resources are being released.</param>
        protected override void Dispose(bool disposing)
        {
            this.isDisposed = true;
            base.Dispose(disposing);

            if (disposing)
            {
                this.ownedViewModel?.Dispose();
                this.ownedViewModel = null;
            }
        }

        /// <summary>Inspects the exact canonical target without revealing or selecting a browser row.</summary>
        /// <param name="element">The canonical target captured by the component.</param>
        private void InspectElement(IElement element)
        {
            if (this.UrlContext is not null)
            {
                this.UrlContext.Inspect(element);
            }
            else
            {
                this.ViewModel.Inspect(element);
            }
        }

        /// <summary>Captures the Back action before delegating to workspace navigation.</summary>
        /// <returns>The navigation operation, or a completed task when no context is available.</returns>
        private Task GoBack()
        {
            return this.UrlContext?.GoBackAsync() ?? Task.CompletedTask;
        }

        /// <summary>Captures the header close event before delegating workspace visibility.</summary>
        /// <returns>The composition callback completion.</returns>
        private Task ClosePanel()
        {
            return this.CloseRequested.InvokeAsync();
        }

        /// <summary>Changes only the local presentation tab.</summary>
        /// <param name="tab">The selected tab.</param>
        private void ChangeTab(string tab)
        {
            this.activeTab = tab;
        }

        /// <summary>Requests a local Browser reveal without changing canonical selection.</summary>
        /// <returns>The composition callback completion.</returns>
        private Task ShowInBrowser()
        {
            return this.ShowInBrowserRequested.InvokeAsync(this.ViewModel.Presentation.Element);
        }

        /// <summary>
        /// Gets the final CSS class list applied to the details panel.
        /// </summary>
        /// <returns>The details-panel CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-details-panel");
        }
    }
}
