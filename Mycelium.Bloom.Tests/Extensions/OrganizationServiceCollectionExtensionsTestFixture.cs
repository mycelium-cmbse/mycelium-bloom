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
    using System.IO;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;

    [TestFixture]
    public sealed class OrganizationServiceCollectionExtensionsTestFixture
    {
        [Test]
        public void VerifyTheDevelopmentSettingsBindToTheOptions()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (!File.Exists(Path.Combine(directory.FullName, "Mycelium.Bloom", "appsettings.Development.json")))
            {
                directory = directory.Parent;
            }

            var configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(directory.FullName, "Mycelium.Bloom", "appsettings.Development.json")).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOrganizationServices(configuration);

            using var serviceProvider = services.BuildServiceProvider();

            Assert.That(serviceProvider.GetRequiredService<IOptions<OrganizationOptions>>().Value.PlaceholderRole, Is.EqualTo(OrganizationMembershipRole.Administrator));
        }

        [Test]
        public void VerifyOrganizationServicesAreRegistered()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var returned = services.AddOrganizationServices(new ConfigurationBuilder().Build());

            using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            using var scope = serviceProvider.CreateScope();
            using var otherScope = serviceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var context = provider.GetRequiredService<IOrganizationContextViewModel>();
            var otherContext = otherScope.ServiceProvider.GetRequiredService<IOrganizationContextViewModel>();
            var header = provider.GetRequiredService<IOrganizationHeaderViewModel>();
            context.Location = OrganizationLocation.Account;
            otherContext.Location = OrganizationLocation.None;
            var options = provider.GetRequiredService<IOptions<OrganizationOptions>>().Value;
            using var projectsScope = serviceProvider.CreateScope();
            projectsScope.ServiceProvider.GetRequiredService<IOrganizationContextViewModel>().Location =
                OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Projects);
            var createProjects = projectsScope.ServiceProvider.GetRequiredService<Func<IProjectsViewModel>>();
            using var projects = createProjects();
            using var otherProjects = createProjects();
            using var members = projectsScope.ServiceProvider.GetRequiredService<Func<IMembersViewModel>>()();
            using var invitations = projectsScope.ServiceProvider.GetRequiredService<Func<IPendingInvitationsViewModel>>()();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(returned, Is.SameAs(services));
                Assert.That(provider.GetRequiredService<IOrganizationContextService>(), Is.TypeOf<PlaceholderOrganizationContextService>());
                Assert.That(context, Is.TypeOf<OrganizationContextViewModel>());
                Assert.That(provider.GetRequiredService<IOrganizationContextViewModel>(), Is.SameAs(context));
                Assert.That(otherContext, Is.Not.SameAs(context));
                Assert.That(header, Is.TypeOf<OrganizationHeaderViewModel>());
                Assert.That(provider.GetRequiredService<IOrganizationHeaderViewModel>(), Is.SameAs(header));
                Assert.That(header.Presentation.Kind, Is.EqualTo(OrganizationLocationKind.Account));
                Assert.That(provider.GetRequiredService<IOrganizationProjectService>(), Is.TypeOf<PlaceholderOrganizationProjectService>());
                Assert.That(projects, Is.TypeOf<ProjectsViewModel>());
                Assert.That(provider.GetRequiredService<IOrganizationMemberService>(), Is.TypeOf<PlaceholderOrganizationMemberService>());
                Assert.That(members, Is.TypeOf<MembersViewModel>());
                Assert.That(provider.GetRequiredService<IOrganizationInvitationService>(), Is.TypeOf<PlaceholderOrganizationInvitationService>());
                Assert.That(invitations, Is.TypeOf<PendingInvitationsViewModel>());
                Assert.That(otherProjects, Is.Not.SameAs(projects));
                Assert.That(options.PlaceholderRole, Is.EqualTo(OrganizationMembershipRole.Administrator));
            }
        }
    }
}
