// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceLayoutTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Layout
{
    using System.IO;
    using System.Threading.Tasks;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Tests the <see cref="WorkspaceLayout" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class WorkspaceLayoutTestFixture : BunitContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceLayoutTestFixture" /> class.
        /// </summary>
        public WorkspaceLayoutTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies the route layout adds no main landmark and owns one portal/error infrastructure set.
        /// </summary>
        [Test]
        public void VerifyRenderOwnsExactlyOneInfrastructureSetWithoutMainLandmark()
        {
            RenderFragment body = builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddAttribute(1, "role", "main");
                builder.AddContent(2, "Workspace content");
                builder.CloseElement();
            };
            using var component = this.Render<WorkspaceLayout>(parameters => parameters
                .Add(layout => layout.Body, body));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-workspace-layout").TextContent.Trim(),
                    Is.EqualTo("Workspace content"));
                Assert.That(component.FindAll("main"), Is.Empty);
                Assert.That(component.FindAll("[role='main']"), Has.Count.EqualTo(1));
                Assert.That(component.FindComponents<BbPortalHost>(), Has.Count.EqualTo(1));
                Assert.That(component.FindAll("#blazor-error-ui"), Has.Count.EqualTo(1));
                Assert.That(component.Find("#blazor-error-ui button.dismiss").GetAttribute("type"),
                    Is.EqualTo("button"));
                Assert.That(component.Find("#blazor-error-ui button.dismiss").GetAttribute("aria-label"),
                    Is.EqualTo("Dismiss error notification"));
            }
        }

        /// <summary>
        /// Verifies the route layout provides an unpadded, overflow-bounded application viewport.
        /// </summary>
        [Test]
        public void VerifyStyleOwnsFullViewportWithoutCardPresentation()
        {
            var style = File.ReadAllText(Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                "Components",
                "Layout",
                "WorkspaceLayout.razor.css"));
            var rootRule = style[..(style.IndexOf('}') + 1)];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rootRule, Does.Contain("height: 100dvh;"));
                Assert.That(rootRule, Does.Contain("overflow: hidden;"));
                Assert.That(rootRule, Does.Not.Contain("border-radius"));
                Assert.That(rootRule, Does.Not.Contain("margin:"));
                Assert.That(rootRule, Does.Not.Contain("padding:"));
            }
        }
    }
}
