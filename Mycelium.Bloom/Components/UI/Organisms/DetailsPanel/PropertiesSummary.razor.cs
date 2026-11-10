// ------------------------------------------------------------------------------------------------
// <copyright file="PropertiesSummary.razor.cs" company="Starion Group S.A.">
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

    /// <summary>
    /// Displays the cached name, kind and compact semantic summary of the selected element.
    /// </summary>
    public partial class PropertiesSummary : ComponentBase
    {
        /// <summary>Gets or sets the cached semantic selection summary.</summary>
        [Parameter, EditorRequired]
        public DetailsPanelPresentation Presentation { get; set; }
    }
}
