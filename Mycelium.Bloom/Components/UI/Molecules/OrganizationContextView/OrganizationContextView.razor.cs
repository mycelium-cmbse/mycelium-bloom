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
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ApplicationErrorState;

    /// <summary>
    /// Shows the content of an account or organization page once the organization context is ready, and a loading,
    /// not found or error state otherwise.
    /// </summary>
    public sealed partial class OrganizationContextView
    {
        /// <summary>
        /// The presentation state of the not found content, created when it is first shown.
        /// </summary>
        private ApplicationErrorStateViewModel notFoundViewModel;

        /// <summary>
        /// The presentation state of the error content, created when it is first shown.
        /// </summary>
        private ApplicationErrorStateViewModel errorViewModel;

        /// <summary>
        /// Gets or sets the content of the page, rendered when the organization context is ready.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets the presentation state of the not found content.
        /// </summary>
        private ApplicationErrorStateViewModel NotFoundViewModel => this.notFoundViewModel ??= new(ApplicationErrorKind.NotFound);

        /// <summary>
        /// Gets the presentation state of the error content.
        /// </summary>
        private ApplicationErrorStateViewModel ErrorViewModel => this.errorViewModel ??= new(ApplicationErrorKind.ServerError);

        /// <summary>
        /// Releases the presentation states owned by this view.
        /// </summary>
        /// <param name="disposing">A value indicating whether managed resources are being released.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.notFoundViewModel?.Dispose();
                this.errorViewModel?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
