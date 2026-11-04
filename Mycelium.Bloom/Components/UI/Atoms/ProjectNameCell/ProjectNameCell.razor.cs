// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectNameCell.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.ProjectNameCell
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Shows the name of a project with its description below it. A long description is cut short and is shown in
    /// full as a tooltip.
    /// </summary>
    public sealed partial class ProjectNameCell : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the project.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;
    }
}
