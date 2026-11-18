// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramFactoryTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;

    [TestFixture]
    public sealed class DiagramFactoryTestFixture
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void VerifyNodeChoosesMostSpecificFactoryBeforeFallback(bool reverseRegistration, bool port)
        {
            IElement element = port ? new PortUsage() : new PartUsage();
            var fallback = new Mock<IDiagramNodeFactory>(MockBehavior.Strict);
            var usage = new Mock<IDiagramNodeFactory>(MockBehavior.Strict);
            var boundary = new Mock<IDiagramNodeFactory>(MockBehavior.Strict);
            fallback.SetupGet(factory => factory.ElementType).Returns(typeof(IElement));
            usage.SetupGet(factory => factory.ElementType).Returns(typeof(IUsage));
            boundary.SetupGet(factory => factory.ElementType).Returns(typeof(IPortUsage));
            var expected = DiagramTestFactory.Node(element);
            var selected = port ? boundary : usage;
            selected.Setup(factory => factory.Create(It.Is<DiagramOccurrence>(occurrence => ReferenceEquals(occurrence.Element, element)), null)).Returns(expected);
            IDiagramNodeFactory[] registrations = reverseRegistration
                ? [boundary.Object, usage.Object, fallback.Object]
                : [fallback.Object, usage.Object, boundary.Object];
            var projector = new GeneralViewProjector(registrations);

            var actual = projector.Node(element);

            Assert.That(actual, Is.SameAs(expected));
            selected.Verify(factory => factory.Create(It.IsAny<DiagramOccurrence>(), It.IsAny<IElement>()), Times.Once);
            fallback.Verify(factory => factory.Create(It.IsAny<DiagramOccurrence>()), Times.Never);
        }

        [TestCase(typeof(Package), typeof(DiagramPackagePresentation), typeof(DiagramPackageNode))]
        [TestCase(typeof(Comment), typeof(DiagramCommentPresentation), typeof(DiagramCommentNode))]
        [TestCase(typeof(PartUsage), typeof(DiagramShellPresentation), typeof(DiagramShellNode))]
        public void VerifyRegisteredProjectionDispatchesTypedPresentation(Type elementType, Type presentationType, Type componentType)
        {
            using var provider = new ServiceCollection().AddDiagramServices().BuildServiceProvider();
            using var scope = provider.CreateScope();
            var element = (IElement)Activator.CreateInstance(elementType);
            var projector = scope.ServiceProvider.GetRequiredService<IGeneralViewProjector>();
            var components = scope.ServiceProvider.GetRequiredService<DiagramNodeComponentRegistry>();

            var presentation = projector.Node(element);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation, Is.InstanceOf(presentationType));
                Assert.That(presentation.Occurrence.Element, Is.SameAs(element));
                Assert.That(components.GetComponentType(presentation.GetType()), Is.SameAs(componentType));
            }
        }

        [Test]
        public void VerifyViewModelFactoryPreservesScopedSelectionAndCallerOwnership()
        {
            var selection = new ContextAwareService();
            var services = new ServiceCollection().AddDiagramServices();
            services.AddSingleton<IElementSelectionService>(selection);
            using var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<Func<DiagramProjection, IDiagramViewModel>>();
            var element = new PartUsage();
            var projection = DiagramTestFactory.Create([element]);
            using var first = factory(projection);
            using var second = factory(projection);
            var nested = first.GetNestedView(projection.Nodes[0].Occurrence.Id, projection);
            first.Select(element);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(selection.SelectedElement, Is.SameAs(element));
                Assert.That(second.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).IsSelected, Is.True);
                Assert.That(nested.GetNodeViewModel(projection.Nodes[0].Occurrence.Id).IsSelected, Is.True);
            }

            scope.Dispose();
            Assert.DoesNotThrow(() => first.ChangeZoom(175));
            first.Dispose();
            Assert.Throws<ObjectDisposedException>(() => nested.Select(element));
            Assert.DoesNotThrow(() => second.Select(element));
        }

        [Test]
        public void VerifyNodeWithoutRegisteredNotationReportsConfigurationFailure()
        {
            var projector = new GeneralViewProjector([]);
            Assert.Throws<InvalidOperationException>(() => projector.Node(new PartUsage()));
        }

        [Test]
        public void VerifyComponentRegistryPrefersSpecificPresentationMapping()
        {
            var components = new DiagramNodeComponentRegistry([
                new(typeof(DiagramNodePresentation), typeof(DiagramCommentNode)),
                new(typeof(DiagramShellPresentation), typeof(DiagramShellNode))]);
            Assert.That(components.GetComponentType(typeof(DiagramShellPresentation)), Is.SameAs(typeof(DiagramShellNode)));
        }
    }
}
