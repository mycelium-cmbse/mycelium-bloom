// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceEditorOptionsServiceCollectionExtensionsTestFixture.cs" company="Starion Group S.A.">
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
    using System.Globalization;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Extensions;

    [TestFixture]
    public sealed class WorkspaceEditorOptionsServiceCollectionExtensionsTestFixture
    {
        private const string MaximumGroupCountConfigurationKey = 
            $"{WorkspaceEditorOptions.SectionName}:{nameof(WorkspaceEditorOptions.MaximumGroupCount)}";

        [Test]
        public void VerifyAddWorkspaceEditorOptionsRejectsNullServices()
        {
            IServiceCollection services = null;
            var configuration = CreateConfiguration(3);

            var exception = Assert.Throws<ArgumentNullException>(
                () => services.AddWorkspaceEditorOptions(configuration));

            Assert.That(exception.ParamName, Is.EqualTo(nameof(services)));
        }

        [Test]
        public void VerifyAddWorkspaceEditorOptionsRejectsNullConfiguration()
        {
            var services = new ServiceCollection();
            IConfiguration configuration = null;

            var exception = Assert.Throws<ArgumentNullException>(
                () => services.AddWorkspaceEditorOptions(configuration));

            Assert.That(exception.ParamName, Is.EqualTo(nameof(configuration)));
        }

        [Test]
        public void VerifyAddWorkspaceEditorOptionsReturnsSameCollection()
        {
            var services = new ServiceCollection();

            Assert.That(
                services.AddWorkspaceEditorOptions(CreateConfiguration(3)),
                Is.SameAs(services));
        }

        [TestCase(3)]
        [TestCase(5)]
        public void VerifyAddWorkspaceEditorOptionsBindsAndValidatesConfiguredMaximumGroupCount(
            int maximumGroupCount)
        {
            var services = new ServiceCollection();
            services.AddWorkspaceEditorOptions(CreateConfiguration(maximumGroupCount));

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();

            Assert.That(() => startupValidator.Validate(), Throws.Nothing);

            var options = serviceProvider.GetRequiredService<IOptions<WorkspaceEditorOptions>>();

            Assert.That(options.Value.MaximumGroupCount, Is.EqualTo(maximumGroupCount));
        }

        [Test]
        public void VerifyAddWorkspaceEditorOptionsRejectsMissingMaximumGroupCount()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();
            var services = new ServiceCollection();
            services.AddWorkspaceEditorOptions(configuration);

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();

            var exception = Assert.Throws<OptionsValidationException>(() => startupValidator.Validate());

            Assert.That(exception.Message, Does.Contain(MaximumGroupCountConfigurationKey));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void VerifyAddWorkspaceEditorOptionsRejectsMaximumGroupCountBelowOne(int maximumGroupCount)
        {
            var services = new ServiceCollection();
            services.AddWorkspaceEditorOptions(CreateConfiguration(maximumGroupCount));

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();

            var exception = Assert.Throws<OptionsValidationException>(() => startupValidator.Validate());

            Assert.That(exception.Message, Does.Contain(MaximumGroupCountConfigurationKey));
        }

        [Test]
        public void VerifyAddWorkspaceEditorOptionsRejectsMalformedMaximumGroupCount()
        {
            var services = new ServiceCollection();
            services.AddWorkspaceEditorOptions(CreateConfiguration("abc"));

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();

            var exception = Assert.Throws<InvalidOperationException>(() => startupValidator.Validate());

            Assert.That(exception.Message, Does.Contain(MaximumGroupCountConfigurationKey));
        }

        private static IConfiguration CreateConfiguration(int maximumGroupCount)
        {
            return CreateConfiguration(maximumGroupCount.ToString(CultureInfo.InvariantCulture));
        }

        private static IConfiguration CreateConfiguration(string maximumGroupCount)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    [MaximumGroupCountConfigurationKey] = maximumGroupCount
                })
                .Build();
        }
    }
}
