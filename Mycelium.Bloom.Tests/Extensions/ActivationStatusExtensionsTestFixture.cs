// ------------------------------------------------------------------------------------------------
// <copyright file="ActivationStatusExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Extensions
{
    using Mycelium.Bloom.Extensions;
    using Mycelium.SDK;

    [TestFixture]
    public sealed class ActivationStatusExtensionsTestFixture
    {
        [TestCase(ActivationStatus.Active, "Active")]
        [TestCase(ActivationStatus.Pending, "Pending")]
        [TestCase(ActivationStatus.Suspended, "Deactivated")]
        [TestCase(ActivationStatus.Archived, "Deactivated")]
        public void VerifyEveryAccountStatusHasItsWords(ActivationStatus status, string expectedLabel)
        {
            Assert.That(status.GetLabel(), Is.EqualTo(expectedLabel));
        }
    }
}
