// ------------------------------------------------------------------------------------------------
// <copyright file="NotFound.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ApplicationErrorState;

    /// <summary>
    /// Owns the presentation state for an unavailable route.
    /// </summary>
    public partial class NotFound : IDisposable
    {
        /// <summary>
        /// Gets the presentation state owned by this page.
        /// </summary>
        private ApplicationErrorStateViewModel ViewModel { get; } = new(ApplicationErrorKind.NotFound);

        /// <summary>
        /// Releases the presentation state owned by this page.
        /// </summary>
        public void Dispose()
        {
            this.ViewModel.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
