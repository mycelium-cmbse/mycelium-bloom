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
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Organization;

    using ReactiveUI;

    [TestFixture]
    public sealed class ProjectsViewModelTestFixture
    {
        private DateTimeOffset now;

        private Organization organization;

        private ProjectSummary project;

        private Mock<IOrganizationContextViewModel> context;

        private Mock<IOrganizationProjectService> projectService;

        private Mock<TimeProvider> timeProvider;

        private Mock<ILogger<ProjectsViewModel>> logger;

        [SetUp]
        public void SetUp()
        {
            this.now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            this.organization = new Organization(Guid.NewGuid(), "orbital-systems", "Orbital Systems");
            this.project = new ProjectSummary("Aurora Orbiter", "Description", ProjectVisibility.Private, ProjectMode.Regular, 4, ProjectLifecycleState.Open,
                this.now.AddHours(-2), true);
            this.context = new Mock<IOrganizationContextViewModel>();
            this.context.SetupGet(model => model.Organization).Returns(this.organization);
            this.context.SetupGet(model => model.Capabilities).Returns(new OrganizationCapabilities(true, true));
            this.projectService = new Mock<IOrganizationProjectService>();
            this.timeProvider = new Mock<TimeProvider>();
            this.timeProvider.Setup(provider => provider.GetUtcNow()).Returns(this.now);
            this.logger = new Mock<ILogger<ProjectsViewModel>>();
        }

        [TestCase(1, 1, 0, "1 project · 1 open · 0 in preparation · 0 in review · 0 archived", "Showing 1 of 1 project")]
        [TestCase(2, 3, 1, "3 projects · 1 open · 1 in preparation · 1 in review · 0 archived", "Showing 3 of 3 projects")]
        public void VerifyProjectsAreShownWithTheirCounts(int shown, int total, int preparation, string expectedSubtitle, string expectedShowing)
        {
            this.SetUpProjects([.. Enumerable.Repeat(this.project, shown)], total, new ProjectCounts(total, preparation, 1, preparation, 0));

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(ProjectsListState.Ready));
                Assert.That(viewModel.CanCreateProject, Is.True);
                Assert.That(viewModel.Subtitle, Is.EqualTo(expectedSubtitle));
                Assert.That(viewModel.ShowingText, Is.EqualTo(expectedShowing));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(shown));
                Assert.That(viewModel.GetTablePageSize(), Is.EqualTo(shown));
                Assert.That(viewModel.Rows[0].Name, Is.EqualTo("Aurora Orbiter"));
                Assert.That(viewModel.Rows[0].VisibilityLabel, Is.EqualTo("Private"));
                Assert.That(viewModel.Rows[0].VisibilityIcon, Is.EqualTo("lock"));
                Assert.That(viewModel.Rows[0].ModeLabel, Is.EqualTo("Regular"));
                Assert.That(viewModel.Rows[0].IsConcurrentDesign, Is.False);
                Assert.That(viewModel.Rows[0].MemberCount, Is.EqualTo(4));
                Assert.That(viewModel.Rows[0].StatusLabel, Is.EqualTo("Open"));
                Assert.That(viewModel.Rows[0].UpdatedText, Is.EqualTo("2 hours ago"));
            }
        }

        [Test]
        public async Task VerifyPickingAStatusReloadsTheProjectsAndKeepsTheCounts()
        {
            this.SetUpProjects([this.project, this.project, this.project], 3, new ProjectCounts(3, 0, 3, 0, 0));
            this.SetUpMatches(new ProjectFilter(string.Empty, ProjectLifecycleState.Open, null), [this.project]);

            using var viewModel = this.CreateViewModel();
            await viewModel.SelectStatusCommand.Execute(new ActionMenuItem { Id = nameof(ProjectLifecycleState.Open) }).ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Rows, Has.Count.EqualTo(1));
                Assert.That(viewModel.ShowingText, Is.EqualTo("Showing 1 of 3 projects"));
                Assert.That(viewModel.StatusFilterLabel, Is.EqualTo("Status: Open"));
                Assert.That(viewModel.StatusFilterItems.Single(item => item.IsSelected).Label, Is.EqualTo("Open"));
                Assert.That(viewModel.ModeFilterLabel, Is.EqualTo("Mode: All"));
            }

            await viewModel.SelectStatusCommand.Execute(new ActionMenuItem { Id = "all" }).ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.StatusFilterLabel, Is.EqualTo("Status: All"));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(3));
                this.projectService.Verify(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task VerifyNoResultsStateAndClearingTheFilters()
        {
            this.SetUpProjects([this.project, this.project], 2, new ProjectCounts(2, 0, 2, 0, 0));
            this.SetUpMatches(new ProjectFilter(string.Empty, null, ProjectMode.ConcurrentDesign), []);

            using var viewModel = this.CreateViewModel();
            await viewModel.SelectModeCommand.Execute(new ActionMenuItem { Id = nameof(ProjectMode.ConcurrentDesign) }).ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(ProjectsListState.Ready));
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.NoResultsTitle, Is.EqualTo("No projects match these filters"));
                Assert.That(viewModel.ShowingText, Is.EqualTo("Showing 0 of 2 projects"));
            }

            viewModel.SearchText = "x";
            await viewModel.ClearFiltersCommand.Execute().ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SearchText, Is.Empty);
                Assert.That(viewModel.ModeFilter, Is.Null);
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public async Task VerifyTypingReloadsOncePerPause()
        {
            this.SetUpProjects([this.project], 1, new ProjectCounts(1, 0, 1, 0, 0));
            this.SetUpMatches(new ProjectFilter("aurora", null, null), []);

            using var viewModel = this.CreateViewModel();
            var shown = viewModel.WhenAnyValue(model => model.NoResultsTitle).Where(title => title.Contains("aurora")).FirstAsync().ToTask();

            viewModel.SearchText = "a";
            viewModel.SearchText = "aurora ";
            await shown.WaitAsync(TimeSpan.FromSeconds(5));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.NoResultsTitle, Is.EqualTo("No projects match \"aurora\""));
                this.VerifyLoaded(new ProjectFilter(string.Empty, null, null), Times.Once());
                this.VerifyLoaded(new ProjectFilter("aurora", null, null), Times.Once());
                this.VerifyLoaded(new ProjectFilter("a", null, null), Times.Never());
            }
        }

        [Test]
        public void VerifyOrganizationWithoutProjectIsEmpty()
        {
            this.context.SetupGet(model => model.Capabilities).Returns(new OrganizationCapabilities(false, false));
            this.SetUpProjects([], 0, new ProjectCounts(0, 0, 0, 0, 0));

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(ProjectsListState.Empty));
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.GetTablePageSize(), Is.EqualTo(1));
                Assert.That(viewModel.CanCreateProject, Is.False);
            }
        }

        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        public void VerifyErrorOrFailureShowsTheError(bool projectsError, bool countsError, bool throws)
        {
            var error = Error.Unexpected(OrganizationErrorCodes.Unavailable, "Unavailable.");
            var projectsSetup = this.projectService.Setup(service =>
                service.GetProjectsAsync(this.organization.Id, It.IsAny<ProjectFilter>(), It.IsAny<PagedRequest>(), It.IsAny<CancellationToken>()));

            if (throws)
            {
                projectsSetup.ThrowsAsync(new InvalidOperationException());
            }
            else
            {
                projectsSetup.ReturnsAsync(projectsError ? error : new PagedResult<ProjectSummary>([], 0));
            }

            this.projectService.Setup(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(countsError ? error : new ProjectCounts(0, 0, 0, 0, 0));

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.State, Is.EqualTo(ProjectsListState.Error));
                Assert.That(viewModel.ErrorMessage, Is.Not.Empty);
            }
        }

        [Test]
        public async Task VerifyNextFilterAfterAnErrorReloadsTheCountsAndShowsTheProjects()
        {
            this.SetUpProjects([this.project], 1, new ProjectCounts(1, 0, 1, 0, 0));
            this.projectService.SetupSequence(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Error.Unexpected(OrganizationErrorCodes.Unavailable, "Unavailable."))
                .ReturnsAsync(new ProjectCounts(1, 0, 1, 0, 0));

            using var viewModel = this.CreateViewModel();
            var stateAfterTheError = viewModel.State;
            await viewModel.SelectModeCommand.Execute(new ActionMenuItem { Id = nameof(ProjectMode.Regular) }).ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stateAfterTheError, Is.EqualTo(ProjectsListState.Error));
                Assert.That(viewModel.State, Is.EqualTo(ProjectsListState.Ready));
                Assert.That(viewModel.ErrorMessage, Is.Empty);
                Assert.That(viewModel.Rows, Has.Count.EqualTo(1));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyDisposingCancelsTheLoadWhichShowsNothing(bool endsCancelled)
        {
            var cancellation = CancellationToken.None;
            var pending = new TaskCompletionSource<ErrorOr<PagedResult<ProjectSummary>>>();
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<ProjectFilter>(), It.IsAny<PagedRequest>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, ProjectFilter, PagedRequest, CancellationToken>((_, _, _, token) => cancellation = token)
                .Returns(pending.Task);
            this.projectService.Setup(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProjectCounts(1, 0, 1, 0, 0));

            var viewModel = this.CreateViewModel();
            var stateWhileLoading = viewModel.State;
            viewModel.Dispose();

            if (endsCancelled)
            {
                pending.SetCanceled(cancellation);
            }
            else
            {
                pending.SetResult(new PagedResult<ProjectSummary>([this.project], 1));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stateWhileLoading, Is.EqualTo(ProjectsListState.Loading));
                Assert.That(cancellation.IsCancellationRequested, Is.True);
                Assert.That(viewModel.State, Is.EqualTo(ProjectsListState.Loading));
                Assert.That(viewModel.Rows, Is.Empty);
            }
        }

        private void SetUpProjects(ProjectSummary[] projects, int total, ProjectCounts counts)
        {
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<ProjectFilter>(), It.IsAny<PagedRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<ProjectSummary>(projects, total));
            this.projectService.Setup(service => service.GetProjectCountsAsync(this.organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(counts);
        }

        private void SetUpMatches(ProjectFilter filter, ProjectSummary[] matches)
        {
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, filter, It.IsAny<PagedRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<ProjectSummary>(matches, matches.Length));
        }

        private void VerifyLoaded(ProjectFilter filter, Times times)
        {
            this.projectService.Verify(
                service => service.GetProjectsAsync(this.organization.Id, filter, It.IsAny<PagedRequest>(), It.IsAny<CancellationToken>()), times);
        }

        private ProjectsViewModel CreateViewModel()
        {
            return new ProjectsViewModel(this.context.Object, this.projectService.Object, this.timeProvider.Object, this.logger.Object);
        }
    }
}
