// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectsViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Organization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;


    using ErrorOr;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Time.Testing;

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class ProjectsViewModelTestFixture
    {
        private static readonly string[] ExpectedNames = ["Newer", "Older"];

        private DateTimeOffset now;

        private Organization organization;

        private Mock<IOrganizationContextViewModel> context;

        private Mock<IOrganizationProjectService> projectService;

        private FakeTimeProvider timeProvider;

        private Mock<ILogger<ProjectsViewModel>> logger;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            this.organization = new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" };
            this.context = new Mock<IOrganizationContextViewModel>();
            this.context.SetupGet(model => model.Organization).Returns(this.organization);
            this.context.SetupGet(model => model.Capabilities).Returns(new OrganizationCapabilities(true, true));
            this.projectService = new Mock<IOrganizationProjectService>();
            this.timeProvider = new FakeTimeProvider(this.now);
            this.logger = new Mock<ILogger<ProjectsViewModel>>();
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [Test]
        public void VerifyProjectsAreLoadedNewestFirst()
        {
            this.SetUpProjects([this.CreateProject("Older", hoursAgo: 5), this.CreateProject("Newer", hoursAgo: 2)]);

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.CanCreateProject, Is.True);
                Assert.That(viewModel.Projects.Count, Is.EqualTo(2));
                Assert.That(viewModel.Rows.Select(row => row.Name), Is.EqualTo(ExpectedNames));
                Assert.That(viewModel.Rows[0].Visibility, Is.EqualTo(ProjectVisibility.Private));
                Assert.That(viewModel.Rows[0].Mode, Is.EqualTo(ProjectMode.Regular));
                Assert.That(viewModel.Rows[0].MemberCount, Is.EqualTo(4));
                Assert.That(viewModel.Rows[0].Lifecycle, Is.EqualTo(ProjectLifecycleKind.Open));
                Assert.That(viewModel.Rows[0].UpdatedText, Is.EqualTo("2 hours ago"));
            }
        }

        [Test]
        public void VerifyOrganizationWithoutProjectHasNoRows()
        {
            this.context.SetupGet(model => model.Capabilities).Returns(new OrganizationCapabilities(false, false));
            this.SetUpProjects([]);

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.CanCreateProject, Is.False);
            }
        }

        private FunctionalProject CreateProject(string name, int hoursAgo, string description = "Description", ProjectLifecycleKind lifecycle = ProjectLifecycleKind.Open,
            ProjectMode projectMode = ProjectMode.Regular)
        {
            return new FunctionalProject
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Visibility = ProjectVisibility.Private,
                CurrentMode = projectMode,
                Lifecycle = lifecycle,
                Involves = [.. Enumerable.Range(0, 4).Select(_ => new ProjectMember { Id = Guid.NewGuid() })],
                UpdatedOn = this.now.UtcDateTime.AddHours(-hoursAgo)
            };
        }

        private void SetUpProjects(FunctionalProject[] projects)
        {
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>(projects));
        }

        private ProjectsViewModel CreateViewModel()
        {
            return new ProjectsViewModel(this.context.Object, this.projectService.Object, this.timeProvider, this.logger.Object);
        }
    }
}
