// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationViewModelPageBaseTestFixture.cs" company="Starion Group S.A.">
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

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages.Organization;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    [TestFixture]
    public sealed class OrganizationViewModelPageBaseTestFixture
    {
        private BunitContext context;

        private Mock<IProjectsViewModel> projectsViewModel;

        private Mock<IMembersViewModel> membersViewModel;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            this.projectsViewModel = new Mock<IProjectsViewModel>();
            this.projectsViewModel.SetupGet(model => model.IsBusy).Returns(true);
            this.projectsViewModel.SetupGet(model => model.Errors).Returns([]);
            this.projectsViewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<ProjectRowViewModel>([]));
            this.membersViewModel = new Mock<IMembersViewModel>();
            this.membersViewModel.SetupGet(model => model.IsBusy).Returns(true);
            this.membersViewModel.SetupGet(model => model.Errors).Returns([]);
            this.membersViewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<MemberRowViewModel>([]));
            this.context.Services.AddSingleton<Func<IProjectsViewModel>>(() => this.projectsViewModel.Object);
            this.context.Services.AddSingleton<Func<IMembersViewModel>>(() => this.membersViewModel.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyThePageReleasesItsViewModelWhenItIsRemoved(bool isProjectsPage)
        {
            var disposals = 0;
            this.projectsViewModel.Setup(model => model.Dispose()).Callback(() => disposals++);
            this.membersViewModel.Setup(model => model.Dispose()).Callback(() => disposals++);

            if (isProjectsPage)
            {
                this.context.Render<OrganizationProjects>();
            }
            else
            {
                this.context.Render<OrganizationMembers>();
            }

            var disposalsBeforeRemoval = disposals;
            await this.context.DisposeComponentsAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposalsBeforeRemoval, Is.Zero);
                Assert.That(disposals, Is.EqualTo(1));
            }
        }
    }
}
