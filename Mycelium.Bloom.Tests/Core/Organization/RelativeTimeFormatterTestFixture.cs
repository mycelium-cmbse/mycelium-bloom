// ------------------------------------------------------------------------------------------------
// <copyright file="RelativeTimeFormatterTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Organization
{
    using System;

    using Mycelium.Bloom.Core.Organization;

    [TestFixture]
    public sealed class RelativeTimeFormatterTestFixture
    {
        [TestCase(-300, "just now")]
        [TestCase(60, "1 minute ago")]
        [TestCase(7200, "2 hours ago")]
        [TestCase(86400, "1 day ago")]
        [TestCase(2419199, "3 weeks ago")]
        [TestCase(2419200, "5 Oct 2026")]
        public void VerifyTheTextFollowsTheElapsedTime(int secondsAgo, string expected)
        {
            var now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

            Assert.That(RelativeTimeFormatter.Format(now.AddSeconds(-secondsAgo), now), Is.EqualTo(expected));
        }
    }
}
