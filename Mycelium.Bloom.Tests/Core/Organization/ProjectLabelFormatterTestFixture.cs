// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectLabelFormatterTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Organization
{
    using System;
    using System.ComponentModel;
    using System.Linq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model.Enum;

    [TestFixture]
    public sealed class ProjectLabelFormatterTestFixture
    {
        [TestCase(ProjectLifecycleState.Preparation, "Preparation")]
        [TestCase(ProjectLifecycleState.Open, "Open")]
        [TestCase(ProjectLifecycleState.Review, "In review")]
        [TestCase(ProjectLifecycleState.Archived, "Archived")]
        public void VerifyLifecycleLabelsAreTheOnesTheUserReads(ProjectLifecycleState state, string expected)
        {
            Assert.That(ProjectLabelFormatter.GetLifecycleLabel(state), Is.EqualTo(expected));
        }

        [Test]
        public void VerifyAnUndefinedValueIsRefused()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => ProjectLabelFormatter.GetLifecycleLabel((ProjectLifecycleState)99), Throws.InstanceOf<InvalidEnumArgumentException>());
                Assert.That(() => ProjectLabelFormatter.GetVisibilityLabel((ProjectVisibility)99), Throws.InstanceOf<InvalidEnumArgumentException>());
                Assert.That(() => ProjectLabelFormatter.GetVisibilityIcon((ProjectVisibility)99), Throws.InstanceOf<InvalidEnumArgumentException>());
                Assert.That(() => ProjectLabelFormatter.GetModeLabel((ProjectMode)99), Throws.InstanceOf<InvalidEnumArgumentException>());
            }
        }

        [Test]
        public void VerifyEveryValueHasItsOwnLabelAndIcon()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Enum.GetValues<ProjectLifecycleState>().Select(ProjectLabelFormatter.GetLifecycleLabel), Is.Unique.And.None.Empty);
                Assert.That(Enum.GetValues<ProjectVisibility>().Select(ProjectLabelFormatter.GetVisibilityLabel), Is.Unique.And.None.Empty);
                Assert.That(Enum.GetValues<ProjectVisibility>().Select(ProjectLabelFormatter.GetVisibilityIcon), Is.Unique.And.None.Empty);
                Assert.That(Enum.GetValues<ProjectMode>().Select(ProjectLabelFormatter.GetModeLabel), Is.Unique.And.None.Empty);
            }
        }
    }
}
