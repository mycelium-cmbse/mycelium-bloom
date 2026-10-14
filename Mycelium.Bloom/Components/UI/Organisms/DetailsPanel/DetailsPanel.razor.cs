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
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.ViewModel.DetailsPanel;

    /// <summary>
    /// Displays read-only SDK properties and documentation for the selected element.
    /// </summary>
    public sealed partial class DetailsPanel : BloomReactiveInjectableComponentBase<IElementSelectionService>
    {
        /// <summary>
        /// The stable identifier of the panel heading.
        /// </summary>
        private readonly string headingId = $"mb-details-panel-title-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets the callback invoked when the panel requests to close.
        /// </summary>
        [Parameter]
        public EventCallback CloseRequested { get; set; }

        /// <summary>
        /// Gets the injected selection service required by this component.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the selection service has not been provided through dependency injection.
        /// </exception>
        private IElementSelectionService RequiredViewModel =>
            this.ViewModel
            ?? throw new InvalidOperationException(
                $"{nameof(DetailsPanel)} requires an {nameof(IElementSelectionService)}.");

        /// <summary>
        /// Projects the current shared selection into read-only property groups.
        /// </summary>
        /// <returns>The selected element's presentation, or null when nothing is selected.</returns>
        private DetailsPanelPresentation GetPresentation() => this.RequiredViewModel.SelectedElement is { } element
            ? new DetailsPanelPresentation(element)
            : null;

        /// <summary>
        /// Gets the final CSS class list applied to the details panel.
        /// </summary>
        /// <returns>The details-panel CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-details-panel");
        }

        /// <summary>
        /// Emits the panel close intent.
        /// </summary>
        /// <returns>A task representing the callback.</returns>
        private Task RequestCloseAsync()
        {
            return this.CloseRequested.InvokeAsync();
        }
    }
}
