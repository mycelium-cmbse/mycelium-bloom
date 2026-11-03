// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationLayoutTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Layout
{
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Components.UI.Organisms.OrganizationHeader;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class OrganizationLayoutTestFixture : BunitContext
    {
        private Mock<IOrganizationContextViewModel> viewModel;

        private Mock<IOrganizationHeaderViewModel> headerViewModel;

        [SetUp]
        public void SetUp()
        {
            BlueprintTestSetup.Configure(this);
            this.viewModel = new Mock<IOrganizationContextViewModel>();
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.headerViewModel = new Mock<IOrganizationHeaderViewModel>();
            this.headerViewModel.SetupGet(model => model.Presentation).Returns(OrganizationHeaderPresentation.Empty);
            this.Services.AddSingleton(this.viewModel.Object);
            this.Services.AddSingleton(this.headerViewModel.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.DisposeAsync();
        }

        [Test]
        public void VerifyLayoutHandsEveryLocationToTheContextAndResetsItWhenDisposed()
        {
            RenderFragment body = builder => builder.AddContent(0, "Page content");
            var membersPage = OrganizationLocation.ForOrganization("orbital-systems", OrganizationSection.Members);
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/account");

            var component = this.Render<OrganizationLayout>(parameters => parameters.Add(layout => layout.Body, body));
            navigation.NavigateTo("/organizations/orbital-systems/members");
            component.Render(parameters => parameters.Add(layout => layout.Body, body));
            component.Instance.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponents<OrganizationHeader>(), Has.Count.EqualTo(1));
                Assert.That(component.Find("main").TextContent, Does.Contain("Page content"));
                this.viewModel.VerifySet(model => model.Location = OrganizationLocation.Account, Times.Once);
                this.viewModel.VerifySet(model => model.Location = membersPage, Times.Once);
                this.viewModel.VerifySet(model => model.Location = null, Times.Once);
            }
        }
    }
}
