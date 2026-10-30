// ------------------------------------------------------------------------------------------------
// <copyright file="PageHeaderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.PageHeader
{
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Molecules.PageHeader;

    /// <summary>
    /// Tests the <see cref="PageHeader" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class PageHeaderTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        /// <returns>A task that completes when the context is disposed.</returns>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that the subtitle and the actions are rendered only when they are provided.
        /// </summary>
        /// <param name="subtitle">The subtitle of the page.</param>
        /// <param name="withActions">A value indicating whether the page has actions.</param>
        [TestCase("12 projects", true)]
        [TestCase("   ", false)]
        public void VerifyOptionalPartsAreRenderedOnlyWhenProvided(string subtitle, bool withActions)
        {
            RenderFragment actions = builder => builder.AddContent(0, "New project");

            var component = this.Render<PageHeader>(parameters =>
            {
                parameters.Add(header => header.Title, "Projects").Add(header => header.Subtitle, subtitle);

                if (withActions)
                {
                    parameters.Add(header => header.ActionsContent, actions);
                }
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("p"), Has.Count.EqualTo(string.IsNullOrWhiteSpace(subtitle) ? 0 : 1));
                Assert.That(component.FindAll(".mb-page-header__actions"), Has.Count.EqualTo(withActions ? 1 : 0));
            }
        }
    }
}
