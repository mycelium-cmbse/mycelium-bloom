// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramLayoutMetrics.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Defines the bounded label envelope used by canvas styling and viewport fitting.</summary>
    public static class DiagramLayoutMetrics
    {
        /// <summary>Reserves enough horizontal room for an accessible name compartment.</summary>
        public const double MinimumNodeWidth = 80;

        /// <summary>Reserves enough vertical room for a name compartment and resize control.</summary>
        public const double MinimumNodeHeight = 60;

        /// <summary>The maximum visible width of a boundary-feature label before ellipsis.</summary>
        public const double BoundaryLabelMaximumWidth = 200;

        /// <summary>Reserves a bounded label, eight-pixel gap and twelve-pixel boundary symbol.</summary>
        public const double BoundaryHorizontalClearance = BoundaryLabelMaximumWidth + 8 + 12;

        /// <summary>Reserves forty pixels for a vertical boundary symbol, gap and single-line label.</summary>
        public const double BoundaryVerticalClearance = 40;

        /// <summary>The outer navigation clearance around represented content.</summary>
        public const double ViewportPadding = 32;
    }
}
