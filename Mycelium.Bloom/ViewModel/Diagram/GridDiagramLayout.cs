// ------------------------------------------------------------------------------------------------
// <copyright file="GridDiagramLayout.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Places bounded read-only occurrences on a configurable initial grid.</summary>
    public sealed class GridDiagramLayout : IDiagramLayout
    {
        /// <summary>The immutable initial-layout configuration.</summary>
        private readonly DiagramLayoutOptions options;

        /// <summary>Gets the projection's canonical-element limit.</summary>
        public int MaximumElements => this.options.MaximumElements;

        /// <summary>Creates a grid using supplied measurements or the existing initial placement.</summary>
        /// <param name="options">The optional immutable layout configuration.</param>
        /// <exception cref="ArgumentException">The element bound, columns or grid measurements are invalid.</exception>
        public GridDiagramLayout(DiagramLayoutOptions options = null)
        {
            this.options = options ?? new DiagramLayoutOptions();

            if (this.options.MaximumElements <= 0 || this.options.MaximumElements == int.MaxValue || this.options.Columns <= 0
                || this.options.Origin is not { } origin || this.options.Spacing is not { } spacing
                || !double.IsFinite(origin.X) || !double.IsFinite(origin.Y) || !double.IsFinite(spacing.X) || !double.IsFinite(spacing.Y)
                || spacing.X <= 0 || spacing.Y <= 0)
            {
                throw new ArgumentException("Diagram layout requires positive bounds and finite grid measurements.", nameof(options));
            }
        }

        /// <summary>Creates a distinct occurrence without modifying the supplied SDK element.</summary>
        /// <param name="element">The canonical semantic object.</param>
        /// <param name="index">The nonnegative initial placement index.</param>
        /// <returns>The new graphical identity and initial rectangle.</returns>
        public DiagramOccurrence CreateOccurrence(IElement element, int index)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return new DiagramOccurrence(Guid.NewGuid(), element, this.options.Origin.X + index % this.options.Columns * this.options.Spacing.X,
                this.options.Origin.Y + index / this.options.Columns * this.options.Spacing.Y);
        }
    }
}
