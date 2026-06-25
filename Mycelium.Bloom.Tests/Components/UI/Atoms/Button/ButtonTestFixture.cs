// ------------------------------------------------------------------------------------------------
// <copyright file="ButtonTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.Button
{
    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model;

    using ButtonComponent = Mycelium.Bloom.Components.UI.Atoms.Button.Button;

    /// <summary>
    /// Tests the <see cref="ButtonComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ButtonTestFixture : BunitContext
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
        /// Verifies that the button displays configured content, classes, type, and attributes.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredButton()
        {
            var component = this.Render<ButtonComponent>(parameters => parameters
                .Add(component => component.Variant, ButtonVariant.Danger)
                .Add(component => component.Size, ButtonSize.Large)
                .Add(component => component.Type, "submit")
                .Add(component => component.FullWidth, true)
                .Add(component => component.Class, "custom-button")
                .Add(component => component.StartIcon, "<span>Start</span>")
                .Add(component => component.EndIcon, "<span>End</span>")
                .AddChildContent("Delete")
                .AddUnmatched("data-testid", "delete-button"));

            var button = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(button.GetAttribute("type"), Is.EqualTo("submit"));
                Assert.That(button.GetAttribute("data-testid"), Is.EqualTo("delete-button"));
                Assert.That(button.GetAttribute("class"), Does.Contain("mb-button--danger"));
                Assert.That(button.GetAttribute("class"), Does.Contain("mb-button--large"));
                Assert.That(button.GetAttribute("class"), Does.Contain("mb-button--full-width"));
                Assert.That(button.GetAttribute("class"), Does.Contain("custom-button"));
                Assert.That(component.Find(".mb-button__content").TextContent.Trim(), Is.EqualTo("Delete"));
                Assert.That(component.FindAll(".mb-button__icon"), Has.Count.EqualTo(2));
            }
        }

        /// <summary>
        /// Verifies that the button uses the expected variant class.
        /// </summary>
        /// <param name="variant">The button variant.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(ButtonVariant.Primary, "mb-button--primary")]
        [TestCase(ButtonVariant.Secondary, "mb-button--secondary")]
        [TestCase(ButtonVariant.Ghost, "mb-button--ghost")]
        [TestCase(ButtonVariant.Danger, "mb-button--danger")]
        public void Render_UsesExpectedVariantClass(ButtonVariant variant, string expectedCssClass)
        {
            var component = this.Render<ButtonComponent>(parameters => parameters
                .Add(component => component.Variant, variant)
                .AddChildContent("Action"));

            Assert.That(component.Find("button").GetAttribute("class"), Does.Contain(expectedCssClass));
        }

        /// <summary>
        /// Verifies that the button uses the expected size class.
        /// </summary>
        /// <param name="size">The button size.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(ButtonSize.Small, "mb-button--small")]
        [TestCase(ButtonSize.Medium, "mb-button--medium")]
        [TestCase(ButtonSize.Large, "mb-button--large")]
        public void Render_UsesExpectedSizeClass(ButtonSize size, string expectedCssClass)
        {
            var component = this.Render<ButtonComponent>(parameters => parameters
                .Add(component => component.Size, size)
                .AddChildContent("Action"));

            Assert.That(component.Find("button").GetAttribute("class"), Does.Contain(expectedCssClass));
        }

        /// <summary>
        /// Verifies that a loading button is disabled and displays the spinner instead of icons.
        /// </summary>
        [Test]
        public void Render_LoadingButtonDisplaysSpinnerAndDisabledState()
        {
            var component = this.Render<ButtonComponent>(parameters => parameters
                .Add(component => component.IsLoading, true)
                .Add(component => component.StartIcon, "<span>Start</span>")
                .Add(component => component.EndIcon, "<span>End</span>")
                .AddChildContent("Saving"));

            var button = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(button.HasAttribute("disabled"), Is.True);
                Assert.That(button.GetAttribute("class"), Does.Contain("mb-button--disabled"));
                Assert.That(component.FindAll(".mb-button__spinner"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".mb-button__icon"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that clicking the button invokes the click callback.
        /// </summary>
        [Test]
        public void Click_InvokesOnClick()
        {
            var clickCount = 0;

            var component = this.Render<ButtonComponent>(parameters => parameters
                .Add(component => component.OnClick, (MouseEventArgs _) => clickCount++)
                .AddChildContent("Save"));

            component.Find("button").Click();

            Assert.That(clickCount, Is.EqualTo(1));
        }
    }
}
