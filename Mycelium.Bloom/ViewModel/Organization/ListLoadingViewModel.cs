// ------------------------------------------------------------------------------------------------
// <copyright file="ListLoadingViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.Collections.ObjectModel;
    using System.Reactive.Linq;

    using DynamicData;

    using ErrorOr;

    using Mycelium.Bloom.Core;

    using ReactiveUI;

    /// <summary>
    /// Loads a list of an organization once and holds it with the failure of the load, for the page that shows it. The rows
    /// live in a source list that others can subscribe to or add to, and a bound collection mirrors it. The load runs on the
    /// synchronization context that the ViewModel was created on, which is the one of the renderer, so that the rows are never
    /// changed while the view reads them. Searching, filtering, counting and paging belong to the view.
    /// </summary>
    /// <remarks>
    /// The list ViewModels of the organization pages share this load so that none of them repeats it: they say what a row
    /// is and where the rows come from, and keep what is theirs (a capability, a child ViewModel).
    /// </remarks>
    /// <typeparam name="TRow">The type of the rows of the list.</typeparam>
    public abstract partial class ListLoadingViewModel<TRow> : DisposableViewModel
    {
        /// <summary>
        /// The identifier of the organization whose list is loaded.
        /// </summary>
        private readonly Guid organizationId;

        /// <summary>
        /// The name of the list in the words of the logs and of the failure, such as "projects".
        /// </summary>
        private readonly string listName;

        /// <summary>
        /// Receives the failure to load the list.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The error reported when the list cannot be retrieved for a reason that the service did not name. The cause is
        /// logged, not shown.
        /// </summary>
        private readonly Error loadFailed;

        /// <summary>
        /// Holds every row of the list.
        /// </summary>
        private readonly SourceList<TRow> source = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ListLoadingViewModel{TRow}" /> class. The load starts when the
        /// derived class calls <see cref="BeginLoad" />, once its own state is ready.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization whose list is loaded.</param>
        /// <param name="listName">The name of the list in the words of the logs and of the failure.</param>
        /// <param name="logger">The logger that receives a failure to load the list.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="listName" /> is empty or white space.</exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="listName" /> or <paramref name="logger" /> is <see langword="null" />.
        /// </exception>
        protected ListLoadingViewModel(Guid organizationId, string listName, ILogger logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(listName);
            ArgumentNullException.ThrowIfNull(logger);

            this.organizationId = organizationId;
            this.listName = listName;
            this.logger = logger;
            this.loadFailed = Error.Unexpected(ErrorCodes.Unavailable, $"The {listName} could not be loaded.");

            var rowBinding = this.source.Connect().ObserveOnRenderContext().Bind(out var rows)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(this.Rows)));
            this.Rows = rows;
            this.Disposables.AddRange([rowBinding, this.source]);
        }

        /// <summary>
        /// Gets a value indicating whether the ViewModel is loading the list.
        /// </summary>
        public bool IsBusy
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = true;

        /// <summary>
        /// Gets the errors that the load returned, or an empty list when it succeeded.
        /// </summary>
        public IReadOnlyList<Error> Errors
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = [];

        /// <summary>
        /// Gets the rows of the list.
        /// </summary>
        public ReadOnlyObservableCollection<TRow> Rows { get; }

        /// <summary>
        /// Gets the source list of the rows, which a later ViewModel can subscribe to or add a created row to.
        /// </summary>
        protected ISourceList<TRow> Source => this.source;

        /// <summary>
        /// Starts loading the list. A derived class calls it last in its constructor, so that the load never runs before the
        /// state that <see cref="LoadRowsAsync" /> reads is ready.
        /// </summary>
        protected void BeginLoad()
        {
            var load = Observable.Return(this.organizationId)
                .LoadLatest((id, cancellationToken) => this.LoadAsync(id, cancellationToken))
                .Subscribe();
            this.Disposables.Add(load);
        }

        /// <summary>
        /// Gets the rows of the list of an organization, in the order that the page shows them.
        /// </summary>
        /// <param name="organizationId">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>The rows, or the errors that tell why they cannot be returned.</returns>
        protected abstract Task<ErrorOr<IReadOnlyList<TRow>>> LoadRowsAsync(Guid organizationId, CancellationToken cancellationToken);

        /// <summary>
        /// Logs the failure to load the list.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="list">The name of the list.</param>
        /// <param name="exception">The failure.</param>
        [LoggerMessage(Level = LogLevel.Error, Message = "The {List} of the organization could not be loaded.")]
        private static partial void LogLoadFailure(ILogger logger, string list, Exception exception);

        /// <summary>
        /// Logs the error code of a refused load of the list.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="list">The name of the list.</param>
        /// <param name="code">The code of the error.</param>
        [LoggerMessage(Level = LogLevel.Warning, Message = "The {List} of the organization could not be loaded: {Code}.")]
        private static partial void LogLoadError(ILogger logger, string list, string code);

        /// <summary>
        /// Loads the list of the organization, then shows it or the failure. A load that the disposal cancelled shows nothing.
        /// </summary>
        /// <param name="id">The identifier of the organization.</param>
        /// <param name="cancellationToken">The token that cancels the load.</param>
        /// <returns>A task that completes when the rows or the failure are shown.</returns>
        private async Task LoadAsync(Guid id, CancellationToken cancellationToken)
        {
            this.IsBusy = true;

            try
            {
                var result = await this.LoadRowsAsync(id, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (result.IsError)
                {
                    LogLoadError(this.logger, this.listName, result.FirstError.Code);
                    this.source.Clear();
                    this.Errors = result.Errors;
                    return;
                }

                this.source.Edit(list =>
                {
                    list.Clear();
                    list.AddRange(result.Value);
                });
                this.Errors = [];
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogLoadFailure(this.logger, this.listName, exception);
                this.source.Clear();
                this.Errors = [this.loadFailed];
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    this.IsBusy = false;
                }
            }
        }
    }
}
