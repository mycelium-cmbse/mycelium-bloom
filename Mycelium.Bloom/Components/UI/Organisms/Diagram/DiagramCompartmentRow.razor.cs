// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCompartmentRow.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams.Extensions;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Captures semantic row input without starting movement of its enclosing node.</summary>
    public partial class DiagramCompartmentRow : SelectableDiagramWidgetBase<DiagramCompartmentRowViewModel>
    {
        /// <summary>Gets or sets the mounted native parent occurrence.</summary>
        [CascadingParameter]
        private DiagramNodeModel Node { get; set; }

        /// <summary>Delegates a row hit to the existing mount-owned gesture dispatcher.</summary>
        /// <param name="args">The browser pointer event captured before native node movement.</param>
        private void BeginSelection(PointerEventArgs args) => this.Pointer?.BeginSelection(this.Node, this.ViewModel, args.ToCore());
    }
}
