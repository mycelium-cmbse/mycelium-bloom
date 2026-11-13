// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNameCompartment.razor.cs" company="Starion Group S.A.">
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

    /// <summary>Renders keyword, SDK declaration and optional alias notation.</summary>
    public partial class DiagramNameCompartment : BloomComponentBase
    {
        /// <summary>Gets or sets the formatted name presentation.</summary>
        [Parameter, EditorRequired]
        public DiagramNamePresentation Presentation { get; set; }
    }
}
