// ------------------------------------------------------------------------------------------------
// <copyright file="ServerConnection.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using System.Reactive.Threading.Tasks;

    using BlazorBlueprint.Components;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.ViewModel.Session;

    /// <summary>
    /// Shows the start page, where the user names the model server to work with. Once a connection is open, the page leaves
    /// for the Home page, at once when the circuit already holds one.
    /// </summary>
    public sealed partial class ServerConnection
    {
        /// <summary>
        /// The route of the Home page, which the page opens once the connection is open.
        /// </summary>
        private const string HomeRoute = "/account";

        /// <summary>
        /// The id of the address field, which its label points to.
        /// </summary>
        private const string AddressInputId = "server-address";

        /// <summary>
        /// The id of the refusal shown under the address field, which the field points to.
        /// </summary>
        private const string AddressErrorId = "server-address-error";

        /// <summary>
        /// The address field, which takes the focus back when its value is refused.
        /// </summary>
        private BbInput addressInput;

        /// <summary>
        /// Gets or sets the factory that creates the state of the page owned by this page instance.
        /// </summary>
        [Inject]
        private Func<IConnectViewModel> ViewModelFactory { get; set; }

        /// <summary>
        /// Gets or sets the navigation manager that opens the Home page.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Gets the refusals of the typed address, shown under the field rather than in the alert.
        /// </summary>
        private string[] AddressErrors => [.. this.ViewModel.Errors.Where(error => error.Type == ErrorType.Validation).Select(error => error.Description)];

        /// <summary>
        /// Gets the failures of the server, shown in the alert above the button.
        /// </summary>
        private string[] ServerErrors => [.. this.ViewModel.Errors.Where(error => error.Type != ErrorType.Validation).Select(error => error.Description)];

        /// <summary>
        /// Creates the state of the page and leaves for the Home page at once when the circuit already holds a connection.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ViewModel = this.ViewModelFactory();
            base.OnInitialized();

            if (this.ViewModel.IsConnected)
            {
                this.OpenHome();
            }
        }

        /// <summary>
        /// Releases the state of the page.
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

        /// <summary>
        /// Opens the connection to the typed address, then the Home page once it is open, and gives the focus back to the field
        /// when the address is refused.
        /// </summary>
        /// <returns>The completion of the attempt.</returns>
        private async Task ConnectAsync()
        {
            bool isConnected;

            try
            {
                isConnected = await this.ViewModel.Connect.Execute().ToTask();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (isConnected)
            {
                this.OpenHome();
            }
            else if (this.AddressErrors.Length > 0)
            {
                await this.addressInput.FocusAsync();
            }
        }

        /// <summary>
        /// Opens the Home page in place of the start page, so that Back does not return to it.
        /// </summary>
        private void OpenHome()
        {
            this.NavigationManager.NavigateTo(HomeRoute, replace: true);
        }
    }
}
