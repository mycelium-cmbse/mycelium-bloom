// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceBreadcrumbTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.WorkspaceBreadcrumb
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Bunit;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.UI.Molecules.WorkspaceBreadcrumb;
    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Workspace;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class WorkspaceBreadcrumbTestFixture
    {
        private BunitContext context;

        private Mock<IWorkspaceBreadcrumbViewModel> viewModel;

        private Guid projectId;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            this.projectId = Guid.NewGuid();
            this.viewModel = new Mock<IWorkspaceBreadcrumbViewModel>();
            this.viewModel.SetupGet(model => model.IsConnected).Returns(true);
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.context.Services.AddSingleton<Func<IWorkspaceBreadcrumbViewModel>>(() => this.viewModel.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [Test]
        public async Task VerifyTheOpenedProjectAndItsOrganizationAreShown()
        {
            this.viewModel.SetupGet(model => model.Project).Returns(new Project { Id = this.projectId, Name = "Spacecraft Mission" });
            this.viewModel.SetupGet(model => model.Organization).Returns(new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" });
            this.context.Services.GetRequiredService<NavigationManager>().NavigateTo($"/workspace/modeling?project={this.projectId}&organization=orbital-systems");

            var breadcrumb = this.context.Render<WorkspaceBreadcrumb>();
            var organizationLinks = breadcrumb.FindAll("a[href='/organizations/orbital-systems']").Count;
            var current = breadcrumb.Find("[aria-current='page']").TextContent;
            await this.context.DisposeComponentsAsync();

            using (Assert.EnterMultipleScope())
            {
                this.viewModel.Verify(model => model.OpenAsync(this.projectId, "orbital-systems", It.IsAny<CancellationToken>()), Times.Once);
                Assert.That(organizationLinks, Is.EqualTo(1));
                Assert.That(current, Does.Contain("Spacecraft Mission"));
                this.viewModel.Verify(model => model.Dispose(), Times.Once);
            }
        }

        [Test]
        public void VerifyAWorkspaceWithoutAProjectOpensNothing()
        {
            var navigation = this.context.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling");

            var breadcrumb = this.context.Render<WorkspaceBreadcrumb>();

            using (Assert.EnterMultipleScope())
            {
                this.viewModel.Verify(model => model.OpenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
                Assert.That(navigation.Uri, Is.EqualTo("http://localhost/workspace/modeling"));
                Assert.That(breadcrumb.Markup.Trim(), Is.Empty);
            }
        }

        [TestCase(false, false, "http://localhost/?returnProject=6f1d2c3a-0b4e-4c1a-9a51-6d2e8f0a1c01")]
        [TestCase(true, true, "http://localhost/Error")]
        public void VerifyTheBreadcrumbLeavesWhenTheProjectCannotBeOpened(bool isConnected, bool hasFailed, string expectedUri)
        {
            var navigation = this.context.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?project=6f1d2c3a-0b4e-4c1a-9a51-6d2e8f0a1c01");
            this.viewModel.SetupGet(model => model.IsConnected).Returns(isConnected);
            this.viewModel.SetupGet(model => model.Errors).Returns(hasFailed ? [Error.Failure(ErrorCodes.Unavailable, "Cannot connect to this server")] : []);

            this.context.Render<WorkspaceBreadcrumb>();

            Assert.That(navigation.Uri, Is.EqualTo(expectedUri));
        }

        [TestCase("6f1d2c3a-0b4e-4c1a-9a51-6d2e8f0a1c01")]
        [TestCase("not-a-project")]
        public void VerifyAMalformedOrUnknownProjectShowsTheNotFoundPage(string project)
        {
            var navigation = this.context.Services.GetRequiredService<NavigationManager>();
            var notFoundShown = false;
            navigation.OnNotFound += (_, _) => notFoundShown = true;
            navigation.NavigateTo($"/workspace/modeling?project={project}");
            this.viewModel.SetupGet(model => model.Errors).Returns([Error.NotFound(ErrorCodes.NotFound, "The project does not exist on this server.")]);

            this.context.Render<WorkspaceBreadcrumb>();

            Assert.That(notFoundShown, Is.True);
        }
    }
}
