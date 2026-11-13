// ------------------------------------------------------------------------------------------------
// <copyright file="ConnectViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Session
{
    using ErrorOr;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Session;

    using ReactiveUI;
    using ReactiveUI.Primitives;

    /// <summary>
    /// Holds the start page: the typed server address, the connection command and its outcome. An address that is not an
    /// absolute http or https address is refused before any request, and an error stays shown until the address changes.
    /// </summary>
    public sealed class ConnectViewModel : DisposableViewModel, IConnectViewModel
    {
        /// <summary>
        /// The error returned for an address that is not an absolute http or https address, or that carries a user name or a
        /// password, which the request would send to the server.
        /// </summary>
        private static readonly Error InvalidAddress = Error.Validation(ErrorCodes.Validation, "Enter the full address of the server, starting with http:// or https://, without a user name or a password.");

        /// <summary>
        /// The session that opens the connection.
        /// </summary>
        private readonly IServerSessionService sessionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectViewModel" /> class.
        /// </summary>
        /// <param name="sessionService">The session that opens the connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sessionService" /> is <see langword="null" />.</exception>
        public ConnectViewModel(IServerSessionService sessionService)
        {
            ArgumentNullException.ThrowIfNull(sessionService);

            this.sessionService = sessionService;
            this.IsConnected = sessionService.ServerAddress is not null;
            this.ServerAddress = sessionService.ServerAddress?.ToString() ?? string.Empty;

            var canConnect = this.WhenAnyValue(model => model.ServerAddress, model => model.IsBusy, (address, busy) => !busy && !string.IsNullOrWhiteSpace(address));
            this.Connect = ReactiveCommand.CreateFromTask(this.ConnectAsync, canConnect);

            this.Disposables.AddRange([
                this.Connect,
                this.Connect.IsExecuting.Subscribe(isExecuting => this.IsBusy = isExecuting),
                this.Connect.ThrownExceptions.Subscribe(_ => this.IsConnected = sessionService.ServerAddress is not null),
                this.WhenAnyValue(model => model.ServerAddress).Skip(1).Subscribe(_ => this.Errors = [])
            ]);
        }

        /// <summary>
        /// Gets or sets the address of the server, as the user types it.
        /// </summary>
        public string ServerAddress
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the command that opens the connection to the typed address.
        /// </summary>
        public ReactiveCommand<RxVoid, RxVoid> Connect { get; }

        /// <summary>
        /// Gets a value indicating whether a connection is being opened.
        /// </summary>
        public bool IsBusy
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Gets the errors of the last attempt: an address that is not valid, or a server that cannot be reached.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Gets a value indicating whether a connection is open, so that the page can leave for the Home page.
        /// </summary>
        public bool IsConnected
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>
        /// Opens the connection to the typed address, or refuses an address that is not an absolute http or https address.
        /// </summary>
        /// <param name="cancellationToken">The token that cancels the request.</param>
        /// <returns>The completion of the attempt.</returns>
        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(this.ServerAddress.Trim(), UriKind.Absolute, out var address) || address.Scheme is not ("http" or "https") || address.UserInfo.Length > 0)
            {
                this.Errors = [InvalidAddress];
                return;
            }

            var result = await this.sessionService.OpenAsync(address, cancellationToken);
            this.Errors = result.IsError ? result.Errors : [];
            this.IsConnected = !result.IsError;
        }
    }
}
