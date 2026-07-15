// ------------------------------------------------------------------------------------------------
// <copyright file="SelectInputOptionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Model
{
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Tests the <see cref="SelectInputOption" /> model.
    /// </summary>
    [TestFixture]
    public sealed class SelectInputOptionTestFixture
    {
        /// <summary>
        /// Verifies the default option state.
        /// </summary>
        [Test]
        public void VerifyDefaults()
        {
            var option = new SelectInputOption();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(option.Value, Is.Empty);
                Assert.That(option.Label, Is.Empty);
                Assert.That(option.Disabled, Is.False);
            }
        }

        /// <summary>
        /// Verifies that option values can be configured.
        /// </summary>
        [Test]
        public void VerifyConfiguredValues()
        {
            var option = new SelectInputOption
            {
                Value = "configured",
                Label = "Configured option",
                Disabled = true
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(option.Value, Is.EqualTo("configured"));
                Assert.That(option.Label, Is.EqualTo("Configured option"));
                Assert.That(option.Disabled, Is.True);
            }
        }
    }
}
