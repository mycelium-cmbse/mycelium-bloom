// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Model.Diagram;

    using ReactiveUI;

    /// <summary>Retains observable notation, selection and boundary presentations for one tab-owned occurrence.</summary>
    public sealed class DiagramNodeViewModel : DiagramElementViewModel
    {
        /// <summary>The current immutable notation snapshot.</summary>
        private DiagramNodePresentation presentation;

        /// <summary>The boundary presentations scoped to this occurrence.</summary>
        private ImmutableArray<DiagramBoundaryViewModel> boundaryFeatures = [];

        /// <summary>Gets the current notation, including geometry, without changing canonical objects.</summary>
        public DiagramNodePresentation Presentation
        {
            get => this.presentation;
            private set => this.RaiseAndSetIfChanged(ref this.presentation, value);
        }

        /// <summary>Gets the observable boundary occurrences; none subscribes to shared selection itself.</summary>
        public ImmutableArray<DiagramBoundaryViewModel> BoundaryFeatures
        {
            get => this.boundaryFeatures;
            private set => this.RaiseAndSetIfChanged(ref this.boundaryFeatures, value);
        }

        /// <summary>Creates presentation state for one graphical node occurrence.</summary>
        /// <param name="presentation">The immutable canonical projection.</param>
        /// <param name="owner">The tab-owned state authority.</param>
        internal DiagramNodeViewModel(DiagramNodePresentation presentation, IGeneralDiagramViewModel owner)
            : base(presentation.Occurrence.Element, owner)
        {
            this.Update(presentation);
        }

        /// <summary>Gets nested state retained by this exact parent occurrence.</summary>
        /// <param name="projection">The child view's immutable notation.</param>
        /// <returns>The tab-owned child state.</returns>
        public IGeneralDiagramViewModel GetNestedView(GeneralViewProjection projection) => this.Owner.GetNestedView(this.Presentation.Occurrence.Id, projection);

        /// <summary>Replaces only presentation data, retaining boundary state where the occurrence association is unchanged.</summary>
        /// <param name="value">The validated notation for this same occurrence and canonical object.</param>
        internal void Update(DiagramNodePresentation value)
        {
            if (ReferenceEquals(this.Presentation, value))
            {
                return;
            }

            var boundaries = ImmutableArray.CreateBuilder<DiagramBoundaryViewModel>(value.Boundary.Length);

            for (var index = 0; index < value.Boundary.Length; index++)
            {
                var boundary = value.Boundary[index];
                var state = index < this.boundaryFeatures.Length && ReferenceEquals(this.boundaryFeatures[index].Element, boundary.Element)
                    ? this.boundaryFeatures[index]
                    : new DiagramBoundaryViewModel(boundary, this.Owner);
                state.Update(boundary);
                boundaries.Add(state);
            }

            var updated = boundaries.MoveToImmutable();

            if (!this.boundaryFeatures.SequenceEqual(updated))
            {
                this.BoundaryFeatures = updated;
            }

            this.Presentation = value;
        }
    }
}
