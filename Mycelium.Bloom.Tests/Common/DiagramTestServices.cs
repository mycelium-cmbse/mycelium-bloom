// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramTestServices.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    // Composes real diagram registrations with explicit test substitutions.
    internal sealed class DiagramTestServices : IDisposable
    {
        private readonly List<ServiceProvider> providers = [];

        private readonly List<IServiceScope> scopes = [];

        public static IDiagramToolCatalogue CreateCatalogue(params DiagramToolDefinition[] definitions)
        {
            using var provider = CreateCollection().BuildServiceProvider();
            return new DiagramToolCatalogue(provider.GetServices<DiagramToolDefinition>().Concat(definitions));
        }

        public static IServiceCollection CreateCollection(IElementSelectionService selection = null,
            IDiagramToolCatalogue catalogue = null, DiagramToolLayout layout = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(selection ?? Mock.Of<IElementSelectionService>());
            services.AddSingleton(selection as IContextAwareService ?? Mock.Of<IContextAwareService>());

            if (catalogue is not null)
            {
                services.AddSingleton(catalogue);
            }

            if (layout is not null)
            {
                services.AddSingleton(layout);
            }

            return services.AddDiagramServices();
        }

        public DiagramViewModel CreateViewModel(DiagramProjection projection, IElementSelectionService selection,
            IDiagramToolCatalogue catalogue = null, DiagramToolLayout layout = null)
        {
            var provider = this.Build(CreateCollection(selection, catalogue, layout));
            var scope = provider.CreateScope();
            this.scopes.Add(scope);
            return (DiagramViewModel)scope.ServiceProvider.GetRequiredService<Func<DiagramProjection, IDiagramViewModel>>()(projection);
        }

        public IDiagramRenderer CreateRenderer(IDiagramViewModel owner, IDiagramToolRuntimeProvider runtimeProvider = null)
        {
            var services = CreateCollection();

            if (runtimeProvider is not null)
            {
                services.AddSingleton(runtimeProvider);
            }

            return this.Build(services).GetRequiredService<Func<IDiagramViewModel, IDiagramRenderer>>()(owner);
        }

        public void Dispose()
        {
            foreach (var scope in this.scopes)
            {
                scope.Dispose();
            }

            this.scopes.Clear();

            foreach (var provider in this.providers)
            {
                provider.Dispose();
            }

            this.providers.Clear();
        }

        private ServiceProvider Build(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            this.providers.Add(provider);
            return provider;
        }
    }
}
