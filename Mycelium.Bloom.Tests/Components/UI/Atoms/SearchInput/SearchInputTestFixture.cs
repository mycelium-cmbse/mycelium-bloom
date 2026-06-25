// // ------------------------------------------------------------------------------------------------
// // <copyright file="SearchInputTestFixture.cs" company="Starion Group S.A.">
// //
// //   Copyright 2026 Starion Group S.A.
// //   SPDX-License-Identifier: Apache-2.0
// //
// // </copyright>
// // ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.SearchInput
{
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

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
        /// Verifies that synchronous disposal completes without JavaScript interop.
        /// </summary>
        [Test]
        public void Dispose_CompletesSynchronousDisposal()
        {
            var component = new SearchInputComponent();

            Assert.That(component.Dispose, Throws.Nothing);
        }

        /// <summary>
        /// Verifies that JavaScript disconnection during shortcut disposal is ignored.
        /// </summary>
        [Test]
        public async Task DisposeAsync_IgnoresDisconnectedJavaScriptRuntime()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", invocation => true);
            var disposeHandler = module.SetupVoid("disposeSearchShortcut");

            registerHandler.SetVoidResult();
            disposeHandler.SetException(new JSDisconnectedException("Disconnected"));

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Id, "search-box")
                .Add(component => component.EnableShortcut, true));

            await component.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(disposeHandler.Invocations, Has.Count.EqualTo(1));
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
                .Add(component => component.ValueChanged, value => changedValue = value));

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
                .Add(component => component.OnKeyDown, args => capturedKey = args.Key));

            component.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.That(capturedKey, Is.EqualTo("Enter"));
        }

        /// <summary>
        /// Verifies that the default shortcut key is registered when a blank shortcut key is configured.
        /// </summary>
        [Test]
        public async Task Render_BlankShortcutKeyRegistersDefaultShortcutKey()
        {
            object shortcutOptions = null;

            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");

            var registerHandler = module.SetupVoid(
                "registerSearchShortcut",
                invocation =>
                {
                    if (invocation.Arguments.Count != 2 || !Equals(invocation.Arguments[0], "search-box"))
                    {
                        return false;
                    }

                    shortcutOptions = invocation.Arguments[1];

                    return true;
                });

            var disposeHandler = module.SetupVoid("disposeSearchShortcut");

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Id, "search-box")
                .Add(component => component.EnableShortcut, true)
                .Add(component => component.ShortcutKey, " "));

            await component.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(disposeHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(GetPropertyValue(shortcutOptions, "key"), Is.EqualTo("k"));
            }
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
        /// Verifies that the search shortcut is registered and disposed through JavaScript interop.
        /// </summary>
        [Test]
        public async Task Render_EnableShortcutRegistersAndDisposesShortcut()
        {
            object shortcutOptions = null;

            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");

            var registerHandler = module.SetupVoid(
                "registerSearchShortcut",
                invocation =>
                {
                    if (invocation.Arguments.Count != 2 || !Equals(invocation.Arguments[0], "search-box"))
                    {
                        return false;
                    }

                    shortcutOptions = invocation.Arguments[1];

                    return true;
                });

            var disposeHandler = module.SetupVoid("disposeSearchShortcut");

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Id, "search-box")
                .Add(component => component.EnableShortcut, true)
                .Add(component => component.ShortcutKey, "/")
                .Add(component => component.ShortcutRequiresControlOrMeta, false)
                .Add(component => component.ShortcutRequiresAlt, true)
                .Add(component => component.ShortcutRequiresShift, true));

            await component.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(disposeHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(GetPropertyValue(shortcutOptions, "key"), Is.EqualTo("/"));
                Assert.That(GetPropertyValue(shortcutOptions, "requiresControlOrMeta"), Is.False);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresAlt"), Is.True);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresShift"), Is.True);
            }
        }

        /// <summary>
        /// Gets a property value from an object passed to JavaScript interop.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The property value.</returns>
        private static object GetPropertyValue(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName);

            return property!.GetValue(instance);
        }
    }
}
