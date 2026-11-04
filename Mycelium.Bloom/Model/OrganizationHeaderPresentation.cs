// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationHeaderPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Describes what the header of the account and organization pages displays. The default value displays nothing.
    /// </summary>
    public sealed record OrganizationHeaderPresentation
    {
        /// <summary>
        /// Gets the presentation of a header whose context is not resolved: no page segment and no account menu.
        /// </summary>
        public static OrganizationHeaderPresentation Empty { get; } = new();

        /// <summary>
        /// Gets what the header displays: the account page segment, the organization segments, or nothing.
        /// </summary>
        public OrganizationLocationKind Kind { get; init; } = OrganizationLocationKind.None;

        /// <summary>
        /// Gets a value indicating whether the current account is resolved, which is when the account menu is shown.
        /// </summary>
        public bool HasAccount => this.Kind != OrganizationLocationKind.None;

        /// <summary>
        /// Gets the name of the resolved organization, or an empty string.
        /// </summary>
        public string OrganizationName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the route of the overview of the resolved organization, or an empty string.
        /// </summary>
        public string OrganizationPath { get; init; } = string.Empty;

        /// <summary>
        /// Gets the label of the current page of the organization, or an empty string.
        /// </summary>
        public string SectionLabel { get; init; } = string.Empty;

        /// <summary>
        /// Gets the name of the icon of the current page of the organization, or an empty string.
        /// </summary>
        public string SectionIcon { get; init; } = string.Empty;

        /// <summary>
        /// Gets the pages of the organization that the current account may open.
        /// </summary>
        public IReadOnlyList<ActionMenuItem> SectionItems { get; init; } = [];

        /// <summary>
        /// Gets the display name of the current account, or an empty string.
        /// </summary>
        public string AccountName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the email address of the current account, or an empty string.
        /// </summary>
        public string AccountEmail { get; init; } = string.Empty;

        /// <summary>
        /// Gets the entries of the account menu.
        /// </summary>
        public IReadOnlyList<ActionMenuItem> AccountItems { get; init; } = [];
    }
}
