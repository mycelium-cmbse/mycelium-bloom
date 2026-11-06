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

    using DynamicData;

    using ErrorOr;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Time.Testing;

    using Moq;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    [TestFixture]
    public sealed class ProjectsViewModelTestFixture
    {
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
        public void VerifyProjectsAreLoadedNewestFirstAndCounted()
        {
            this.SetUpProjects([this.CreateProject("Older", hoursAgo: 5), this.CreateProject("Newer", hoursAgo: 2)]);

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.CanCreateProject, Is.True);
                Assert.That(viewModel.TotalCount, Is.EqualTo(2));
                Assert.That(viewModel.Projects.Count, Is.EqualTo(2));
                Assert.That(viewModel.Rows.Select(row => row.Name), Is.EqualTo(new[] { "Newer", "Older" }));
                Assert.That(viewModel.Rows[0].Visibility, Is.EqualTo(ProjectVisibility.Private));
                Assert.That(viewModel.Rows[0].Mode, Is.EqualTo(ProjectMode.Regular));
                Assert.That(viewModel.Rows[0].MemberCount, Is.EqualTo(4));
                Assert.That(viewModel.Rows[0].Lifecycle, Is.EqualTo(ProjectLifecycleKind.Open));
                Assert.That(viewModel.Rows[0].UpdatedText, Is.EqualTo("2 hours ago"));
            }
        }

        [TestCase(ProjectLifecycleKind.Open, null, new[] { "Aurora Orbiter" })]
        [TestCase(null, ProjectMode.Concurrent, new[] { "Lunar Relay" })]
        [TestCase(ProjectLifecycleKind.Archived, ProjectMode.Concurrent, new string[0])]
        public void VerifyTheFiltersNarrowTheRowsWithoutReloading(ProjectLifecycleKind? status, ProjectMode? mode, string[] expected)
        {
            this.SetUpProjects(
            [
                this.CreateProject("Aurora Orbiter", hoursAgo: 1),
                this.CreateProject("Lunar Relay", hoursAgo: 2, lifecycle: ProjectLifecycleKind.Review, projectMode: ProjectMode.Concurrent)
            ]);

            using var viewModel = this.CreateViewModel();
            viewModel.StatusFilter = status;
            viewModel.ModeFilter = mode;
            var narrowed = viewModel.Rows.Select(row => row.Name).ToArray();
            viewModel.StatusFilter = null;
            viewModel.ModeFilter = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(narrowed, Is.EqualTo(expected));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
                Assert.That(viewModel.TotalCount, Is.EqualTo(2));
                this.projectService.Verify(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public void VerifyTypingNarrowsTheRowsOncePerPauseAndEmptyingTheSearchShowsThemAtOnce()
        {
            this.SetUpProjects([this.CreateProject("Aurora Orbiter", hoursAgo: 1), this.CreateProject("Lunar Relay", hoursAgo: 2, description: "Phase A")]);

            using var viewModel = this.CreateViewModel();
            viewModel.SearchText = "a";
            viewModel.SearchText = " PHASE ";
            var rowsBeforeThePause = viewModel.Rows.Count;

            Assert.That(() => viewModel.Rows.Count == 1, Is.True.After(5000, 20));
            var narrowed = viewModel.Rows.Single().Name;
            viewModel.SearchText = string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rowsBeforeThePause, Is.EqualTo(2));
                Assert.That(narrowed, Is.EqualTo("Lunar Relay"));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
                this.projectService.Verify(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task VerifyClearingTheFiltersShowsEveryProjectAgain()
        {
            this.SetUpProjects([this.CreateProject("Aurora Orbiter", hoursAgo: 1), this.CreateProject("Lunar Relay", hoursAgo: 2)]);

            using var viewModel = this.CreateViewModel();
            viewModel.ModeFilter = ProjectMode.Concurrent;
            var rowsWithoutMatch = viewModel.Rows.Count;
            viewModel.StatusFilter = ProjectLifecycleKind.Open;
            viewModel.SearchText = "x";
            await viewModel.ClearFiltersCommand.Execute().ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rowsWithoutMatch, Is.Zero);
                Assert.That(viewModel.SearchText, Is.Empty);
                Assert.That(viewModel.StatusFilter, Is.Null);
                Assert.That(viewModel.ModeFilter, Is.Null);
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void VerifyAProjectAddedToTheSourceListJoinsTheRowsWhenItMatches()
        {
            this.SetUpProjects([this.CreateProject("Aurora Orbiter", hoursAgo: 1)]);

            using var viewModel = this.CreateViewModel();
            viewModel.StatusFilter = ProjectLifecycleKind.Open;
            viewModel.Projects.Add(new ProjectRowViewModel(this.CreateProject("Draft", hoursAgo: 0, lifecycle: ProjectLifecycleKind.Preparation), this.now));
            var withTheDraftHidden = viewModel.Rows.Count;
            viewModel.StatusFilter = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withTheDraftHidden, Is.EqualTo(1));
                Assert.That(viewModel.Rows, Has.Count.EqualTo(2));
                Assert.That(viewModel.TotalCount, Is.EqualTo(2));
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
                Assert.That(viewModel.TotalCount, Is.Zero);
                Assert.That(viewModel.CanCreateProject, Is.False);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyErrorOrFailureIsKeptAsAnErrorAndLeavesNoRow(bool returnsError)
        {
            var setup = this.projectService.Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<CancellationToken>()));

            if (returnsError)
            {
                setup.ReturnsAsync(Error.Unexpected(ErrorCodes.Unavailable, "Unavailable."));
            }
            else
            {
                setup.ThrowsAsync(new InvalidOperationException());
            }

            using var viewModel = this.CreateViewModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors.Single().Code, Is.EqualTo(ErrorCodes.Unavailable));
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.TotalCount, Is.Zero);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyDisposingCancelsTheLoadWhichShowsNothing(bool endsCancelled)
        {
            var cancellation = CancellationToken.None;
            var pending = new TaskCompletionSource<ErrorOr<IReadOnlyList<FunctionalProject>>>();
            this.projectService
                .Setup(service => service.GetProjectsAsync(this.organization.Id, It.IsAny<CancellationToken>()))
                .Callback<Guid, CancellationToken>((_, token) => cancellation = token)
                .Returns(pending.Task);

            var viewModel = this.CreateViewModel();
            var busyWhileLoading = viewModel.IsBusy;
            viewModel.Dispose();

            if (endsCancelled)
            {
                pending.SetCanceled(cancellation);
            }
            else
            {
                pending.SetResult(ErrorOrFactory.From<IReadOnlyList<FunctionalProject>>([this.CreateProject("Aurora Orbiter", hoursAgo: 1)]));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(busyWhileLoading, Is.True);
                Assert.That(cancellation.IsCancellationRequested, Is.True);
                Assert.That(viewModel.IsBusy, Is.True);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.TotalCount, Is.Zero);
            }
        }

        [Test]
        public void VerifyTheLoadAndTheSearchRunOnTheContextTheViewModelWasCreatedOn()
        {
            var renderContext = new CountingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(renderContext);
            this.SetUpProjects([this.CreateProject("Aurora Orbiter", hoursAgo: 1)]);

            using var viewModel = this.CreateViewModel();
            var postsAfterTheLoad = renderContext.Posts;
            viewModel.SearchText = "no such project";

            Assert.That(() => viewModel.Rows.Count, Is.Zero.After(5000, 20));
            Assert.That(renderContext.Posts, Is.GreaterThan(postsAfterTheLoad));
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
