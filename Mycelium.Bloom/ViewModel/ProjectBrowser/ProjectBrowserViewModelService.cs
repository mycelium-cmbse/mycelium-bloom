// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserViewModelService.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using Mycelium.Bloom.Core.ModelLoading;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Creates project browser view models from SysML models.
    /// </summary>
    public sealed class ProjectBrowserViewModelService : IProjectBrowserViewModelService
    {
        private readonly IModelLoaderService modelLoaderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserViewModelService" /> class.
        /// </summary>
        /// <param name="modelLoaderService">The model loader service used to retrieve cached SysML models.</param>
        public ProjectBrowserViewModelService(IModelLoaderService modelLoaderService)
        {
            ArgumentNullException.ThrowIfNull(modelLoaderService);

            this.modelLoaderService = modelLoaderService;
        }

        /// <summary>
        /// Creates a project browser view model for the Quantities model.
        /// </summary>
        /// <returns>A fresh project browser view model instance.</returns>
        public IProjectBrowserViewModel CreateQuantitiesProjectBrowserViewModel()
        {
            var viewModel = new ProjectBrowserViewModel(this.modelLoaderService);

            return viewModel;
        }

        /// <summary>
        /// Creates a project browser view model initialized from the provided namespace root.
        /// </summary>
        /// <param name="namespaceRoot">The SysML namespace root used to initialize the project browser.</param>
        /// <returns>A fresh project browser view model instance.</returns>
        public IProjectBrowserViewModel CreateFromNamespace(INamespace namespaceRoot)
        {
            ArgumentNullException.ThrowIfNull(namespaceRoot);

            var viewModel = new ProjectBrowserViewModel(this.modelLoaderService);

            viewModel.Initialize(namespaceRoot);

            return viewModel;
        }
    }
}
