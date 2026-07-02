// ------------------------------------------------------------------------------------------------
// <copyright file="DetailPanelHeaderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.DetailPanelHeader
{
    using Bunit;

    using Microsoft.AspNetCore.Components.Web;

    using DetailPanelHeaderComponent = Mycelium.Bloom.Components.UI.Molecules.DetailPanelHeader.DetailPanelHeader;

    /// <summary>
    /// Tests the <see cref="DetailPanelHeaderComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DetailPanelHeaderTestFixture : BunitContext
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
        /// Verifies that configured header content is rendered and collapse invokes the callback.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredHeader()
        {
            var collapseCount = 0;

            var component = this.Render<DetailPanelHeaderComponent>(parameters => parameters
                .Add(component => component.Stereotype, "requirement")
                .Add(component => component.Title, "Power budget")
                .Add(component => component.QualifiedName, "System::Power::Budget")
                .Add(component => component.Owner, "AOCS")
                .Add(component => component.OwnerColor, "#123456")
                .Add(component => component.OnCollapse, (MouseEventArgs _) => collapseCount++)
                .Add(component => component.Class, "custom-header")
                .AddUnmatched("data-testid", "detail-header"));

            component.Find(".mb-detail-panel-header__collapse-button").Click();

            var header = component.Find(".mb-detail-panel-header");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collapseCount, Is.EqualTo(1));
                Assert.That(header.GetAttribute("data-testid"), Is.EqualTo("detail-header"));
                Assert.That(header.GetAttribute("class"), Does.Contain("custom-header"));
                Assert.That(component.Find(".mb-detail-panel-header__stereotype").TextContent, Does.Contain("requirement"));
                Assert.That(component.Find(".mb-detail-panel-header__title").TextContent.Trim(), Is.EqualTo("Power budget"));
                Assert.That(component.Find(".mb-detail-panel-header__qualified-name").TextContent.Trim(), Is.EqualTo("System::Power::Budget"));
                Assert.That(component.Find(".mb-chip").TextContent.Trim(), Is.EqualTo("AOCS"));
            }
        }

        /// <summary>
        /// Verifies that optional header content can be hidden.
        /// </summary>
        [Test]
        public void VerifyRenderHidesOptionalContent()
        {
            var component = this.Render<DetailPanelHeaderComponent>(parameters => parameters
                .Add(component => component.Title, "Power budget")
                .Add(component => component.ShowCollapseButton, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-detail-panel-header__title").TextContent.Trim(), Is.EqualTo("Power budget"));
                Assert.That(component.FindAll(".mb-detail-panel-header__stereotype"), Is.Empty);
                Assert.That(component.FindAll(".mb-detail-panel-header__collapse-button"), Is.Empty);
                Assert.That(component.FindAll(".mb-detail-panel-header__qualified-name"), Is.Empty);
                Assert.That(component.FindAll(".mb-detail-panel-header__owner-row"), Is.Empty);
            }
        }
    }
}
