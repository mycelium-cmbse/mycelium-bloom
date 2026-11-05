// ------------------------------------------------------------------------------------------------
// <copyright file="StatusIndicator.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.StatusIndicator
{
    using System.ComponentModel;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.SDK;

    /// <summary>
    /// Shows a status as a coloured dot followed by its label.
    /// </summary>
    public sealed partial class StatusIndicator : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the label of the status.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the lifecycle state that the status stands for, which gives the dot its colour.
        /// </summary>
        [Parameter]
        public ProjectLifecycleKind State { get; set; }

        /// <summary>
        /// Builds the CSS classes of the root, with the modifier that gives the dot the colour of the lifecycle state.
        /// </summary>
        /// <returns>The CSS classes of the root.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown when <see cref="State" /> is not a defined state.</exception>
        private string GetRootCssClass()
        {
            var modifier = this.State switch
            {
                ProjectLifecycleKind.Preparation => "mb-status-indicator--preparation",
                ProjectLifecycleKind.Open => "mb-status-indicator--open",
                ProjectLifecycleKind.Review => "mb-status-indicator--review",
                ProjectLifecycleKind.Archived => "mb-status-indicator--archived",
                _ => throw new InvalidEnumArgumentException(nameof(this.State), (int)this.State, typeof(ProjectLifecycleKind))
            };

            return this.BuildRootCssClass("mb-status-indicator", modifier);
        }
    }
}
