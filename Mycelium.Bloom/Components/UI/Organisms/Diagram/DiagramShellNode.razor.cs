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
    using Blazor.Diagrams.Components.Renderers;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
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

        /// <summary>Gets or sets the native port identities for this occurrence.</summary>
        [CascadingParameter]
        public DiagramNodeModel Node { get; set; }

        /// <summary>Renders only the features assigned to one node-shell boundary slot.</summary>
        /// <param name="side">The slot's graphical boundary.</param>
        /// <returns>The boundary components capturing their canonical selection intent.</returns>
        private RenderFragment BoundaryFeatures(BoundarySide side) => builder =>
        {
            foreach (var port in this.Node.Ports.OfType<DiagramBoundaryPortModel>().Where(port => port.ViewModel.Presentation.Side == side))
            {
                builder.OpenComponent<PortRenderer>(0);
                builder.SetKey(port);
                builder.AddAttribute(1, nameof(PortRenderer.Port), port);
                builder.AddAttribute(2, nameof(PortRenderer.Class), "diagram-boundary-port");
                builder.AddAttribute(3, nameof(PortRenderer.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<DiagramBoundaryFeature>(0);
                    content.AddComponentParameter(1, nameof(DiagramBoundaryFeature.ViewModel), port.ViewModel);
                    content.CloseComponent();
                }));
                builder.CloseComponent();
            }
        };
    }
}
