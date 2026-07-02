// ------------------------------------------------------------------------------------------------
// <copyright file="SplitButtonTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.SplitButton
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using SplitButtonComponent = Mycelium.Bloom.Components.UI.Molecules.SplitButton.SplitButton;

    /// <summary>
    /// Tests the <see cref="SplitButtonComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SplitButtonTestFixture : BunitContext
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
        /// Verifies that the primary action invokes the configured callback when enabled.
        /// </summary>
        [Test]
        public void VerifyPrimaryActionInvokesCallback()
        {
            var primaryActionCount = 0;

            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.Text, "Create")
                .Add(component => component.PrimaryAction, () => primaryActionCount++)
                .Add(component => component.Variant, ButtonVariant.Danger)
                .Add(component => component.Size, ButtonSize.Large)
                .Add(component => component.Class, "custom-split-button")
                .AddUnmatched("data-testid", "create-actions"));

            component.Find(".mb-split-button__main").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(primaryActionCount, Is.EqualTo(1));
                Assert.That(component.Find(".mb-split-button").GetAttribute("data-testid"), Is.EqualTo("create-actions"));
                Assert.That(component.Find(".mb-split-button").GetAttribute("class"), Does.Contain("mb-split-button--danger"));
                Assert.That(component.Find(".mb-split-button").GetAttribute("class"), Does.Contain("mb-split-button--large"));
                Assert.That(component.Find(".mb-split-button").GetAttribute("class"), Does.Contain("custom-split-button"));
            }
        }

        /// <summary>
        /// Verifies that selecting an enabled dropdown item invokes the item callback and closes the menu.
        /// </summary>
        [Test]
        public void VerifySelectItemInvokesCallbackAndClosesMenu()
        {
            var selectedValue = string.Empty;

            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.Text, "Create")
                .Add(component => component.Items, GetItems())
                .Add(component => component.ItemSelected, item => selectedValue = item.Value)
                .Add(component => component.MenuTitle, "More create actions")
                .Add(component => component.MenuAriaLabel, "More create actions")
                .Add(component => component.Variant, ButtonVariant.Secondary)
                .Add(component => component.Size, ButtonSize.Small));

            component.Find(".mb-split-button__toggle").Click();

            var items = component.FindAll("[role='menuitem']");

            items[0].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedValue, Is.EqualTo("from-template"));
                Assert.That(component.Find(".mb-split-button__toggle").GetAttribute("title"), Is.EqualTo("More create actions"));
                Assert.That(component.Find(".mb-split-button__toggle").GetAttribute("aria-label"), Is.EqualTo("More create actions"));
                Assert.That(component.Find(".mb-split-button__toggle").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.Find(".mb-split-button").GetAttribute("class"), Does.Contain("mb-split-button--secondary"));
                Assert.That(component.Find(".mb-split-button").GetAttribute("class"), Does.Contain("mb-split-button--small"));
                Assert.That(component.FindAll(".mb-split-button__menu"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that disabled and loading states disable actions.
        /// </summary>
        [Test]
        public void VerifyDisabledStatePreventsActions()
        {
            var primaryActionCount = 0;

            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.Text, "Create")
                .Add(component => component.Items, GetItems())
                .Add(component => component.Disabled, true)
                .Add(component => component.IsLoading, true)
                .Add(component => component.PrimaryAction, () => primaryActionCount++));

            component.Find(".mb-split-button__main").Click();
            component.Find(".mb-split-button__toggle").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(primaryActionCount, Is.EqualTo(0));
                Assert.That(component.Find(".mb-split-button").GetAttribute("class"), Does.Contain("mb-split-button--disabled"));
                Assert.That(component.Find(".mb-split-button__main").HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-split-button__toggle").HasAttribute("disabled"), Is.True);
                Assert.That(component.FindAll(".mb-split-button__menu"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that the menu renders item variants and disabled item state.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysMenuItems()
        {
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.Text, "Create")
                .Add(component => component.Items, GetItems())
                .Add(component => component.Variant, ButtonVariant.Ghost));

            component.Find(".mb-split-button__toggle").Click();

            var items = component.FindAll("[role='menuitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-split-button").GetAttribute("class"), Does.Contain("mb-split-button--ghost"));
                Assert.That(items, Has.Count.EqualTo(3));
                Assert.That(items[0].TextContent, Does.Contain("From template"));
                Assert.That(items[0].TextContent, Does.Contain("Start from a reusable pattern."));
                Assert.That(items[0].TextContent, Does.Contain("T"));
                Assert.That(items[1].GetAttribute("class"), Does.Contain("mb-split-button__item--danger"));
                Assert.That(items[1].GetAttribute("class"), Does.Contain("mb-split-button__item--separator"));
                Assert.That(items[2].GetAttribute("class"), Does.Contain("mb-split-button__item--disabled"));
                Assert.That(items[2].HasAttribute("disabled"), Is.True);
            }
        }

        /// <summary>
        /// Gets sample action menu items.
        /// </summary>
        /// <returns>The sample action menu items.</returns>
        private static IReadOnlyList<ActionMenuItem> GetItems()
        {
            return
            [
                new()
                {
                    Value = "from-template",
                    Label = "From template",
                    Description = "Start from a reusable pattern.",
                    Icon = "T"
                },
                new()
                {
                    Value = "delete",
                    Label = "Delete",
                    SeparatorBefore = true,
                    Variant = ActionMenuItemVariant.Danger
                },
                new()
                {
                    Value = "disabled",
                    Label = "Disabled",
                    Disabled = true
                }
            ];
        }
    }
}
