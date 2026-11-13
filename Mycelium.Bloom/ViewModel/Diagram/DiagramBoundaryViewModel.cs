// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBoundaryViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    using ReactiveUI;

    /// <summary>Owns reactive placement and selection decoration for one boundary occurrence.</summary>
    public sealed class DiagramBoundaryViewModel : DiagramElementViewModel
    {
        /// <summary>The current immutable boundary notation.</summary>
        private DiagramBoundaryPresentation presentation;

        /// <summary>Gets the current boundary notation.</summary>
        public DiagramBoundaryPresentation Presentation
        {
            get => this.presentation;
            private set => this.RaiseAndSetIfChanged(ref this.presentation, value);
        }

        /// <summary>Creates state for a port or parameter occurrence.</summary>
        /// <param name="presentation">The projected boundary notation.</param>
        /// <param name="owner">The tab-owned action authority.</param>
        public DiagramBoundaryViewModel(DiagramBoundaryPresentation presentation, IGeneralDiagramViewModel owner)
            : base(presentation.Element, owner)
        {
            this.Presentation = presentation;
        }

        /// <summary>Replaces presentation without modifying its canonical element.</summary>
        /// <param name="value">The validated notation for the same canonical boundary element.</param>
        internal void Update(DiagramBoundaryPresentation value) => this.Presentation = value;
    }
}
