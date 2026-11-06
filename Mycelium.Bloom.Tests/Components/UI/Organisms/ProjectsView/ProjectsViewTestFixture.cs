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
    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;
    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

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
            this.SetUpRows(0);
        }

        [TearDown]
        public async Task TearDown()
        {
            this.portalHost.Dispose();
            await this.context.DisposeAsync();
        }

        [TestCase(true, 0, false, true, 1, 0, 0, 0, 0, 0, 1)]
        [TestCase(false, 4, false, true, 0, 4, 0, 1, 0, 0, 1)]
        [TestCase(false, 30, false, true, 0, 25, 0, 1, 0, 1, 1)]
        [TestCase(false, 0, false, false, 0, 0, 0, 0, 1, 0, 0)]
        [TestCase(false, 0, true, true, 0, 0, 1, 1, 0, 0, 1)]
        public void VerifyTheViewFlipsOnBusyErrorsAndRows(bool isBusy, int rowCount, bool failed, bool canCreateProject, int skeletons, int rows, int alerts, int searchBoxes, int emptyStates, int pagers, int createButtons)
        {
            this.SetUpRows(rowCount);
            this.viewModel.SetupGet(model => model.IsBusy).Returns(isBusy);
            this.viewModel.SetupGet(model => model.Errors).Returns(failed ? [Error.Unexpected(ErrorCodes.Unavailable, "Failed.")] : []);
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
        public async Task VerifyTheSearchNarrowsTheRowsAndTheNoResultsActionShowsThemAgain()
        {
            this.SetUpRows(4);

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));
            await cut.InvokeAsync(() => cut.FindComponent<BbInputGroupInput>().Instance.JsOnInput("Project 2"));
            var narrowed = cut.FindAll("tbody tr").Count;
            var showing = cut.Find("p[aria-live]").TextContent;
            await cut.InvokeAsync(() => cut.FindComponent<BbInputGroupInput>().Instance.JsOnInput(" PHASE "));
            var narrowedByDescription = cut.FindAll("tbody tr").Count;
            await cut.InvokeAsync(() => cut.FindComponent<BbInputGroupInput>().Instance.JsOnInput("thermal"));
            var title = cut.FindComponent<BbEmpty>().Instance.Title;
            var searchBoxes = cut.FindAll("#projects-search").Count;
            cut.FindAll("button").Single(button => button.TextContent.Contains("Clear search and filters", StringComparison.Ordinal)).Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(narrowed, Is.EqualTo(1));
                Assert.That(showing, Is.EqualTo("Showing 1 of 4 projects"));
                Assert.That(narrowedByDescription, Is.EqualTo(1));
                Assert.That(title, Is.EqualTo("No projects match \"thermal\""));
                Assert.That(searchBoxes, Is.EqualTo(1));
                Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(4));
            }
        }

        [TestCase(true, 1, "Status: Review")]
        [TestCase(false, 2, "Mode: Concurrent")]
        public async Task VerifyPickingAFilterEntryNarrowsTheRowsAndNamesTheChoiceInTheMenu(bool isStatusMenu, int expected, string label)
        {
            this.SetUpRows(4);
            var entryCount = (isStatusMenu ? Enum.GetValues<ProjectLifecycleKind>().Length : Enum.GetValues<ProjectMode>().Length) + 1;

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));
            await cut.Find(isStatusMenu ? "button[title='Filter by status']" : "button[title='Filter by mode']").ClickAsync();
            var entries = await this.portalHost.WaitForElementsAsync("[role='menuitem']", entryCount);
            entries.Single(entry => entry.TextContent.Contains(isStatusMenu ? "Review" : "Concurrent", StringComparison.Ordinal)).Click();
            await cut.WaitForAssertionAsync(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(expected)));
            var trigger = cut.Find(isStatusMenu ? "button[title='Filter by status']" : "button[title='Filter by mode']").TextContent.Trim();

            Assert.That(trigger, Is.EqualTo(label));
        }

        private void SetUpRows(int rowCount)
        {
            var projects = Enumerable.Range(0, rowCount).Select(index => new FunctionalProject
            {
                Id = Guid.NewGuid(),
                Name = $"Project {index}",
                Description = index == 3 ? "Phase A" : "Description",
                Visibility = (ProjectVisibility)(index % 3),
                CurrentMode = (ProjectMode)(index % 2),
                Lifecycle = (ProjectLifecycleKind)(index % 4),
                Involves = [new ProjectMember { Id = Guid.NewGuid() }],
                UpdatedOn = this.now.UtcDateTime
            });

            this.viewModel.SetupGet(model => model.Rows).Returns(new ReadOnlyObservableCollection<ProjectRowViewModel>(
                new ObservableCollection<ProjectRowViewModel>(projects.Select(project => new ProjectRowViewModel(project, this.now)))));
        }
    }
}
