// ------------------------------------------------------------------------------------------------
// <copyright file="PropertyRowTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.PropertyRow
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using PropertyRowComponent = Mycelium.Bloom.Components.UI.Molecules.PropertyRow.PropertyRow;

    /// <summary>
    /// Tests the <see cref="PropertyRowComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class PropertyRowTestFixture : BunitContext
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
        /// Verifies that the plain property row renders configured content and attributes.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysPlainValue()
        {
            var component = this.Render<PropertyRowComponent>(parameters => parameters
                .Add(component => component.Label, "Qualified name")
                .Add(component => component.Value, "Package::Element")
                .Add(component => component.IsMonospace, true)
                .Add(component => component.Variant, PropertyRowVariant.Inline)
                .Add(component => component.Class, "custom-row")
                .AddUnmatched("data-testid", "qualified-name-row"));

            var row = component.Find(".mb-property-row");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.GetAttribute("data-testid"), Is.EqualTo("qualified-name-row"));
                Assert.That(row.GetAttribute("class"), Does.Contain("mb-property-row--inline"));
                Assert.That(row.GetAttribute("class"), Does.Contain("custom-row"));
                Assert.That(component.Find(".mb-property-row__label").TextContent.Trim(), Is.EqualTo("Qualified name"));
                Assert.That(component.Find(".mb-property-row__text").TextContent.Trim(), Is.EqualTo("Package::Element"));
                Assert.That(component.Find(".mb-property-row__text").GetAttribute("class"), Does.Contain("mb-property-row__text--mono"));
            }
        }

        /// <summary>
        /// Verifies that custom value content overrides the plain value.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCustomValueContent()
        {
            var component = this.Render<PropertyRowComponent>(parameters => parameters
                .Add(component => component.Label, "Status")
                .Add(component => component.Value, "Plain status")
                .Add(component => component.ValueContent, "<strong>Custom status</strong>"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-property-row").GetAttribute("class"), Does.Contain("mb-property-row--stacked"));
                Assert.That(component.Find(".mb-property-row__value").TextContent.Trim(), Is.EqualTo("Custom status"));
                Assert.That(component.FindAll(".mb-property-row__text"), Is.Empty);
            }
        }
    }
}
