// ------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Extensions
{
    using System;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    using Moq;

    [TestFixture]
    public sealed class ServiceCollectionExtensionsTestFixture
    {
        [Test]
        public void VerifyAddApplicationServicesRejectsNullCollection()
        {
            IServiceCollection services = null;

            var exception = Assert.Throws<ArgumentNullException>(() => services.AddApplicationServices());

            Assert.That(exception.ParamName, Is.EqualTo(nameof(services)));
        }

        [Test]
        public void VerifyAddApplicationServicesReturnsSameCollection()
        {
            var services = new ServiceCollection();

            Assert.That(services.AddApplicationServices(), Is.SameAs(services));
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersNavigationRailLifetimes()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            using var firstScope = serviceProvider.CreateScope();
            using var secondScope = serviceProvider.CreateScope();
            var firstProvider = firstScope.ServiceProvider.GetRequiredService<INavigationRailItemProvider>();
            var secondProvider = secondScope.ServiceProvider.GetRequiredService<INavigationRailItemProvider>();
            var firstViewModel = firstScope.ServiceProvider.GetRequiredService<INavigationRailViewModel>();
            var secondViewModel = firstScope.ServiceProvider.GetRequiredService<INavigationRailViewModel>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstProvider, Is.TypeOf<NavigationRailItemProvider>());
                Assert.That(secondProvider, Is.SameAs(firstProvider));
                Assert.That(firstViewModel, Is.TypeOf<NavigationRailViewModel>());
                Assert.That(secondViewModel, Is.TypeOf<NavigationRailViewModel>());
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
            }
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersWorkspaceEditorViewModelAsTransient()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<WorkspaceEditorOptions>>(
                Options.Create(new WorkspaceEditorOptions
                {
                    MaximumGroupCount = 3
                }));
            services.AddApplicationServices();

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            using var scope = serviceProvider.CreateScope();
            var firstViewModel = scope.ServiceProvider.GetRequiredService<IWorkspaceEditorViewModel>();
            var secondViewModel = scope.ServiceProvider.GetRequiredService<IWorkspaceEditorViewModel>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel, Is.TypeOf<WorkspaceEditorViewModel>());
                Assert.That(secondViewModel, Is.TypeOf<WorkspaceEditorViewModel>());
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
                Assert.That(firstViewModel.MaximumGroupCount, Is.EqualTo(3));
                Assert.That(secondViewModel.MaximumGroupCount, Is.EqualTo(3));
            }
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersScopedContextAliases()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            using var firstScope = serviceProvider.CreateScope();
            using var secondScope = serviceProvider.CreateScope();
            var firstConcreteContext = firstScope.ServiceProvider.GetRequiredService<ContextAwareService>();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<IContextAwareService>();
            var firstSelection = firstScope.ServiceProvider.GetRequiredService<IElementSelectionService>();
            var secondConcreteContext = secondScope.ServiceProvider.GetRequiredService<ContextAwareService>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<IContextAwareService>();
            var secondSelection = secondScope.ServiceProvider.GetRequiredService<IElementSelectionService>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstContext, Is.SameAs(firstConcreteContext));
                Assert.That(firstSelection, Is.SameAs(firstConcreteContext));
                Assert.That(secondContext, Is.SameAs(secondConcreteContext));
                Assert.That(secondSelection, Is.SameAs(secondConcreteContext));
                Assert.That(secondConcreteContext, Is.Not.SameAs(firstConcreteContext));
            }
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersProjectBrowserViewModelAsTransient()
        {
            var services = new ServiceCollection();
            var modelLoaderService = new Mock<IModelLoaderService>();
            var modelLoaderDisposal = modelLoaderService.As<IDisposable>();
            var selectionService = new Mock<IElementSelectionService>();
            var selectionServiceDisposal = selectionService.As<IDisposable>();
            selectionService.SetupProperty(service => service.SelectedElement);
            services.AddApplicationServices();
            services.AddScoped(_ => modelLoaderService.Object);
            services.AddScoped(_ => selectionService.Object);

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var scope = serviceProvider.CreateScope();
            var firstViewModel = scope.ServiceProvider.GetRequiredService<IProjectBrowserViewModel>();
            var secondViewModel = scope.ServiceProvider.GetRequiredService<IProjectBrowserViewModel>();
            var firstNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("first", "First");
            var secondNode = ProjectBrowserNodeTestFactory.CreateNamespaceNode("second", "Second");

            firstViewModel.SelectNode(firstNode);
            secondViewModel.SelectNode(secondNode);
            firstViewModel.Dispose();
            secondViewModel.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel, Is.TypeOf<ProjectBrowserViewModel>());
                Assert.That(secondViewModel, Is.TypeOf<ProjectBrowserViewModel>());
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
                Assert.That(selectionService.Object.SelectedElement, Is.SameAs(secondNode.SourceElement));
                selectionService.VerifySet(
                    service => service.SelectedElement = firstNode.SourceElement,
                    Times.Once);
                selectionService.VerifySet(
                    service => service.SelectedElement = secondNode.SourceElement,
                    Times.Once);
                modelLoaderDisposal.Verify(service => service.Dispose(), Times.Never);
                selectionServiceDisposal.Verify(service => service.Dispose(), Times.Never);
            }

            Assert.That(scope.Dispose, Throws.Nothing);

            using (Assert.EnterMultipleScope())
            {
                modelLoaderDisposal.Verify(service => service.Dispose(), Times.Once);
                selectionServiceDisposal.Verify(service => service.Dispose(), Times.Once);
            }
        }
    }
}
