// ------------------------------------------------------------------------------------------------
// <copyright file="AccountDashboardTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages.Organization
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Time.Testing;

    using Moq;

    using Mycelium.Bloom.Components.Pages.Organization;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Home;

    [TestFixture]
    public sealed class AccountDashboardTestFixture
    {
        private BunitContext context;

        private Mock<IHomeViewModel> viewModel;

        private DateTime now;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            this.now = new DateTime(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc);
            this.viewModel = new Mock<IHomeViewModel>();
            this.viewModel.SetupGet(model => model.IsConnected).Returns(true);
            this.viewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<Project>([]));
            this.context.Services.AddSingleton<Func<IHomeViewModel>>(() => this.viewModel.Object);
            this.context.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(this.now)));
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [TestCase(0)]
        [TestCase(2)]
        public void VerifyThePageShowsTheProjectsOrAnEmptyState(int projectCount)
        {
            var rows = new ObservableCollection<Project>();

            for (var index = 0; index < projectCount; index++)
            {
                rows.Add(new Project { Id = Guid.NewGuid(), Name = $"Project {index}", Description = "A model", Created = this.now.AddDays(-2) });
            }

            this.viewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<Project>(rows));

            var page = this.context.Render<AccountDashboard>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(page.FindComponents<BbEmpty>(), Has.Count.EqualTo(projectCount == 0 ? 1 : 0));
                Assert.That(page.FindAll("tbody tr"), Has.Count.EqualTo(projectCount));
                Assert.That(page.FindAll("tbody tr"), Has.All.Property("TextContent").Contains("2 days ago"));
            }
        }

        [TestCase(true, "http://localhost/account")]
        [TestCase(false, "http://localhost/")]
        public async Task VerifyThePageGoesBackToTheStartPageWithoutAConnectionAndReleasesItsViewModel(bool isConnected, string expectedUri)
        {
            var navigation = this.context.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/account");
            this.viewModel.SetupGet(model => model.IsConnected).Returns(isConnected);

            this.context.Render<AccountDashboard>();
            var uri = navigation.Uri;
            await this.context.DisposeComponentsAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(uri, Is.EqualTo(expectedUri));
                this.viewModel.Verify(model => model.Dispose(), Times.Once);
            }
        }
    }
}
