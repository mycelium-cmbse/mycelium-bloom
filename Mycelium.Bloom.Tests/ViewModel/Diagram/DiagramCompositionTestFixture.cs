// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCompositionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;
    using System.Linq;

    using Blazor.Diagrams.Core;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    [TestFixture]
    public sealed class DiagramCompositionTestFixture
    {
        [Test]
        public void VerifyRegisteredFactoriesPreserveTabAndMountOwnership()
        {
            using var context = new ContextAwareService();
            using var services = DiagramTestServices.CreateCollection(context).BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            using var scope = services.CreateScope();
            var createView = scope.ServiceProvider.GetRequiredService<Func<DiagramProjection, IDiagramViewModel>>();
            var createRenderer = services.GetRequiredService<Func<IDiagramViewModel, IDiagramRenderer>>();
            using var first = createView(new DiagramProjection([], []));
            using var second = createView(new DiagramProjection([], []));
            var placement = first.Tools.Active;
            var runtimes = services.GetRequiredService<IDiagramToolRuntimeProvider>();
            using var secondRenderer = createRenderer(second);

            using (var firstRenderer = createRenderer(first))
            {
                var runtimeContext = new DiagramToolContext(firstRenderer.Diagram, first, _ => null);
                using var firstRuntime = runtimes.Create(placement, runtimeContext);
                using var secondRuntime = runtimes.Create(placement, runtimeContext);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(first.Tools, Is.Not.SameAs(second.Tools));
                    Assert.That(firstRenderer.Pointer, Is.Not.SameAs(secondRenderer.Pointer));
                    Assert.That(firstRuntime, Is.Not.SameAs(secondRuntime));
                }

                first.Tools.TryActivate(DiagramToolDefaults.Pan);
                firstRenderer.Pointer.KeyChanged(" ", "Space", true);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Tools.Active.Tool.Id, Is.EqualTo(DiagramToolDefaults.Pan));
                Assert.That(first.Tools.Temporary, Is.Null);
                Assert.That(second.Tools.Active.Tool.Id, Is.EqualTo(DiagramToolDefaults.Move));
                Assert.That(services.GetServices<DiagramToolDefinition>().Select(tool => tool.Id), Is.EqualTo((string[])["move", "pan"]));
                Assert.That(typeof(Behavior).IsAssignableFrom(typeof(DiagramPointerBehavior)), Is.True);
                Assert.That(typeof(IDiagramPointerBehavior).IsAssignableFrom(typeof(DiagramPointerBehavior)), Is.True);
            }
        }

        [TestCase("catalogue")]
        [TestCase("state")]
        [TestCase("context")]
        [TestCase("runtime-provider")]
        public void VerifyMissingRequiredRegistrationFailsClearly(string missing)
        {
            var registrations = DiagramTestServices.CreateCollection();

            switch (missing)
            {
                case "catalogue":
                    registrations.RemoveAll<IDiagramToolCatalogue>();
                    break;
                case "state":
                    registrations.RemoveAll<Func<IObservable<bool>, IDiagramToolState>>();
                    break;
                case "context":
                    registrations.RemoveAll<IContextAwareService>();
                    break;
                case "runtime-provider":
                    registrations.RemoveAll<IDiagramToolRuntimeProvider>();
                    break;
            }

            using var services = registrations.BuildServiceProvider();
            using var scope = services.CreateScope();
            Action resolve = missing == "runtime-provider"
                ? () => services.GetRequiredService<Func<IDiagramViewModel, IDiagramRenderer>>()
                : () => scope.ServiceProvider.GetRequiredService<Func<DiagramProjection, IDiagramViewModel>>()(new DiagramProjection([], []));

            Assert.That(resolve, Throws.TypeOf<InvalidOperationException>().With.Message.Contains("No service").Or.Message.Contains("Unable to resolve service"));
        }

        [Test]
        public void VerifyMissingRuntimeRegistrationDoesNotConstructBuiltInRuntime()
        {
            var registrations = DiagramTestServices.CreateCollection();
            registrations.RemoveAll<DiagramToolRuntimeRegistration>();
            using var services = registrations.BuildServiceProvider();
            var definition = services.GetServices<DiagramToolDefinition>().First();
            var placement = new DiagramToolPlacement(definition, new DiagramToolLayout.Item(definition.Id), 0);
            var runtimes = services.GetRequiredService<IDiagramToolRuntimeProvider>();

            Assert.That(() => runtimes.Create(placement, null), Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("No runtime is registered for diagram tool 'move'."));
        }

        [Test]
        public void VerifyViewModelBorrowsRegisteredStateContractAndOwnsItsDisposal()
        {
            var projection = new DiagramProjection([], []);
            var tools = new Mock<IDiagramToolState>(MockBehavior.Strict);
            tools.Setup(state => state.ForProjection(projection));
            tools.Setup(state => state.Dispose());
            var registrations = DiagramTestServices.CreateCollection();
            registrations.AddSingleton<Func<IObservable<bool>, IDiagramToolState>>(_ => tools.Object);
            using var services = registrations.BuildServiceProvider();
            using var scope = services.CreateScope();
            var owner = scope.ServiceProvider.GetRequiredService<Func<DiagramProjection, IDiagramViewModel>>()(projection);

            Assert.That(owner.Tools, Is.SameAs(tools.Object));
            owner.Dispose();
            owner.Dispose();

            tools.Verify(state => state.ForProjection(projection), Times.Once);
            tools.Verify(state => state.Dispose(), Times.Once);
        }
    }
}
