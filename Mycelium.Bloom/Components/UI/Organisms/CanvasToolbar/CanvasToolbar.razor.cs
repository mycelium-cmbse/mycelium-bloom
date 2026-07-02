// ------------------------------------------------------------------------------------------------
// <copyright file="CanvasToolbar.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.CanvasToolbar
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Reusable compact toolbar for workspace canvas surfaces.
    /// </summary>
    public partial class CanvasToolbar : ComponentBase
    {
        /// <summary>
        /// Gets the canvas tools shown in the center tool group.
        /// </summary>
        private static readonly IReadOnlyList<CanvasTool> AvailableTools =
        [
            CanvasTool.Select,
            CanvasTool.Pan,
            CanvasTool.Inspect
        ];

        /// <summary>
        /// Gets or sets the breadcrumb items shown in the start area.
        /// </summary>
        [Parameter]
        public IReadOnlyList<BreadcrumbItem> BreadcrumbItems { get; set; } = [];

        /// <summary>
        /// Gets or sets the callback invoked when a breadcrumb item is selected.
        /// </summary>
        [Parameter]
        public EventCallback<string> BreadcrumbSelected { get; set; }

        /// <summary>
        /// Gets or sets the active canvas tool.
        /// </summary>
        [Parameter]
        public CanvasTool ActiveTool { get; set; } = CanvasTool.Select;

        /// <summary>
        /// Gets or sets the callback invoked when the active canvas tool changes.
        /// </summary>
        [Parameter]
        public EventCallback<CanvasTool> ActiveToolChanged { get; set; }

        /// <summary>
        /// Gets or sets the current canvas zoom percentage.
        /// </summary>
        [Parameter]
        public int ZoomPercentage { get; set; } = 100;

        /// <summary>
        /// Gets or sets the callback invoked when the canvas zoom percentage changes.
        /// </summary>
        [Parameter]
        public EventCallback<int> ZoomPercentageChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when fit-to-view is selected.
        /// </summary>
        [Parameter]
        public EventCallback FitToView { get; set; }

        /// <summary>
        /// Gets or sets custom content rendered in the start area.
        /// </summary>
        [Parameter]
        public RenderFragment StartContent { get; set; }

        /// <summary>
        /// Gets or sets custom content rendered after zoom controls in the end area.
        /// </summary>
        [Parameter]
        public RenderFragment EndContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the toolbar container.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the available canvas tools.
        /// </summary>
        private static IReadOnlyList<CanvasTool> Tools => AvailableTools;

        /// <summary>
        /// Gets the final CSS class list applied to the toolbar.
        /// </summary>
        /// <returns>The toolbar CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-canvas-toolbar",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Checks whether the provided tool is active.
        /// </summary>
        /// <param name="tool">The canvas tool to check.</param>
        /// <returns>A value indicating whether the tool is active.</returns>
        private bool IsActiveTool(CanvasTool tool)
        {
            return tool == this.ActiveTool;
        }

        /// <summary>
        /// Gets the CSS class for a tool button.
        /// </summary>
        /// <param name="tool">The canvas tool.</param>
        /// <returns>The CSS class for the tool button.</returns>
        private string GetToolCssClass(CanvasTool tool)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-canvas-toolbar__tool",
                CssClassBuilder.When("mb-canvas-toolbar__tool--active", this.IsActiveTool(tool)));

            return cssClass;
        }

        /// <summary>
        /// Selects the provided canvas tool.
        /// </summary>
        /// <param name="tool">The canvas tool to select.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectToolAsync(CanvasTool tool)
        {
            this.ActiveTool = tool;

            await this.ActiveToolChanged.InvokeAsync(tool);
        }

        /// <summary>
        /// Gets the visual glyph for a tool.
        /// </summary>
        /// <param name="tool">The canvas tool.</param>
        /// <returns>The visual glyph for the tool.</returns>
        private static string GetToolIcon(CanvasTool tool)
        {
            var icon = tool switch
            {
                CanvasTool.Pan => "P",
                CanvasTool.Inspect => "I",
                _ => "S"
            };

            return icon;
        }

        /// <summary>
        /// Gets the accessible label for a tool.
        /// </summary>
        /// <param name="tool">The canvas tool.</param>
        /// <returns>The accessible label for the tool.</returns>
        private static string GetToolAriaLabel(CanvasTool tool)
        {
            return $"{tool} tool";
        }

        /// <summary>
        /// Gets the title for a tool.
        /// </summary>
        /// <param name="tool">The canvas tool.</param>
        /// <returns>The title for the tool.</returns>
        private static string GetToolTitle(CanvasTool tool)
        {
            return tool.ToString();
        }
    }
}
