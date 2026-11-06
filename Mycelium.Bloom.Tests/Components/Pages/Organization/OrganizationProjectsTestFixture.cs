// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationProjectsTestFixture.cs" company="Starion Group S.A.">
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
    public sealed class OrganizationProjectsTestFixture
    {
        private BunitContext context;

        private Mock<IProjectsViewModel> viewModel;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            this.viewModel = new Mock<IProjectsViewModel>();
            this.viewModel.SetupGet(model => model.IsBusy).Returns(true);
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.viewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<ProjectRowViewModel>([]));
            this.context.Services.AddSingleton<Func<IProjectsViewModel>>(() => this.viewModel.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [Test]
        public async Task VerifyThePageReleasesItsViewModelWhenItIsRemoved()
        {
            this.context.Render<OrganizationProjects>();
            this.viewModel.Verify(model => model.Dispose(), Times.Never);

            await this.context.DisposeComponentsAsync();

            this.viewModel.Verify(model => model.Dispose(), Times.Once);
        }
    }
}
