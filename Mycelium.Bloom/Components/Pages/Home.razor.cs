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

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Represents the Bloom workspace prototype home page.
    /// </summary>
    public partial class Home : ComponentBase
    {
        /// <summary>
        /// Gets or sets the current workspace search text.
        /// </summary>
        private string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the active detail panel tab.
        /// </summary>
        private string ActiveDetailTab { get; set; } = "properties";

        /// <summary>
        /// Gets or sets the model loader service.
        /// </summary>
        [Inject]
        public IModelLoaderService ModelLoaderService { get; set; }

        /// <summary>
        /// Gets the project browser view model.
        /// </summary>
        private ProjectBrowserViewModel ProjectBrowserViewModel { get; } = new();

        /// <summary>
        /// Gets or sets the selected project browser node name.
        /// </summary>
        private string SelectedProjectBrowserNodeName { get; set; } = "None";

        /// <summary>
        /// Gets the detail panel tabs.
        /// </summary>
        private IReadOnlyList<TabItem> DetailTabs { get; } =
        [
            new()
            {
                Value = "properties",
                Label = "Properties"
            },
            new()
            {
                Value = "relations",
                Label = "Relations"
            },
            new()
            {
                Value = "history",
                Label = "History"
            }
        ];

        /// <summary>
        /// Gets the requirement trace table columns.
        /// </summary>
        private IReadOnlyList<DataTableColumn> TableColumns { get; } =
        [
            new()
            {
                Key = "id",
                Header = "ID",
                IsMonospace = true,
                Width = "140px"
            },
            new()
            {
                Key = "name",
                Header = "Trace"
            },
            new()
            {
                Key = "status",
                Header = "Status"
            }
        ];

        /// <summary>
        /// Gets the requirement trace table rows.
        /// </summary>
        private IReadOnlyList<IReadOnlyDictionary<string, string>> TableRows { get; } =
        [
            new Dictionary<string, string>
            {
                ["id"] = "REQ-ADCS-011",
                ["name"] = "Pointing accuracy",
                ["status"] = "Satisfied"
            },
            new Dictionary<string, string>
            {
                ["id"] = "VER-ADCS-018",
                ["name"] = "Slew response case",
                ["status"] = "Pending review"
            },
            new Dictionary<string, string>
            {
                ["id"] = "ALLOC-ADCS-004",
                ["name"] = "Controller to flight software",
                ["status"] = "Linked"
            }
        ];

        /// <summary>
        /// Gets the workspace status bar items.
        /// </summary>
        private IReadOnlyList<StatusBarItem> StatusItems { get; } =
        [
            new()
            {
                Label = "Sync",
                Value = "Up to date",
                Variant = StatusIndicatorVariant.Success,
                ShowIndicator = true
            },
            new()
            {
                Label = "Review",
                Value = "2 pending",
                Variant = StatusIndicatorVariant.Warning,
                ShowIndicator = true
            },
            new()
            {
                Label = "Elements",
                Value = "1,248"
            }
        ];

        /// <summary>
        /// Initializes the page model tree from the cached Quantities model.
        /// </summary>
        protected override void OnInitialized()
        {
            var model = this.ModelLoaderService.LoadQuantitiesModel();

            this.ProjectBrowserViewModel.Initialize(model);
        }

        private Task SelectProjectBrowserNodeAsync(ProjectBrowserNodeViewModel node)
        {
            this.SelectedProjectBrowserNodeName = node.DisplayName;

            return Task.CompletedTask;
        }
    }
}
