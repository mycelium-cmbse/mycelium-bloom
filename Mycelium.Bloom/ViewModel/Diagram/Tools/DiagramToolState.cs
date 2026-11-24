// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolState.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram.Tools
{
    using System.Collections.Immutable;
    using System.Reactive.Linq;

    using Mycelium.Bloom.Model.Diagram;

    using ReactiveUI;

    /// <summary>Owns one tab's tool choice and the observable presentation of its navigation override.</summary>
    public sealed class DiagramToolState : ReactiveObject, IDiagramToolState
    {
        /// <summary>The source of compatible tools for the current projection.</summary>
        private readonly IDiagramToolCatalogue catalogue;

        /// <summary>The injected arrangement of toolbox controls.</summary>
        private readonly DiagramToolLayout layout;

        /// <summary>The tab-owned subscription to editing permission.</summary>
        private readonly IDisposable policySubscription;

        /// <summary>Derives the visible effective placement from the retained choice and the mount's override.</summary>
        private readonly ObservableAsPropertyHelper<DiagramToolPlacement> effective;

        /// <summary>Prevents activation after the owning tab closes.</summary>
        private bool closed;

        /// <summary>Gets the registered tools applicable to the current projection.</summary>
        public ImmutableArray<DiagramToolDefinition> Available
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>Gets the supported toolbox placements in layout order.</summary>
        public ImmutableArray<DiagramToolPlacement> Toolbox
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>Gets the retained tool placement.</summary>
        public DiagramToolPlacement Active
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Gets the temporary placement requested by the mounted dispatcher.</summary>
        public DiagramToolPlacement Temporary
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Gets the placement currently handling interaction.</summary>
        public DiagramToolPlacement Effective => this.effective.Value;

        /// <summary>Gets whether editing tools are currently permitted by the tab.</summary>
        public bool CanEdit
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Gets the configured temporary navigation tool when it is enabled.</summary>
        public DiagramToolPlacement Navigation => this.FindNavigationPlacement();

        /// <summary>Creates tab-owned tool state over the available catalogue and layout.</summary>
        /// <param name="catalogue">The DI-registered tool catalogue.</param>
        /// <param name="layout">The fixed or caller-provided toolbox layout.</param>
        /// <param name="editingPolicy">The observable live tab editing permission.</param>
        public DiagramToolState(IDiagramToolCatalogue catalogue, DiagramToolLayout layout, IObservable<bool> editingPolicy)
        {
            ArgumentNullException.ThrowIfNull(catalogue);
            ArgumentNullException.ThrowIfNull(layout);
            ArgumentNullException.ThrowIfNull(editingPolicy);
            this.catalogue = catalogue;
            this.layout = layout;
            this.effective = this.WhenAnyValue(state => state.Active, state => state.Temporary, (retained, navigation) => navigation ?? retained)
                .ToProperty(this, state => state.Effective);
            this.policySubscription = editingPolicy.DistinctUntilChanged().Subscribe(canEdit =>
            {
                this.CanEdit = canEdit;
                this.EnsureEnabled();
            });
        }

        /// <summary>Returns whether this exact toolbox placement can be activated.</summary>
        /// <param name="placement">A placement from the toolbox.</param>
        /// <returns>Whether the placement remains available and enabled.</returns>
        public bool CanActivate(DiagramToolPlacement placement) => placement is not null && this.Toolbox.Contains(placement) && this.IsEnabled(placement.Tool);

        /// <summary>Activates the first enabled placement for the specified tool ID in layout order.</summary>
        /// <param name="id">The registration's stable ID.</param>
        /// <returns>Whether an enabled placement was found.</returns>
        public bool TryActivate(string id) => this.TryActivate(this.FindPlacement(id));

        /// <summary>Activates the specified placement when supported and enabled.</summary>
        /// <param name="placement">The exact toolbox entry to activate.</param>
        /// <returns>Whether activation was allowed.</returns>
        public bool TryActivate(DiagramToolPlacement placement)
        {
            if (!this.CanActivate(placement))
            {
                return false;
            }

            this.Active = placement;
            return true;
        }

        /// <summary>Updates applicability and ordered controls for a replacement projection.</summary>
        /// <param name="projection">The validated projection for this tab.</param>
        public void ForProjection(DiagramProjection projection)
        {
            var definitions = this.catalogue.For(projection);

            if (this.Available.SequenceEqual(definitions))
            {
                return;
            }

            this.Available = definitions;
            this.Toolbox = this.layout.Items.Select((item, index) => new DiagramToolPlacement(
                    definitions.FirstOrDefault(tool => tool.Id == item.ToolId), item, index))
                .Where(entry => entry.Tool?.SupportsParameters(entry.Item.Parameters) == true)
                .ToImmutableArray();
            this.EnsureEnabled();
        }

        /// <summary>Releases the policy subscription and the tool selection when the owning tab closes.</summary>
        public void Dispose()
        {
            if (this.closed)
            {
                return;
            }

            this.closed = true;
            this.policySubscription.Dispose();
            this.CanEdit = false;
            this.Active = null;
            this.Temporary = null;
            this.effective.Dispose();
        }

        /// <summary>Finds enabled navigation in the layout or the applicable catalogue.</summary>
        /// <returns>The configured navigation placement, or null when unavailable.</returns>
        private DiagramToolPlacement FindNavigationPlacement()
        {
            var placement = this.FindPlacement(this.layout.NavigationToolId);

            if (placement is not null)
            {
                return placement;
            }

            var tool = this.Available.FirstOrDefault(candidate =>
                candidate.Id == this.layout.NavigationToolId && candidate.SupportsParameters(null) && this.IsEnabled(candidate));

            return tool is null ? null : new DiagramToolPlacement(tool, new DiagramToolLayout.Item(tool.Id), -1);
        }

        /// <summary>Checks whether the tool's editing requirement is met.</summary>
        /// <param name="tool">The tool metadata.</param>
        /// <returns>Whether the tab currently permits the tool.</returns>
        private bool IsEnabled(DiagramToolDefinition tool) => !this.closed && (!tool.RequiresEditableDiagram || this.CanEdit);

        /// <summary>Finds the first enabled placement for one tool ID.</summary>
        /// <param name="id">The registration's stable ID.</param>
        /// <returns>The enabled placement, or null when unavailable.</returns>
        private DiagramToolPlacement FindPlacement(string id) => this.Toolbox.FirstOrDefault(entry => entry.Tool.Id == id && this.IsEnabled(entry.Tool));

        /// <summary>Falls back to an enabled placement when the current one becomes unavailable.</summary>
        private void EnsureEnabled()
        {
            if (!this.CanActivate(this.Active))
            {
                this.Active = this.FindPlacement(this.layout.DefaultToolId) ?? this.Toolbox.FirstOrDefault(entry => this.IsEnabled(entry.Tool));
            }
        }
    }
}
