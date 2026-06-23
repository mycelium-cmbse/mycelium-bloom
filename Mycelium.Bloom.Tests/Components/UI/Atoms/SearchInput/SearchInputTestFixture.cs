namespace Mycelium.Bloom.Tests.Components.UI.Atoms.SearchInput
{
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using SearchInputComponent = Mycelium.Bloom.Components.UI.Atoms.SearchInput.SearchInput;

    /// <summary>
    /// Tests the <see cref="SearchInputComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SearchInputTestFixture : BunitContext
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
        /// Verifies that the search input displays configured state, classes, and attributes.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredSearchInput()
        {
            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Id, "search-box")
                .Add(component => component.Value, "query")
                .Add(component => component.Placeholder, "Find node")
                .Add(component => component.ShortcutText, "Ctrl F")
                .Add(component => component.FullWidth, true)
                .Add(component => component.Disabled, true)
                .Add(component => component.Class, "custom-search")
                .AddUnmatched("data-testid", "search-input"));

            var label = component.Find("label");
            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(label.GetAttribute("class"), Does.Contain("mb-search-input--full-width"));
                Assert.That(label.GetAttribute("class"), Does.Contain("mb-search-input--disabled"));
                Assert.That(label.GetAttribute("class"), Does.Contain("custom-search"));
                Assert.That(input.GetAttribute("id"), Is.EqualTo("search-box"));
                Assert.That(input.GetAttribute("type"), Is.EqualTo("search"));
                Assert.That(input.GetAttribute("value"), Is.EqualTo("query"));
                Assert.That(input.GetAttribute("placeholder"), Is.EqualTo("Find node"));
                Assert.That(input.GetAttribute("data-testid"), Is.EqualTo("search-input"));
                Assert.That(input.HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-search-input__shortcut").TextContent.Trim(), Is.EqualTo("Ctrl F"));
                Assert.That(component.Find(".mb-search-input__icon svg"), Is.Not.Null);
            }
        }

        /// <summary>
        /// Verifies that the search input renders a custom icon and hides the shortcut when configured.
        /// </summary>
        [Test]
        public void Render_DisplaysCustomIconAndHidesShortcut()
        {
            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.ShowShortcut, false)
                .Add(component => component.StartIcon, "<span>Custom icon</span>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-search-input__icon").TextContent.Trim(), Is.EqualTo("Custom icon"));
                Assert.That(component.FindAll(".mb-search-input__shortcut"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that typing in the search input updates the value and invokes the value change callback.
        /// </summary>
        [Test]
        public void Input_UpdatesValueAndInvokesValueChanged()
        {
            var changedValue = string.Empty;

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Value, "old")
                .Add(component => component.ValueChanged, (string value) => changedValue = value));

            component.Find("input").Input("new query");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedValue, Is.EqualTo("new query"));
                Assert.That(component.Find("input").GetAttribute("value"), Is.EqualTo("new query"));
            }
        }

        /// <summary>
        /// Verifies that key down events are forwarded to the configured callback.
        /// </summary>
        [Test]
        public void KeyDown_InvokesOnKeyDown()
        {
            var capturedKey = string.Empty;

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.OnKeyDown, (KeyboardEventArgs args) => capturedKey = args.Key));

            component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.That(capturedKey, Is.EqualTo("Enter"));
        }

        /// <summary>
        /// Verifies that the search shortcut is registered and disposed through JavaScript interop.
        /// </summary>
        [Test]
        public async Task Render_EnableShortcutRegistersAndDisposesShortcut()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", "search-box");
            var disposeHandler = module.SetupVoid("disposeSearchShortcut");

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Id, "search-box")
                .Add(component => component.EnableShortcut, true));

            await component.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registerHandler.Invocations.Count, Is.EqualTo(1));
                Assert.That(disposeHandler.Invocations.Count, Is.EqualTo(1));
            }
        }
    }
}
