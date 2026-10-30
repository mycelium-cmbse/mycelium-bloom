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

    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Composes the shared frame of the account and organization pages: a header and a content area
    /// that follows the state of the organization designated by the location.
    /// </summary>
    public sealed partial class OrganizationLayout : LayoutComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the factory that creates the organization context owned by this layout instance.
        /// </summary>
        [Inject]
        private Func<IOrganizationContextViewModel> ViewModelFactory { get; set; }

        /// <summary>
        /// Gets or sets the factory that creates the header state owned by this layout instance.
        /// </summary>
        [Inject]
        private Func<IOrganizationContextViewModel, IOrganizationHeaderViewModel> HeaderViewModelFactory { get; set; }

        /// <summary>
        /// Gets the organization context owned and cascaded by this layout instance.
        /// </summary>
        private IOrganizationContextViewModel ViewModel { get; set; }

        /// <summary>
        /// Gets the header state owned by this layout instance.
        /// </summary>
        private IOrganizationHeaderViewModel HeaderViewModel { get; set; }

        /// <summary>
        /// Creates the organization context and the header state owned by this layout instance.
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            this.ViewModel = this.ViewModelFactory();
            this.HeaderViewModel = this.HeaderViewModelFactory(this.ViewModel);
        }

        /// <summary>
        /// Releases the header state and the organization context owned by this layout instance.
        /// </summary>
        public void Dispose()
        {
            this.HeaderViewModel?.Dispose();
            this.ViewModel?.Dispose();
        }
    }
}
