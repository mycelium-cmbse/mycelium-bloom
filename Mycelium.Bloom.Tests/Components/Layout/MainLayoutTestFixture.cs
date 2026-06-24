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
    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Layout;

    /// <summary>
    /// Tests the <see cref="MainLayout" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class MainLayoutTestFixture : BunitContext
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
        /// Verifies that the main layout displays navigation, body content, and the error UI.
        /// </summary>
        [Test]
        public void Render_DisplaysMainLayoutContent()
        {
            RenderFragment body = builder => { builder.AddContent(0, "Workspace content"); };

            var component = this.Render<MainLayout>(parameters => parameters
                .Add(component => component.Body, body));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("article").TextContent.Trim(), Is.EqualTo("Workspace content"));
                Assert.That(component.Find("a[href='https://learn.microsoft.com/aspnet/core/']").TextContent.Trim(), Is.EqualTo("About"));
                Assert.That(component.Find("#blazor-error-ui").TextContent, Does.Contain("An unhandled error has occurred."));
            }
        }
    }
}
