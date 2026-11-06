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
    using Mycelium.SDK;

    [TestFixture]
    public sealed class ProjectExtensionsTestFixture
    {
        private static readonly string[] VisibilityIcons = ["lock", "building-2", "globe"];

        [Test]
        public void VerifyEveryVisibilityHasItsIcon()
        {
            Assert.That(Enum.GetValues<ProjectVisibility>().Select(visibility => visibility.Icon), Is.EqualTo(VisibilityIcons));
        }
    }
}
