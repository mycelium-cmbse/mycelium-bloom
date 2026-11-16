// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramShellNode.razor.cs" company="Starion Group S.A.">
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

    /// <summary>Composes a typed shell from the existing graphical building blocks.</summary>
    public partial class DiagramShellNode : BloomComponentBase
    {
        /// <summary>Gets or sets the immutable shell notation.</summary>
        [Parameter, EditorRequired]
        public DiagramShellPresentation Presentation { get; set; }

        /// <summary>Gets or sets the occurrence-owned state borrowed from the native widget.</summary>
        [CascadingParameter]
        public DiagramNodeViewModel ViewModel { get; set; }

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
    }
}
