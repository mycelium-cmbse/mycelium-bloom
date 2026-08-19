// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceEditorOptionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;

    [TestFixture]
    public sealed class WorkspaceEditorOptionsTestFixture
    {
        private static readonly string MaximumGroupCountConfigurationKey =
            $"{WorkspaceEditorOptions.SectionName}:{nameof(WorkspaceEditorOptions.MaximumGroupCount)}";

        [TestCase(3)]
        [TestCase(5)]
        public async Task VerifyOptionsBindValidMaximumGroupCountOnStartup(int maximumGroupCount)
        {
            using var host = CreateHost(CreateConfigurationValues(maximumGroupCount));

            await host.StartAsync();

            var options = host.Services.GetRequiredService<IOptions<WorkspaceEditorOptions>>();

            Assert.That(options.Value.MaximumGroupCount, Is.EqualTo(maximumGroupCount));

            await host.StopAsync();
        }

        [Test]
        public void VerifyOptionsValidationRejectsMissingMaximumGroupCountOnStartup()
        {
            using var host = CreateHost(new Dictionary<string, string>());

            var exception = Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

            Assert.That(exception.Message, Does.Contain(MaximumGroupCountConfigurationKey));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void VerifyOptionsValidationRejectsMaximumGroupCountBelowOneOnStartup(int maximumGroupCount)
        {
            using var host = CreateHost(CreateConfigurationValues(maximumGroupCount));

            var exception = Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

            Assert.That(exception.Message, Does.Contain(MaximumGroupCountConfigurationKey));
        }

        [Test]
        public void VerifyOptionsBindingRejectsMalformedMaximumGroupCountOnStartup()
        {
            using var host = CreateHost(new Dictionary<string, string>
            {
                [MaximumGroupCountConfigurationKey] = "not-an-integer"
            });

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

            Assert.That(exception.Message, Does.Contain(MaximumGroupCountConfigurationKey));
        }

        private static IHost CreateHost(Dictionary<string, string> configurationValues)
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(configurationValues))
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions<WorkspaceEditorOptions>()
                        .Bind(context.Configuration.GetSection(WorkspaceEditorOptions.SectionName))
                        .Validate(
                            options => options.MaximumGroupCount >= 1,
                            $"{MaximumGroupCountConfigurationKey} must be at least 1.")
                        .ValidateOnStart();
                })
                .Build();
        }

        private static Dictionary<string, string> CreateConfigurationValues(int maximumGroupCount)
        {
            return new Dictionary<string, string>
            {
                [MaximumGroupCountConfigurationKey] = maximumGroupCount.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
