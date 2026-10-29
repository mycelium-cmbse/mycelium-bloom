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

        /// <summary>
        /// Gets or sets the factory for panel-owned presentation state.
        /// </summary>
        [Inject]
        private Func<IDetailsPanelViewModel> ViewModelFactory { get; set; }

        /// <summary>The ViewModel constructed and disposed by this panel.</summary>
        private IDetailsPanelViewModel ownedViewModel;

        /// <summary>The design's fixed semantic tabs.</summary>
        private static readonly string[] Tabs = ["Properties", "Relations", "Comments", "Attachments"];

        /// <summary>The tab selected in this component instance.</summary>
        private string activeTab = "Properties";

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
            this.ViewModel.Inspect(element);
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
