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
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;

    using BlazorBlueprint.Components;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Core.Session;
    using Mycelium.Bloom.ViewModel.Session;

    using ReactiveUI;

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
        /// The subscription that leaves for the Home page once the connection is open.
        /// </summary>
        private IDisposable connectedSubscription;

        /// <summary>
        /// Gets or sets the project of a workspace that sent the user here for a connection, which the page opens again once the
        /// connection is open. Only an identifier is read from it, never an address, so that a link cannot send the user
        /// to another site.
        /// </summary>
        [SupplyParameterFromQuery(Name = WorkspaceRoutes.ReturnProjectParameter)]
        public string ReturnProject { get; set; }

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
        /// Creates the state of the page and leaves for the Home page once a connection is open, at once when the circuit
        /// already holds one. The subscription is kept before that first check, because a navigation during prerendering ends
        /// the method with an exception.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ViewModel = this.ViewModelFactory();
            this.connectedSubscription = this.ViewModel.WhenAnyValue(model => model.IsConnected).Skip(1).Where(isConnected => isConnected).Subscribe(_ => this.OpenHome());
            base.OnInitialized();

            if (this.ViewModel.IsConnected)
            {
                this.OpenHome();
            }
        }

        /// <summary>
        /// Releases the state of the page and its subscription.
        /// </summary>
        /// <param name="disposing">Whether the call comes from <see cref="IDisposable.Dispose" />.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.connectedSubscription?.Dispose();
                this.ViewModel?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Opens the connection to the typed address and gives the focus back to the field when the address is refused.
        /// </summary>
        /// <returns>The completion of the attempt.</returns>
        private async Task ConnectAsync()
        {
            try
            {
                await this.ViewModel.Connect.Execute().ToTask();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (this.AddressErrors.Length > 0)
            {
                await this.addressInput.FocusAsync();
            }
        }

        /// <summary>
        /// Opens the workspace of the project that sent the user here, or else the Home page, in place of the start page, so that
        /// Back does not return to it.
        /// </summary>
        private void OpenHome()
        {
            this.NavigationManager.NavigateTo(Guid.TryParse(this.ReturnProject, out var projectId) ? WorkspaceRoutes.GetProjectPath(projectId) : HomeRoute, replace: true);
        }
    }
}
