// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModelFactoryTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Tests the <see cref="ProjectBrowserViewModelFactory" />.
    /// </summary>
    [TestFixture]
    public sealed class ProjectBrowserViewModelFactoryTestFixture
    {
        [Test]
        public void VerifyConstructorRejectsNullDependencies()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            var selectionService = new ContextAwareService();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => new ProjectBrowserViewModelFactory(null, selectionService),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("modelLoaderService"));
                Assert.That(
                    () => new ProjectBrowserViewModelFactory(modelLoaderService.Object, null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("elementSelectionService"));
            }
        }

        [Test]
        public void VerifyCreateReturnsFreshCallerOwnedViewModelsWithSharedDependencies()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            var modelLoaderDisposal = modelLoaderService.As<IDisposable>();
            var selectionService = new Mock<IElementSelectionService>();
            var selectionServiceDisposal = selectionService.As<IDisposable>();
            selectionService.SetupProperty(service => service.SelectedElement);
            var factory = new ProjectBrowserViewModelFactory(
                modelLoaderService.Object,
                selectionService.Object);

            var firstViewModel = factory.Create();
            var secondViewModel = factory.Create();
            var firstNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First");
            var secondNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("second", "Second");

            firstViewModel.SelectNode(firstNode);
            selectionService.VerifySet(
                service => service.SelectedElement = firstNode.SourceElement,
                Times.Once);
            secondViewModel.SelectNode(secondNode);
            selectionService.VerifySet(
                service => service.SelectedElement = secondNode.SourceElement,
                Times.Once);

            firstViewModel.Dispose();
            secondViewModel.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel, Is.InstanceOf<IProjectBrowserViewModel>());
                Assert.That(firstViewModel, Is.TypeOf<ProjectBrowserViewModel>());
                Assert.That(secondViewModel, Is.TypeOf<ProjectBrowserViewModel>());
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
                modelLoaderDisposal.Verify(service => service.Dispose(), Times.Never);
                selectionServiceDisposal.Verify(service => service.Dispose(), Times.Never);
            }
        }
    }
}
