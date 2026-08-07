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
    using ReactiveUI;

    /// <summary>
    /// Provides common loading and error state for Bloom view models.
    /// </summary>
    public abstract class BloomBaseViewModel : ReactiveObject
    {
        /// <summary>
        /// A value indicating whether the view model has loaded.
        /// </summary>
        private bool isLoaded;

        /// <summary>
        /// A value indicating whether the view model is loading.
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// The view model loading error message.
        /// </summary>
        private string errorMessage = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the view model is loading.
        /// </summary>
        public bool IsLoading
        {
            get => this.isLoading;
            protected set => this.RaiseAndSetIfChanged(ref this.isLoading, value);
        }

        /// <summary>
        /// Gets a value indicating whether the view model has loaded.
        /// </summary>
        public bool IsLoaded
        {
            get => this.isLoaded;
            protected set => this.RaiseAndSetIfChanged(ref this.isLoaded, value);
        }

        /// <summary>
        /// Gets the view model loading error message.
        /// </summary>
        public string ErrorMessage
        {
            get => this.errorMessage;
            protected set => this.RaiseAndSetIfChanged(ref this.errorMessage, value);
        }

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
        /// <param name="errorMsg">The loading error message.</param>
        protected void SetError(string errorMsg)
        {
            this.IsLoaded = false;
            this.ErrorMessage = errorMsg ?? string.Empty;
        }
    }
}
