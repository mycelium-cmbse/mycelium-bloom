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

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Reusable Bloom workspace shell that arranges the model browser, main workspace, and detail panel.
    /// </summary>
    public partial class WorkspaceShell : ComponentBase
    {
        /// <summary>
        /// Gets or sets optional header content.
        /// </summary>
        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        /// <summary>
        /// Gets or sets the left panel content.
        /// </summary>
        [Parameter]
        public RenderFragment LeftContent { get; set; }

        /// <summary>
        /// Gets or sets the main workspace content.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the right panel content.
        /// </summary>
        [Parameter]
        public RenderFragment RightContent { get; set; }

        /// <summary>
        /// Gets or sets whether the left panel is visible.
        /// </summary>
        [Parameter]
        public bool ShowLeftPanel { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the right panel is visible.
        /// </summary>
        [Parameter]
        public bool ShowRightPanel { get; set; } = true;

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the workspace shell.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-workspace-shell",
                this.Class);

            return cssClass;
        }
    }
}
