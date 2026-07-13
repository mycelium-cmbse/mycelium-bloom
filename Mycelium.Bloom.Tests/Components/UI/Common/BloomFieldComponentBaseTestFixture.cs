// ------------------------------------------------------------------------------------------------
// <copyright file="BloomFieldComponentBaseTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Common
{
    using System.Collections.Generic;
    using System.Reflection;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Tests the <see cref="BloomFieldComponentBase" /> component base class.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class BloomFieldComponentBaseTestFixture : BunitContext
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
        /// Verifies that shared field parameters use the expected default values.
        /// </summary>
        [Test]
        public void VerifyDefaults()
        {
            var component = this.Render<BloomFieldComponentBase>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.Class, Is.Empty);
                Assert.That(component.Instance.AdditionalAttributes, Is.Not.Null);
                Assert.That(component.Instance.AdditionalAttributes, Is.Empty);
                Assert.That(component.Instance.Id, Is.Empty);
                Assert.That(component.Instance.Name, Is.Empty);
                Assert.That(component.Instance.Label, Is.Empty);
                Assert.That(component.Instance.HelpText, Is.Empty);
                Assert.That(component.Instance.ErrorText, Is.Empty);
                Assert.That(component.Instance.Disabled, Is.False);
                Assert.That(component.Instance.Required, Is.False);
                Assert.That(GetHasError(component.Instance), Is.False);
            }
        }

        /// <summary>
        /// Verifies that shared field parameters are bound through Blazor.
        /// </summary>
        [Test]
        public void VerifyParameterBinding()
        {
            var component = this.Render<BloomFieldComponentBase>(parameters => parameters
                .Add(component => component.Id, "field-id")
                .Add(component => component.Name, "field-name")
                .Add(component => component.Label, "Field label")
                .Add(component => component.HelpText, "Helpful text")
                .Add(component => component.ErrorText, "Required field")
                .Add(component => component.Disabled, true)
                .Add(component => component.Required, true)
                .Add(component => component.Class, "custom-field")
                .AddUnmatched("data-testid", "field-base"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.Class, Is.EqualTo("custom-field"));
                Assert.That(component.Instance.AdditionalAttributes.ContainsKey("data-testid"), Is.True);
                Assert.That(component.Instance.AdditionalAttributes["data-testid"], Is.EqualTo("field-base"));
                Assert.That(component.Instance.Id, Is.EqualTo("field-id"));
                Assert.That(component.Instance.Name, Is.EqualTo("field-name"));
                Assert.That(component.Instance.Label, Is.EqualTo("Field label"));
                Assert.That(component.Instance.HelpText, Is.EqualTo("Helpful text"));
                Assert.That(component.Instance.ErrorText, Is.EqualTo("Required field"));
                Assert.That(component.Instance.Disabled, Is.True);
                Assert.That(component.Instance.Required, Is.True);
                Assert.That(GetHasError(component.Instance), Is.True);
            }
        }

        /// <summary>
        /// Verifies that whitespace error text is not treated as an error.
        /// </summary>
        [Test]
        public void VerifyHasError()
        {
            var component = this.Render<BloomFieldComponentBase>(parameters => parameters
                .Add(component => component.ErrorText, "   "));

            Assert.That(GetHasError(component.Instance), Is.False);
        }

        /// <summary>
        /// Verifies that shared field properties are configured as Blazor parameters.
        /// </summary>
        [Test]
        public void VerifyParameterAttributes()
        {
            foreach (var propertyName in GetFieldParameterNames())
            {
                Assert.That(GetParameterAttribute(propertyName), Is.Not.Null);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the provided field component has an error.
        /// </summary>
        /// <param name="component">The field component.</param>
        /// <returns>A value indicating whether the field component has an error.</returns>
        private static bool GetHasError(BloomFieldComponentBase component)
        {
            var property = typeof(BloomFieldComponentBase).GetProperty(
                "HasError",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(property, Is.Not.Null);

            return (bool)property.GetValue(component)!;
        }

        /// <summary>
        /// Gets the field parameter names declared by <see cref="BloomFieldComponentBase" />.
        /// </summary>
        /// <returns>The field parameter names.</returns>
        private static IEnumerable<string> GetFieldParameterNames()
        {
            return
            [
                nameof(BloomFieldComponentBase.Id),
                nameof(BloomFieldComponentBase.Name),
                nameof(BloomFieldComponentBase.Label),
                nameof(BloomFieldComponentBase.HelpText),
                nameof(BloomFieldComponentBase.ErrorText),
                nameof(BloomFieldComponentBase.Disabled),
                nameof(BloomFieldComponentBase.Required)
            ];
        }

        /// <summary>
        /// Gets the parameter attribute for a shared field property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The parameter attribute.</returns>
        private static ParameterAttribute GetParameterAttribute(string propertyName)
        {
            var property = typeof(BloomFieldComponentBase).GetProperty(propertyName);

            Assert.That(property, Is.Not.Null);

            var parameterAttribute = property.GetCustomAttribute<ParameterAttribute>();

            Assert.That(parameterAttribute, Is.Not.Null);

            return parameterAttribute;
        }
    }
}
