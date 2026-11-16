// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationViewModelPageBase.cs" company="Starion Group S.A.">
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
    /// Owns the state of an organization page that loads its own content. The context view creates the page again
    /// when the organization changes, so a page instance always belongs to one organization.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the state of the page.</typeparam>
    public abstract class OrganizationViewModelPageBase<TViewModel> : OrganizationPageBase, IDisposable
        where TViewModel : class, IDisposable
    {
        /// <summary>
        /// Gets the state of the page owned by this page instance.
        /// </summary>
        protected TViewModel ViewModel { get; private set; }

        /// <summary>
        /// Gets or sets the factory that creates the state of the page.
        /// </summary>
        [Inject]
        private Func<TViewModel> ViewModelFactory { get; set; }

        /// <summary>
        /// Releases the state of the page, which cancels a load that is still running.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the state of the page when the page is disposed.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> when called from <see cref="Dispose()" />.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ViewModel?.Dispose();
            }
        }

        /// <summary>
        /// Creates the state of the page, which starts loading its content.
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.ViewModel = this.ViewModelFactory();
        }
    }
}
