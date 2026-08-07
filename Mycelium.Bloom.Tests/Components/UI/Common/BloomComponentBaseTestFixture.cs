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
            var component = this.Render<TestBloomComponent>();

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
            var component = this.Render<TestBloomComponent>(parameters => parameters
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
        /// Verifies that root CSS classes include the configured custom class.
        /// </summary>
        [Test]
        public void VerifyBuildRootCssClassAppendsCustomClass()
        {
            var component = new TestBloomComponent("custom-component");

            var cssClass = component.BuildRootCssClassForTest("mb-component", "mb-component--active");

            Assert.That(cssClass, Is.EqualTo("mb-component mb-component--active custom-component"));
        }

        /// <summary>
        /// Verifies that generated component identifiers preserve their prefix and remain unique.
        /// </summary>
        [Test]
        public void VerifyCreateGeneratedIdUsesPrefixAndUniqueValue()
        {
            var firstId = TestBloomComponent.CreateGeneratedIdForTest("mb-component");
            var secondId = TestBloomComponent.CreateGeneratedIdForTest("mb-component");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstId, Does.StartWith("mb-component-"));
                Assert.That(secondId, Does.StartWith("mb-component-"));
                Assert.That(secondId, Is.Not.EqualTo(firstId));
            }
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

        /// <summary>
        /// Exposes protected <see cref="BloomComponentBase" /> behavior for testing.
        /// </summary>
        private sealed class TestBloomComponent : BloomComponentBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestBloomComponent" /> class.
            /// </summary>
            public TestBloomComponent()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TestBloomComponent" /> class.
            /// </summary>
            /// <param name="cssClass">The custom root CSS class.</param>
            public TestBloomComponent(string cssClass)
            {
                this.Class = cssClass;
            }

            /// <summary>
            /// Builds root CSS classes through the component base implementation.
            /// </summary>
            /// <param name="cssClasses">The component-owned CSS classes.</param>
            /// <returns>The root CSS class list.</returns>
            public string BuildRootCssClassForTest(params string[] cssClasses)
            {
                return this.BuildRootCssClass(cssClasses);
            }

            /// <summary>
            /// Creates a generated component identifier through the component base implementation.
            /// </summary>
            /// <param name="prefix">The identifier prefix.</param>
            /// <returns>The generated component identifier.</returns>
            public static string CreateGeneratedIdForTest(string prefix)
            {
                return CreateGeneratedId(prefix);
            }
        }
    }
}
