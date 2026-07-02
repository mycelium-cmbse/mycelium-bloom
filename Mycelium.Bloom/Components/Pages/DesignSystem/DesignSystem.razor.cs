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
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Internal development page used to verify Bloom reusable UI components.
    /// </summary>
    public partial class DesignSystem : ComponentBase
    {
        private const string ReactionWheelAssemblyName = "ReactionWheelAssembly";
        private const string ReactionWheelAssemblyId = "reaction-wheel-assembly";
        private const string SpacecraftName = "Spacecraft";
        private const string AocsName = "AOCS";
        private const string AocsPackageQualifiedName = SpacecraftName + "::" + AocsName;
        private const string ReactionWheelAssemblyQualifiedName = AocsPackageQualifiedName + "::" + ReactionWheelAssemblyName;
        private const string SearchInputPlaceholder = "Search " + SpacecraftName + ", " + AocsName + ", or " + ReactionWheelAssemblyName;
        private const string ReviewLabel = "Review";
        private const string ProjectAdminName = "Project Admin";
        private const string ProjectAdminInitials = "PA";
        private const string AocsLeadName = "AOCS Lead";
        private const string OwnershipAocsColor = "var(--mb-color-ownership-aocs)";
        private const string ProjectAdminColor = "var(--mb-color-collaborator-c08)";
        private const string AocsLeadColor = "var(--mb-color-collaborator-c10)";
        private const string PackageElementKind = "package";
        private const string OwnerColumnKey = "owner";
        private const string LinksColumnKey = "links";

        /// <summary>
        /// The sample active canvas tool.
        /// </summary>
        protected CanvasTool ActiveCanvasTool = CanvasTool.Select;

        /// <summary>
        /// The sample canvas zoom percentage.
        /// </summary>
        protected int CanvasZoomPercentage = 100;

        /// <summary>
        /// Gets or sets the project browser view model service.
        /// </summary>
        [Inject]
        public IProjectBrowserViewModelService ProjectBrowserViewModelService { get; set; }

        /// <summary>
        /// Gets or sets the sample atom search text.
        /// </summary>
        protected string SearchText { get; set; } = "reaction wheel";

        /// <summary>
        /// Gets or sets the sample unchecked checkbox value.
        /// </summary>
        protected bool UncheckedCheckboxValue { get; set; }

        /// <summary>
        /// Gets or sets the sample checked checkbox value.
        /// </summary>
        protected bool CheckedCheckboxValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the sample disabled checkbox value.
        /// </summary>
        protected bool DisabledCheckboxValue { get; set; }

        /// <summary>
        /// Gets or sets the sample described checkbox value.
        /// </summary>
        protected bool DescribedCheckboxValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the sample off toggle value.
        /// </summary>
        protected bool OffToggleValue { get; set; }

        /// <summary>
        /// Gets or sets the sample on toggle value.
        /// </summary>
        protected bool OnToggleValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the sample disabled toggle value.
        /// </summary>
        protected bool DisabledToggleValue { get; set; }

        /// <summary>
        /// Gets or sets the sample state text toggle value.
        /// </summary>
        protected bool StateTextToggleValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the sample default text input value.
        /// </summary>
        protected string DefaultTextInputValue { get; set; } = ReactionWheelAssemblyName;

        /// <summary>
        /// Gets or sets the sample text input value with help text.
        /// </summary>
        protected string HelpTextInputValue { get; set; } = "Spacecraft::AOCS";

        /// <summary>
        /// Gets or sets the sample text input value with error text.
        /// </summary>
        protected string ErrorTextInputValue { get; set; } = "REQ ADCS 042";

        /// <summary>
        /// Gets or sets the sample disabled text input value.
        /// </summary>
        protected string DisabledTextInputValue { get; set; } = "Baseline locked";

        /// <summary>
        /// Gets or sets the sample adorned text input value.
        /// </summary>
        protected string AdornedTextInputValue { get; set; } = "0.05";

        /// <summary>
        /// Gets or sets the sample default text area value.
        /// </summary>
        protected string DefaultTextAreaValue { get; set; } = "Package notes and review context.";

        /// <summary>
        /// Gets or sets the sample text area value with help text.
        /// </summary>
        protected string HelpTextAreaValue { get; set; } = "Captures assumptions for the ADCS workspace review.";

        /// <summary>
        /// Gets or sets the sample text area value with error text.
        /// </summary>
        protected string ErrorTextAreaValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sample disabled text area value.
        /// </summary>
        protected string DisabledTextAreaValue { get; set; } = "This baseline comment is locked.";

        /// <summary>
        /// Gets or sets the sample read-only text area value.
        /// </summary>
        protected string ReadOnlyTextAreaValue { get; set; } = "Generated from the current SysML package summary.";

        /// <summary>
        /// Gets or sets the sample counted text area value.
        /// </summary>
        protected string CountedTextAreaValue { get; set; } = "Ready for subsystem owner review.";

        /// <summary>
        /// Gets or sets the sample default select input value.
        /// </summary>
        protected string DefaultSelectInputValue { get; set; } = "review";

        /// <summary>
        /// Gets or sets the sample select input value with help text.
        /// </summary>
        protected string HelpSelectInputValue { get; set; } = "aocs";

        /// <summary>
        /// Gets or sets the sample select input value with error text.
        /// </summary>
        protected string ErrorSelectInputValue { get; set; } = "external";

        /// <summary>
        /// Gets or sets the sample disabled select input value.
        /// </summary>
        protected string DisabledSelectInputValue { get; set; } = "locked";

        /// <summary>
        /// Gets or sets the sample adorned select input value.
        /// </summary>
        protected string AdornedSelectInputValue { get; set; } = "power";

        /// <summary>
        /// Gets or sets the sample SysML select input value.
        /// </summary>
        protected string SysmlSelectInputValue { get; set; } = "part";

        /// <summary>
        /// Gets or sets the sample app header search text.
        /// </summary>
        protected string HeaderSearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sample new comment value.
        /// </summary>
        protected string NewCommentValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the selected sample project identifier.
        /// </summary>
        protected string SelectedProjectId { get; set; } = "spacecraft-model";

        /// <summary>
        /// Gets or sets the active tab shown in molecule and organism samples.
        /// </summary>
        protected string ActiveDetailTab { get; set; } = "properties";

        /// <summary>
        /// Gets or sets the active tree item shown in model tree samples.
        /// </summary>
        protected string ActiveTreeItemId { get; set; } = ReactionWheelAssemblyId;

        /// <summary>
        /// Gets the sample typography rows.
        /// </summary>
        private IReadOnlyList<TypographySample> TypographySamples { get; } =
        [
            new()
            {
                CssClass = "mb-text-display-lg",
                Text = "Spacecraft Architecture",
                Color = "var(--mb-color-text-primary)"
            },
            new()
            {
                CssClass = "mb-text-heading-md",
                Text = "AOCS model element overview",
                Color = "var(--mb-color-text-primary)"
            },
            new()
            {
                CssClass = "mb-text-body-md",
                Text = "The active package contains structural, behavioral, requirement, and verification views.",
                Color = "var(--mb-color-text-secondary)"
            },
            new()
            {
                CssClass = "mb-text-mono-sm",
                Text = "Spacecraft::AOCS::ReactionWheelAssembly",
                Color = "var(--mb-color-text-muted)"
            }
        ];

        /// <summary>
        /// Gets the sample foundation color chips.
        /// </summary>
        private IReadOnlyList<ChipSample> FoundationColorChips { get; } =
        [
            new()
            {
                Label = "AOCS",
                Variant = ChipVariant.Ownership,
                Color = OwnershipAocsColor
            },
            new()
            {
                Label = "Power",
                Variant = ChipVariant.Ownership,
                Color = "var(--mb-color-ownership-power)"
            },
            new()
            {
                Label = "Comms",
                Variant = ChipVariant.Ownership,
                Color = "var(--mb-color-ownership-comms)"
            },
            new()
            {
                Label = "Software",
                Variant = ChipVariant.Ownership,
                Color = "var(--mb-color-ownership-software)"
            },
            new()
            {
                Label = ReviewLabel,
                Variant = ChipVariant.Lifecycle,
                Color = "var(--mb-color-lifecycle-review)"
            }
        ];

        /// <summary>
        /// Gets the sample status indicator entries.
        /// </summary>
        private IReadOnlyList<StatusIndicatorSample> StatusIndicatorSamples { get; } =
        [
            new()
            {
                Label = "Synced",
                Variant = StatusIndicatorVariant.Success
            },
            new()
            {
                Label = ReviewLabel,
                Variant = StatusIndicatorVariant.Warning
            },
            new()
            {
                Label = "Conflict",
                Variant = StatusIndicatorVariant.Danger
            },
            new()
            {
                Label = "Indexing",
                Variant = StatusIndicatorVariant.Info
            },
            new()
            {
                Label = "Idle",
                Variant = StatusIndicatorVariant.Neutral
            }
        ];

        /// <summary>
        /// Gets the sample project browser view model.
        /// </summary>
        protected IProjectBrowserViewModel ProjectBrowserViewModel { get; private set; }

        /// <summary>
        /// Gets or sets whether the sample modal shell is open.
        /// </summary>
        protected bool IsSampleModalOpen { get; set; }

        /// <summary>
        /// Gets or sets whether the default sample confirmation dialog is open.
        /// </summary>
        protected bool IsDefaultConfirmDialogOpen { get; set; }

        /// <summary>
        /// Gets or sets whether the warning sample confirmation dialog is open.
        /// </summary>
        protected bool IsWarningConfirmDialogOpen { get; set; }

        /// <summary>
        /// Gets or sets whether the danger sample confirmation dialog is open.
        /// </summary>
        protected bool IsDangerConfirmDialogOpen { get; set; }

        /// <summary>
        /// Gets the sample informational toast notification.
        /// </summary>
        protected ToastNotification InfoToastNotification { get; } = new()
        {
            Id = "sample-info",
            Title = "Model indexing started",
            Message = "The workspace index is being refreshed.",
            Variant = ToastNotificationVariant.Info,
            IsDismissible = true
        };

        /// <summary>
        /// Gets the sample success toast notification.
        /// </summary>
        protected ToastNotification SuccessToastNotification { get; } = new()
        {
            Id = "sample-success",
            Title = "Changes saved",
            Message = "The selected model elements were persisted.",
            Variant = ToastNotificationVariant.Success,
            IsDismissible = true
        };

        /// <summary>
        /// Gets the sample warning toast notification.
        /// </summary>
        protected ToastNotification WarningToastNotification { get; } = new()
        {
            Id = "sample-warning",
            Title = "Review needed",
            Message = "One requirement trace needs owner confirmation.",
            Variant = ToastNotificationVariant.Warning,
            IsDismissible = true
        };

        /// <summary>
        /// Gets the sample danger toast notification.
        /// </summary>
        protected ToastNotification DangerToastNotification { get; } = new()
        {
            Id = "sample-danger",
            Title = "Validation conflict",
            Message = "A constraint update conflicts with the active baseline.",
            Variant = ToastNotificationVariant.Danger,
            IsDismissible = true
        };

        /// <summary>
        /// Gets the sample toast notification stack.
        /// </summary>
        protected IReadOnlyList<ToastNotification> SampleToastNotifications { get; } =
        [
            new()
            {
                Id = "container-info",
                Title = "Branch synced",
                Message = "Latest workspace changes are available.",
                Variant = ToastNotificationVariant.Info,
                IsDismissible = true
            },
            new()
            {
                Id = "container-success",
                Title = "Baseline created",
                Message = "A new review baseline is ready.",
                Variant = ToastNotificationVariant.Success,
                IsDismissible = true
            },
            new()
            {
                Id = "container-warning",
                Title = "Ownership pending",
                Message = "Two elements are waiting for subsystem owner review.",
                Variant = ToastNotificationVariant.Warning,
                IsDismissible = true
            }
        ];

        /// <summary>
        /// Gets the sample workspace collaborators.
        /// </summary>
        protected IReadOnlyList<CollaboratorStackItem> CollaboratorStackItems { get; } =
        [
            new()
            {
                Id = "collaborator-project-admin",
                Name = ProjectAdminName,
                Initials = "PA",
                Color = ProjectAdminColor,
                Role = ProjectAdminName,
                IsOnline = true,
                IsCurrentUser = true
            },
            new()
            {
                Id = "collaborator-aocs-lead",
                Name = AocsLeadName,
                Initials = "AL",
                Color = AocsLeadColor,
                Role = "Subsystem owner",
                IsOnline = true
            },
            new()
            {
                Id = "collaborator-reviewer",
                Name = "Review Lead",
                Initials = "RL",
                Color = "var(--mb-color-collaborator-c05)",
                Role = "Reviewer",
                IsOnline = true
            },
            new()
            {
                Id = "collaborator-fabric-sync",
                Name = "Fabric Sync",
                Initials = "FS",
                Color = "var(--mb-color-brand-700)",
                Role = "Model sync",
                IsOnline = true
            },
            new()
            {
                Id = "collaborator-viewer",
                Name = "Workspace Viewer",
                Initials = "WV",
                Color = "var(--mb-color-neutral-500)",
                Role = "Viewer"
            }
        ];

        /// <summary>
        /// Gets the sample collaboration comments.
        /// </summary>
        protected IReadOnlyList<CommentThreadItem> CommentItems { get; } =
        [
            new()
            {
                Id = "comment-multiplicity",
                AuthorName = AocsLeadName,
                AuthorInitials = "AL",
                AuthorColor = AocsLeadColor,
                CreatedAtText = "12 min ago",
                Body = "Can we verify the commandedAxis multiplicity before this part definition is committed?",
                IsEdited = true
            },
            new()
            {
                Id = "comment-sysml-check",
                AuthorName = ProjectAdminName,
                AuthorInitials = "PA",
                AuthorColor = ProjectAdminColor,
                CreatedAtText = "8 min ago",
                Body = "I will check it against the loaded SysML model and align the property row if the source differs.",
                IsCurrentUser = true
            },
            new()
            {
                Id = "comment-resolved-trace",
                AuthorName = AocsLeadName,
                AuthorInitials = "AL",
                AuthorColor = AocsLeadColor,
                CreatedAtText = "Yesterday",
                Body = "Resolved the requirement trace after linking REQ-ADCS-042 to the verification case.",
                IsResolved = true
            }
        ];

        /// <summary>
        /// Gets the sample element history timeline.
        /// </summary>
        protected IReadOnlyList<HistoryTimelineItem> HistoryTimelineItems { get; } =
        [
            new()
            {
                Id = "history-created-reaction-wheel-assembly",
                Title = "Created ReactionWheelAssembly",
                Description = "Added the part definition under Spacecraft::AOCS for wheel cluster modeling.",
                ActorName = ProjectAdminName,
                ActorInitials = "PA",
                ActorColor = ProjectAdminColor,
                TimestampText = "Today, 09:12",
                Variant = HistoryTimelineItemVariant.Created
            },
            new()
            {
                Id = "history-updated-multiplicity",
                Title = "Updated multiplicity",
                Description = "Changed wheel[3..4] to wheel[4] after subsystem owner review.",
                ActorName = AocsLeadName,
                ActorInitials = "AL",
                ActorColor = AocsLeadColor,
                TimestampText = "Today, 09:38",
                Variant = HistoryTimelineItemVariant.Updated
            },
            new()
            {
                Id = "history-commented-mass-property",
                Title = "Commented on mass property",
                Description = "Requested confirmation that the assembly mass includes harness and mounting hardware.",
                ActorName = "Review Lead",
                ActorInitials = "RL",
                ActorColor = "var(--mb-color-collaborator-c05)",
                TimestampText = "Today, 10:04",
                Variant = HistoryTimelineItemVariant.Commented
            },
            new()
            {
                Id = "history-reviewed-project-admin",
                Title = "Reviewed by Project Admin",
                Description = "Approved the ADCS package change for the current review baseline.",
                ActorName = ProjectAdminName,
                ActorInitials = "PA",
                ActorColor = "var(--mb-color-brand-700)",
                TimestampText = "Today, 10:26",
                Variant = HistoryTimelineItemVariant.Reviewed
            },
            new()
            {
                Id = "history-synced-fabric",
                Title = "Synced with Fabric",
                Description = "Published the latest element history to the shared workspace.",
                ActorName = "Fabric Sync",
                ActorInitials = "FS",
                ActorColor = "var(--mb-color-success-500)",
                TimestampText = "Today, 10:31",
                Variant = HistoryTimelineItemVariant.Synced
            }
        ];

        /// <summary>
        /// Gets the sample project activity feed.
        /// </summary>
        protected IReadOnlyList<ActivityFeedItem> ActivityFeedItems { get; } =
        [
            new()
            {
                Id = "activity-created-reaction-wheel-assembly",
                Title = "Project Admin created ReactionWheelAssembly",
                Description = "Added the reaction wheel assembly to the active ADCS workspace.",
                ActorName = ProjectAdminName,
                ActorInitials = "PA",
                ActorColor = ProjectAdminColor,
                TimestampText = "Today, 09:12",
                TargetName = ReactionWheelAssemblyName,
                TargetQualifiedName = "Spacecraft::AOCS::ReactionWheelAssembly",
                Variant = ActivityFeedItemVariant.Created
            },
            new()
            {
                Id = "activity-reviewed-aocs-package",
                Title = "AOCS Lead reviewed AOCS package",
                Description = "Marked the current package changes ready for project administrator approval.",
                ActorName = AocsLeadName,
                ActorInitials = "AL",
                ActorColor = AocsLeadColor,
                TimestampText = "Today, 09:46",
                TargetName = "AOCS",
                TargetQualifiedName = "Spacecraft::AOCS",
                Variant = ActivityFeedItemVariant.Reviewed
            },
            new()
            {
                Id = "activity-synced-model",
                Title = "Fabric synced the model",
                Description = "Pulled the latest committed workspace state into Bloom.",
                ActorName = "Fabric",
                ActorInitials = "FB",
                ActorColor = "var(--mb-color-brand-700)",
                TimestampText = "Today, 10:02",
                TargetName = "Workspace model",
                TargetQualifiedName = SpacecraftName,
                Variant = ActivityFeedItemVariant.Synced
            },
            new()
            {
                Id = "activity-commented-mass-property",
                Title = "A collaborator commented on mass property",
                Description = "Asked whether the reported assembly mass includes harness and mounting hardware.",
                ActorName = "Review Lead",
                ActorInitials = "RL",
                ActorColor = "var(--mb-color-collaborator-c05)",
                TimestampText = "Today, 10:14",
                TargetName = "mass",
                TargetQualifiedName = "Spacecraft::AOCS::ReactionWheelAssembly::mass",
                Variant = ActivityFeedItemVariant.Commented
            },
            new()
            {
                Id = "activity-member-joined-workspace",
                Title = "A project member joined the workspace",
                Description = "Opened the shared Spacecraft workspace for concurrent design review.",
                ActorName = "Project Member",
                ActorInitials = "PM",
                ActorColor = "var(--mb-color-success-500)",
                TimestampText = "Today, 10:28",
                TargetName = "Spacecraft workspace",
                TargetQualifiedName = SpacecraftName,
                Variant = ActivityFeedItemVariant.Joined
            }
        ];

        /// <summary>
        /// Gets the sample lifecycle select options.
        /// </summary>
        protected IReadOnlyList<SelectInputOption> LifecycleSelectOptions { get; } =
        [
            new()
            {
                Value = "preparation",
                Label = "Preparation"
            },
            new()
            {
                Value = "open",
                Label = "Open"
            },
            new()
            {
                Value = "review",
                Label = ReviewLabel
            },
            new()
            {
                Value = "archived",
                Label = "Archived",
                Disabled = true
            }
        ];

        /// <summary>
        /// Gets the sample subsystem select options.
        /// </summary>
        protected IReadOnlyList<SelectInputOption> SubsystemSelectOptions { get; } =
        [
            new()
            {
                Value = "aocs",
                Label = "AOCS"
            },
            new()
            {
                Value = "power",
                Label = "Power"
            },
            new()
            {
                Value = "comms",
                Label = "Comms"
            },
            new()
            {
                Value = "external",
                Label = "External supplier",
                Disabled = true
            }
        ];

        /// <summary>
        /// Gets the sample access select options.
        /// </summary>
        protected IReadOnlyList<SelectInputOption> AccessSelectOptions { get; } =
        [
            new()
            {
                Value = "editable",
                Label = "Editable"
            },
            new()
            {
                Value = "readonly",
                Label = "Read-only"
            },
            new()
            {
                Value = "locked",
                Label = "Locked"
            }
        ];

        /// <summary>
        /// Gets the sample SysML select options.
        /// </summary>
        protected IReadOnlyList<SelectInputOption> SysmlSelectOptions { get; } =
        [
            new()
            {
                Value = "part",
                Label = "Part"
            },
            new()
            {
                Value = "requirement",
                Label = "Requirement"
            },
            new()
            {
                Value = "interface",
                Label = "Interface"
            },
            new()
            {
                Value = PackageElementKind,
                Label = "Package"
            }
        ];

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
                Label = SpacecraftName
            },
            new()
            {
                Value = "aocs",
                Label = "AOCS"
            },
            new()
            {
                Value = ReactionWheelAssemblyId,
                Label = ReactionWheelAssemblyName,
                IsCurrent = true
            }
        ];

        /// <summary>
        /// Gets the sample canvas breadcrumb trail.
        /// </summary>
        protected IReadOnlyList<BreadcrumbItem> CanvasBreadcrumbs { get; } =
        [
            new()
            {
                Value = "spacecraft",
                Label = SpacecraftName
            },
            new()
            {
                Value = "aocs",
                Label = "AOCS"
            },
            new()
            {
                Value = ReactionWheelAssemblyId,
                Label = ReactionWheelAssemblyName,
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
        /// Gets the sample user menu items.
        /// </summary>
        protected IReadOnlyList<ActionMenuItem> UserMenuItems { get; } =
        [
            new()
            {
                Value = "profile",
                Label = "Profile",
                Description = "View account details",
                Icon = "P"
            },
            new()
            {
                Value = "preferences",
                Label = "Preferences",
                Description = "Workspace and editor settings",
                Icon = "G"
            },
            new()
            {
                Value = "keyboard-shortcuts",
                Label = "Keyboard shortcuts",
                Description = "Review available commands",
                Icon = "K"
            },
            new()
            {
                Value = "sign-out",
                Label = "Sign out",
                Description = "Leave this Bloom session",
                Icon = "S",
                SeparatorBefore = true,
                Variant = ActionMenuItemVariant.Danger
            }
        ];

        /// <summary>
        /// Gets the sample project switcher items.
        /// </summary>
        protected IReadOnlyList<ProjectSwitcherItem> ProjectSwitcherItems { get; } =
        [
            new()
            {
                Id = "spacecraft-model",
                Name = "Spacecraft Model",
                Description = "Active collaborative SysML workspace",
                Lifecycle = ReviewLabel
            },
            new()
            {
                Id = "rover-platform",
                Name = "Rover Platform",
                Description = "Mobility and autonomy architecture",
                Lifecycle = "Open"
            },
            new()
            {
                Id = "payload-study",
                Name = "Payload Study",
                Description = "Early concept trade space",
                Lifecycle = "Preparation"
            },
            new()
            {
                Id = "archived-demo-project",
                Name = "Archived Demo Project",
                Description = "Read-only reference baseline",
                Lifecycle = "Archived",
                Disabled = true
            }
        ];

        /// <summary>
        /// Gets the sample SysML model tree.
        /// </summary>
        protected IReadOnlyList<ModelTreeItem> ModelTreeItems { get; } =
        [
            new()
            {
                Id = "spacecraft",
                Title = SpacecraftName,
                Stereotype = PackageElementKind,
                ElementColor = "var(--mb-color-sysml-structure-header)",
                Children =
                [
                    new ModelTreeItem
                    {
                        Id = "aocs",
                        Title = "AOCS",
                        Stereotype = PackageElementKind,
                        Ownership = TreeNodeOwnership.Mine,
                        OwnershipColor = OwnershipAocsColor,
                        HasComment = true,
                        Children =
                        [
                            new ModelTreeItem
                            {
                                Id = ReactionWheelAssemblyId,
                                Title = ReactionWheelAssemblyName,
                                Stereotype = "part def",
                                Ownership = TreeNodeOwnership.Mine,
                                OwnershipColor = OwnershipAocsColor,
                                IsModified = true
                            },
                            new ModelTreeItem
                            {
                                Id = "attitude-controller",
                                Title = "AttitudeController",
                                Stereotype = "part",
                                Ownership = TreeNodeOwnership.Mine,
                                OwnershipColor = OwnershipAocsColor
                            },
                            new ModelTreeItem
                            {
                                Id = "pointing-accuracy",
                                Title = "PointingAccuracy",
                                Stereotype = "constraint def",
                                ElementColor = "var(--mb-color-sysml-verification-header)"
                            }
                        ]
                    },
                    new ModelTreeItem
                    {
                        Id = "power",
                        Title = "PowerSubsystem",
                        Stereotype = PackageElementKind,
                        Ownership = TreeNodeOwnership.Others,
                        OwnershipColor = "var(--mb-color-ownership-power)",
                        Children =
                        [
                            new ModelTreeItem
                            {
                                Id = "battery",
                                Title = "BatteryAssembly",
                                Stereotype = "part"
                            },
                            new ModelTreeItem
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
                Key = OwnerColumnKey,
                Header = "Owner"
            },
            new()
            {
                Key = LinksColumnKey,
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
                [OwnerColumnKey] = "AOCS",
                [LinksColumnKey] = "4"
            },
            new Dictionary<string, string>
            {
                ["id"] = "BLK-ADCS-002",
                ["name"] = ReactionWheelAssemblyName,
                ["type"] = "part",
                [OwnerColumnKey] = "AOCS",
                [LinksColumnKey] = "7"
            },
            new Dictionary<string, string>
            {
                ["id"] = "VER-ADCS-018",
                ["name"] = "Slew response case",
                ["type"] = "verification",
                [OwnerColumnKey] = "Analysis",
                [LinksColumnKey] = "3"
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
                Label = ReviewLabel,
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
        /// Initializes design-system sample state.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ProjectBrowserViewModel = this.ProjectBrowserViewModelService.CreateQuantitiesProjectBrowserViewModel();

            if (this.ProjectBrowserViewModel.RootNodes.Count > 0)
            {
                var rootNode = this.ProjectBrowserViewModel.RootNodes[0];

                this.ProjectBrowserViewModel.SelectNode(rootNode);
                this.ProjectBrowserViewModel.ToggleNode(rootNode);
            }
        }

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

        /// <summary>
        /// Opens the default sample confirmation dialog.
        /// </summary>
        protected void OpenDefaultConfirmDialog()
        {
            this.IsDefaultConfirmDialogOpen = true;
        }

        /// <summary>
        /// Opens the warning sample confirmation dialog.
        /// </summary>
        protected void OpenWarningConfirmDialog()
        {
            this.IsWarningConfirmDialogOpen = true;
        }

        /// <summary>
        /// Opens the danger sample confirmation dialog.
        /// </summary>
        protected void OpenDangerConfirmDialog()
        {
            this.IsDangerConfirmDialogOpen = true;
        }

        /// <summary>
        /// Handles confirming the default sample confirmation dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected Task HandleDefaultConfirmDialogConfirmed()
        {
            this.IsDefaultConfirmDialogOpen = false;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles confirming the warning sample confirmation dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected Task HandleWarningConfirmDialogConfirmed()
        {
            this.IsWarningConfirmDialogOpen = false;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles confirming the danger sample confirmation dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected Task HandleDangerConfirmDialogConfirmed()
        {
            this.IsDangerConfirmDialogOpen = false;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles cancelling a sample confirmation dialog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected static Task HandleConfirmDialogCancelled()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles submitting a sample comment.
        /// </summary>
        /// <param name="commentBody">The submitted comment body.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected static Task HandleCommentSubmitted(string commentBody)
        {
            _ = commentBody;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles resolving a sample comment.
        /// </summary>
        /// <param name="commentId">The resolved comment identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected static Task HandleCommentResolved(string commentId)
        {
            _ = commentId;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles deleting a sample comment.
        /// </summary>
        /// <param name="commentId">The deleted comment identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected static Task HandleCommentDeleted(string commentId)
        {
            return Task.FromResult(commentId);
        }

        /// <summary>
        /// Handles changing the sample canvas zoom percentage.
        /// </summary>
        /// <param name="zoomPercentage">The updated zoom percentage.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected Task HandleCanvasZoomPercentageChanged(int zoomPercentage)
        {
            this.CanvasZoomPercentage = zoomPercentage;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles selecting the sample active canvas tool.
        /// </summary>
        /// <param name="canvasTool">The selected canvas tool.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected Task HandleActiveCanvasToolChanged(CanvasTool canvasTool)
        {
            this.ActiveCanvasTool = canvasTool;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles selecting a sample canvas breadcrumb item.
        /// </summary>
        /// <param name="breadcrumbValue">The selected breadcrumb value.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected static Task HandleCanvasBreadcrumbSelected(string breadcrumbValue)
        {
            _ = breadcrumbValue;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles fitting the sample canvas to view.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected static Task HandleCanvasFitToView()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles selecting a sample project browser node.
        /// </summary>
        /// <param name="node">The selected project browser node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected static Task HandleProjectBrowserNodeSelectedAsync(ProjectBrowserNodeViewModel node)
        {
            _ = node;

            return Task.CompletedTask;
        }

        private sealed class TypographySample
        {
            public string CssClass { get; init; } = string.Empty;

            public string Text { get; init; } = string.Empty;

            public string Color { get; init; } = string.Empty;
        }

        private sealed class ChipSample
        {
            public string Label { get; init; } = string.Empty;

            public ChipVariant Variant { get; init; } = ChipVariant.Default;

            public string Color { get; init; } = string.Empty;
        }

        private sealed class StatusIndicatorSample
        {
            public string Label { get; init; } = string.Empty;

            public StatusIndicatorVariant Variant { get; init; } = StatusIndicatorVariant.Neutral;
        }
    }
}
