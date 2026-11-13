// ------------------------------------------------------------------------------------------------
// <copyright file="AccountDashboard.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages.Organization
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.ViewModel.Home;

    /// <summary>
    /// Shows the Home page, on the account route: the projects of the server of the open connection. Without an open connection
    /// the page renders nothing and goes back to the start page.
    /// </summary>
    public sealed partial class AccountDashboard
    {
        /// <summary>
        /// The number of projects on one page of the table.
        /// </summary>
        private const int PageSize = 25;

        /// <summary>
        /// The route of the start page, which opens a connection.
        /// </summary>
        private const string StartRoute = "/";

        /// <summary>
        /// Gets or sets the factory that creates the state of the page owned by this page instance.
        /// </summary>
        [Inject]
        private Func<IHomeViewModel> ViewModelFactory { get; set; }

        /// <summary>
        /// Gets or sets the navigation manager that goes back to the start page.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Gets or sets the clock that dates the projects, read once per render.
        /// </summary>
        [Inject]
        private TimeProvider TimeProvider { get; set; }

        /// <summary>
        /// Creates the state of the page, which starts loading the projects, or goes back to the start page without a connection.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ViewModel = this.ViewModelFactory();
            base.OnInitialized();

            if (!this.ViewModel.IsConnected)
            {
                this.NavigationManager.NavigateTo(StartRoute, replace: true);
            }
        }

        /// <summary>
        /// Releases the state of the page, which cancels a load that is still running.
        /// </summary>
        /// <param name="disposing">Whether the call comes from <see cref="IDisposable.Dispose" />.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ViewModel?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
