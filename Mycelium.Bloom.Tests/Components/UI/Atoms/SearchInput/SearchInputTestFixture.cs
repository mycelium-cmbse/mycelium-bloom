// ------------------------------------------------------------------------------------------------
// <copyright file="SearchInputTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.SearchInput
{
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;

    using Bunit;

    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Tests.Common;

    using SearchInputComponent = Mycelium.Bloom.Components.UI.Atoms.SearchInput.SearchInput;

    /// <summary>
    /// Tests the <see cref="SearchInputComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class SearchInputTestFixture : BunitContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchInputTestFixture" /> class.
        /// </summary>
        public SearchInputTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public System.Threading.Tasks.Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that synchronous disposal completes without JavaScript interop.
        /// </summary>
        [Test]
        public void VerifyDisposeCompletesSynchronousDisposal()
        {
            var component = new SearchInputComponent();

            Assert.That(component.Dispose, Throws.Nothing);
        }

        /// <summary>
        /// Verifies that JavaScript disconnection during shortcut disposal is ignored.
        /// </summary>
        [Test]
        public async Task VerifyDisposeAsyncIgnoresDisconnectedJavaScriptRuntime()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", invocation => true);
            var disposeHandler = module.SetupVoid("disposeSearchShortcut", invocation => true);

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
        public async Task VerifyInputUpdatesValueAndInvokesValueChanged()
        {
            var changedValue = string.Empty;

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Value, "old")
                .Add(component => component.ValueChanged, value => changedValue = value));

            var blueprintInput = component.FindComponent<BbInputGroupInput>();

            await component.InvokeAsync(() => blueprintInput.Instance.JsOnInput("new query"));

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
        public async Task VerifyKeyDownInvokesOnKeyDown()
        {
            var capturedKey = string.Empty;

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.OnKeyDown, args => capturedKey = args.Key));

            await component.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

            Assert.That(capturedKey, Is.EqualTo("Enter"));
        }

        /// <summary>
        /// Verifies that the default shortcut key is registered when a blank shortcut key is configured.
        /// </summary>
        [Test]
        public async Task VerifyBlankShortcutKeyRegistersDefaultShortcutKey()
        {
            object shortcutOptions = null;

            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");

            var registerHandler = module.SetupVoid(
                "registerSearchShortcut",
                invocation =>
                {
                    if (invocation.Arguments.Count != 3 || !Equals(invocation.Arguments[1], "search-box"))
                    {
                        return false;
                    }

                    shortcutOptions = invocation.Arguments[2];

                    return true;
                });

            var disposeHandler = module.SetupVoid("disposeSearchShortcut", invocation => true);

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
        public void VerifyRenderDisplaysConfiguredSearchInput()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", invocation => true);
            var disposeHandler = module.SetupVoid("disposeSearchShortcut", invocation => true);

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(component => component.Id, "search-box")
                .Add(component => component.Value, "query")
                .Add(component => component.Placeholder, "Find node")
                .Add(component => component.AriaLabel, "Search model elements")
                .Add(component => component.ShortcutText, "Ctrl F")
                .Add(component => component.EnableShortcut, true)
                .Add(component => component.FullWidth, true)
                .Add(component => component.Disabled, true)
                .Add(component => component.Class, "custom-search")
                .AddUnmatched("data-testid", "search-input"));

            var group = component.Find(".mb-search-input");
            var input = component.Find("input");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(group.GetAttribute("class"), Does.Contain("mb-search-input--full-width"));
                Assert.That(group.GetAttribute("class"), Does.Contain("mb-search-input--disabled"));
                Assert.That(group.GetAttribute("class"), Does.Contain("custom-search"));
                Assert.That(input.GetAttribute("id"), Is.EqualTo("search-box"));
                Assert.That(input.GetAttribute("type"), Is.EqualTo("search"));
                Assert.That(input.GetAttribute("value"), Is.EqualTo("query"));
                Assert.That(input.GetAttribute("placeholder"), Is.EqualTo("Find node"));
                Assert.That(input.GetAttribute("aria-label"), Is.EqualTo("Search model elements"));
                Assert.That(input.GetAttribute("data-testid"), Is.EqualTo("search-input"));
                Assert.That(input.HasAttribute("disabled"), Is.True);
                Assert.That(component.Find(".mb-search-input__shortcut").TextContent.Trim(), Is.EqualTo("Ctrl F"));
                Assert.That(component.Find(".mb-search-input__shortcut").GetAttribute("aria-hidden"), Is.EqualTo("true"));
                Assert.That(component.Find(".mb-search-input__icon svg"), Is.Not.Null);
            }
        }

        /// <summary>
        /// Verifies that enabling the default shortcut renders its visual hint and registers the rendered input.
        /// </summary>
        [Test]
        public void VerifyEnabledShortcutRendersDefaultHintAndRegistersRenderedId()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", invocation => true);
            var disposeHandler = module.SetupVoid("disposeSearchShortcut", invocation => true);

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(searchInput => searchInput.Id, "shortcut-search")
                .Add(searchInput => searchInput.EnableShortcut, true));

            var shortcut = component.Find("kbd");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shortcut.TextContent.Trim(), Is.EqualTo("Ctrl K"));
                Assert.That(shortcut.GetAttribute("aria-hidden"), Is.EqualTo("true"));
                Assert.That(component.Find(".mb-search-input").GetAttribute("class"),
                    Does.Contain("mb-search-input--with-shortcut"));
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(registerHandler.Invocations["registerSearchShortcut"][0].Arguments[1],
                    Is.EqualTo(component.Find("input").Id));
                Assert.That(
                    this.JSInterop.Invocations["import"].Count(invocation =>
                        Equals(invocation.Arguments[0], "./Components/UI/Atoms/SearchInput/SearchInput.razor.js")),
                    Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Verifies that a disabled shortcut neither renders a hint nor imports its JavaScript module.
        /// </summary>
        [Test]
        public void VerifyDisabledShortcutDoesNotRenderOrRegister()
        {
            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(searchInput => searchInput.ShowShortcut, true)
                .Add(searchInput => searchInput.EnableShortcut, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("kbd"), Is.Empty);
                Assert.That(component.Find(".mb-search-input").GetAttribute("class"),
                    Does.Not.Contain("mb-search-input--with-shortcut"));
                Assert.That(
                    this.JSInterop.Invocations["import"].Count(invocation =>
                        Equals(invocation.Arguments[0], "./Components/UI/Atoms/SearchInput/SearchInput.razor.js")),
                    Is.Zero);
            }
        }

        /// <summary>
        /// Verifies that the search input renders a custom icon and hides the shortcut when configured.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCustomIconAndHidesShortcut()
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
        public async Task VerifyEnableShortcutRegistersAndDisposesShortcut()
        {
            object shortcutOptions = null;

            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");

            var registerHandler = module.SetupVoid(
                "registerSearchShortcut",
                invocation =>
                {
                    if (invocation.Arguments.Count != 3 || !Equals(invocation.Arguments[1], "search-box"))
                    {
                        return false;
                    }

                    shortcutOptions = invocation.Arguments[2];

                    return true;
                });

            var disposeHandler = module.SetupVoid("disposeSearchShortcut", invocation => true);

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
                Assert.That(disposeHandler.Invocations["disposeSearchShortcut"][0].Arguments[0],
                    Is.EqualTo(registerHandler.Invocations["registerSearchShortcut"][0].Arguments[0]));
                Assert.That(GetPropertyValue(shortcutOptions, "key"), Is.EqualTo("/"));
                Assert.That(GetPropertyValue(shortcutOptions, "requiresControlOrMeta"), Is.False);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresAlt"), Is.True);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresShift"), Is.True);
            }
        }

        /// <summary>
        /// Verifies that each component owns an independent shortcut registration and disposal token.
        /// </summary>
        [Test]
        public async Task VerifyMultipleInstancesDisposeOnlyTheirOwnShortcutRegistration()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", invocation => true);
            var disposeHandler = module.SetupVoid("disposeSearchShortcut", invocation => true);

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var firstComponent = this.Render<SearchInputComponent>(parameters => parameters
                .Add(searchInput => searchInput.Id, "first-search")
                .Add(searchInput => searchInput.EnableShortcut, true));
            var secondComponent = this.Render<SearchInputComponent>(parameters => parameters
                .Add(searchInput => searchInput.Id, "second-search")
                .Add(searchInput => searchInput.EnableShortcut, true));

            var firstRegistrationId = registerHandler.Invocations["registerSearchShortcut"][0].Arguments[0];
            var secondRegistrationId = registerHandler.Invocations["registerSearchShortcut"][1].Arguments[0];

            await firstComponent.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(2));
                Assert.That(firstRegistrationId, Is.Not.EqualTo(secondRegistrationId));
                Assert.That(disposeHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(disposeHandler.Invocations["disposeSearchShortcut"][0].Arguments[0],
                    Is.EqualTo(firstRegistrationId));
            }

            await secondComponent.Instance.DisposeAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposeHandler.Invocations, Has.Count.EqualTo(2));
                Assert.That(disposeHandler.Invocations["disposeSearchShortcut"][1].Arguments[0],
                    Is.EqualTo(secondRegistrationId));
            }
        }

        /// <summary>
        /// Verifies that rerenders retain one registration and changed identifiers refresh that registration safely.
        /// </summary>
        [Test]
        public async Task VerifyRerenderRefreshesOnlyChangedShortcutRegistration()
        {
            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid("registerSearchShortcut", invocation => true);
            var disposeHandler = module.SetupVoid("disposeSearchShortcut", invocation => true);

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var component = this.Render<SearchInputComponent>(parameters => parameters
                .Add(searchInput => searchInput.Id, "initial-search")
                .Add(searchInput => searchInput.EnableShortcut, true));

            component.Render(parameters => parameters
                .Add(searchInput => searchInput.Id, "initial-search"));

            Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));

            component.Render(parameters => parameters
                .Add(searchInput => searchInput.Id, "renamed-search"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(2));
                Assert.That(registerHandler.Invocations["registerSearchShortcut"][0].Arguments[0],
                    Is.EqualTo(registerHandler.Invocations["registerSearchShortcut"][1].Arguments[0]));
                Assert.That(registerHandler.Invocations["registerSearchShortcut"][0].Arguments[1],
                    Is.EqualTo("initial-search"));
                Assert.That(registerHandler.Invocations["registerSearchShortcut"][1].Arguments[1],
                    Is.EqualTo("renamed-search"));
            }

            component.Render(parameters => parameters
                .Add(searchInput => searchInput.EnableShortcut, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposeHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(disposeHandler.Invocations["disposeSearchShortcut"][0].Arguments[0],
                    Is.EqualTo(registerHandler.Invocations["registerSearchShortcut"][0].Arguments[0]));
            }

            await component.Instance.DisposeAsync();

            Assert.That(disposeHandler.Invocations, Has.Count.EqualTo(1));
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
