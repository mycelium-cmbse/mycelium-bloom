// ------------------------------------------------------------------------------------------------
// <copyright file="MyceliumSdkServiceCollectionExtensionsTestFixture.cs" company="Starion Group S.A.">
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

    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Extensions;
    using Mycelium.SDK.Serializer.Json;

    using SysMLDeSerializer = SysML2.NET.Serializer.Json.IDeSerializer;

    [TestFixture]
    public sealed class MyceliumSdkServiceCollectionExtensionsTestFixture
    {
        [Test]
        public void VerifyAddMyceliumSdkServicesRejectsNullCollection()
        {
            IServiceCollection services = null;

            var exception = Assert.Throws<ArgumentNullException>(() => services.AddMyceliumSdkServices());

            Assert.That(exception.ParamName, Is.EqualTo(nameof(services)));
        }

        [Test]
        public void VerifyAddMyceliumSdkServicesReturnsSameCollection()
        {
            var services = new ServiceCollection();

            Assert.That(services.AddMyceliumSdkServices(), Is.SameAs(services));
        }

        [Test]
        public void VerifyAddMyceliumSdkServicesResolvesCompleteSharedGraph()
        {
            var services = new ServiceCollection();
            services.AddMyceliumSdkServices();

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();
            var serializer = firstScope.ServiceProvider.GetRequiredService<ISerializer>();
            var deSerializer = firstScope.ServiceProvider.GetRequiredService<IDeSerializer>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(serializer, Is.TypeOf<Serializer>());
                Assert.That(deSerializer, Is.TypeOf<DeSerializer>());
                Assert.That(secondScope.ServiceProvider.GetRequiredService<ISerializer>(), Is.SameAs(serializer));
                Assert.That(secondScope.ServiceProvider.GetRequiredService<IDeSerializer>(), Is.SameAs(deSerializer));
            }
        }

        [Test]
        public void VerifyAddApplicationServicesKeepsSerializerContractsIndependent()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();
            var serializers = scope.ServiceProvider.GetServices<ISerializer>().ToArray();
            var deSerializers = scope.ServiceProvider.GetServices<IDeSerializer>().ToArray();
            var sysMlDeSerializers = scope.ServiceProvider.GetServices<SysMLDeSerializer>().ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(serializers, Has.Length.EqualTo(1));
                Assert.That(deSerializers, Has.Length.EqualTo(1));
                Assert.That(sysMlDeSerializers, Has.Length.EqualTo(1));
                Assert.That(serializers[0], Is.TypeOf<Serializer>());
                Assert.That(deSerializers[0], Is.TypeOf<DeSerializer>());
                Assert.That(sysMlDeSerializers[0], Is.TypeOf<SysML2.NET.Serializer.Json.DeSerializer>());
            }
        }
    }
}
