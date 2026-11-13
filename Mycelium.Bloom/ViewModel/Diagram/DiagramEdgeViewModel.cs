// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEdgeViewModel.cs" company="Starion Group S.A.">
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

    /// <summary>Retains reactive notation and selection decoration for one graphical relationship occurrence.</summary>
    public sealed class DiagramEdgeViewModel : DiagramElementViewModel
    {
        /// <summary>The current immutable relationship notation.</summary>
        private DiagramEdgePresentation presentation;

        /// <summary>Gets the current relationship presentation independently of native path resources.</summary>
        public DiagramEdgePresentation Presentation
        {
            get => this.presentation;
            private set => this.RaiseAndSetIfChanged(ref this.presentation, value);
        }

        /// <summary>Creates state for one relationship occurrence.</summary>
        /// <param name="presentation">The immutable projected relationship.</param>
        /// <param name="owner">The tab-owned action authority.</param>
        internal DiagramEdgeViewModel(DiagramEdgePresentation presentation, IGeneralDiagramViewModel owner)
            : base(presentation.Element, owner)
        {
            this.Presentation = presentation;
        }

        /// <summary>Replaces relationship notation without mutating its canonical element.</summary>
        /// <param name="value">The validated notation for the same graphical and canonical relationship.</param>
        internal void Update(DiagramEdgePresentation value) => this.Presentation = value;
    }
}
