// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBoundsCalculator.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Measures fixed occurrence rectangles, visible boundary envelopes and manual bends.</summary>
    public static class DiagramBoundsCalculator
    {
        /// <summary>Measures the retained content, including boundary labels and manual routing bends.</summary>
        /// <param name="projection">The validated nonempty graphical projection.</param>
        /// <returns>The padded model-space bounds used for navigation, without changing occurrence geometry.</returns>
        public static (double Left, double Top, double Right, double Bottom) Measure(DiagramProjection projection)
        {
            const double padding = DiagramLayoutMetrics.ViewportPadding;
            var nodes = projection.Nodes.Select(GetNodeBounds).ToArray();
            var left = nodes.Min(node => node.Left) - padding;
            var top = nodes.Min(node => node.Top) - padding;
            var right = nodes.Max(node => node.Right) + padding;
            var bottom = nodes.Max(node => node.Bottom) + padding;
            var bends = projection.Edges.Where(edge => !edge.Bends.IsDefaultOrEmpty).SelectMany(edge => edge.Bends).ToArray();

            if (bends.Length > 0)
            {
                left = Math.Min(left, bends.Min(point => point.X) - padding);
                right = Math.Max(right, bends.Max(point => point.X) + padding);
                top = Math.Min(top, bends.Min(point => point.Y) - padding);
                bottom = Math.Max(bottom, bends.Max(point => point.Y) + padding);
            }

            return (left, top, right, bottom);
        }

        /// <summary>Measures an occurrence with the bounded envelope of its visible boundary features.</summary>
        /// <param name="node">The validated graphical occurrence.</param>
        /// <returns>The occurrence rectangle including conservative boundary-label clearance.</returns>
        private static (double Left, double Top, double Right, double Bottom) GetNodeBounds(DiagramNodePresentation node)
        {
            var hasBoundaries = node is DiagramShellPresentation { Boundary.IsEmpty: false };
            var horizontal = hasBoundaries ? DiagramLayoutMetrics.BoundaryHorizontalClearance : 0;
            var vertical = hasBoundaries ? DiagramLayoutMetrics.BoundaryVerticalClearance : 0;
            var occurrence = node.Occurrence;

            return (occurrence.X - horizontal, occurrence.Y - vertical,
                occurrence.X + occurrence.Width + horizontal, occurrence.Y + occurrence.Height + vertical);
        }
    }
}
