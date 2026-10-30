// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationRoutes.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Describes the routes of the account and organization pages, and reads them back from a browser location.
    /// </summary>
    public static class OrganizationRoutes
    {
        /// <summary>
        /// The route of the account dashboard.
        /// </summary>
        public const string AccountPath = "/" + AccountSegment;

        /// <summary>
        /// The segment of the account dashboard.
        /// </summary>
        private const string AccountSegment = "account";

        /// <summary>
        /// The first segment of every organization route.
        /// </summary>
        private const string OrganizationsSegment = "organizations";

        /// <summary>
        /// The longest slug that is read from a location. A longer one cannot designate an organization.
        /// </summary>
        private const int MaxSlugLength = 64;

        /// <summary>
        /// The segment that follows the organization slug, by page. The overview has none.
        /// </summary>
        private static readonly Dictionary<OrganizationSection, string> SectionSegments = new()
        {
            [OrganizationSection.Overview] = string.Empty,
            [OrganizationSection.Projects] = "projects",
            [OrganizationSection.Members] = "members",
            [OrganizationSection.Settings] = "settings"
        };

        /// <summary>
        /// Builds the route of a page of an organization.
        /// </summary>
        /// <param name="slug">The slug of the organization.</param>
        /// <param name="section">The page of the organization.</param>
        /// <returns>The absolute path of the page.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="slug" /> is null, empty or whitespace.</exception>
        public static string GetOrganizationPath(string slug, OrganizationSection section)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(slug);

            var organizationPath = $"/{OrganizationsSegment}/{Uri.EscapeDataString(slug)}";
            var segment = SectionSegments[section];

            return segment.Length == 0 ? organizationPath : $"{organizationPath}/{segment}";
        }

        /// <summary>
        /// Reads what a browser location designates. The query string and the fragment are ignored, as is a trailing slash.
        /// </summary>
        /// <param name="location">The absolute URI or the path of the location.</param>
        /// <returns>
        /// The designated page, or <see cref="OrganizationLocation.None" /> when the location is not in the area or when
        /// the organization slug is not valid: empty, longer than 64 characters, or holding a separator, a whitespace
        /// or a control character once decoded.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="location" /> is <see langword="null" />.</exception>
        public static OrganizationLocation Parse(string location)
        {
            ArgumentNullException.ThrowIfNull(location);

            var segments = GetPath(location).Split('/', StringSplitOptions.RemoveEmptyEntries);

            return segments switch
            {
                [var first] when IsSegment(first, AccountSegment) => OrganizationLocation.Account,
                [var first, var slug] when IsSegment(first, OrganizationsSegment) && TryReadSlug(slug, out var organization) =>
                    OrganizationLocation.ForOrganization(organization, OrganizationSection.Overview),
                [var first, var slug, var page] when IsSegment(first, OrganizationsSegment)
                                                     && TryReadSlug(slug, out var organization)
                                                     && TryGetSection(page, out var section) =>
                    OrganizationLocation.ForOrganization(organization, section),
                _ => OrganizationLocation.None
            };
        }

        /// <summary>
        /// Extracts the path of a location, without its scheme, host, query string and fragment.
        /// </summary>
        /// <param name="location">The absolute URI or the path of the location.</param>
        /// <returns>The path of the location.</returns>
        private static string GetPath(string location)
        {
            if (Uri.TryCreate(location, UriKind.Absolute, out var absolute)
                && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                return absolute.AbsolutePath;
            }

            var end = location.IndexOfAny(['?', '#']);

            return end < 0 ? location : location[..end];
        }

        /// <summary>
        /// Compares a route segment with an expected segment without regard to case.
        /// </summary>
        /// <param name="segment">The segment read from the location.</param>
        /// <param name="expected">The expected segment.</param>
        /// <returns>A value indicating whether the segments are the same.</returns>
        private static bool IsSegment(string segment, string expected)
        {
            return string.Equals(segment, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Decodes the slug of an organization and checks that it can designate one.
        /// </summary>
        /// <param name="segment">The segment read from the location.</param>
        /// <param name="slug">The decoded slug.</param>
        /// <returns>A value indicating whether the slug is valid.</returns>
        private static bool TryReadSlug(string segment, out string slug)
        {
            slug = Uri.UnescapeDataString(segment);

            return slug.Length <= MaxSlugLength
                   && slug.All(character => !char.IsControl(character)
                                            && !char.IsWhiteSpace(character)
                                            && character is not '/' and not '\\');
        }

        /// <summary>
        /// Reads the page of an organization from its route segment.
        /// </summary>
        /// <param name="segment">The segment read from the location.</param>
        /// <param name="section">The page designated by the segment.</param>
        /// <returns>A value indicating whether the segment designates a page.</returns>
        private static bool TryGetSection(string segment, out OrganizationSection section)
        {
            foreach (var candidate in SectionSegments)
            {
                if (candidate.Value.Length > 0 && IsSegment(segment, candidate.Value))
                {
                    section = candidate.Key;

                    return true;
                }
            }

            section = OrganizationSection.Overview;

            return false;
        }
    }
}
