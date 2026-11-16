// ------------------------------------------------------------------------------------------------
// <copyright file="UserExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Extensions
{
    using Mycelium.Bloom.Extensions;
    using Mycelium.SDK.POCO;

    [TestFixture]
    public sealed class UserExtensionsTestFixture
    {
        [TestCase("Jean Dupont", "j.dupont@example.org", "JD")]
        [TestCase("  marie   anne claire ", "m@example.org", "MA")]
        [TestCase("", "j.dupont@example.org", "J")]
        [TestCase("", "", "")]
        public void VerifyInitialsComeFromTheNameThenTheEmail(string name, string mail, string expected)
        {
            Assert.That(new User { Name = name, Email = mail }.GetInitials(), Is.EqualTo(expected));
        }

        [Test]
        public void VerifyTheAvatarColorIsStableAndWithinTheCollaboratorColors()
        {
            var first = new User { Email = "j.dupont@example.org" }.GetAvatarColor();
            var again = new User { Email = "j.dupont@example.org" }.GetAvatarColor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(again, Is.EqualTo(first));
                Assert.That(first, Is.InRange(1, 12));
            }
        }
    }
}
