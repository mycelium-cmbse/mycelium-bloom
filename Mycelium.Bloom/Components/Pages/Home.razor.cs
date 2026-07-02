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

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Represents the Bloom workspace prototype home page.
    /// </summary>
    public partial class Home : ComponentBase
    {
        /// <summary>
        /// Gets or sets the project browser view model service.
        /// </summary>
        [Inject]
        public IProjectBrowserViewModelService ProjectBrowserViewModelService { get; set; }

        /// <summary>
        /// Gets or sets the current workspace search text.
        /// </summary>
        private string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets the selected model element name.
        /// </summary>
        private string SelectedModelElementName
        {
            get
            {
                var title = this.ProjectBrowserViewModel?.SelectedNode?.DisplayName;

                return string.IsNullOrWhiteSpace(title) ? "None" : title;
            }
        }

        /// <summary>
        /// Gets or sets the project browser view model.
        /// </summary>
        private IProjectBrowserViewModel ProjectBrowserViewModel { get; set; }

        /// <summary>
        /// Gets the workspace status bar items.
        /// </summary>
        private IReadOnlyList<StatusBarItem> StatusItems { get; } =
        [
            new()
            {
                Label = "Model",
                Value = "Quantities",
                Variant = StatusIndicatorVariant.Success,
                ShowIndicator = true
            },
            new()
            {
                Label = "Browser",
                Value = "Tree view",
                Variant = StatusIndicatorVariant.Info,
                ShowIndicator = true
            },
            new()
            {
                Label = "Source",
                Value = "SysML2.NET"
            }
        ];

        /// <summary>
        /// Initializes the workspace page state.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ProjectBrowserViewModel = this.ProjectBrowserViewModelService.CreateQuantitiesProjectBrowserViewModel();
        }

        /// <summary>
        /// Handles selecting a project browser node.
        /// </summary>
        /// <param name="node">The selected project browser node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static Task HandleProjectBrowserNodeSelectedAsync(ProjectBrowserNodeViewModel node)
        {
            _ = node;

            return Task.CompletedTask;
        }
    }
}
