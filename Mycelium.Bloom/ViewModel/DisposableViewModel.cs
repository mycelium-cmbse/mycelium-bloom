// ------------------------------------------------------------------------------------------------
// <copyright file="DisposableViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel
{
    /// <summary>
    /// Provides the dispose pattern and a list of the resources that a Bloom view model releases when it is disposed,
    /// so that no view model repeats the pattern.
    /// </summary>
    public abstract class DisposableViewModel : BloomBaseViewModel, IDisposable
    {
        /// <summary>
        /// Gets the resources that are disposed, in the order they were added, when this view model is disposed.
        /// </summary>
        protected List<IDisposable> Disposables { get; } = [];

        /// <summary>
        /// Releases the resources of this view model.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources of <see cref="Disposables" /> once and empties the list.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release the managed resources; <see langword="false" /> to release only unmanaged ones.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            foreach (var disposable in this.Disposables)
            {
                disposable.Dispose();
            }

            this.Disposables.Clear();
        }
    }
}
