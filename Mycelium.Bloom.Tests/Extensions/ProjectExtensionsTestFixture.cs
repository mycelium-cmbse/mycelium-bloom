// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Extensions
{
    using System;
    using System.Linq;

    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model;
    using Mycelium.SDK;

    [TestFixture]
    public sealed class ProjectExtensionsTestFixture
    {
        private static readonly string[] VisibilityIcons = ["lock", "building-2", "globe"];

        [Test]
        public void VerifyEveryVisibilityHasItsIcon()
        {
            Assert.That(Enum.GetValues<ProjectVisibility>().Select(visibility => visibility.GetIcon()), Is.EqualTo(VisibilityIcons));
        }

        [TestCase(1, 1, "Showing 1 of 1 project")]
        [TestCase(3, 2, "Showing 2 of 3 projects")]
        public void VerifyTheShownCountIsWrittenInWords(int total, int shown, string expectedShowing)
        {
            var counts = new ProjectCounts(total, 0, total, 0, 0);

            Assert.That(counts.GetShowingText(shown), Is.EqualTo(expectedShowing));
        }
    }
}
