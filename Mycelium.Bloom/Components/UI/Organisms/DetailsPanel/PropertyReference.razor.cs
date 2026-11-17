// ------------------------------------------------------------------------------------------------
// <copyright file="PropertyReference.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DetailsPanel
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.ViewModel.DetailsPanel;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Displays a canonical reference as a navigation action or a non-interactive label.
    /// </summary>
    public partial class PropertyReference : ComponentBase
    {
        /// <summary>Gets or sets the presentation's unchanged canonical reference.</summary>
        [Parameter, EditorRequired]
        public ElementLinkPresentation Link { get; set; }

        /// <summary>Gets or sets the selected element to suppress self-navigation.</summary>
        [Parameter]
        public IElement SelectedElement { get; set; }

        /// <summary>Gets or sets the visual treatment supplied by the parent.</summary>
        [Parameter]
        public string Class { get; set; } = "mb-property-link";

        /// <summary>Gets or sets optional rich link content.</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>Gets or sets the component-owned navigation callback.</summary>
        [Parameter]
        public EventCallback<IElement> InspectRequested { get; set; }

        /// <summary>Checks stable identity before presenting a reference as actionable.</summary>
        /// <returns>Whether the target has a distinct stable identifier.</returns>
        private bool CanInspect()
        {
            return !string.IsNullOrWhiteSpace(this.Link.Element?.ElementId)
                   && !string.Equals(this.Link.Element.ElementId, this.SelectedElement?.ElementId, StringComparison.Ordinal);
        }

        /// <summary>Dispatches the captured canonical reference.</summary>
        /// <returns>The navigation callback completion.</returns>
        private Task Inspect()
        {
            return this.CanInspect() ? this.InspectRequested.InvokeAsync(this.Link.Element) : Task.CompletedTask;
        }
    }
}
