// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceShell.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.WorkspaceShell
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable top-level engineering workspace layout.
    /// </summary>
    public partial class WorkspaceShell : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the accessible label of the workspace.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Engineering workspace";

        /// <summary>
        /// Gets or sets the accessible label of the header region.
        /// </summary>
        [Parameter]
        public string HeaderAriaLabel { get; set; } = "Workspace header";

        /// <summary>
        /// Gets or sets the accessible label of the left panel.
        /// </summary>
        [Parameter]
        public string LeftPanelAriaLabel { get; set; } = "Workspace navigation";

        /// <summary>
        /// Gets or sets the accessible label of the main content region.
        /// </summary>
        [Parameter]
        public string MainContentAriaLabel { get; set; } = "Workspace content";

        /// <summary>
        /// Gets or sets the accessible label of the right panel.
        /// </summary>
        [Parameter]
        public string RightPanelAriaLabel { get; set; } = "Workspace details";

        /// <summary>
        /// Gets or sets the accessible label of the status region.
        /// </summary>
        [Parameter]
        public string StatusBarAriaLabel { get; set; } = "Workspace status";

        /// <summary>
        /// Gets or sets a value indicating whether the left panel is visible.
        /// </summary>
        [Parameter]
        public bool LeftPanelVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the right panel is visible.
        /// </summary>
        [Parameter]
        public bool RightPanelVisible { get; set; } = true;

        /// <summary>
        /// Gets or sets optional workspace header content.
        /// </summary>
        [Parameter]
        public RenderFragment Header { get; set; }

        /// <summary>
        /// Gets or sets optional left navigation or sidebar content.
        /// </summary>
        [Parameter]
        public RenderFragment LeftPanel { get; set; }

        /// <summary>
        /// Gets or sets the primary workspace content.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment MainContent { get; set; }

        /// <summary>
        /// Gets or sets optional right detail or inspector content.
        /// </summary>
        [Parameter]
        public RenderFragment RightPanel { get; set; }

        /// <summary>
        /// Gets or sets optional bottom status content.
        /// </summary>
        [Parameter]
        public RenderFragment StatusBar { get; set; }

        /// <summary>
        /// Gets or sets optional content layered over the workspace.
        /// </summary>
        [Parameter]
        public RenderFragment OverlayContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the workspace shell.
        /// </summary>
        /// <returns>The workspace-shell CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-workspace-shell");
        }

        /// <summary>
        /// Gets a value indicating whether the left panel has content and is visible.
        /// </summary>
        /// <returns>True when the left panel should render; otherwise, false.</returns>
        private bool ShouldRenderLeftPanel()
        {
            return this.LeftPanelVisible && this.LeftPanel is not null;
        }

        /// <summary>
        /// Gets a value indicating whether the right panel has content and is visible.
        /// </summary>
        /// <returns>True when the right panel should render; otherwise, false.</returns>
        private bool ShouldRenderRightPanel()
        {
            return this.RightPanelVisible && this.RightPanel is not null;
        }
    }
}
