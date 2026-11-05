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
        private static readonly string[] LifecycleLabels = ["Preparation", "Open", "In review", "Archived"];

        private static readonly string[] VisibilityLabels = ["Private", "Organization", "Public"];

        private static readonly string[] VisibilityIcons = ["lock", "building-2", "globe"];

        private static readonly string[] ModeLabels = ["Regular", "Concurrent Design"];

        [Test]
        public void VerifyEveryMemberHasItsLabelAndEveryVisibilityItsIcon()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Enum.GetValues<ProjectLifecycleKind>().Select(lifecycle => lifecycle.GetLabel()), Is.EqualTo(LifecycleLabels));
                Assert.That(Enum.GetValues<ProjectVisibility>().Select(visibility => visibility.GetLabel()), Is.EqualTo(VisibilityLabels));
                Assert.That(Enum.GetValues<ProjectVisibility>().Select(visibility => visibility.GetIcon()), Is.EqualTo(VisibilityIcons));
                Assert.That(Enum.GetValues<ProjectMode>().Select(mode => mode.GetLabel()), Is.EqualTo(ModeLabels));
            }
        }

        [TestCase(1, 0, 1, 0, 0, 1, "1 project · 1 open · 0 in preparation · 0 in review · 0 archived", "Showing 1 of 1 project")]
        [TestCase(3, 1, 1, 1, 0, 2, "3 projects · 1 open · 1 in preparation · 1 in review · 0 archived", "Showing 2 of 3 projects")]
        public void VerifyCountsAreWrittenInWords(int total, int preparation, int open, int review, int archived, int shown, string expectedSubtitle, string expectedShowing)
        {
            var counts = new ProjectCounts(total, preparation, open, review, archived);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(counts.GetSubtitle(), Is.EqualTo(expectedSubtitle));
                Assert.That(counts.GetShowingText(shown), Is.EqualTo(expectedShowing));
            }
        }
    }
}
