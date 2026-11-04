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
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;

    using Moq;

    using Mycelium.Bloom.Components.UI.Molecules.ActionMenu;
    using Mycelium.Bloom.Components.UI.Organisms.ProjectsView;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    using ReactiveUI;

    [TestFixture]
    public sealed class ProjectsViewTestFixture
    {
        private BunitContext context;

        private Mock<IProjectsViewModel> viewModel;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            var now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
            var regular = new ProjectSummary("Aurora Orbiter", "Description", ProjectVisibility.Private, ProjectMode.Regular, 4, ProjectLifecycleState.Open,
                now, true);
            var concurrent = regular with { Name = "Deimos Lander", Mode = ProjectMode.ConcurrentDesign, LifecycleState = ProjectLifecycleState.Preparation };
            var inReview = regular with { Name = "Europa Probe", Visibility = ProjectVisibility.Organization, LifecycleState = ProjectLifecycleState.Review };
            var archived = regular with { Name = "Ganymede Relay", Visibility = ProjectVisibility.Public, LifecycleState = ProjectLifecycleState.Archived };

            this.viewModel = new Mock<IProjectsViewModel>();
            this.viewModel.SetupGet(model => model.Rows).Returns([new ProjectRowViewModel(regular, now), new ProjectRowViewModel(concurrent, now), new ProjectRowViewModel(inReview, now), new ProjectRowViewModel(archived, now)]);
            this.viewModel.SetupGet(model => model.ErrorMessage).Returns("The projects could not be loaded.");
            this.viewModel.SetupGet(model => model.StatusFilterItems).Returns([]);
            this.viewModel.SetupGet(model => model.ModeFilterItems).Returns([]);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [Test]
        public void VerifyNoResultsStateClearsTheSearchAndFilters()
        {
            var cleared = 0;
            this.viewModel.SetupGet(model => model.State).Returns(ProjectsListState.Ready);
            this.viewModel.SetupGet(model => model.Rows).Returns([]);
            this.viewModel.SetupGet(model => model.NoResultsTitle).Returns("No projects match \"thermal\"");
            this.viewModel.SetupGet(model => model.ClearFiltersCommand).Returns(ReactiveCommand.Create(() => { cleared++; }));

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));

            cut.FindAll("button").Single(button => button.TextContent.Contains("Clear search and filters")).Click();

            Assert.That(cleared, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyPickingAFilterEntryRunsTheCommandOfItsMenu(bool isStatusMenu)
        {
            ActionMenuItem pickedStatus = null;
            ActionMenuItem pickedMode = null;
            var item = new ActionMenuItem { Id = "Open", Label = "Open" };
            this.viewModel.SetupGet(model => model.State).Returns(ProjectsListState.Ready);
            this.viewModel.SetupGet(model => model.SelectStatusCommand).Returns(ReactiveCommand.Create<ActionMenuItem>(entry => pickedStatus = entry));
            this.viewModel.SetupGet(model => model.SelectModeCommand).Returns(ReactiveCommand.Create<ActionMenuItem>(entry => pickedMode = entry));

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));
            var menu = cut.FindComponents<ActionMenu>()[isStatusMenu ? 0 : 1];
            await cut.InvokeAsync(() => menu.Instance.ItemSelected.InvokeAsync(item));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pickedStatus, isStatusMenu ? Is.SameAs(item) : Is.Null);
                Assert.That(pickedMode, isStatusMenu ? Is.Null : Is.SameAs(item));
            }
        }

        [TestCase(ProjectsListState.Loading, 1, 0, 0, false)]
        [TestCase(ProjectsListState.Ready, 0, 4, 0, true)]
        [TestCase(ProjectsListState.Empty, 0, 0, 0, true)]
        [TestCase(ProjectsListState.Error, 0, 0, 1, false)]
        public void VerifyEachStateShowsItsContent(ProjectsListState state, int statuses, int rows, int alerts, bool canCreateProject)
        {
            this.viewModel.SetupGet(model => model.State).Returns(state);
            this.viewModel.SetupGet(model => model.CanCreateProject).Returns(canCreateProject);

            var cut = this.context.Render<ProjectsView>(parameters => parameters.Add(component => component.ViewModel, this.viewModel.Object));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cut.FindAll("[role=status]"), Has.Count.EqualTo(statuses));
                Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(rows));
                Assert.That(cut.FindAll("[role=alert]"), Has.Count.EqualTo(alerts));
                Assert.That(cut.FindAll("header button"), Has.Count.EqualTo(canCreateProject ? 1 : 0));
            }
        }
    }
}
