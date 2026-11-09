// ------------------------------------------------------------------------------------------------
// <copyright file="InterconnectionPrototype.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>Loads the explicitly chosen demonstration into the circuit's existing assembler.</summary>
    public sealed class InterconnectionPrototype
    {
        /// <summary>Owns one canonical load shared by demonstration browsers and diagrams.</summary>
        private readonly Lazy<INamespace> model;
        private readonly IHostEnvironment environment;

        /// <summary>Creates the circuit-scoped demonstration source.</summary>
        /// <param name="loader">The existing SDK loading boundary.</param>
        /// <param name="environment">The application resource location.</param>
        public InterconnectionPrototype(IModelLoaderService loader, IHostEnvironment environment)
        {
            this.environment = environment;
            this.model = new Lazy<INamespace>(() => loader.LoadModel(new Uri(Path.Combine(
                environment.ContentRootPath, "Resources", "Prototypes", "Interconnection.json"))));
        }

        /// <summary>Creates an explicit fixture provider with metadata for distinct semantic elements.</summary>
        /// <param name="projection">The canonical template notation.</param>
        /// <param name="presentation">The template layout.</param>
        /// <param name="tileCount">The number of independent dependency bundles.</param>
        /// <returns>A provider that constructs payloads only when requested.</returns>
        public OnDemandDiagramSource CreateOnDemandSource(Model.Diagram.DiagramProjection projection, Model.Diagram.DiagramPresentationState presentation, int tileCount) =>
            new(File.ReadAllText(Path.Combine(this.environment.ContentRootPath, "Resources", "Prototypes", "Interconnection.json")), projection, presentation, tileCount);

        /// <summary>Gets the canonical demonstration root through the supported loading path.</summary>
        public INamespace Model => this.model.Value;
    }
}
