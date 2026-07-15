// ------------------------------------------------------------------------------------------------
// <copyright file="BreadcrumbsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.Breadcrumbs
{
    using Bunit;

    using Mycelium.Bloom.Model;

    using BreadcrumbsComponent = Mycelium.Bloom.Components.UI.Molecules.Breadcrumbs.Breadcrumbs;

    /// <summary>
    /// Tests the <see cref="BreadcrumbsComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class BreadcrumbsTestFixture : BunitContext
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
        /// Verifies that all items render with interactive, disabled, and current semantics.
        /// </summary>
        [Test]
        public void VerifyItemsRenderWithExpectedSemantics()
        {
            var component = this.Render<BreadcrumbsComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new BreadcrumbItem { Id = "workspace", Label = "Workspace" },
                    new BreadcrumbItem { Id = "package", Label = "Package", Disabled = true },
                    new BreadcrumbItem { Id = "element", Label = "Element", IsCurrent = true }
                })
                .Add(component => component.AriaLabel, "Model location")
                .Add(component => component.Separator, ">"));

            var navigation = component.Find("nav");
            var currentItem = component.Find("[aria-current='page']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(navigation.GetAttribute("aria-label"), Is.EqualTo("Model location"));
                Assert.That(component.FindAll("li"), Has.Count.EqualTo(3));
                Assert.That(component.FindAll("button"), Has.Count.EqualTo(1));
                Assert.That(component.Find("[aria-disabled='true']").TextContent.Trim(), Is.EqualTo("Package"));
                Assert.That(currentItem.TextContent.Trim(), Is.EqualTo("Element"));
                Assert.That(component.FindAll(".mb-breadcrumbs__separator"), Has.Count.EqualTo(2));
            }
        }

        /// <summary>
        /// Verifies that selecting an enabled non-current item returns the selected model.
        /// </summary>
        [Test]
        public void VerifyEnabledItemInvokesSelectionCallback()
        {
            BreadcrumbItem selectedItem = null;
            var expectedItem = new BreadcrumbItem { Id = "workspace", Label = "Workspace", Target = "workspace" };

            var component = this.Render<BreadcrumbsComponent>(parameters => parameters
                .Add(component => component.Items, new[] { expectedItem })
                .Add(component => component.ItemSelected, item => selectedItem = item));

            component.Find("button").Click();

            Assert.That(selectedItem, Is.SameAs(expectedItem));
        }

        /// <summary>
        /// Verifies that disabled and current items cannot invoke navigation.
        /// </summary>
        [Test]
        public void VerifyDisabledAndCurrentItemsAreNotInteractive()
        {
            var selectionCount = 0;

            var component = this.Render<BreadcrumbsComponent>(parameters => parameters
                .Add(component => component.Items, new[]
                {
                    new BreadcrumbItem { Id = "disabled", Label = "Disabled", Disabled = true },
                    new BreadcrumbItem { Id = "current", Label = "Current", IsCurrent = true }
                })
                .Add(component => component.ItemSelected, _ => selectionCount++));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("button"), Is.Empty);
                Assert.That(selectionCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that an empty collection renders an empty semantic trail without failing.
        /// </summary>
        [Test]
        public void VerifyEmptyCollectionRendersGracefully()
        {
            var component = this.Render<BreadcrumbsComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("nav"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("li"), Is.Empty);
            }
        }
    }
}
