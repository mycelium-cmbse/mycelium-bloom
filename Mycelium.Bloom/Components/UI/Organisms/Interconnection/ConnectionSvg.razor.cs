// ------------------------------------------------------------------------------------------------
// <copyright file="ConnectionSvg.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Interconnection
{
    using Microsoft.AspNetCore.Components;
    using Blazor.Diagrams.Core.Models.Base;

    /// <summary>Renders connection notation and editing handles using mounted route geometry.</summary>
    public partial class ConnectionSvg
    {
        /// <summary>Gets or sets whether interactive handles are excluded from detached export.</summary>
        [Parameter]
        public bool Export { get; set; }

        /// <summary>Gets or sets the mounted connection.</summary>
        [Parameter]
        public BaseLinkModel Link { get; set; }

        /// <summary>Formats native SVG scalar values independently of the circuit culture.</summary>
        /// <param name="value">The model-space scalar.</param>
        /// <returns>The invariant SVG number.</returns>
        private static string SvgNumber(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
