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

        /// <summary>Constructs panel state once while preserving a caller-supplied ViewModel's ownership.</summary>
        protected override void OnInitialized()
        {
            if (this.ViewModel is null)
            {
                this.ownedViewModel = this.ViewModelFactory()
                    ?? throw new InvalidOperationException("The Details Panel ViewModel factory returned null.");
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
        private void InspectElement(IElement element)
        {
            this.ViewModel.Inspect(element);
        }

        /// <summary>Adds namespace boundary wrap opportunities while preserving the property's text.</summary>
        /// <param name="text">The SDK-backed property text.</param>
        /// <returns>Escaped text with optional line breaks between namespace segments.</returns>
        private static RenderFragment RenderPropertyText(string text) => builder =>
        {
            var segments = text.Split("::", StringSplitOptions.None);
            for (var index = 0; index < segments.Length; index++)
            {
                if (index > 0)
                {
                    builder.AddContent(0, "::");
                    builder.OpenElement(1, "wbr");
                    builder.CloseElement();
                }
                builder.AddContent(2, segments[index]);
            }
        };

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
