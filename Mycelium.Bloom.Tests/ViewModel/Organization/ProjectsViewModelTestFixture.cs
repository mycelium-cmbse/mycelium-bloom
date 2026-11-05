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
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.DTO;

    using Organization = Mycelium.SDK.DTO.Organization;

    [TestFixture]
    public sealed class ProjectsViewModelTestFixture
    {
        private DateTimeOffset now;

        private Organization organization;

        private FunctionalProject project;

        private Mock<IOrganizationContextViewModel> context;

        private Mock<IOrganizationProjectService> projectService;

        private Mock<TimeProvider> timeProvider;

        private Mock<ILogger<ProjectsViewModel>> logger;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            this.organization = new Organization { Id = Guid.NewGuid(), Name = "Orbital Systems" };
            this.project = new FunctionalProject
            {
                Id = Guid.NewGuid(),
                Name = "Aurora Orbiter",
                Description = "Description",
                Visibility = ProjectVisibility.Private,
                CurrentMode = ProjectMode.Regular,
                Lifecycle = ProjectLifecycleKind.Open,
                Involves = [.. Enumerable.Range(0, 4).Select(_ => Guid.NewGuid())],
                UpdatedOn = this.now.UtcDateTime.AddHours(-2)
            };
            this.context = new Mock<IOrganizationContextViewModel>();
            this.context.SetupGet(model => model.Organization).Returns(this.organization);
            this.context.SetupGet(model => model.Capabilities).Returns(new OrganizationCapabilities(true, true));
            this.projectService = new Mock<IOrganizationProjectService>();
            this.timeProvider = new Mock<TimeProvider>();
            this.timeProvider.Setup(provider => provider.GetUtcNow()).Returns(this.now);
            this.logger = new Mock<ILogger<ProjectsViewModel>>();
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [Test]
        public void VerifyProjectsAreLoadedWithTheirCounts()
        {
            var counts = new ProjectCounts(3, 1, 1, 1, 0);
            this.SetUpProjects([this.project, this.project], counts);

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.CanCreateProject, Is.True);
                Assert.That(viewModel.Counts, Is.EqualTo(counts));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
                Assert.That(viewModel.Rows[0].Name, Is.EqualTo("Aurora Orbiter"));
                Assert.That(viewModel.Rows[0].Visibility, Is.EqualTo(ProjectVisibility.Private));
                Assert.That(viewModel.Rows[0].Mode, Is.EqualTo(ProjectMode.Regular));
                Assert.That(viewModel.Rows[0].MemberCount, Is.EqualTo(4));
                Assert.That(viewModel.Rows[0].Lifecycle, Is.EqualTo(ProjectLifecycleKind.Open));
                Assert.That(viewModel.Rows[0].UpdatedText, Is.EqualTo("2 hours ago"));
            }
        }

        [Test]
        public void VerifyPickingAStatusReloadsTheProjectsAndKeepsTheCounts()
        {
            this.SetUpProjects([this.project, this.project, this.project], new ProjectCounts(3, 0, 3, 0, 0));
            this.SetUpMatches(new ProjectFilter(string.Empty, ProjectLifecycleKind.Open, null), [this.project]);

            using var viewModel = this.CreateViewModel();
            viewModel.StatusFilter = ProjectLifecycleKind.Open;
            var rowsWithTheFilter = viewModel.Rows.Count;
            viewModel.StatusFilter = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rowsWithTheFilter, Is.EqualTo(1));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(3));
                Assert.That(viewModel.Counts.Total, Is.EqualTo(3));
                this.projectService.Verify(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task VerifyNoMatchLeavesNoRowsAndClearingTheFiltersShowsThemAgain()
        {
            this.SetUpProjects([this.project, this.project], new ProjectCounts(2, 0, 2, 0, 0));
            this.SetUpMatches(new ProjectFilter(string.Empty, null, ProjectMode.Concurrent), []);

            using var viewModel = this.CreateViewModel();
            viewModel.ModeFilter = ProjectMode.Concurrent;
            var rowsWithoutMatch = viewModel.Rows.Count;
            viewModel.StatusFilter = ProjectLifecycleKind.Open;
            viewModel.SearchText = "x";
            await viewModel.ClearFiltersCommand.Execute().ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rowsWithoutMatch, Is.Zero);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.SearchText, Is.Empty);
                Assert.That(viewModel.StatusFilter, Is.Null);
                Assert.That(viewModel.ModeFilter, Is.Null);
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
                this.VerifyLoaded(new ProjectFilter("x", null, null), Times.Never());
            }
        }

        [Test]
        public void VerifyTypingReloadsOncePerPause()
        {
            this.SetUpProjects([this.project], new ProjectCounts(1, 0, 1, 0, 0));
            this.SetUpMatches(new ProjectFilter("aurora", null, null), []);

            using var viewModel = this.CreateViewModel();
            viewModel.SearchText = "a";
            viewModel.SearchText = "aurora ";

            Assert.That(() => viewModel.Rows.Count, Is.Zero.After(5000, 20));
            this.VerifyLoaded(new ProjectFilter(string.Empty, null, null), Times.Once());
            this.VerifyLoaded(new ProjectFilter("aurora", null, null), Times.Once());
            this.VerifyLoaded(new ProjectFilter("a", null, null), Times.Never());
        }

        [Test]
        public void VerifyOrganizationWithoutProjectHasNoRows()
        {
            this.context.SetupGet(model => model.Capabilities).Returns(new OrganizationCapabilities(false, false));
            this.SetUpProjects([], new ProjectCounts(0, 0, 0, 0, 0));

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.Counts.Total, Is.Zero);
                Assert.That(viewModel.CanCreateProject, Is.False);
            }
        }

        [TestCase(true, false, false, OrganizationErrorCodes.Unavailable)]
        [TestCase(false, true, false, OrganizationErrorCodes.Unavailable)]
        [TestCase(false, false, true, "Projects.LoadFailed")]
        public void VerifyErrorOrFailureIsKeptAsAnErrorAndLeavesNoRow(bool projectsError, bool countsError, bool throws, string expectedCode)
        {
            var error = Error.Unexpected(OrganizationErrorCodes.Unavailable, "Unavailable.");
            var projectsSetup = this.projectService.Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<ProjectFilter>(), It.IsAny<CancellationToken>()));

            if (throws)
            {
                projectsSetup.ThrowsAsync(new InvalidOperationException());
            }
            else
            {
                projectsSetup.ReturnsAsync(projectsError ? error : ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>(Array.Empty<FunctionalProject>()));
            }

            this.projectService.Setup(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(countsError ? error : new ProjectCounts(0, 0, 0, 0, 0));

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors.Single().Code, Is.EqualTo(expectedCode));
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.Counts, Is.Null);
            }
        }

        [Test]
        public void VerifyNextFilterAfterAnErrorReloadsTheCountsAndShowsTheProjects()
        {
            this.SetUpProjects([this.project], new ProjectCounts(1, 0, 1, 0, 0));
            this.projectService.SetupSequence(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Error.Unexpected(OrganizationErrorCodes.Unavailable, "Unavailable."))
                .ReturnsAsync(new ProjectCounts(1, 0, 1, 0, 0));

            using var viewModel = this.CreateViewModel();
            var errorsAfterTheFailure = viewModel.Errors.Count;
            viewModel.ModeFilter = ProjectMode.Regular;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(errorsAfterTheFailure, Is.EqualTo(1));
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Has.Count.EqualTo(1));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyDisposingCancelsTheLoadWhichShowsNothing(bool endsCancelled)
        {
            var cancellation = CancellationToken.None;
            var pending = new TaskCompletionSource<ErrorOr<IReadOnlyList<FunctionalProject>>>();
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<ProjectFilter>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, ProjectFilter, CancellationToken>((_, _, token) => cancellation = token)
                .Returns(pending.Task);
            this.projectService.Setup(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProjectCounts(1, 0, 1, 0, 0));

            var viewModel = this.CreateViewModel();
            var busyWhileLoading = viewModel.IsBusy;
            viewModel.Dispose();

            if (endsCancelled)
            {
                pending.SetCanceled(cancellation);
            }
            else
            {
                pending.SetResult(ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>([this.project]));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(busyWhileLoading, Is.True);
                Assert.That(cancellation.IsCancellationRequested, Is.True);
                Assert.That(viewModel.IsBusy, Is.True);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.Counts, Is.Null);
            }
        }

        [Test]
        public void VerifyTheLoadsRunOnTheContextTheViewModelWasCreatedOn()
        {
            var renderContext = new CountingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(renderContext);
            this.SetUpProjects([this.project], new ProjectCounts(1, 0, 1, 0, 0));
            this.SetUpMatches(new ProjectFilter("aurora", null, null), []);

            using var viewModel = this.CreateViewModel();
            var postsAfterTheFirstLoad = renderContext.Posts;
            viewModel.SearchText = "aurora";

            Assert.That(() => viewModel.Rows.Count, Is.Zero.After(5000, 20));
            Assert.That(renderContext.Posts, Is.GreaterThan(postsAfterTheFirstLoad));
        }

        [Test]
        public void VerifyANewerFilterLeavesTheViewModelBusyUntilItsOwnLoadEnds()
        {
            var older = new TaskCompletionSource<ErrorOr<IReadOnlyList<FunctionalProject>>>();
            var newer = new TaskCompletionSource<ErrorOr<IReadOnlyList<FunctionalProject>>>();
            this.projectService.Setup(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProjectCounts(1, 0, 1, 0, 0));
            this.projectService.Setup(service => service.GetProjectsAsync(this.organization.Id, new ProjectFilter(string.Empty, null, null), It.IsAny<CancellationToken>()))
                .Returns(older.Task);
            this.projectService.Setup(service => service.GetProjectsAsync(this.organization.Id, new ProjectFilter(string.Empty, ProjectLifecycleKind.Open, null), It.IsAny<CancellationToken>()))
                .Returns(newer.Task);

            using var viewModel = this.CreateViewModel();
            viewModel.StatusFilter = ProjectLifecycleKind.Open;
            older.SetResult(ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>([this.project, this.project]));
            var busyAfterTheOlderEnded = viewModel.IsBusy;
            var rowsAfterTheOlderEnded = viewModel.Rows.Count;
            newer.SetResult(ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>([this.project]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(busyAfterTheOlderEnded, Is.True);
                Assert.That(rowsAfterTheOlderEnded, Is.Zero);
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Rows, Has.Count.EqualTo(1));
            }
        }

        private void SetUpProjects(FunctionalProject[] projects, ProjectCounts counts)
        {
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<ProjectFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>(projects));
            this.projectService.Setup(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(counts);
        }

        private void SetUpMatches(ProjectFilter filter, FunctionalProject[] matches)
        {
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>(matches));
        }

        private void VerifyLoaded(ProjectFilter filter, Times times)
        {
            this.projectService.Verify(service => service.GetProjectsAsync(this.organization.Id, filter, It.IsAny<CancellationToken>()), times);
        }

        private ProjectsViewModel CreateViewModel()
        {
            return new ProjectsViewModel(this.context.Object, this.projectService.Object, this.timeProvider.Object, this.logger.Object);
        }
    }
}
