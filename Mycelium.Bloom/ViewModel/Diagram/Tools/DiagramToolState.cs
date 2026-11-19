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

    using Mycelium.Bloom.Model.Diagram;

    using ReactiveUI;

    /// <summary>Owns one tab's tool choice and its temporary navigation policy.</summary>
    public sealed class DiagramToolState : ReactiveObject
    {
        /// <summary>The source of compatible tools for the current projection.</summary>
        private readonly DiagramToolCatalogue catalogue;

        /// <summary>The injected arrangement of toolbox controls.</summary>
        private readonly DiagramToolLayout layout;

        /// <summary>Reads the live tab editing policy.</summary>
        private readonly Func<bool> canEdit;

        /// <summary>The retained tool over canvas remounts.</summary>
        private DiagramToolDefinition active;

        /// <summary>The temporary tool selected by the mounted dispatcher.</summary>
        private DiagramToolDefinition temporary;

        /// <summary>Prevents activation after the owning tab closes.</summary>
        private bool closed;

        /// <summary>Gets the registered tools applicable to the current projection.</summary>
        public ImmutableArray<DiagramToolDefinition> Available { get; private set; } = [];

        /// <summary>Gets the supported toolbox placements in layout order.</summary>
        public ImmutableArray<(DiagramToolDefinition Tool, DiagramToolLayout.Item Item)> Toolbox { get; private set; } = [];

        /// <summary>Gets the retained tool choice.</summary>
        public DiagramToolDefinition Active
        {
            get => this.active;
            private set => this.RaiseAndSetIfChanged(ref this.active, value);
        }

        /// <summary>Gets the runtime override for temporary navigation.</summary>
        public DiagramToolDefinition Temporary
        {
            get => this.temporary;
            internal set => this.RaiseAndSetIfChanged(ref this.temporary, value);
        }

        /// <summary>Gets the tool currently handling interaction.</summary>
        public DiagramToolDefinition Effective => this.Temporary ?? this.Active;

        /// <summary>Gets or sets whether the originating pointer press prevents a subsequent DOM click from selecting.</summary>
        internal bool BlocksPointerSelection { get; set; }

        /// <summary>Gets the configured temporary navigation tool when it is enabled.</summary>
        internal DiagramToolDefinition Navigation => this.Available.FirstOrDefault(tool => tool.Id == this.layout.NavigationToolId && this.CanActivate(tool));

        /// <summary>Creates tab-owned tool state over the available catalogue and layout.</summary>
        /// <param name="catalogue">The DI-registered tool catalogue.</param>
        /// <param name="layout">The fixed or caller-provided toolbox layout.</param>
        /// <param name="canEdit">The live tab editing policy.</param>
        public DiagramToolState(DiagramToolCatalogue catalogue, DiagramToolLayout layout, Func<bool> canEdit)
        {
            this.catalogue = catalogue;
            this.layout = layout;
            this.canEdit = canEdit;
        }

        /// <summary>Returns whether a compatible tool can run under the current tab policy.</summary>
        /// <param name="tool">An available registered definition.</param>
        /// <returns>Whether the tool can be activated.</returns>
        public bool CanActivate(DiagramToolDefinition tool) => !this.closed && tool is not null && this.Available.Contains(tool)
            && (!tool.RequiresEditableDiagram || this.canEdit());

        /// <summary>Activates the specified tool when supported and enabled.</summary>
        /// <param name="id">The registration's stable ID.</param>
        /// <returns>Whether activation was allowed.</returns>
        public bool TryActivate(string id)
        {
            var tool = this.Available.FirstOrDefault(candidate => candidate.Id == id);

            if (!this.CanActivate(tool))
            {
                return false;
            }

            this.Active = tool;
            return true;
        }

        /// <summary>Updates applicability and ordered controls for a replacement projection.</summary>
        /// <param name="projection">The validated projection for this tab.</param>
        internal void ForProjection(DiagramProjection projection)
        {
            this.Available = this.catalogue.For(projection);
            this.Toolbox = this.layout.Items
                .Select(item => (Tool: this.Available.FirstOrDefault(tool => tool.Id == item.ToolId), Item: item))
                .Where(entry => entry.Tool is not null).ToImmutableArray();
            this.EnsureEnabled();
        }

        /// <summary>Releases the tool selection when its owning tab closes.</summary>
        internal void Close()
        {
            this.closed = true;
            this.Active = null;
            this.Temporary = null;
        }

        /// <summary>Falls back to an available tool when the current tool becomes disabled.</summary>
        internal void EnsureEnabled()
        {
            if (!this.CanActivate(this.Active))
            {
                this.Active = this.Available.FirstOrDefault(tool => tool.Id == this.layout.DefaultToolId && this.CanActivate(tool))
                    ?? this.Available.FirstOrDefault(this.CanActivate);
            }
        }
    }
}
