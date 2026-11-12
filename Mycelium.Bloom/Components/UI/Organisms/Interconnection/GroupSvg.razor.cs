// ------------------------------------------------------------------------------------------------
// <copyright file="GroupSvg.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Interconnection
{
    using Microsoft.AspNetCore.Components;
    using Blazor.Diagrams.Core.Models;

    /// <summary>Renders a nested part container and its mounted child components.</summary>
    public partial class GroupSvg
    {
        /// <summary>Gets or sets the mounted container.</summary>
        [Parameter]
        public GroupModel Group { get; set; }
    }
}
