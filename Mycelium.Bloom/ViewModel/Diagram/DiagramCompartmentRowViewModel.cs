// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCompartmentRowViewModel.cs" company="Starion Group S.A.">
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

    /// <summary>Projects selection and formatted text for one semantic compartment row.</summary>
    public sealed class DiagramCompartmentRowViewModel : DiagramElementViewModel
    {
        /// <summary>Gets the current immutable row notation.</summary>
        public DiagramCompartmentRowPresentation Presentation
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Creates row state owned by its containing diagram occurrence.</summary>
        /// <param name="presentation">The formatted row with its canonical element.</param>
        /// <param name="owner">The tab-owned action authority.</param>
        public DiagramCompartmentRowViewModel(DiagramCompartmentRowPresentation presentation, IDiagramViewModel owner)
            : base(presentation.Element, owner)
        {
            this.Presentation = presentation;
        }

        /// <summary>Updates notation while retaining the canonical element and selection decoration.</summary>
        /// <param name="presentation">The replacement notation for the same canonical element.</param>
        internal void Update(DiagramCompartmentRowPresentation presentation) => this.Presentation = presentation;
    }
}
