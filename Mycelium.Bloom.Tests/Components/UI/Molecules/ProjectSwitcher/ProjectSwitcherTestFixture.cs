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
    using System.Collections.Generic;

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
        /// Verifies that selecting an enabled project invokes the callback and closes the dropdown.
        /// </summary>
        [Test]
        public void VerifySelectProjectInvokesCallback()
        {
            var selectedProjectId = string.Empty;

            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, GetItems())
                .Add(component => component.SelectedProjectId, "project-a")
                .Add(component => component.SelectedProjectIdChanged, id => selectedProjectId = id)
                .Add(component => component.Class, "custom-project-switcher")
                .AddUnmatched("data-testid", "project-switcher"));

            component.Find(".mb-project-switcher__trigger").Click();

            var options = component.FindAll("[role='option']");

            options[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedProjectId, Is.EqualTo("project-b"));
                Assert.That(component.Find(".mb-project-switcher").GetAttribute("data-testid"), Is.EqualTo("project-switcher"));
                Assert.That(component.Find(".mb-project-switcher").GetAttribute("class"), Does.Contain("custom-project-switcher"));
                Assert.That(component.Find(".mb-project-switcher__trigger").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.Find(".mb-project-switcher__label").TextContent.Trim(), Is.EqualTo("Project B"));
                Assert.That(component.Find(".mb-project-switcher__meta").TextContent.Trim(), Is.EqualTo("Active"));
                Assert.That(component.FindAll(".mb-project-switcher__menu"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that the dropdown renders project item content and disabled state.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysProjectOptions()
        {
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, GetItems())
                .Add(component => component.SelectedProjectId, "project-a"));

            component.Find(".mb-project-switcher__trigger").Click();

            var options = component.FindAll("[role='option']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher").GetAttribute("class"), Does.Contain("mb-project-switcher--open"));
                Assert.That(options, Has.Count.EqualTo(3));
                Assert.That(options[0].GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(options[0].GetAttribute("class"), Does.Contain("mb-project-switcher__item--selected"));
                Assert.That(options[0].TextContent, Does.Contain("Project A"));
                Assert.That(options[0].TextContent, Does.Contain("Baseline model"));
                Assert.That(options[0].TextContent, Does.Contain("Draft"));
                Assert.That(options[2].GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(options[2].GetAttribute("class"), Does.Contain("mb-project-switcher__item--disabled"));
                Assert.That(options[2].HasAttribute("disabled"), Is.True);
            }
        }

        /// <summary>
        /// Verifies that disabled project switcher state closes the dropdown and prevents reopening.
        /// </summary>
        [Test]
        public void VerifyDisabledStateClosesDropdown()
        {
            var component = this.Render<ProjectSwitcherComponent>(parameters => parameters
                .Add(component => component.Items, GetItems())
                .Add(component => component.Placeholder, "Choose a project")
                .Add(component => component.Disabled, true));

            component.Find(".mb-project-switcher__trigger").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-switcher").GetAttribute("class"), Does.Contain("mb-project-switcher--disabled"));
                Assert.That(component.Find(".mb-project-switcher__trigger").HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-project-switcher__placeholder").TextContent.Trim(), Is.EqualTo("Choose a project"));
                Assert.That(component.FindAll(".mb-project-switcher__menu"), Is.Empty);
            }
        }

        /// <summary>
        /// Gets sample project switcher items.
        /// </summary>
        /// <returns>The sample project switcher items.</returns>
        private static IReadOnlyList<ProjectSwitcherItem> GetItems()
        {
            return
            [
                new()
                {
                    Id = "project-a",
                    Name = "Project A",
                    Description = "Baseline model",
                    Lifecycle = "Draft"
                },
                new()
                {
                    Id = "project-b",
                    Name = "Project B",
                    Description = "Review model",
                    Lifecycle = "Active"
                },
                new()
                {
                    Id = "project-c",
                    Name = "Project C",
                    Description = "Archived model",
                    Lifecycle = "Archived",
                    Disabled = true
                }
            ];
        }
    }
}
