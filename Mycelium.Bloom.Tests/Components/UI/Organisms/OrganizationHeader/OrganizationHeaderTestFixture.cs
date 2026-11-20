// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationHeaderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.OrganizationHeader
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.OrganizationHeader;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK.POCO;

    [TestFixture]
    public sealed class OrganizationHeaderTestFixture
    {
        private BunitContext context;

        private IRenderedComponent<BbPortalHost> portalHost;

        private Mock<IOrganizationHeaderViewModel> viewModel;

        private OrganizationHeaderPresentation organizationPresentation;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();

            this.organizationPresentation = new OrganizationHeaderPresentation
            {
                Kind = OrganizationLocationKind.Organization,
                OrganizationName = "Orbital Systems",
                OrganizationPath = "/organizations/orbital-systems",
                SectionLabel = "Members",
                SectionIcon = "users",
                SectionItems = [new ActionMenuItem { Id = "/organizations/orbital-systems/projects", Label = "Projects", IsSelected = true }],
                Account = new User { Id = Guid.NewGuid(), Name = "Sample Account", Email = "sample@example.org" },
                AccountItems = [new ActionMenuItem { Id = "/account", Label = "Your profile" }]
            };

            this.viewModel = new Mock<IOrganizationHeaderViewModel>();
            this.context.Services.AddSingleton(this.viewModel.Object);
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this.context);
        }

        [TearDown]
        public async Task TearDown()
        {
            this.portalHost.Dispose();
            await this.context.DisposeAsync();
        }

        [TestCase(OrganizationLocationKind.Organization, true, true, true)]
        [TestCase(OrganizationLocationKind.Account, false, false, true)]
        [TestCase(OrganizationLocationKind.None, false, false, false)]
        public void VerifyPresentationSelectsTheOrganizationLinkAndTheMenus(OrganizationLocationKind kind, bool hasOrganizationLink, bool hasSectionMenu, bool hasAccountMenu)
        {
            var presentation = kind == OrganizationLocationKind.None
                ? OrganizationHeaderPresentation.Empty
                : this.organizationPresentation with { Kind = kind };
            this.viewModel.SetupGet(model => model.Presentation).Returns(presentation);

            var component = this.context.Render<OrganizationHeader>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("a[href='/organizations/orbital-systems']"), Has.Count.EqualTo(hasOrganizationLink ? 1 : 0));
                Assert.That(component.FindAll("button[aria-label='Organization section']"), Has.Count.EqualTo(hasSectionMenu ? 1 : 0));
                Assert.That(component.FindAll("button[aria-label='Account menu']"), Has.Count.EqualTo(hasAccountMenu ? 1 : 0));
                Assert.That(component.FindAll("a[href='/account']"), Has.Count.EqualTo(1));
            }
        }

        [TestCase("Account menu", "/account", "sample@example.org")]
        [TestCase("Organization section", "/organizations/orbital-systems/projects", "Current page")]
        public async Task VerifyOpeningAMenuListsItsEntries(string trigger, string href, string text)
        {
            this.viewModel.SetupGet(model => model.Presentation).Returns(this.organizationPresentation);
            var component = this.context.Render<OrganizationHeader>();

            await component.Find($"button[aria-label='{trigger}']").ClickAsync();
            var entries = await this.portalHost.WaitForElementsAsync("[role='menuitem']", 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries.Single().GetAttribute("href"), Is.EqualTo(href));
                Assert.That(this.portalHost.Markup, Does.Contain(text));
            }
        }
    }
}
