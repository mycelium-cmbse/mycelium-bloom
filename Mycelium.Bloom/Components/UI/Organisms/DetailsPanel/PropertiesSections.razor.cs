// ------------------------------------------------------------------------------------------------
// <copyright file="PropertiesSections.razor.cs" company="Starion Group S.A.">
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
    /// Renders the selected tab's semantic sections and forwards canonical reference navigation.
    /// </summary>
    public partial class PropertiesSections : ComponentBase
    {
        /// <summary>Gets or sets the ViewModel's semantic sections.</summary>
        [Parameter, EditorRequired]
        public IReadOnlyList<PropertySectionPresentation> Sections { get; set; } = [];

        /// <summary>Gets or sets the tab to render.</summary>
        [Parameter]
        public string Tab { get; set; } = "Properties";

        /// <summary>Gets or sets the canonical selection, to avoid self-navigation.</summary>
        [Parameter]
        public IElement SelectedElement { get; set; }

        /// <summary>Gets or sets the parent component's navigation handler.</summary>
        [Parameter]
        public EventCallback<IElement> InspectRequested { get; set; }

        /// <summary>Captures a reference activation at the component boundary.</summary>
        /// <param name="element">The unchanged canonical target.</param>
        /// <returns>The navigation callback completion.</returns>
        private Task Inspect(IElement element)
        {
            return this.InspectRequested.InvokeAsync(element);
        }
    }
}
