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
    using Bunit;

    using Mycelium.Bloom.Model;

    using ProjectSwitcherComponent = Mycelium.Bloom.Components.UI.Molecules.ProjectSwitcher.ProjectSwitcher;

    /// <summary>
    /// Tests the <see cref="ProjectSwitcherComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ProjectSwitcherTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies current-project rendering and selected-state announcement.
        /// </summary>
        [Test]
        public void VerifyCurrentProjectAndOptionsRender()
        {
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, CreateItems())
                .Add(component => component.SelectedProjectId, "project-a"));

            var trigger = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher__name").TextContent, Is.EqualTo("Guidance"));
                Assert.That(component.Find(".mb-project-switcher__description").TextContent,
                    Is.EqualTo("Flight systems"));
                Assert.That(trigger.GetAttribute("aria-label"),
                    Is.EqualTo("Select project. Current project: Guidance"));
            }

            trigger.Click();

            var options = component.FindAll("[role='menuitemradio']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options, Has.Count.EqualTo(2));
                Assert.That(options[0].GetAttribute("aria-checked"), Is.EqualTo("true"));
                Assert.That(options[1].GetAttribute("aria-checked"), Is.EqualTo("false"));
                Assert.That(options[0].TextContent, Does.Contain("Guidance"));
                Assert.That(options[1].TextContent, Does.Contain("Payload"));
            }
        }

        /// <summary>
        /// Verifies that selecting an enabled project returns its identifier.
        /// </summary>
        [Test]
        public void VerifyEnabledProjectInvokesSelectionCallback()
        {
            var selectedProjectId = string.Empty;

            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, CreateItems())
                .Add(component => component.SelectedProjectId, "project-a")
                .Add(component => component.SelectedProjectIdChanged, id => selectedProjectId = id));

            component.Find("button").Click();
            component.FindAll("[role='menuitemradio']")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedProjectId, Is.EqualTo("project-b"));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
                Assert.That(component.Find(".mb-project-switcher__name").TextContent, Is.EqualTo("Guidance"));
            }
        }

        /// <summary>
        /// Verifies that a disabled project cannot be selected and leaves the menu open.
        /// </summary>
        [Test]
        public void VerifyDisabledProjectCannotBeSelected()
        {
            var selectionCount = 0;
            var items = CreateItems();
            items[1].Disabled = true;

            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.SelectedProjectId, "project-a")
                .Add(component => component.SelectedProjectIdChanged, _ => selectionCount++));

            component.Find("button").Click();
            component.FindAll("[role='menuitemradio']")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionCount, Is.Zero);
                Assert.That(component.FindAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("[role='menuitemradio']")[1].HasAttribute("disabled"), Is.True);
            }
        }

        /// <summary>
        /// Verifies placeholder rendering and generated initials when no project is selected.
        /// </summary>
        [Test]
        public void VerifyPlaceholderAndGeneratedInitialsRender()
        {
            var items = new[]
            {
                new ProjectSwitcherItem { Id = "guidance", Name = "guidance" },
                new ProjectSwitcherItem { Id = "unnamed", Name = string.Empty }
            };

            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.Placeholder, "Choose project"));

            var trigger = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher__placeholder").TextContent,
                    Is.EqualTo("Choose project"));
                Assert.That(component.Find(".mb-project-switcher__initial").TextContent.Trim(), Is.EqualTo("P"));
                Assert.That(trigger.GetAttribute("aria-label"), Is.EqualTo("Select project"));
            }

            trigger.Click();

            var icons = component.FindAll(".mb-action-menu__item-icon");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(icons[0].TextContent, Is.EqualTo("G"));
                Assert.That(icons[1].TextContent, Is.EqualTo("P"));
            }
        }

        /// <summary>
        /// Verifies that separate switcher instances do not share menu state.
        /// </summary>
        [Test]
        public void VerifyInstancesMaintainIndependentOpenState()
        {
            var first = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, CreateItems())
                .Add(component => component.SelectedProjectId, "project-a"));
            var second = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, CreateItems())
                .Add(component => component.SelectedProjectId, "project-b"));

            first.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FindAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(second.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Creates standard project options.
        /// </summary>
        /// <returns>The project options.</returns>
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
                }
            ];
        }
    }
}
