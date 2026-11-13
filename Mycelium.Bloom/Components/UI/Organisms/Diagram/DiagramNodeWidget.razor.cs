// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeWidget.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;


    /// <summary>Adapts a native occurrence to the shared node components and captures selection.</summary>
    public partial class DiagramNodeWidget : BloomReactiveComponentBase<DiagramNodeViewModel>
    {
        /// <summary>Gets or sets the native occurrence adapter.</summary>
        [Parameter, EditorRequired]
        public GeneralDiagramNodeModel Node { get; set; }

        /// <summary>Gets invariant occurrence dimensions without changing canonical geometry.</summary>
        private string Size => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"width:{this.Node.Size.Width}px;height:{this.Node.Size.Height}px");

        /// <summary>Supplies occurrence state before the reactive base attaches its initial observation.</summary>
        protected override void OnInitialized()
        {
            this.ViewModel = this.Node.ViewModel;
            base.OnInitialized();
        }

        /// <summary>Borrows occurrence state from the native adapter without subscribing to native refresh events.</summary>
        protected override void OnParametersSet()
        {
            this.ViewModel = this.Node.ViewModel;
            base.OnParametersSet();
        }

        /// <summary>Renders only the features assigned to one node-shell boundary slot.</summary>
        /// <param name="side">The slot's graphical boundary.</param>
        /// <returns>The boundary components capturing their canonical selection intent.</returns>
        private RenderFragment BoundaryFeatures(BoundarySide side) => builder =>
        {
            foreach (var feature in this.ViewModel.BoundaryFeatures.Where(feature => feature.Presentation.Side == side))
            {
                builder.OpenComponent<DiagramBoundaryFeature>(0);
                builder.SetKey(feature);
                builder.AddAttribute(1, nameof(DiagramBoundaryFeature.ViewModel), feature);
                builder.CloseComponent();
            }
        };

        /// <summary>Delegates occurrence selection to its presentation owner.</summary>
        private void Select() => this.ViewModel.Select();
    }
}
