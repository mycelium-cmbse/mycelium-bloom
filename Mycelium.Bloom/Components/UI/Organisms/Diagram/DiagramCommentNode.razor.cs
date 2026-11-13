// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCommentNode.razor.cs" company="Starion Group S.A.">
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


    /// <summary>Renders a folded-corner comment without a graphical keyword.</summary>
    public partial class DiagramCommentNode : BloomComponentBase
    {
        /// <summary>Gets or sets the canonical comment text.</summary>
        [Parameter]
        public string Body { get; set; }
    }
}
