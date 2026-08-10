// ------------------------------------------------------------------------------------------------
// <copyright file="DisplayStringFormatterTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Common
{
    using System.Globalization;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Tests the <see cref="DisplayStringFormatter" /> helper.
    /// </summary>
    [TestFixture]
    public sealed class DisplayStringFormatterTestFixture
    {
        /// <summary>
        /// Verifies a null value produces an empty display string.
        /// </summary>
        [Test]
        public void VerifyToDisplayStringReturnsEmptyForNull()
        {
            object value = null;

            Assert.That(value.ToDisplayString(), Is.Empty);
        }

        /// <summary>
        /// Verifies a string value is preserved.
        /// </summary>
        [Test]
        public void VerifyToDisplayStringPreservesString()
        {
            Assert.That("display value".ToDisplayString(), Is.EqualTo("display value"));
        }

        /// <summary>
        /// Verifies convertible values use invariant culture rather than the active culture.
        /// </summary>
        [Test]
        public void VerifyToDisplayStringUsesInvariantCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

                Assert.That(1234.5m.ToDisplayString(), Is.EqualTo("1234.5"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
