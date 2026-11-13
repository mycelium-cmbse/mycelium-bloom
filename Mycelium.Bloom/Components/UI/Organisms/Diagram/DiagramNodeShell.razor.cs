// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeShell.razor.cs" company="Starion Group S.A.">
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

    /// <summary>Composes reusable graphical node slots in the BNF-defined order.</summary>
    public partial class DiagramNodeShell : BloomComponentBase
    {
        /// <summary>Gets or sets the shared node shape variant.</summary>
        [Parameter]
        public DiagramNodeKind Kind { get; set; }

        /// <summary>Gets or sets the name compartment.</summary>
        [Parameter]
        public RenderFragment Name { get; set; }

        /// <summary>Gets or sets the optional graphical view preceding compartments.</summary>
        [Parameter]
        public RenderFragment NestedView { get; set; }

        /// <summary>Gets or sets the ordered compartment stack.</summary>
        [Parameter]
        public RenderFragment Compartments { get; set; }

        /// <summary>Gets or sets features on the left boundary.</summary>
        [Parameter]
        public RenderFragment LeftBoundary { get; set; }

        /// <summary>Gets or sets features on the right boundary.</summary>
        [Parameter]
        public RenderFragment RightBoundary { get; set; }

        /// <summary>Gets or sets features on the top boundary.</summary>
        [Parameter]
        public RenderFragment TopBoundary { get; set; }

        /// <summary>Gets or sets features on the bottom boundary.</summary>
        [Parameter]
        public RenderFragment BottomBoundary { get; set; }
    }
}
