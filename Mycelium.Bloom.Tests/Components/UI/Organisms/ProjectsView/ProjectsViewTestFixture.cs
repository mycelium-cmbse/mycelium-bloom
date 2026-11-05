// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectsViewTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.ProjectsView
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using ErrorOr;

    using Moq;

    using Mycelium.Bloom.Components.UI.Organisms.ProjectsView;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.DTO;

    using ReactiveUI;

    [TestFixture]
    public sealed class ProjectsViewTestFixture
    {
        private BunitContext context;

        private IRenderedComponent<BbPortalHost> portalHost;

        private Mock<IProjectsViewModel> viewModel;

        private DateTimeOffset now;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this.context);
            this.now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            this.viewModel = new Mock<IProjectsViewModel>();
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.viewModel.SetupProperty(model => model.SearchText, string.Empty);
            this.viewModel.SetupProperty(model => model.StatusFilter);
            this.viewModel.SetupProperty(model => model.ModeFilter);
            this.SetUpRows(0);
        }

        [TearDown]
        public async Task TearDown()
        {
            this.portalHost.Dispose();
            await this.context.DisposeAsync();
        }

        [TestCase(true, -1, 0, false, true, 1, 0, 0, 0, 0, 0, 1)]
        [TestCase(false, 4, 4, false, true, 0, 4, 0, 1, 0, 0, 1)]
        [TestCase(false, 30, 30, false, true, 0, 25, 0, 1, 0, 1, 1)]
        [TestCase(false, 0, 0, false, false, 0, 0, 0, 0, 1, 0, 0)]
        [TestCase(false, -1, 0, true, true, 0, 0, 1, 1, 0, 0, 1)]
        public void VerifyTheViewFlipsOnBusyErrorsAndRows(bool isBusy, int total, int rowCount, bool failed, bool canCreateProject, int skeletons, int rows, int alerts, int searchBoxes, int emptyStates, int pagers, int createButtons)
        {
            this.SetUpRows(rowCount);
            this.viewModel.SetupGet(model => model.IsBusy).Returns(isBusy);
            this.viewModel.SetupGet(model => model.Counts).Returns(total < 0 ? null : new ProjectCounts(total, 0, total, 0, 0));
            this.viewModel.SetupGet(model => model.Errors).Returns(failed ? [Error.Unexpected("Projects.LoadFailed", "Failed.")] : []);
            this.viewModel.SetupGet(model => model.CanCreateProject).Returns(canCreateProject);

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.FindAll("[role=status]"), Has.Count.EqualTo(skeletons));
                Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(rows));
                Assert.That(cut.FindAll("[role=alert]"), Has.Count.EqualTo(alerts));
                Assert.That(cut.FindAll("#projects-search"), Has.Count.EqualTo(searchBoxes));
                Assert.That(cut.FindComponents<BbEmpty>(), Has.Count.EqualTo(emptyStates));
                Assert.That(cut.FindComponents<BbPagination>(), Has.Count.EqualTo(pagers));
                Assert.That(cut.FindAll("header button"), Has.Count.EqualTo(createButtons));
            }
        }

        [Test]
        public async Task VerifyTypingSetsTheSearchTextAndTheNoResultsActionClearsTheSearchAndFilters()
        {
            var cleared = 0;
            this.viewModel.SetupGet(model => model.Counts).Returns(new ProjectCounts(4, 0, 4, 0, 0));
            this.viewModel.Object.SearchText = "thermal";
            this.viewModel.SetupGet(model => model.ClearFiltersCommand).Returns(ReactiveCommand.Create(() => { cleared++; }));

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));
            var title = cut.FindComponent<BbEmpty>().Instance.Title;
            await cut.InvokeAsync(() => cut.FindComponent<BbInputGroupInput>().Instance.JsOnInput("cold"));
            var typed = this.viewModel.Object.SearchText;
            cut.FindAll("button").Single(button => button.TextContent.Contains("Clear search and filters", StringComparison.Ordinal)).Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(title, Is.EqualTo("No projects match \"thermal\""));
                Assert.That(typed, Is.EqualTo("cold"));
                Assert.That(cleared, Is.EqualTo(1));
            }
        }

        [TestCase(true, ProjectLifecycleKind.Review, null)]
        [TestCase(false, null, ProjectMode.Concurrent)]
        public async Task VerifyPickingAFilterEntryAppliesItToTheViewModel(bool isStatusMenu, ProjectLifecycleKind? expectedStatus, ProjectMode? expectedMode)
        {
            this.SetUpRows(4);
            this.viewModel.SetupGet(model => model.Counts).Returns(new ProjectCounts(4, 0, 4, 0, 0));
            var entryCount = (isStatusMenu ? Enum.GetValues<ProjectLifecycleKind>().Length : Enum.GetValues<ProjectMode>().Length) + 1;

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));
            await cut.Find(isStatusMenu ? "button[title='Filter by status']" : "button[title='Filter by mode']").ClickAsync();
            var entries = await this.portalHost.WaitForElementsAsync("[role='menuitem']", entryCount);
            entries.Single(entry => entry.TextContent.Contains(isStatusMenu ? "Review" : "Concurrent", StringComparison.Ordinal)).Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Object.StatusFilter, Is.EqualTo(expectedStatus));
                Assert.That(this.viewModel.Object.ModeFilter, Is.EqualTo(expectedMode));
            }
        }

        private void SetUpRows(int rowCount)
        {
            var projects = Enumerable.Range(0, rowCount).Select(index => new FunctionalProject
            {
                Id = Guid.NewGuid(),
                Name = $"Project {index}",
                Description = "Description",
                Visibility = (ProjectVisibility)(index % 3),
                CurrentMode = (ProjectMode)(index % 2),
                Lifecycle = (ProjectLifecycleKind)(index % 4),
                Involves = [Guid.NewGuid()],
                UpdatedOn = this.now.UtcDateTime
            });

            this.viewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<ProjectRowViewModel>(
                new ObservableCollection<ProjectRowViewModel>(projects.Select(project => new ProjectRowViewModel(project, this.now)))));
        }
    }
}
