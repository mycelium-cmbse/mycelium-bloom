// ------------------------------------------------------------------------------------------------
// <copyright file="HomeTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

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
            object shortcutOptions = null;

            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid(
                "registerSearchShortcut",
                invocation =>
                {
                    if (invocation.Arguments.Count != 2 || !Equals(invocation.Arguments[0], "global-search"))
                    {
                        return false;
                    }

                    shortcutOptions = invocation.Arguments[1];

                    return true;
                });

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
                Assert.That(GetPropertyValue(shortcutOptions, "key"), Is.EqualTo("k"));
                Assert.That(GetPropertyValue(shortcutOptions, "requiresControlOrMeta"), Is.True);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresAlt"), Is.False);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresShift"), Is.False);
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
