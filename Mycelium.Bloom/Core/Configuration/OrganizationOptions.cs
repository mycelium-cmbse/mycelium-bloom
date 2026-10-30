// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationOptions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Configuration
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Provides strongly typed application configuration for the organization screens.
    /// </summary>
    public sealed class OrganizationOptions
    {
        /// <summary>
        /// The configuration section containing the organization settings.
        /// </summary>
        public const string SectionName = "Organization";

        /// <summary>
        /// Gets or sets the role that the placeholder service gives to the current account in every organization,
        /// so that every variant of every screen can be seen.
        /// </summary>
        public OrganizationRole PlaceholderRole { get; set; } = OrganizationRole.OrganizationAdministrator;
    }
}
