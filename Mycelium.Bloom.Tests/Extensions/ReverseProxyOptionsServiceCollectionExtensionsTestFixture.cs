// ------------------------------------------------------------------------------------------------
// <copyright file="ReverseProxyOptionsServiceCollectionExtensionsTestFixture.cs" company="Starion Group S.A.">
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
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Extensions;

    [TestFixture]
    public sealed class ReverseProxyOptionsServiceCollectionExtensionsTestFixture
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void VerifyAddReverseProxyOptionsRetainsRestrictedDefaultsWithoutConfiguredProxy(string knownProxy)
        {
            using var provider = CreateProvider(knownProxy);

            Assert.That(() => provider.GetRequiredService<IStartupValidator>().Validate(), Throws.Nothing);

            var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
            var defaults = new ForwardedHeadersOptions();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options.ForwardedHeaders,
                    Is.EqualTo(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto));
                Assert.That(options.RequireHeaderSymmetry, Is.True);
                Assert.That(options.ForwardLimit, Is.EqualTo(1));
                Assert.That(options.KnownProxies, Is.EqualTo(defaults.KnownProxies));
                Assert.That(options.KnownIPNetworks, Is.EqualTo(defaults.KnownIPNetworks));
            }
        }

        [TestCase("192.0.2.10")]
        [TestCase("2001:db8::10")]
        public void VerifyAddReverseProxyOptionsAddsConfiguredProxy(string knownProxy)
        {
            using var provider = CreateProvider(knownProxy);

            Assert.That(() => provider.GetRequiredService<IStartupValidator>().Validate(), Throws.Nothing);

            var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

            Assert.That(options.KnownProxies, Does.Contain(IPAddress.Parse(knownProxy)));
            Assert.That(options.KnownProxies, Has.Count.EqualTo(2));
        }

        [TestCase("invalid-proxy")]
        [TestCase("192.0.2.10/24")]
        [TestCase("192.0.2.10:8080")]
        public void VerifyAddReverseProxyOptionsRejectsInvalidProxyAtStartup(string knownProxy)
        {
            using var provider = CreateProvider(knownProxy);
            var validator = provider.GetRequiredService<IStartupValidator>();

            var exception = Assert.Throws<OptionsValidationException>(() => validator.Validate());

            Assert.That(exception.Message, Does.Contain("ReverseProxy:KnownProxy"));
        }

        [TestCase("192.0.2.10", "https")]
        [TestCase("192.0.2.11", "http")]
        public async Task VerifyAddReverseProxyOptionsOnlyAcceptsForwardingFromTrustedProxy(
            string remoteAddress, string expectedScheme)
        {
            using var provider = CreateProvider("192.0.2.10");
            var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
            var middleware = new ForwardedHeadersMiddleware(
                _ => Task.CompletedTask, NullLoggerFactory.Instance, options);
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
            context.Request.Scheme = "http";
            context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName] = "198.51.100.20";
            context.Request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName] = "https";

            await middleware.Invoke(context);

            Assert.That(context.Request.Scheme, Is.EqualTo(expectedScheme));
        }

        [Test]
        public async Task VerifyAddReverseProxyOptionsRejectsAsymmetricForwardingHeaders()
        {
            using var provider = CreateProvider("192.0.2.10");
            var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
            var middleware = new ForwardedHeadersMiddleware(
                _ => Task.CompletedTask, NullLoggerFactory.Instance, options);
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
            context.Request.Scheme = "http";
            context.Request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName] = "https";

            await middleware.Invoke(context);

            Assert.That(context.Request.Scheme, Is.EqualTo("http"));
        }

        [Test]
        public void VerifyAddReverseProxyOptionsRejectsNullServices()
        {
            IServiceCollection services = null;
            var configuration = new ConfigurationBuilder().Build();

            Assert.That(() => services.AddReverseProxyOptions(configuration), Throws.ArgumentNullException);
        }

        [Test]
        public void VerifyAddReverseProxyOptionsRejectsNullConfiguration()
        {
            var services = new ServiceCollection();

            Assert.That(() => services.AddReverseProxyOptions(null), Throws.ArgumentNullException);
        }

        private static ServiceProvider CreateProvider(string knownProxy)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ReverseProxy:KnownProxy"] = knownProxy
                })
                .Build();
            var services = new ServiceCollection();

            Assert.That(services.AddReverseProxyOptions(configuration), Is.SameAs(services));

            return services.BuildServiceProvider(validateScopes: true);
        }
    }
}
