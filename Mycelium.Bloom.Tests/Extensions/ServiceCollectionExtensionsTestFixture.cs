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

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.ViewModel.NavigationRail;

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
    }
}
