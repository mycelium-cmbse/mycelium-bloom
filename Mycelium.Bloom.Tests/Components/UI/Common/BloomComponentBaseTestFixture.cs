// ------------------------------------------------------------------------------------------------
// <copyright file="BloomComponentBaseTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Common
{
    using System.Reflection;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Tests the <see cref="BloomComponentBase" /> component base class.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class BloomComponentBaseTestFixture : BunitContext
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
        /// Verifies that shared component parameters use the expected default values.
        /// </summary>
        [Test]
        public void VerifyDefaults()
        {
            var component = this.Render<BloomComponentBase>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.Class, Is.Empty);
                Assert.That(component.Instance.AdditionalAttributes, Is.Not.Null);
                Assert.That(component.Instance.AdditionalAttributes, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that shared component parameters are bound through Blazor.
        /// </summary>
        [Test]
        public void VerifyParameterBinding()
        {
            var component = this.Render<BloomComponentBase>(parameters => parameters
                .Add(component => component.Class, "custom-component")
                .AddUnmatched("data-testid", "common-base"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.Class, Is.EqualTo("custom-component"));
                Assert.That(component.Instance.AdditionalAttributes.ContainsKey("data-testid"), Is.True);
                Assert.That(component.Instance.AdditionalAttributes["data-testid"], Is.EqualTo("common-base"));
            }
        }

        /// <summary>
        /// Verifies that shared component properties are configured as Blazor parameters.
        /// </summary>
        [Test]
        public void VerifyParameterAttributes()
        {
            Assert.That(GetParameterAttribute(nameof(BloomComponentBase.Class)), Is.Not.Null);

            var additionalAttributesParameter = GetParameterAttribute(nameof(BloomComponentBase.AdditionalAttributes));

            Assert.That(additionalAttributesParameter.CaptureUnmatchedValues, Is.True);
        }

        /// <summary>
        /// Gets the parameter attribute for a shared component property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The parameter attribute.</returns>
        private static ParameterAttribute GetParameterAttribute(string propertyName)
        {
            var property = typeof(BloomComponentBase).GetProperty(propertyName);

            Assert.That(property, Is.Not.Null);

            var parameterAttribute = property.GetCustomAttribute<ParameterAttribute>();

            Assert.That(parameterAttribute, Is.Not.Null);

            return parameterAttribute;
        }
    }
}
