// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBenchmarkOptionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Hosting
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Extensions;

    [TestFixture]
    public sealed class DiagramBenchmarkOptionsTestFixture
    {
        private const string Capability = "deterministic-test-capability-value";

        [TestCase(2048, 30, 2, 1, true)]
        [TestCase(0, 30, 2, 1, false)]
        [TestCase(2048, 0, 2, 1, false)]
        [TestCase(2048, 30, 33, 1, false)]
        [TestCase(2048, 30, 2, 5, false)]
        public void VerifyDiagramDataOptionsBindAndRejectInvalidReadBounds(int capacity, int expiration, int batch,
            int concurrency, bool valid)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DiagramData:CapacityBytes"] = capacity.ToString(),
                ["DiagramData:ExpirationSeconds"] = expiration.ToString(),
                ["DiagramData:BatchSize"] = batch.ToString(),
                ["DiagramData:ConcurrentRequests"] = concurrency.ToString()
            }).Build();
            var services = new ServiceCollection();
            services.AddDiagramBenchmarkOptions(configuration);
            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService<IStartupValidator>();
            if (!valid)
            {
                Assert.That(validator.Validate, Throws.TypeOf<OptionsValidationException>());
                return;
            }

            Assert.That(validator.Validate, Throws.Nothing);
            var options = provider.GetRequiredService<IOptions<DiagramDataOptions>>().Value;
            Assert.That(options.CapacityBytes, Is.EqualTo(capacity));
            Assert.That(options.ExpirationSeconds, Is.EqualTo(expiration));
            Assert.That(options.BatchSize, Is.EqualTo(batch));
            Assert.That(options.ConcurrentRequests, Is.EqualTo(concurrency));
        }

        [TestCase(false, 80, false, false)]
        [TestCase(true, 80, false, true)]
        [TestCase(true, 80, true, true)]
        [TestCase(true, 0, true, false)]
        [TestCase(true, 1429, false, false)]
        public void VerifyOnDemandFixtureRequiresExplicitIsolatedConfiguration(bool isolated, int tiles, bool eager,
            bool valid)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DiagramBenchmark:Enabled"] = isolated.ToString(),
                ["DiagramBenchmark:LargeSceneEnabled"] = isolated.ToString(),
                ["DiagramBenchmark:AccessKey"] = Capability,
                ["DiagramBenchmark:OnDemandTiles"] = tiles.ToString(),
                ["DiagramBenchmark:PreloadOnDemandFixture"] = eager.ToString()
            }).Build();
            var services = new ServiceCollection();
            services.AddDiagramBenchmarkOptions(configuration);
            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService<IStartupValidator>();
            if (valid)
            {
                Assert.That(validator.Validate, Throws.Nothing);
                Assert.That(provider.GetRequiredService<IOptions<DiagramBenchmarkOptions>>().Value.OnDemandTiles,
                    Is.EqualTo(tiles));
            }
            else
            {
                Assert.That(validator.Validate, Throws.TypeOf<OptionsValidationException>());
            }
        }

        [TestCase("0.5", "160", true)]
        [TestCase("0.01", "160", false)]
        [TestCase("0.5", "-1", false)]
        public void VerifyViewportOptionsBindAndRejectInvalidBounds(string minimum, string margin, bool valid)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DiagramViewport:MinimumZoom"] = minimum,
                ["DiagramViewport:Margin"] = margin
            }).Build();
            var services = new ServiceCollection();
            services.AddDiagramBenchmarkOptions(configuration);
            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService<IStartupValidator>();
            if (valid)
            {
                Assert.That(validator.Validate, Throws.Nothing);
                Assert.That(provider.GetRequiredService<IOptions<DiagramViewportOptions>>().Value.MinimumZoom,
                    Is.EqualTo(0.5));
                Assert.That(provider.GetRequiredService<IOptions<DiagramViewportOptions>>().Value.Margin,
                    Is.EqualTo(160));
            }
            else
            {
                Assert.That(validator.Validate, Throws.TypeOf<OptionsValidationException>());
            }
        }

        [TestCase(false, null, true)]
        [TestCase(true, null, false)]
        [TestCase(true, "short", false)]
        [TestCase(true, Capability, true)]
        public void VerifyStartupValidationRequiresExplicitCapability(bool enabled, string key, bool valid)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DiagramBenchmark:Enabled"] = enabled.ToString(),
                ["DiagramBenchmark:AccessKey"] = key
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDiagramBenchmarkOptions(configuration);
            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService<IStartupValidator>();
            if (valid)
            {
                Assert.That(validator.Validate, Throws.Nothing);
                Assert.That(provider.GetRequiredService<IOptions<DiagramBenchmarkOptions>>().Value.Enabled,
                    Is.EqualTo(enabled));
            }
            else
            {
                Assert.That(validator.Validate, Throws.TypeOf<OptionsValidationException>());
            }
        }

        [TestCase(false, true, false, false, false)]
        [TestCase(true, false, true, false, false)]
        [TestCase(true, true, true, false, true)]
        [TestCase(true, false, false, true, false)]
        [TestCase(false, true, false, true, false)]
        [TestCase(true, true, false, true, true)]
        public void VerifyLargeRenderingOptionsRequireIsolatedConfiguration(bool enabled, bool large, bool asynchronous,
            bool simple, bool valid)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DiagramBenchmark:Enabled"] = enabled.ToString(),
                ["DiagramBenchmark:LargeSceneEnabled"] = large.ToString(),
                ["DiagramBenchmark:AsyncRendering"] = asynchronous.ToString(),
                ["DiagramBenchmark:SimpleRouting"] = simple.ToString(),
                ["DiagramBenchmark:AccessKey"] = Capability
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDiagramBenchmarkOptions(configuration);
            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService<IStartupValidator>();
            if (valid)
            {
                Assert.That(validator.Validate, Throws.Nothing);
                var options = provider.GetRequiredService<IOptions<DiagramBenchmarkOptions>>().Value;
                Assert.That(options.AsyncRendering, Is.EqualTo(asynchronous));
                Assert.That(options.SimpleRouting, Is.EqualTo(simple));
            }
            else
            {
                Assert.That(validator.Validate, Throws.TypeOf<OptionsValidationException>());
            }
        }

        [TestCase(false, "/workspace/modeling", null, 204)]
        [TestCase(true, "/workspace/modeling", null, 403)]
        [TestCase(true, "/_blazor/negotiate", "incorrect", 403)]
        [TestCase(true, "/vendor/x6/x6.js", null, 403)]
        [TestCase(true, "/workspace/modeling", Capability, 204)]
        [TestCase(true, "/ready", null, 204)]
        [TestCase(true, "/healthz", null, 204)]
        public async Task VerifyDiagnosticPipelineRequiresCapabilityExceptHealth(bool enabled, string path, string key,
            int expectedStatus)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<DiagramBenchmarkOptions>>(Options.Create(new DiagramBenchmarkOptions
            { Enabled = enabled, AccessKey = Capability }));
            using var provider = services.BuildServiceProvider();
            var application = new ApplicationBuilder(provider);
            application.UseDiagramBenchmarkAccess();
            application.Run(context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
            var request = new DefaultHttpContext { RequestServices = provider };
            request.Request.Path = path;
            if (key is not null)
            {
                request.Request.Headers.Cookie = $"{DiagramBenchmarkOptions.CookieName}={key}";
            }

            await application.Build()(request);
            Assert.That(request.Response.StatusCode, Is.EqualTo(expectedStatus));
        }
    }
}