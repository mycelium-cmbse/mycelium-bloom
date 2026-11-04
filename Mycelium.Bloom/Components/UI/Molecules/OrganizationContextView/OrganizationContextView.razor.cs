// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContextView.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.OrganizationContextView
{
    using System.Reactive.Linq;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;

    using ReactiveUI;

    /// <summary>
    /// Shows the content of an account or organization page once the organization context is resolved and a loading
    /// state before. When the resolution fails, it shows the not found page for a missing or inaccessible organization
    /// and opens the error page otherwise.
    /// </summary>
    public sealed partial class OrganizationContextView
    {
        /// <summary>
        /// The route of the page shown when the organization context could not be resolved.
        /// </summary>
        private const string ErrorPath = "/Error";

        /// <summary>
        /// Owns the observation of the errors that the organization context reports after the first render.
        /// </summary>
        private IDisposable redirectSubscription;

        /// <summary>
        /// Gets or sets the content of the page, rendered once the organization context is resolved.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the navigation service that shows the not found page or opens the error page.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Redirects at once when the organization context already reports errors, so that a prerendered response
        /// carries the not found status or the redirection, and redirects again when later errors are reported.
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            this.Redirect(this.ViewModel.Errors);

            this.redirectSubscription = this.ViewModel
                .WhenAnyValue(viewModel => viewModel.Errors)
                .Skip(1)
                .Subscribe(errors => _ = this.InvokeAsync(() => this.Redirect(errors)));
        }

        /// <summary>
        /// Stops observing the errors of the organization context.
        /// </summary>
        /// <param name="disposing">A value indicating whether managed resources are being released.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.redirectSubscription?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Leads to the page that the first error designates: a missing or forbidden organization looks the same to the
        /// account, so both show the not found page of the router with a not found status; any other error replaces the
        /// current page with the error page.
        /// </summary>
        /// <param name="errors">The errors of the organization context.</param>
        private void Redirect(IReadOnlyList<Error> errors)
        {
            if (errors.Count == 0)
            {
                return;
            }

            if (errors[0].Type is ErrorType.NotFound or ErrorType.Forbidden)
            {
                this.NavigationManager.NotFound();
            }
            else
            {
                this.NavigationManager.NavigateTo(ErrorPath, replace: true);
            }
        }
    }
}
