// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModelFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;

    /// <summary>
    /// Creates independent Project Browser ViewModels over shared application-scoped dependencies.
    /// </summary>
    public sealed class ProjectBrowserViewModelFactory : IProjectBrowserViewModelFactory
    {
        /// <summary>
        /// The shared model loader supplied to each created ViewModel.
        /// </summary>
        private readonly IModelLoaderService modelLoaderService;

        /// <summary>
        /// The shared element selection service supplied to each created ViewModel.
        /// </summary>
        private readonly IElementSelectionService elementSelectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserViewModelFactory" /> class.
        /// </summary>
        /// <param name="modelLoaderService">The shared model loader service.</param>
        /// <param name="elementSelectionService">The shared element selection service.</param>
        public ProjectBrowserViewModelFactory(
            IModelLoaderService modelLoaderService,
            IElementSelectionService elementSelectionService)
        {
            ArgumentNullException.ThrowIfNull(modelLoaderService);
            ArgumentNullException.ThrowIfNull(elementSelectionService);

            this.modelLoaderService = modelLoaderService;
            this.elementSelectionService = elementSelectionService;
        }

        /// <inheritdoc />
        public IProjectBrowserViewModel Create()
        {
            return new ProjectBrowserViewModel(
                this.modelLoaderService,
                this.elementSelectionService);
        }
    }
}
