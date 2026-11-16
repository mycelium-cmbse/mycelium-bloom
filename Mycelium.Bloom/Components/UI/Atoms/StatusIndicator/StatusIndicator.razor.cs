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
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Shows a status as a coloured dot followed by its label. The colour comes from the name of the state, an enum of the SDK
    /// such as the lifecycle of a project or the status of an account, so that no enum of Bloom has to mirror it.
    /// </summary>
    /// <typeparam name="TState">The enum of the SDK that the status is a value of.</typeparam>
    public sealed partial class StatusIndicator<TState> : BloomComponentBase
        where TState : struct, Enum
    {
        /// <summary>
        /// Gets or sets the label of the status.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state that the status stands for, which gives the dot its colour.
        /// </summary>
        [Parameter]
        public TState State { get; set; }

        /// <summary>
        /// Builds the CSS classes of the root, with the modifier named after the state, which the stylesheet colours.
        /// </summary>
        /// <returns>The CSS classes of the root.</returns>
        private string GetRootCssClass()
        {
            return this.BuildRootCssClass("mb-status-indicator", $"mb-status-indicator--{this.State.ToString().ToLowerInvariant()}");
        }
    }
}
