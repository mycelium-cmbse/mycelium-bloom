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
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.UI.Molecules.ActionMenu;
    using Mycelium.Bloom.Components.UI.Organisms.OrganizationHeader;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    using ReactiveUI;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class OrganizationHeaderTestFixture : BunitContext
    {
        private Mock<IOrganizationHeaderViewModel> viewModel;

        private ActionMenuItem projectsItem;

        private OrganizationHeaderPresentation organizationPresentation;

        [SetUp]
        public void SetUp()
        {
            BlueprintTestSetup.Configure(this);
            this.projectsItem = new ActionMenuItem { Id = "/organizations/orbital-systems/projects", Label = "Projects" };

            this.organizationPresentation = new OrganizationHeaderPresentation
            {
                Kind = OrganizationLocationKind.Organization,
                OrganizationName = "Orbital Systems",
                OrganizationPath = "/organizations/orbital-systems",
                SectionLabel = "Members",
                SectionIcon = "users",
                SectionItems = [this.projectsItem],
                AccountName = "Sample Account",
                AccountEmail = "sample@example.org",
                AccountItems = [new ActionMenuItem { Id = "/account", Label = "Your profile" }]
            };

            this.viewModel = new Mock<IOrganizationHeaderViewModel>();
            this.viewModel.SetupGet(model => model.SelectItemCommand).Returns(ReactiveCommand.Create<ActionMenuItem, string>(item => item.Id));
            this.Services.AddSingleton(this.viewModel.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.DisposeAsync();
        }

        [TestCase(OrganizationLocationKind.Organization, 2, true, true)]
        [TestCase(OrganizationLocationKind.Account, 1, false, true)]
        [TestCase(OrganizationLocationKind.None, 0, false, false)]
        public void VerifyPresentationSelectsTheSegmentsAndTheMenus(OrganizationLocationKind kind, int segments, bool hasSectionMenu, bool hasAccountMenu)
        {
            var presentation = kind == OrganizationLocationKind.None
                ? OrganizationHeaderPresentation.Empty
                : this.organizationPresentation with { Kind = kind };
            this.viewModel.SetupGet(model => model.Presentation).Returns(presentation);

            var component = this.Render<OrganizationHeader>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-organization-header__separator"), Has.Count.EqualTo(segments));
                Assert.That(component.FindAll("nav button"), Has.Count.EqualTo(hasSectionMenu ? 1 : 0));
                Assert.That(component.FindAll(".mb-user-menu"), Has.Count.EqualTo(hasAccountMenu ? 1 : 0));
                Assert.That(component.Find(".mb-organization-header__brand").GetAttribute("href"), Is.EqualTo("/account"));
            }
        }

        [Test]
        public async Task VerifySelectingAnEntryNavigatesToItsRoute()
        {
            this.viewModel.SetupGet(model => model.Presentation).Returns(this.organizationPresentation);
            var component = this.Render<OrganizationHeader>();

            await component.InvokeAsync(() => component.FindComponent<ActionMenu>().Instance.ItemSelected.InvokeAsync(this.projectsItem));

            Assert.That(this.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/organizations/orbital-systems/projects"));
        }
    }
}
