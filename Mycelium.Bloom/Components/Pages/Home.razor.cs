// ------------------------------------------------------------------------------------------------
// <copyright file="Home.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Represents the Bloom home page with the issue #8 Project Browser feature.
    /// </summary>
    public partial class Home : ComponentBase
    {
        /// <summary>
        /// Gets or sets the project browser view model service.
        /// </summary>
        [Inject]
        public IProjectBrowserViewModelService ProjectBrowserViewModelService { get; set; }

        /// <summary>
        /// Gets the selected model element name.
        /// </summary>
        private string SelectedModelElementName
        {
            get
            {
                var displayName = this.ProjectBrowserViewModel?.SelectedNode?.DisplayName;

                return string.IsNullOrWhiteSpace(displayName) ? "None" : displayName;
            }
        }

        /// <summary>
        /// Gets or sets the project browser view model.
        /// </summary>
        private IProjectBrowserViewModel ProjectBrowserViewModel { get; set; }

        /// <summary>
        /// Initializes the project browser state used by the home page.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ProjectBrowserViewModel = this.ProjectBrowserViewModelService.CreateQuantitiesProjectBrowserViewModel();
        }

        /// <summary>
        /// Handles project browser node selection changes.
        /// </summary>
        /// <param name="node">The selected project browser node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleProjectBrowserNodeSelectedAsync(ProjectBrowserNodeViewModel node)
        {
            _ = node;

            return Task.CompletedTask;
        }
    }
}
