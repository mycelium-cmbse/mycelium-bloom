// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaServiceCollectionExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Extensions
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Hypha;
    using Mycelium.Bloom.Extensions;

    [TestFixture]
    public sealed class HyphaServiceCollectionExtensionsTestFixture
    {
        private const string EndpointKey = $"{HyphaOptions.SectionName}:{nameof(HyphaOptions.Endpoint)}";

        [Test]
        public void VerifyAddHyphaServicesRejectsNullArguments()
        {
            IServiceCollection services = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => services.AddHyphaServices(CreateConfiguration("http://localhost:5180/")), Throws.ArgumentNullException);
                Assert.That(() => new ServiceCollection().AddHyphaServices(null), Throws.ArgumentNullException);
            }
        }

        [TestCase("http://localhost:5180/")]
        [TestCase("http://localhost:5180")]
        public void VerifyTheFabricAddressIsBoundAndTheClientIsRegistered(string endpoint)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHyphaServices(CreateConfiguration(endpoint));

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => serviceProvider.GetRequiredService<IStartupValidator>().Validate(), Throws.Nothing);
                Assert.That(serviceProvider.GetRequiredService<IOptions<HyphaOptions>>().Value.Endpoint, Is.EqualTo(new Uri(endpoint)));
                Assert.That(serviceProvider.GetRequiredService<IHyphaClient>(), Is.TypeOf<HyphaClient>());
            }
        }

        [TestCase(null)]
        [TestCase("localhost:5180/hypha")]
        public void VerifyAMissingOrRelativeFabricAddressStopsTheStart(string endpoint)
        {
            var services = new ServiceCollection();
            services.AddHyphaServices(CreateConfiguration(endpoint));

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            Assert.That(() => serviceProvider.GetRequiredService<IStartupValidator>().Validate(), Throws.InstanceOf<OptionsValidationException>());
        }

        private static IConfiguration CreateConfiguration(string endpoint)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { [EndpointKey] = endpoint })
                .Build();
        }
    }
}
