// ------------------------------------------------------------------------------------------------
// <copyright file="FieldShellTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Common.FieldShell
{
    using Bunit;

    using FieldShellComponent = Mycelium.Bloom.Components.UI.Common.FieldShell.FieldShell;

    /// <summary>
    /// Tests the <see cref="FieldShellComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class FieldShellTestFixture : BunitContext
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
        /// Verifies shared field content, state classes, and root attributes.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredFieldShell()
        {
            var component = this.Render<FieldShellComponent>(parameters => parameters
                .Add(component => component.ControlId, "field-control")
                .Add(component => component.Label, "Field label")
                .Add(component => component.HelpText, "Helpful text")
                .Add(component => component.HelpTextId, "field-control-help")
                .Add(component => component.ErrorText, "Error text")
                .Add(component => component.ErrorTextId, "field-control-error")
                .Add(component => component.Disabled, true)
                .Add(component => component.Required, true)
                .Add(component => component.ReadOnly, true)
                .Add(component => component.Class, "custom-field")
                .AddChildContent("<input id=\"field-control\" />")
                .AddUnmatched("data-testid", "field-shell"));

            var root = component.Find(".mb-field-shell");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList, Does.Contain("mb-field-shell--disabled"));
                Assert.That(root.ClassList, Does.Contain("mb-field-shell--readonly"));
                Assert.That(root.ClassList, Does.Contain("mb-field-shell--error"));
                Assert.That(root.ClassList, Does.Contain("custom-field"));
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("field-shell"));
                Assert.That(component.Find("label").GetAttribute("for"), Is.EqualTo("field-control"));
                Assert.That(component.Find("label").TextContent, Does.Contain("Field label"));
                Assert.That(component.Find(".mb-field-shell__required").TextContent, Is.EqualTo("*"));
                Assert.That(component.Find("#field-control-help").TextContent, Is.EqualTo("Helpful text"));
                Assert.That(component.Find("#field-control-error").TextContent, Is.EqualTo("Error text"));
                Assert.That(component.Find("input"), Is.Not.Null);
            }
        }

        /// <summary>
        /// Verifies that optional field metadata is omitted when not configured.
        /// </summary>
        [Test]
        public void VerifyRenderOmitsEmptyMetadata()
        {
            var component = this.Render<FieldShellComponent>(parameters => parameters
                .AddChildContent("<input />"));
            var root = component.Find(".mb-field-shell");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("label"), Is.Empty);
                Assert.That(component.FindAll("p"), Is.Empty);
                Assert.That(root.ClassList, Has.Count.EqualTo(1));
                Assert.That(root.ClassList, Does.Contain("mb-field-shell"));
            }
        }
    }
}
