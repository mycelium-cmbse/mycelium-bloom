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

        /// <summary>The occurrence-local position retained across renderer mounts.</summary>
        private DiagramPoint position;

        /// <summary>The boundary presentations scoped to this occurrence.</summary>
        private ImmutableArray<DiagramBoundaryViewModel> boundaryFeatures = [];

        /// <summary>Gets the current graphical position independently of canonical model data.</summary>
        public DiagramPoint Position
        {
            get => this.position;
            private set => this.RaiseAndSetIfChanged(ref this.position, value);
        }

        /// <summary>Gets the current graphical dimensions independently of canonical model data.</summary>
        public DiagramSize Size
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

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
        internal DiagramNodeViewModel(DiagramNodePresentation presentation, IDiagramViewModel owner)
            : base(presentation.Occurrence.Element, owner)
        {
            this.Update(presentation);
            this.Position = new DiagramPoint(presentation.Occurrence.X, presentation.Occurrence.Y);
            this.Size = new DiagramSize(presentation.Occurrence.Width, presentation.Occurrence.Height);
        }

        /// <summary>Applies graphical coordinates validated by the tab owner.</summary>
        /// <param name="value">The accepted finite model-space position.</param>
        internal void SetPosition(DiagramPoint value) => this.Position = value;

        /// <summary>Applies graphical dimensions validated by the tab owner.</summary>
        /// <param name="value">The accepted model-space dimensions.</param>
        internal void SetSize(DiagramSize value) => this.Size = value;

        /// <summary>Delegates an accessible resize request to the tab's geometry authority.</summary>
        /// <param name="width">The requested model-space width.</param>
        /// <param name="height">The requested model-space height.</param>
        /// <returns>Whether this occurrence accepted resizing.</returns>
        public bool Resize(double width, double height) => this.Owner.ResizeNode(this.Presentation.Occurrence.Id, width, height);

        /// <summary>Gets nested state retained by this exact parent occurrence.</summary>
        /// <param name="projection">The child view's immutable notation.</param>
        /// <returns>The tab-owned child state.</returns>
        public IDiagramViewModel GetNestedView(DiagramProjection projection) => this.Owner.GetNestedView(this.Presentation.Occurrence.Id, projection);

        /// <summary>Replaces only presentation data, retaining boundary state where the occurrence association is unchanged.</summary>
        /// <param name="value">The validated notation for this same occurrence and canonical object.</param>
        internal void Update(DiagramNodePresentation value)
        {
            if (ReferenceEquals(this.Presentation, value))
            {
                return;
            }

            var features = value is DiagramShellPresentation shell ? shell.Boundary : [];
            var boundaries = ImmutableArray.CreateBuilder<DiagramBoundaryViewModel>(features.Length);

            for (var index = 0; index < features.Length; index++)
            {
                var boundary = features[index];
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

            if (this.Presentation is { } previous && !(previous.Occurrence.X, previous.Occurrence.Y).Equals((value.Occurrence.X, value.Occurrence.Y)))
            {
                this.Position = new DiagramPoint(value.Occurrence.X, value.Occurrence.Y);
            }

            if (this.Presentation is { } prior && !(prior.Occurrence.Width, prior.Occurrence.Height).Equals((value.Occurrence.Width, value.Occurrence.Height)))
            {
                this.Size = new DiagramSize(value.Occurrence.Width, value.Occurrence.Height);
            }

            this.Presentation = value;
        }
    }
}
