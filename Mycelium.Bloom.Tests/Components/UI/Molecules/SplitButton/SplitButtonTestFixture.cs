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
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ButtonComponent = Mycelium.Bloom.Components.UI.Atoms.Button.Button;
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
        /// Verifies that the primary action invokes its callback and renders the requested appearance.
        /// </summary>
        [Test]
        public void VerifyPrimaryActionAndAppearance()
        {
            var primaryActionCount = 0;

            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Publish")
                .Add(component => component.Variant, ButtonVariant.Danger)
                .Add(component => component.Size, ButtonSize.Large)
                .Add(component => component.Items, CreateItems())
                .Add(component => component.PrimaryAction, () => primaryActionCount++));

            component.Find("button").Click();

            var root = component.Find(".mb-split-button");
            var primaryButton = component.Find(".mb-split-button__primary");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(primaryActionCount, Is.EqualTo(1));
                Assert.That(root.GetAttribute("class"), Does.Contain("mb-split-button--danger"));
                Assert.That(root.GetAttribute("class"), Does.Contain("mb-split-button--large"));
                Assert.That(primaryButton.GetAttribute("class"), Does.Contain("mb-button--danger"));
                Assert.That(primaryButton.GetAttribute("class"), Does.Contain("mb-button--large"));
            }
        }

        /// <summary>
        /// Verifies additional supported variant and size combinations.
        /// </summary>
        /// <param name="variant">The configured button variant.</param>
        /// <param name="size">The configured button size.</param>
        /// <param name="variantClass">The expected split-button variant class.</param>
        /// <param name="sizeClass">The expected split-button size class.</param>
        [TestCase(ButtonVariant.Secondary, ButtonSize.Small, "mb-split-button--secondary", "mb-split-button--small")]
        [TestCase(ButtonVariant.Ghost, ButtonSize.Medium, "mb-split-button--ghost", "mb-split-button--medium")]
        public void VerifyAdditionalAppearances(
            ButtonVariant variant,
            ButtonSize size,
            string variantClass,
            string sizeClass)
        {
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Publish")
                .Add(component => component.Variant, variant)
                .Add(component => component.Size, size)
                .Add(component => component.Items, CreateItems()));

            var rootClass = component.Find(".mb-split-button").GetAttribute("class");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootClass, Does.Contain(variantClass));
                Assert.That(rootClass, Does.Contain(sizeClass));
            }
        }

        /// <summary>
        /// Verifies that the secondary trigger opens ActionMenu and forwards the selected action.
        /// </summary>
        [Test]
        public void VerifySecondaryActionIsForwarded()
        {
            ActionMenuItem selectedItem = null;
            var items = CreateItems();

            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Publish")
                .Add(component => component.Items, items)
                .Add(component => component.ItemSelected, item => selectedItem = item));

            component.FindAll("button")[1].Click();
            component.FindAll("[role='menuitem']")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedItem, Is.SameAs(items[1]));
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that disabled state prevents both primary and secondary actions.
        /// </summary>
        [Test]
        public void VerifyDisabledStatePreventsActions()
        {
            var primaryActionCount = 0;
            var selectionCount = 0;

            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Publish")
                .Add(component => component.Disabled, true)
                .Add(component => component.Items, CreateItems())
                .Add(component => component.PrimaryAction, () => primaryActionCount++)
                .Add(component => component.ItemSelected, _ => selectionCount++));

            var buttons = component.FindAll("button");
            buttons[0].Click();
            buttons[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons[0].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].HasAttribute("disabled"), Is.True);
                Assert.That(primaryActionCount, Is.Zero);
                Assert.That(selectionCount, Is.Zero);
                Assert.That(component.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that external loading state disables both actions and renders progress.
        /// </summary>
        [Test]
        public void VerifyLoadingStateDisablesActions()
        {
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Publishing")
                .Add(component => component.IsLoading, true)
                .Add(component => component.Items, CreateItems()));

            var buttons = component.FindAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons[0].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].HasAttribute("disabled"), Is.True);
                Assert.That(component.FindAll(".mb-button__spinner"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that repeated primary actions are ignored while the callback is pending.
        /// </summary>
        [Test]
        public async Task VerifyPendingPrimaryActionIgnoresRepeatedActions()
        {
            var primaryActionCount = 0;
            var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Publish")
                .Add(component => component.Items, CreateItems())
                .Add(component => component.PrimaryAction, async () =>
                {
                    primaryActionCount++;
                    callbackStarted.TrySetResult();
                    await releaseCallback.Task;
                }));

            var primaryButton = component.FindComponent<ButtonComponent>();
            var primaryAction = primaryButton.Instance.OnClick;
            var firstAction = component.InvokeAsync(() => primaryAction.InvokeAsync(new MouseEventArgs()));

            await callbackStarted.Task;

            primaryButton = component.FindComponent<ButtonComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(primaryButton.Instance.Disabled, Is.True);
                Assert.That(primaryButton.Instance.IsLoading, Is.True);
                Assert.That(component.FindAll("button")[1].HasAttribute("disabled"), Is.True);
            }

            var repeatedAction = component.InvokeAsync(() => primaryAction.InvokeAsync(new MouseEventArgs()));

            releaseCallback.SetResult();

            await Task.WhenAll(firstAction, repeatedAction);

            Assert.That(primaryActionCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that separate split-button instances do not share menu state.
        /// </summary>
        [Test]
        public void VerifyInstancesMaintainIndependentMenuState()
        {
            var first = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Publish")
                .Add(component => component.Items, CreateItems()));
            var second = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(component => component.PrimaryText, "Export")
                .Add(component => component.Items, CreateItems()));

            first.FindAll("button")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.FindAll("[role='menu']"), Has.Count.EqualTo(1));
                Assert.That(second.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Creates standard secondary actions.
        /// </summary>
        /// <returns>The secondary actions.</returns>
        private static ActionMenuItem[] CreateItems()
        {
            return
            [
                new ActionMenuItem { Id = "publish-copy", Label = "Publish copy" },
                new ActionMenuItem { Id = "publish-draft", Label = "Publish draft" }
            ];
        }
    }
}
