namespace Mycelium.Bloom.Tests.Components.UI.Atoms.IconButton
{
    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using IconButtonComponent = Mycelium.Bloom.Components.UI.Atoms.IconButton.IconButton;

    /// <summary>
    /// Tests the <see cref="IconButtonComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class IconButtonTestFixture : BunitContext
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
        /// Verifies that the icon button displays configured content and accessibility attributes.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredIconButton()
        {
            var component = this.Render<IconButtonComponent>(parameters => parameters
                .Add(component => component.Type, "submit")
                .Add(component => component.AriaLabel, "Open command palette")
                .Add(component => component.Title, "Commands")
                .Add(component => component.Disabled, true)
                .Add(component => component.Class, "custom-icon-button")
                .Add(component => component.ChildContent, "<span>Icon</span>")
                .AddUnmatched("data-testid", "command-button"));

            var button = component.Find("button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(button.GetAttribute("type"), Is.EqualTo("submit"));
                Assert.That(button.GetAttribute("aria-label"), Is.EqualTo("Open command palette"));
                Assert.That(button.GetAttribute("title"), Is.EqualTo("Commands"));
                Assert.That(button.GetAttribute("data-testid"), Is.EqualTo("command-button"));
                Assert.That(button.GetAttribute("class"), Does.Contain("mb-icon-button"));
                Assert.That(button.GetAttribute("class"), Does.Contain("custom-icon-button"));
                Assert.That(button.HasAttribute("disabled"), Is.True);
                Assert.That(button.TextContent.Trim(), Is.EqualTo("Icon"));
            }
        }

        /// <summary>
        /// Verifies that clicking the icon button invokes the click callback.
        /// </summary>
        [Test]
        public void Click_InvokesOnClick()
        {
            var clickCount = 0;

            var component = this.Render<IconButtonComponent>(parameters => parameters
                .Add(component => component.AriaLabel, "Refresh")
                .Add(component => component.ChildContent, "<span>Refresh</span>")
                .Add(component => component.OnClick, (MouseEventArgs _) => clickCount++));

            component.Find("button").Click();

            Assert.That(clickCount, Is.EqualTo(1));
        }
    }
}
