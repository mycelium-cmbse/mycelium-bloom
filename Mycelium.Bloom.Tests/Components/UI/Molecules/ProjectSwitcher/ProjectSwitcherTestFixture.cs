// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectSwitcherTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.ProjectSwitcher
{
    using System.Threading.Tasks;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;

    using ProjectSwitcherComponent = Mycelium.Bloom.Components.UI.Molecules.ProjectSwitcher.ProjectSwitcher;

    /// <summary>
    /// Tests Bloom project data mapped onto a styled Blueprint menu.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ProjectSwitcherTestFixture : BunitContext
    {
        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectSwitcherTestFixture" /> class.
        /// </summary>
        public ProjectSwitcherTestFixture()
        {
            this.portalHost = BlueprintTestSetup.ConfigureWithPortalHost(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies current project identity, active indication, and unavailable projects.
        /// </summary>
        [Test]
        public async Task VerifyCurrentProjectAndOptionsRender()
        {
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(switcher => switcher.Items, CreateItems())
                .Add(switcher => switcher.SelectedProjectId, "project-a"));

            var trigger = component.Find("button");
            await trigger.ClickAsync();
            var options = this.portalHost.WaitForElements("[role='menuitem']", 3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher__name").TextContent, Is.EqualTo("Guidance"));
                Assert.That(component.Find(".mb-project-switcher__description").TextContent, Is.EqualTo("Flight systems"));
                Assert.That(trigger.GetAttribute("aria-label"), Is.EqualTo("Select project. Current project: Guidance"));
                Assert.That(options[0].ClassList, Does.Contain("bg-accent"));
                Assert.That(options[0].TextContent, Does.Contain("Current selection"));
                Assert.That(options[1].TextContent, Does.Not.Contain("Current selection"));
                Assert.That(options[2].GetAttribute("aria-disabled"), Is.EqualTo("true"));
            }
        }

        /// <summary>
        /// Verifies selecting an enabled project reports its identifier without mutating controlled display state.
        /// </summary>
        [Test]
        public async Task VerifyEnabledProjectReportsSelection()
        {
            var selectedProjectId = string.Empty;
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(switcher => switcher.Items, CreateItems())
                .Add(switcher => switcher.SelectedProjectId, "project-a")
                .Add(switcher => switcher.SelectedProjectIdChanged, id => selectedProjectId = id));

            await component.Find("button").ClickAsync();
            await this.portalHost.WaitForElements("[role='menuitem']", 3)[1].ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedProjectId, Is.EqualTo("project-b"));
                Assert.That(component.Find(".mb-project-switcher__name").TextContent, Is.EqualTo("Guidance"));
                Assert.That(component.Find("button").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }

            component.Render(parameters => parameters
                .Add(switcher => switcher.Items, CreateItems())
                .Add(switcher => switcher.SelectedProjectId, selectedProjectId));

            Assert.That(component.Find(".mb-project-switcher__name").TextContent, Is.EqualTo("Payload"));
        }

        /// <summary>
        /// Verifies a disabled project cannot report a selection.
        /// </summary>
        [Test]
        public async Task VerifyDisabledProjectCannotBeSelected()
        {
            var selectionCount = 0;
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(switcher => switcher.Items, CreateItems())
                .Add(switcher => switcher.SelectedProjectId, "project-a")
                .Add(switcher => switcher.SelectedProjectIdChanged, _ => selectionCount++));

            await component.Find("button").ClickAsync();
            await this.portalHost.WaitForElements("[role='menuitem']", 3)[2].ClickAsync();

            Assert.That(selectionCount, Is.Zero);
        }

        /// <summary>
        /// Verifies placeholder and generated initials when no project is selected.
        /// </summary>
        [Test]
        public async Task VerifyPlaceholderAndGeneratedInitialsRender()
        {
            var items = new[]
            {
                new ProjectSwitcherItem { Id = "guidance", Name = "guidance" },
                new ProjectSwitcherItem { Id = "unnamed", Name = string.Empty }
            };
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(switcher => switcher.Items, items)
                .Add(switcher => switcher.Placeholder, "Choose project"));

            await component.Find("button").ClickAsync();
            var icons = this.portalHost.WaitForElements(".mb-action-menu__item-symbol", 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher__placeholder").TextContent, Is.EqualTo("Choose project"));
                Assert.That(component.Find(".mb-project-switcher__initial").TextContent.Trim(), Is.EqualTo("P"));
                Assert.That(component.Find("button").GetAttribute("aria-label"), Is.EqualTo("Select project"));
                Assert.That(icons[0].TextContent.Trim(), Is.EqualTo("G"));
                Assert.That(icons[1].TextContent.Trim(), Is.EqualTo("P"));
            }
        }

        /// <summary>
        /// Verifies long project metadata remains available through titles in the width-matched popup.
        /// </summary>
        [Test]
        public async Task VerifyLongProjectMetadataRemainsAvailable()
        {
            var longName = "Orbital platform architecture workspace with a deliberately long project name";
            var longDescription = "A long project description retained for inspection";
            var items = new[]
            {
                new ProjectSwitcherItem
                {
                    Id = "long",
                    Name = longName,
                    Description = longDescription
                }
            };
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(switcher => switcher.Items, items)
                .Add(switcher => switcher.SelectedProjectId, "long"));

            await component.Find("button").ClickAsync();
            var menuItem = this.portalHost.WaitForElement("[role='menuitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher__name").GetAttribute("title"), Is.EqualTo(longName));
                Assert.That(menuItem.QuerySelector(".mb-action-menu__item-label")?.GetAttribute("title"), Is.EqualTo(longName));
                Assert.That(menuItem.QuerySelector(".mb-action-menu__item-description")?.GetAttribute("title"), Is.EqualTo(longDescription));
            }
        }

        /// <summary>
        /// Verifies an empty project collection leaves a named, unavailable trigger and placeholder.
        /// </summary>
        [Test]
        public async Task VerifyEmptyStateRemainsAvailable()
        {
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(switcher => switcher.Items, [])
                .Add(switcher => switcher.Placeholder, "No projects available"));
            var trigger = component.Find("button");

            await trigger.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher__placeholder").TextContent, Is.EqualTo("No projects available"));
                Assert.That(trigger.GetAttribute("aria-label"), Is.EqualTo("Select project"));
                Assert.That(trigger.GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
            }
        }

        private static ProjectSwitcherItem[] CreateItems()
        {
            return
            [
                new ProjectSwitcherItem
                {
                    Id = "project-a",
                    Name = "Guidance",
                    Description = "Flight systems",
                    Initial = "G"
                },
                new ProjectSwitcherItem
                {
                    Id = "project-b",
                    Name = "Payload",
                    Description = "Instrument package",
                    Initial = "P"
                },
                new ProjectSwitcherItem
                {
                    Id = "project-c",
                    Name = "Archive",
                    Description = "Read only",
                    Initial = "A",
                    Disabled = true
                }
            ];
        }
    }
}
