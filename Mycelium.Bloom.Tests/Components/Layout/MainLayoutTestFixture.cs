// ------------------------------------------------------------------------------------------------
// <copyright file="MainLayoutTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Layout
{
    using System.IO;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Tests the <see cref="MainLayout" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class MainLayoutTestFixture : BunitContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainLayoutTestFixture" /> class.
        /// </summary>
        public MainLayoutTestFixture()
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
        /// Verifies that the main layout displays navigation, body content, and the error UI.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysMainLayoutContent()
        {
            RenderFragment body = builder => { builder.AddContent(0, "Workspace content"); };

            var component = this.Render<MainLayout>(parameters => parameters
                .Add(component => component.Body, body));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("article").TextContent.Trim(), Is.EqualTo("Workspace content"));
                Assert.That(component.Find("a[href='design-system'] span:last-child").TextContent, Is.EqualTo("Design System"));
                Assert.That(component.Find("a[href='https://learn.microsoft.com/aspnet/core/']").TextContent.Trim(), Is.EqualTo("About"));
                Assert.That(component.Find("#blazor-error-ui").TextContent, Does.Contain("An unhandled error has occurred."));
                Assert.That(component.FindAll("nav[aria-label='Primary navigation'] a.mb-nav-menu__link"),
                    Has.Count.EqualTo(3));
                Assert.That(component.Find(".mb-nav-menu__brand-link img[data-brand='bloom']").GetAttribute("alt"), Is.Empty);
                Assert.That(component.Find(".mb-nav-menu__toggle").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            }

            component.Find(".mb-nav-menu__toggle").Click();

            Assert.That(component.Find("nav[aria-label='Primary navigation']").ClassList,
                Does.Contain("mb-nav-menu__links--expanded"));
        }

        /// <summary>
        /// Verifies that the runtime error notification exposes a named keyboard-operable dismiss button with visible focus styling.
        /// </summary>
        [Test]
        public void VerifyErrorDismissControlIsSemanticAndNamed()
        {
            var component = this.Render<MainLayout>();
            var dismissButton = component.Find("#blazor-error-ui button.dismiss");
            var style = File.ReadAllText(Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "Layout",
                "MainLayout.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dismissButton.GetAttribute("type"), Is.EqualTo("button"));
                Assert.That(dismissButton.GetAttribute("aria-label"), Is.EqualTo("Dismiss error notification"));
                Assert.That(component.FindAll("#blazor-error-ui span.dismiss"), Is.Empty);
                Assert.That(style, Does.Contain(".mb-main-layout__error .dismiss:focus-visible"));
                Assert.That(style, Does.Contain("outline: 2px solid currentColor;"));
            }
        }
    }
}
