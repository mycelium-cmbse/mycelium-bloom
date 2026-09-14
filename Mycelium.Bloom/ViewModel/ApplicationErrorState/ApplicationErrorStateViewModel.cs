// ------------------------------------------------------------------------------------------------
// <copyright file="ApplicationErrorStateViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ApplicationErrorState
{
    using System.Reactive.Linq;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;
    using ReactiveUI.Primitives.Concurrency;

    /// <summary>
    /// Owns error presentation state updated by its page on the Blazor renderer.
    /// </summary>
    public sealed class ApplicationErrorStateViewModel : ReactiveObject, IDisposable
    {
        /// <summary>
        /// Observes whether the normalized request reference is available.
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> hasReferenceId;

        /// <summary>
        /// Indicates whether the page has released this ViewModel.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationErrorStateViewModel" /> class.
        /// </summary>
        /// <param name="kind">The error presentation selected by the page.</param>
        /// <param name="referenceId">The optional request reference.</param>
        public ApplicationErrorStateViewModel(ApplicationErrorKind kind, string referenceId = null)
        {
            this.Kind = kind;

            var modelling = new ErrorRecoveryAction { Label = "Back to Modelling", Href = "/workspace/modeling" };

            switch (kind)
            {
                case ApplicationErrorKind.NotFound:
                    this.Title = "This branch leads nowhere.";
                    this.Description = "The page may have moved or the route no longer exists.";
                    this.PageTitle = "Page unavailable | Mycelium Bloom";
                    this.PrimaryAction = modelling;
                    this.SecondaryAction = new ErrorRecoveryAction { Label = "Go back", BrowserAction = "back" };
                    break;
                case ApplicationErrorKind.ServerError:
                    this.Title = "Something failed to resolve.";
                    this.Description = "Mycelium encountered an unexpected error while processing this request.";
                    this.PageTitle = "Request error | Mycelium Bloom";
                    this.PrimaryAction = new ErrorRecoveryAction { Label = "Reload", BrowserAction = "reload" };
                    this.SecondaryAction = modelling;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported application error kind.");
            }

            this.ReferenceId = referenceId;
            this.hasReferenceId = this.WhenAnyValue(viewModel => viewModel.ReferenceId)
                .Select(value => value is not null)
                .ToProperty(this, viewModel => viewModel.HasReferenceId, scheduler: ImmediateSequencer.Instance);
        }

        /// <summary>
        /// Gets the error kind selected for this page lifetime.
        /// </summary>
        public ApplicationErrorKind Kind { get; }

        /// <summary>
        /// Gets the page heading.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the supporting explanation.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the browser document title.
        /// </summary>
        public string PageTitle { get; }

        /// <summary>
        /// Gets the primary recovery action.
        /// </summary>
        public ErrorRecoveryAction PrimaryAction { get; }

        /// <summary>
        /// Gets the secondary recovery action.
        /// </summary>
        public ErrorRecoveryAction SecondaryAction { get; }

        /// <summary>
        /// Gets or sets the request reference, preserving nonblank values and normalizing missing references to null.
        /// </summary>
        public string ReferenceId
        {
            get;
            set
            {
                ObjectDisposedException.ThrowIf(this.isDisposed, this);
                this.RaiseAndSetIfChanged(ref field, string.IsNullOrWhiteSpace(value) ? null : value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the request reference is available.
        /// </summary>
        public bool HasReferenceId => this.hasReferenceId.Value;

        /// <summary>
        /// Releases the page-owned reference availability observation.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.hasReferenceId.Dispose();
        }
    }
}
