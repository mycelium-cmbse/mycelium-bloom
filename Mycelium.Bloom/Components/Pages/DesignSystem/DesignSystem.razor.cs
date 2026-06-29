// ------------------------------------------------------------------------------------------------
// <copyright file="DesignSystem.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages.DesignSystem
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Internal development page used to verify Bloom reusable UI components.
    /// </summary>
    public partial class DesignSystem : ComponentBase
    {
        /// <summary>
        /// Gets or sets the sample atom search text.
        /// </summary>
        protected string SearchText { get; set; } = "attitude";

        /// <summary>
        /// Gets or sets the sample app header search text.
        /// </summary>
        protected string HeaderSearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the active tab shown in molecule and organism samples.
        /// </summary>
        protected string ActiveDetailTab { get; set; } = "properties";

        /// <summary>
        /// Gets or sets the active tree item shown in model tree samples.
        /// </summary>
        protected string ActiveTreeItemId { get; set; } = "attitude-controller";

        /// <summary>
        /// Gets or sets whether the sample modal shell is open.
        /// </summary>
        protected bool IsSampleModalOpen { get; set; }

        /// <summary>
        /// Gets the sample detail tabs.
        /// </summary>
        protected IReadOnlyList<TabItem> DetailTabs { get; } =
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
            },
            new()
            {
                Value = "simulation",
                Label = "Simulation",
                Disabled = true
            }
        ];

        /// <summary>
        /// Gets the sample breadcrumb trail.
        /// </summary>
        protected IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } =
        [
            new()
            {
                Value = "project",
                Label = "AuroraSat"
            },
            new()
            {
                Value = "adcs",
                Label = "ADCS"
            },
            new()
            {
                Value = "attitude-controller",
                Label = "AttitudeController",
                IsCurrent = true
            }
        ];

        /// <summary>
        /// Gets the sample action menu items.
        /// </summary>
        protected IReadOnlyList<ActionMenuItem> ActionItems { get; } =
        [
            new()
            {
                Value = "open",
                Label = "Open in editor",
                Description = "Inspect the selected SysML element",
                Icon = ">"
            },
            new()
            {
                Value = "baseline",
                Label = "Create baseline",
                Description = "Snapshot current element state",
                Icon = "+"
            },
            new()
            {
                Value = "trace",
                Label = "Show traces",
                Description = "Requirements and verification links",
                Icon = "T"
            },
            new()
            {
                Value = "delete",
                Label = "Delete element",
                Description = "Remove from working model",
                Icon = "x",
                SeparatorBefore = true,
                Variant = ActionMenuItemVariant.Danger
            }
        ];

        /// <summary>
        /// Gets the sample SysML model tree.
        /// </summary>
        protected IReadOnlyList<ModelTreeItem> ModelTreeItems { get; } =
        [
            new()
            {
                Id = "aurorasat",
                Title = "AuroraSat",
                Stereotype = "package",
                ElementColor = "var(--mb-color-sysml-structure-header)",
                Children =
                [
                    new()
                    {
                        Id = "adcs",
                        Title = "ADCS",
                        Stereotype = "part def",
                        Ownership = TreeNodeOwnership.Mine,
                        OwnershipColor = "var(--mb-color-ownership-aocs)",
                        HasComment = true,
                        Children =
                        [
                            new()
                            {
                                Id = "attitude-controller",
                                Title = "AttitudeController",
                                Stereotype = "part",
                                Ownership = TreeNodeOwnership.Mine,
                                OwnershipColor = "var(--mb-color-ownership-aocs)",
                                IsModified = true
                            },
                            new()
                            {
                                Id = "pointing-accuracy",
                                Title = "PointingAccuracy",
                                Stereotype = "constraint def",
                                ElementColor = "var(--mb-color-sysml-verification-header)"
                            }
                        ]
                    },
                    new()
                    {
                        Id = "power",
                        Title = "PowerSubsystem",
                        Stereotype = "part def",
                        Ownership = TreeNodeOwnership.Others,
                        OwnershipColor = "var(--mb-color-ownership-power)",
                        Children =
                        [
                            new()
                            {
                                Id = "battery",
                                Title = "BatteryAssembly",
                                Stereotype = "part"
                            },
                            new()
                            {
                                Id = "solar-array",
                                Title = "SolarArray",
                                Stereotype = "part"
                            }
                        ]
                    }
                ]
            }
        ];

        /// <summary>
        /// Gets the sample table column definitions.
        /// </summary>
        protected IReadOnlyList<DataTableColumn> TableColumns { get; } =
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
                Header = "Element"
            },
            new()
            {
                Key = "type",
                Header = "Type"
            },
            new()
            {
                Key = "owner",
                Header = "Owner"
            },
            new()
            {
                Key = "links",
                Header = "Links",
                IsRightAligned = true,
                Width = "80px"
            }
        ];

        /// <summary>
        /// Gets the sample table rows.
        /// </summary>
        protected IReadOnlyList<IReadOnlyDictionary<string, string>> TableRows { get; } =
        [
            new Dictionary<string, string>
            {
                ["id"] = "REQ-ADCS-011",
                ["name"] = "Pointing accuracy",
                ["type"] = "requirement",
                ["owner"] = "AOCS",
                ["links"] = "4"
            },
            new Dictionary<string, string>
            {
                ["id"] = "BLK-ADCS-002",
                ["name"] = "AttitudeController",
                ["type"] = "part",
                ["owner"] = "AOCS",
                ["links"] = "7"
            },
            new Dictionary<string, string>
            {
                ["id"] = "VER-ADCS-018",
                ["name"] = "Slew response case",
                ["type"] = "verification",
                ["owner"] = "Analysis",
                ["links"] = "3"
            }
        ];

        /// <summary>
        /// Gets the sample status bar items.
        /// </summary>
        protected IReadOnlyList<StatusBarItem> StatusItems { get; } =
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
            },
            new()
            {
                Label = "Validation",
                Value = "1 conflict",
                Variant = StatusIndicatorVariant.Danger,
                ShowIndicator = true
            }
        ];

        /// <summary>
        /// Opens the sample modal shell.
        /// </summary>
        protected void OpenSampleModal()
        {
            this.IsSampleModalOpen = true;
        }

        /// <summary>
        /// Closes the sample modal shell.
        /// </summary>
        protected void CloseSampleModal()
        {
            this.IsSampleModalOpen = false;
        }
    }
}
