// ------------------------------------------------------------------------------------------------
// <copyright file="BloomBaseViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel
{
    /// <summary>
    /// Provides common loading and error state for Bloom view models.
    /// </summary>
    public abstract class BloomBaseViewModel
    {
        /// <summary>
        /// Gets a value indicating whether the view model is loading.
        /// </summary>
        public bool IsLoading { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the view model has loaded.
        /// </summary>
        public bool IsLoaded { get; protected set; }

        /// <summary>
        /// Gets the view model loading error message.
        /// </summary>
        public string ErrorMessage { get; protected set; } = string.Empty;

        /// <summary>
        /// Marks the view model as loading and clears previous errors.
        /// </summary>
        protected void StartLoading()
        {
            this.IsLoading = true;
            this.ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Marks the view model as no longer loading.
        /// </summary>
        protected void StopLoading()
        {
            this.IsLoading = false;
        }

        /// <summary>
        /// Marks the view model as successfully loaded and clears previous errors.
        /// </summary>
        protected void SetLoaded()
        {
            this.IsLoaded = true;
            this.ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Marks the view model as not loaded and stores the loading error.
        /// </summary>
        /// <param name="errorMessage">The loading error message.</param>
        protected void SetError(string errorMessage)
        {
            this.IsLoaded = false;
            this.ErrorMessage = errorMessage ?? string.Empty;
        }
    }
}
