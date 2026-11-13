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

    using Mycelium.Bloom.Components.UI.Molecules.UserMenu;
    using Mycelium.Bloom.Components.UI.Organisms.OrganizationHeader;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    using ReactiveUI;

    [TestFixture]
    public sealed class OrganizationHeaderTestFixture
    {
        private BunitContext context;

        private Mock<IOrganizationHeaderViewModel> viewModel;

        private OrganizationHeaderPresentation organizationPresentation;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);

            this.organizationPresentation = new OrganizationHeaderPresentation
            {
                Kind = OrganizationLocationKind.Organization,
                OrganizationName = "Orbital Systems",
                OrganizationPath = "/organizations/orbital-systems",
                SectionLabel = "Members",
                SectionIcon = "users",
                SectionItems = [new ActionMenuItem { Id = "/organizations/orbital-systems/projects", Label = "Projects" }],
                AccountName = "Sample Account",
                AccountEmail = "sample@example.org",
                AccountItems = [new ActionMenuItem { Id = "/account", Label = "Your profile" }]
            };

            this.viewModel = new Mock<IOrganizationHeaderViewModel>();
            this.viewModel.SetupGet(model => model.SelectItemCommand).Returns(ReactiveCommand.Create<ActionMenuItem, string>(item => item.Id));
            this.context.Services.AddSingleton(this.viewModel.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
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
                Assert.That(component.FindComponents<UserMenu>(), Has.Count.EqualTo(hasAccountMenu ? 1 : 0));
                Assert.That(component.FindAll("a[href='/account']"), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifySelectingAnAccountEntryNavigatesToItsRoute()
        {
            this.viewModel.SetupGet(model => model.Presentation).Returns(this.organizationPresentation);
            var component = this.context.Render<OrganizationHeader>();

            await component.InvokeAsync(() => component.FindComponent<UserMenu>().Instance.ItemSelected.InvokeAsync(this.organizationPresentation.AccountItems[0]));

            Assert.That(this.context.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/account"));
        }
    }
}
