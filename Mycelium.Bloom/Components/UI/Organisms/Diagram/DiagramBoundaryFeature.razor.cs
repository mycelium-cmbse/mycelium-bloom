// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBoundaryFeature.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Renders a reusable port or parameter and captures its selection intent.</summary>
    public partial class DiagramBoundaryFeature : BloomReactiveComponentBase<DiagramBoundaryViewModel>
    {
        /// <summary>Gets or sets the owning canvas's shared keyboard and pointer routing.</summary>
        [CascadingParameter]
        private DiagramPointerBehavior Pointer { get; set; }

        /// <summary>Gets the current boundary notation from its occurrence-owned reactive state.</summary>
        private DiagramBoundaryPresentation Presentation => this.ViewModel.Presentation;

        /// <summary>Computes the invariant CSS position along the selected boundary.</summary>
        /// <returns>The normalized horizontal or vertical CSS position.</returns>
        private string GetPosition() => string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{(this.Presentation.Side is BoundarySide.Left or BoundarySide.Right ? "top" : "left")}: {Math.Clamp(this.Presentation.Offset, 0, 1) * 100}%");

        /// <summary>Gets the direction glyph oriented relative to the selected boundary.</summary>
        /// <returns>The graphical direction glyph, or an empty string when no direction is supplied.</returns>
        private string GetGlyph() => (this.Presentation.Side, this.Presentation.Direction) switch
        {
            (BoundarySide.Left, "In") or (BoundarySide.Right, "Out") => "→",
            (BoundarySide.Right, "In") or (BoundarySide.Left, "Out") => "←",
            (BoundarySide.Top, "In") or (BoundarySide.Bottom, "Out") => "↓",
            (BoundarySide.Bottom, "In") or (BoundarySide.Top, "Out") => "↑",
            (BoundarySide.Top or BoundarySide.Bottom, "InOut") => "↕",
            (_, "InOut") => "↔",
            _ => ""
        };

        /// <summary>Captures selection intent and delegates it to the boundary presentation owner.</summary>
        /// <param name="args">The browser click, including keyboard-activated buttons.</param>
        private void Select(MouseEventArgs args) => this.ViewModel.Select(args.Detail > 0);
    }
}
