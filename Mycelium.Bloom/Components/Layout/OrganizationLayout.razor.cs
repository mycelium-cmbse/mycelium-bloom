// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationLayout.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Layout
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Composes the shared frame of the account and organization pages: a header and a content area
    /// that follows the state of the organization designated by the location.
    /// </summary>
    public sealed partial class OrganizationLayout : LayoutComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the navigation service that provides the current location.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Gets or sets the organization context that follows the location.
        /// </summary>
        [Inject]
        private IOrganizationContextViewModel ViewModel { get; set; }

        /// <summary>
        /// Hands the current location to the organization context. The router sets the body again on every
        /// navigation, so the context follows every page change.
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            this.ViewModel.Location = OrganizationRoutes.Parse(this.NavigationManager.ToBaseRelativePath(this.NavigationManager.Uri));
        }

        /// <summary>
        /// Resets the organization context, which lives as long as the circuit, when the account leaves the account and
        /// organization pages, so that coming back loads the organization again and no earlier error is reported again.
        /// </summary>
        public void Dispose()
        {
            this.ViewModel.Location = null;
        }
    }
}
