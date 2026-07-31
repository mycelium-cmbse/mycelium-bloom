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

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using BlueprintButton = BlazorBlueprint.Components.BbButton;
    using BlueprintButtonSize = BlazorBlueprint.Components.ButtonSize;
    using BlueprintButtonVariant = BlazorBlueprint.Components.ButtonVariant;
    using SplitButtonComponent = Mycelium.Bloom.Components.UI.Molecules.SplitButton.SplitButton;

    /// <summary>
    /// Tests Bloom's split-action contract composed from styled Blueprint controls.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SplitButtonTestFixture : BunitContext
    {
        private readonly IRenderedComponent<BbPortalHost> portalHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitButtonTestFixture" /> class.
        /// </summary>
        public SplitButtonTestFixture()
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
        /// Verifies the styled primary action uses a safe button type and invokes its callback once.
        /// </summary>
        [Test]
        public async Task VerifyPrimaryActionIsForwarded()
        {
            var actionCount = 0;
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Publish")
                .Add(split => split.Items, CreateItems())
                .Add(split => split.PrimaryAction, () => actionCount++));

            var primary = component.Find(".mb-split-button__primary");
            await primary.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actionCount, Is.EqualTo(1));
                Assert.That(primary.GetAttribute("type"), Is.EqualTo("button"));
                Assert.That(component.FindComponent<BlueprintButton>().Instance.Loading, Is.False);
            }
        }

        /// <summary>
        /// Verifies Bloom variants and compact sizes map to public styled Button parameters.
        /// </summary>
        /// <param name="variant">The Bloom variant.</param>
        /// <param name="size">The Bloom size.</param>
        /// <param name="expectedVariant">The styled Blueprint variant.</param>
        /// <param name="expectedSize">The styled Blueprint size.</param>
        [TestCase(ButtonVariant.Primary, ButtonSize.Small, BlueprintButtonVariant.Default, BlueprintButtonSize.Small)]
        [TestCase(ButtonVariant.Secondary, ButtonSize.Medium, BlueprintButtonVariant.Outline, BlueprintButtonSize.Small)]
        [TestCase(ButtonVariant.Ghost, ButtonSize.Large, BlueprintButtonVariant.Ghost, BlueprintButtonSize.Default)]
        [TestCase(ButtonVariant.Danger, ButtonSize.Medium, BlueprintButtonVariant.Destructive, BlueprintButtonSize.Small)]
        public void VerifyVariantAndSizeMapToStyledButton(
            ButtonVariant variant,
            ButtonSize size,
            BlueprintButtonVariant expectedVariant,
            BlueprintButtonSize expectedSize)
        {
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Publish")
                .Add(split => split.Variant, variant)
                .Add(split => split.Size, size)
                .Add(split => split.Items, CreateItems()));

            var primary = component.FindComponent<BlueprintButton>().Instance;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(primary.Variant, Is.EqualTo(expectedVariant));
                Assert.That(primary.Size, Is.EqualTo(expectedSize));
            }
        }

        /// <summary>
        /// Verifies the styled secondary menu forwards an enabled action and closes.
        /// </summary>
        [Test]
        public async Task VerifySecondaryActionIsForwarded()
        {
            ActionMenuItem selectedItem = null;
            var callbackCount = 0;
            var items = CreateItems();
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Publish")
                .Add(split => split.Items, items)
                .Add(split => split.ItemSelected, item =>
                {
                    selectedItem = item;
                    callbackCount++;
                }));

            var toggle = component.Find(".mb-split-button__toggle");
            await toggle.ClickAsync();
            await this.portalHost.WaitForElements("[role='menuitem']", items.Length)[1].ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedItem, Is.SameAs(items[1]));
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(toggle.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies an unavailable secondary item cannot invoke the application callback.
        /// </summary>
        [Test]
        public async Task VerifyDisabledSecondaryActionIsIgnored()
        {
            var selectionCount = 0;
            var items = CreateItems();
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Publish")
                .Add(split => split.Items, items)
                .Add(split => split.ItemSelected, _ => selectionCount++));

            await component.Find(".mb-split-button__toggle").ClickAsync();
            var disabledItem = this.portalHost.WaitForElements("[role='menuitem']", items.Length)[2];
            await disabledItem.ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disabledItem.GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(selectionCount, Is.Zero);
            }
        }

        /// <summary>
        /// Verifies disabled state prevents both primary and secondary actions.
        /// </summary>
        [Test]
        public async Task VerifyDisabledStatePreventsActions()
        {
            var primaryActionCount = 0;
            var selectionCount = 0;
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Publish")
                .Add(split => split.Disabled, true)
                .Add(split => split.Items, CreateItems())
                .Add(split => split.PrimaryAction, () => primaryActionCount++)
                .Add(split => split.ItemSelected, _ => selectionCount++));

            var buttons = component.FindAll("button");
            await buttons[0].ClickAsync();
            await buttons[1].ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons[0].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(primaryActionCount, Is.Zero);
                Assert.That(selectionCount, Is.Zero);
                Assert.That(this.portalHost.FindAll("[role='menu']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies external loading state uses the styled button progress treatment and disables both actions.
        /// </summary>
        [Test]
        public void VerifyLoadingStateDisablesActions()
        {
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Publishing")
                .Add(split => split.IsLoading, true)
                .Add(split => split.Items, CreateItems()));

            var buttons = component.FindAll("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttons[0].HasAttribute("disabled"), Is.True);
                Assert.That(buttons[1].GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(buttons[0].GetAttribute("aria-busy"), Is.EqualTo("true"));
                Assert.That(component.FindComponent<BlueprintButton>().Instance.Loading, Is.True);
                Assert.That(component.FindAll("svg.animate-spin"), Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies repeated primary actions are ignored while a callback is pending.
        /// </summary>
        [Test]
        public async Task VerifyPendingPrimaryActionIgnoresRepeatedActions()
        {
            var primaryActionCount = 0;
            var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var component = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Publish")
                .Add(split => split.Items, CreateItems())
                .Add(split => split.PrimaryAction, async () =>
                {
                    primaryActionCount++;
                    callbackStarted.TrySetResult();
                    await releaseCallback.Task;
                }));

            var firstAction = component.Find(".mb-split-button__primary").ClickAsync();
            await callbackStarted.Task;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-split-button__primary").HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-split-button__toggle").GetAttribute("aria-disabled"), Is.EqualTo("true"));
                Assert.That(component.FindAll("svg.animate-spin"), Has.Count.EqualTo(1));
            }

            await component.Find(".mb-split-button__primary").ClickAsync();
            releaseCallback.SetResult();
            await firstAction;

            Assert.That(primaryActionCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies independent instances own distinct menu relationships and callbacks.
        /// </summary>
        [Test]
        public async Task VerifyMultipleInstancesRemainIndependent()
        {
            var firstCount = 0;
            var secondCount = 0;
            var first = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "First")
                .Add(split => split.Items, CreateItems())
                .Add(split => split.PrimaryAction, () => firstCount++));
            var second = this.Render<SplitButtonComponent>(parameters => parameters
                .Add(split => split.PrimaryText, "Second")
                .Add(split => split.Items, CreateItems())
                .Add(split => split.PrimaryAction, () => secondCount++));
            var firstToggle = first.Find(".mb-split-button__toggle");
            var secondToggle = second.Find(".mb-split-button__toggle");

            await second.Find(".mb-split-button__primary").ClickAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstCount, Is.Zero);
                Assert.That(secondCount, Is.EqualTo(1));
                Assert.That(firstToggle.GetAttribute("aria-controls"), Is.Not.EqualTo(secondToggle.GetAttribute("aria-controls")));
            }
        }

        private static ActionMenuItem[] CreateItems()
        {
            return
            [
                new ActionMenuItem { Id = "publish-copy", Label = "Publish copy", Symbol = SymbolIconName.Copy },
                new ActionMenuItem { Id = "publish-draft", Label = "Publish draft", Symbol = SymbolIconName.Document },
                new ActionMenuItem { Id = "publish-protected", Label = "Publish protected copy", Disabled = true },
                new ActionMenuItem { Id = "discard", Label = "Discard", Symbol = SymbolIconName.Delete, Destructive = true, SeparatorBefore = true }
            ];
        }
    }
}
