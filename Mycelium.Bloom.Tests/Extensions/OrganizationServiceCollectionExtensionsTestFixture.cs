// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationServiceCollectionExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Extensions
{
    using System;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Tests the registration of the organization services.
    /// </summary>
    [TestFixture]
    public sealed class OrganizationServiceCollectionExtensionsTestFixture
    {
        /// <summary>
        /// Verifies that the registration returns the same collection, provides the placeholder context service, and
        /// that the factories create the ViewModels with the services of the container.
        /// </summary>
        [Test]
        public void VerifyOrganizationServicesAreRegistered()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<NavigationManager, StubNavigationManager>();

            var returned = services.AddOrganizationServices(new ConfigurationBuilder().Build());

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<Func<IOrganizationContextViewModel>>()();
            using var header = scope.ServiceProvider
                .GetRequiredService<Func<IOrganizationContextViewModel, IOrganizationHeaderViewModel>>()(context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(returned, Is.SameAs(services));
                Assert.That(scope.ServiceProvider.GetRequiredService<IOrganizationContextService>(), Is.TypeOf<PlaceholderOrganizationContextService>());
                Assert.That(context, Is.TypeOf<OrganizationContextViewModel>());
                Assert.That(header, Is.TypeOf<OrganizationHeaderViewModel>());
            }
        }

        /// <summary>
        /// A navigation service positioned on the account page, for the ViewModels that observe the location.
        /// </summary>
        private sealed class StubNavigationManager : NavigationManager
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StubNavigationManager" /> class.
            /// </summary>
            public StubNavigationManager()
            {
                this.Initialize("http://localhost/", "http://localhost/account");
            }
        }
    }
}
