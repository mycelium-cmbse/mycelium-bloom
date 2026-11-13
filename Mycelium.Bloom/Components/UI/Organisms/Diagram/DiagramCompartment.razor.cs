// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCompartment.razor.cs" company="Starion Group S.A.">
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

    /// <summary>Renders a reusable titled textual or graphical compartment.</summary>
    public partial class DiagramCompartment : BloomComponentBase
    {
        /// <summary>Gets or sets the preformatted compartment content.</summary>
        [Parameter, EditorRequired]
        public DiagramCompartmentPresentation Presentation { get; set; }

        /// <summary>Gets or sets the optional nested graphical fragment.</summary>
        [Parameter]
        public RenderFragment GraphicalView { get; set; }
    }
}
