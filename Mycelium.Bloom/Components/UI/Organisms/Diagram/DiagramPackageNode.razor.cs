// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPackageNode.razor.cs" company="Starion Group S.A.">
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


    /// <summary>Renders a reusable package name tab and namespace body.</summary>
    public partial class DiagramPackageNode : BloomComponentBase
    {
        /// <summary>Gets or sets the formatted package name.</summary>
        [Parameter]
        public string Name { get; set; }

        /// <summary>Gets or sets optional package body content.</summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
