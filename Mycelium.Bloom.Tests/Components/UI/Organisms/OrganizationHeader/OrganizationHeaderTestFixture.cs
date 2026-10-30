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

    /// <summary>
    /// Tests the <see cref="OrganizationHeader" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class OrganizationHeaderTestFixture : BunitContext
    {
        /// <summary>
        /// The entry of the account menu.
        /// </summary>
        private static readonly ActionMenuItem[] AccountItems = [new() { Id = "/account", Label = "Your profile" }];

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationHeaderTestFixture" /> class.
        /// </summary>
        public OrganizationHeaderTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        /// <returns>A task that completes when the context is disposed.</returns>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies which segments and menus the header displays for each kind of presentation: the organization and
        /// its pages, the account page, or the logo only.
        /// </summary>
        /// <param name="kind">What the header displays.</param>
        /// <param name="segments">The number of page segments: one for the account, two for an organization.</param>
        /// <param name="hasSectionMenu">A value indicating whether the section menu is displayed.</param>
        /// <param name="hasAccountMenu">A value indicating whether the account menu is displayed.</param>
        [TestCase(OrganizationLocationKind.Organization, 2, true, true)]
        [TestCase(OrganizationLocationKind.Account, 1, false, true)]
        [TestCase(OrganizationLocationKind.None, 0, false, false)]
        public void VerifyPresentationSelectsTheSegmentsAndTheMenus(
            OrganizationLocationKind kind,
            int segments,
            bool hasSectionMenu,
            bool hasAccountMenu)
        {
            var presentation = kind == OrganizationLocationKind.None
                ? OrganizationHeaderPresentation.Empty
                : new OrganizationHeaderPresentation
                {
                    Kind = kind,
                    OrganizationName = kind == OrganizationLocationKind.Organization ? "Orbital Systems" : string.Empty,
                    OrganizationPath = kind == OrganizationLocationKind.Organization ? "/organizations/orbital-systems" : string.Empty,
                    SectionLabel = "Members",
                    SectionIcon = "users",
                    SectionItems = [new ActionMenuItem { Id = "/organizations/orbital-systems/members", Label = "Members", IsSelected = true }],
                    AccountName = "Sample Account",
                    AccountEmail = "sample@example.org",
                    AccountItems = AccountItems
                };

            var component = this.RenderFor(presentation);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-organization-header__separator"), Has.Count.EqualTo(segments));
                Assert.That(component.FindAll("nav button"), Has.Count.EqualTo(hasSectionMenu ? 1 : 0));
                Assert.That(component.FindAll(".mb-user-menu"), Has.Count.EqualTo(hasAccountMenu ? 1 : 0));
                Assert.That(component.Find(".mb-organization-header__brand").GetAttribute("href"), Is.EqualTo("/account"));
            }
        }

        /// <summary>
        /// Verifies that selecting a menu entry navigates to the route that the ViewModel yields for it.
        /// </summary>
        /// <returns>A task that completes when the verification has run.</returns>
        [Test]
        public async Task VerifySelectingAnEntryNavigatesToItsRoute()
        {
            var item = new ActionMenuItem { Id = "/organizations/orbital-systems/projects", Label = "Projects" };
            var presentation = new OrganizationHeaderPresentation
            {
                Kind = OrganizationLocationKind.Organization,
                OrganizationName = "Orbital Systems",
                OrganizationPath = "/organizations/orbital-systems",
                SectionLabel = "Members",
                SectionIcon = "users",
                SectionItems = [item],
                AccountName = "Sample Account",
                AccountItems = AccountItems
            };
            var component = this.RenderFor(presentation);

            await component.InvokeAsync(() => component.FindComponent<ActionMenu>().Instance.ItemSelected.InvokeAsync(item));

            Assert.That(this.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/organizations/orbital-systems/projects"));
        }

        /// <summary>
        /// Renders the component over a mocked ViewModel that displays the given presentation.
        /// </summary>
        /// <param name="presentation">What the header displays.</param>
        /// <returns>The rendered component.</returns>
        private IRenderedComponent<OrganizationHeader> RenderFor(OrganizationHeaderPresentation presentation)
        {
            var viewModel = new Mock<IOrganizationHeaderViewModel>();
            viewModel.SetupGet(model => model.Presentation).Returns(presentation);
            viewModel.SetupGet(model => model.SelectItemCommand)
                .Returns(ReactiveCommand.Create<ActionMenuItem, string>(menuItem => menuItem.Id));

            return this.Render<OrganizationHeader>(parameters => parameters
                .Add(header => header.ViewModel, viewModel.Object));
        }
    }
}
