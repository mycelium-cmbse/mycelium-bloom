// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationPageBase.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages.Organization
{
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// Receives the organization slug of the route of an organization page. The page content will read the organization
    /// from the organization context, which the layout resolves from the same location.
    /// </summary>
    public abstract class OrganizationPageBase : ComponentBase
    {
        /// <summary>
        /// Gets or sets the slug of the organization in the route.
        /// </summary>
        [Parameter]
        public string Slug { get; set; } = string.Empty;
    }
}
