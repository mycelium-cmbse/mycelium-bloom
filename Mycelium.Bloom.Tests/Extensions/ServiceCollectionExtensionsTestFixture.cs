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
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.ChangeNotifications;
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

    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;

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
        public void VerifyAddApplicationServicesRegistersScopedChangeNotifications()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            var notifications = services.Single(descriptor => descriptor.ServiceType == typeof(IChangeNotificationService));
            var refresh = services.Single(descriptor => descriptor.ServiceType == typeof(IModelRefreshCoordinator));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notifications.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(notifications.ImplementationType, Is.EqualTo(typeof(ChangeNotificationService)));
                Assert.That(refresh.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(refresh.ImplementationType, Is.EqualTo(typeof(ModelRefreshCoordinator)));
            }
        }

        [Test]
        public async Task VerifyAddApplicationServicesSharesChangeNotificationsOnlyWithinScope()
        {
            var services = new ServiceCollection();
            var scheduler = new HistoricalScheduler();
            services.AddSingleton<IScheduler>(scheduler);
            services.AddApplicationServices();
            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();
            var firstService = firstScope.ServiceProvider.GetRequiredService<IChangeNotificationService>();
            var secondService = secondScope.ServiceProvider.GetRequiredService<IChangeNotificationService>();
            var firstRefresh = firstScope.ServiceProvider.GetRequiredService<IModelRefreshCoordinator>();
            var secondRefresh = secondScope.ServiceProvider.GetRequiredService<IModelRefreshCoordinator>();
            Assert.That(firstService, Is.SameAs(firstScope.ServiceProvider.GetRequiredService<IChangeNotificationService>()));
            Assert.That(firstRefresh, Is.SameAs(firstScope.ServiceProvider.GetRequiredService<IModelRefreshCoordinator>()));
            Assert.That(secondService, Is.Not.SameAs(firstService));
            Assert.That(secondRefresh, Is.Not.SameAs(firstRefresh));
            var handler = new Mock<IModelRefreshHandler>(MockBehavior.Strict);
            handler.Setup(x => x.ReloadAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            using var registration = firstRefresh.Register(handler.Object);

            await firstService.RequestRefresh();

            handler.Verify(x => x.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(Assert.ThrowsAsync<InvalidOperationException>(() => secondService.RequestRefresh()), Is.Not.Null);
            firstScope.Dispose();
            secondScope.Dispose();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersUrlContextResolutionBoundaries()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            var assembler = services.Single(descriptor => descriptor.ServiceType == typeof(IAssembler));
            var deSerializer = services.Single(descriptor => descriptor.ServiceType == typeof(IDeSerializer));
            var elementResolver = services.Single(descriptor => descriptor.ServiceType == typeof(IElementIdResolver));
            var urlContextFactory = services.Single(descriptor =>
                descriptor.ServiceType == typeof(Func<IWorkspaceUrlContextService>));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(assembler.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(assembler.ImplementationType, Is.EqualTo(typeof(Assembler)));
                Assert.That(deSerializer.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(deSerializer.ImplementationType, Is.EqualTo(typeof(DeSerializer)));
                Assert.That(elementResolver.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(elementResolver.ImplementationType, Is.EqualTo(typeof(ElementIdResolver)));
                Assert.That(urlContextFactory.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(urlContextFactory.ImplementationFactory, Is.Not.Null);
            }
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersCallerOwnedNavigationFactory()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            using var firstScope = serviceProvider.CreateScope();
            using var secondScope = serviceProvider.CreateScope();
            var firstProvider = firstScope.ServiceProvider.GetRequiredService<INavigationRailItemProvider>();
            var secondProvider = secondScope.ServiceProvider.GetRequiredService<INavigationRailItemProvider>();
            var firstFactory = firstScope.ServiceProvider.GetRequiredService<Func<INavigationRailViewModel>>();
            var sameScopeFactory = firstScope.ServiceProvider.GetRequiredService<Func<INavigationRailViewModel>>();
            var secondScopeFactory = secondScope.ServiceProvider.GetRequiredService<Func<INavigationRailViewModel>>();
            using var firstViewModel = firstFactory();
            using var secondViewModel = firstFactory();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstProvider, Is.TypeOf<NavigationRailItemProvider>());
                Assert.That(secondProvider, Is.SameAs(firstProvider));
                Assert.That((object)sameScopeFactory, Is.SameAs(firstFactory));
                Assert.That((object)secondScopeFactory, Is.Not.SameAs(firstFactory));
                Assert.That(firstViewModel, Is.TypeOf<NavigationRailViewModel>());
                Assert.That(secondViewModel, Is.TypeOf<NavigationRailViewModel>());
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
                Assert.That(firstScope.ServiceProvider.GetService<INavigationRailViewModel>(), Is.Null);
            }
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersCallerOwnedWorkspaceEditorFactory()
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
            var factory = scope.ServiceProvider.GetRequiredService<Func<IWorkspaceEditorViewModel>>();
            using var firstViewModel = factory();
            using var secondViewModel = factory();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel, Is.TypeOf<WorkspaceEditorViewModel>());
                Assert.That(secondViewModel, Is.TypeOf<WorkspaceEditorViewModel>());
                Assert.That(secondViewModel, Is.Not.SameAs(firstViewModel));
                Assert.That(firstViewModel.MaximumGroupCount, Is.EqualTo(3));
                Assert.That(secondViewModel.MaximumGroupCount, Is.EqualTo(3));
                Assert.That(scope.ServiceProvider.GetService<IWorkspaceEditorViewModel>(), Is.Null);
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
        public void VerifyCallerOwnedFactoriesUseSharedScopedContextAliases()
        {
            var services = new ServiceCollection();
            var modelLoaderService = new Mock<IModelLoaderService>();
            services.AddApplicationServices();
            services.AddScoped(_ => modelLoaderService.Object);

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ContextAwareService>();
            var navigationFactory = scope.ServiceProvider.GetRequiredService<Func<INavigationRailViewModel>>();
            var projectBrowserFactory = scope.ServiceProvider.GetRequiredService<Func<IProjectBrowserViewModel>>();
            using var navigation = navigationFactory();
            using var projectBrowser = projectBrowserFactory();
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("shared", "Shared");
            var navigationNotificationCount = 0;
            navigation.PropertyChanged += (_, args) =>
            {
                if (string.Equals(args.PropertyName, nameof(navigation.NavigationItems), StringComparison.Ordinal))
                {
                    navigationNotificationCount++;
                }
            };

            projectBrowser.SelectNode(node);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scope.ServiceProvider.GetRequiredService<IContextAwareService>(), Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<IElementSelectionService>(), Is.SameAs(context));
                Assert.That(context.SelectedElement, Is.SameAs(node.SourceElement));
                Assert.That(navigationNotificationCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void VerifyAddApplicationServicesRegistersCallerOwnedProjectBrowserFactory()
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
            var factory = scope.ServiceProvider.GetRequiredService<Func<IProjectBrowserViewModel>>();
            var firstViewModel = factory();
            var secondViewModel = factory();
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
                Assert.That(scope.ServiceProvider.GetService<IProjectBrowserViewModel>(), Is.Null);
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
