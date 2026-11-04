// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationProjects.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages.Organization
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Shows the projects of the organization that the layout resolved. The layout shows the page only once the
    /// organization is resolved, and removes it while another organization loads, so a page instance always belongs to
    /// one organization.
    /// </summary>
    public sealed partial class OrganizationProjects : OrganizationPageBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the factory that creates the state of the projects page owned by this page instance.
        /// </summary>
        [Inject]
        private Func<IProjectsViewModel> ViewModelFactory { get; set; }

        /// <summary>
        /// Gets the state of the projects page owned by this page instance.
        /// </summary>
        private IProjectsViewModel ViewModel { get; set; }

        /// <summary>
        /// Creates the state of the projects page, which starts loading the projects.
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.ViewModel = this.ViewModelFactory();
        }

        /// <summary>
        /// Releases the state of the projects page, which cancels a load that is still running.
        /// </summary>
        public void Dispose()
        {
            this.ViewModel?.Dispose();
        }
    }
}
