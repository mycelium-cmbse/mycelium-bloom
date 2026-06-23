namespace Mycelium.Bloom.Tests.Components.Pages
{
    using Bunit;

    using Mycelium.Bloom.Components.Pages;

    /// <summary>
    /// Tests the <see cref="Home" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class HomeTestFixture : BunitContext
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
        /// Verifies that the home page displays the expected workspace content.
        /// </summary>
        [Test]
        public void Render_DisplaysHomeContent()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", "global-search");
            var disposeHandler = module.SetupVoid("disposeSearchShortcut");

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var component = this.Render<Home>();

            component.Find("input").Input("model");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("h1").TextContent.Trim(), Is.EqualTo("Welcome To Mycelium Bloom"));
                Assert.That(component.Find("input").GetAttribute("id"), Is.EqualTo("global-search"));
                Assert.That(component.Markup, Does.Contain("Search value: model"));
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));
            }
        }
    }
}
