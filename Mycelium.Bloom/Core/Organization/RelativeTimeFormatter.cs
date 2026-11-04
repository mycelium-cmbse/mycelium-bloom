// ------------------------------------------------------------------------------------------------
// <copyright file="RelativeTimeFormatter.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using System.Globalization;

    /// <summary>
    /// Tells how long ago a moment was, in words, for the lists that show the last update of an item.
    /// </summary>
    /// <remarks>
    /// A pure function of the moment and of the current time, so that the text is tested without a clock and is not rebuilt
    /// in each list ViewModel or view.
    /// </remarks>
    public static class RelativeTimeFormatter
    {
        /// <summary>
        /// The number of weeks from which the date is shown instead of a number of weeks.
        /// </summary>
        private const int WeeksShownAsDate = 4;

        /// <summary>
        /// Formats how long ago a moment was. A moment less than a minute ago, or in the future, reads "just now"; an
        /// older one reads as minutes, hours, days or weeks ago, and from four weeks on as its date.
        /// </summary>
        /// <param name="moment">The moment to describe.</param>
        /// <param name="now">The current moment.</param>
        /// <returns>The text to show.</returns>
        public static string Format(DateTimeOffset moment, DateTimeOffset now)
        {
            var elapsed = now - moment;

            if (elapsed < TimeSpan.FromMinutes(1))
            {
                return "just now";
            }

            if (elapsed < TimeSpan.FromHours(1))
            {
                return Ago((int)elapsed.TotalMinutes, "minute");
            }

            if (elapsed < TimeSpan.FromDays(1))
            {
                return Ago((int)elapsed.TotalHours, "hour");
            }

            if (elapsed < TimeSpan.FromDays(7))
            {
                return Ago((int)elapsed.TotalDays, "day");
            }

            var weeks = (int)(elapsed.TotalDays / 7);

            return weeks < WeeksShownAsDate
                ? Ago(weeks, "week")
                : moment.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Writes a number of time units ago.
        /// </summary>
        /// <param name="count">The number of units.</param>
        /// <param name="unit">The singular name of the unit.</param>
        /// <returns>The text, with the unit in the plural when the count is not one.</returns>
        private static string Ago(int count, string unit)
        {
            return count == 1 ? $"1 {unit} ago" : $"{count} {unit}s ago";
        }
    }
}
