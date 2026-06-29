// ------------------------------------------------------------------------------------------------
// <copyright file="DetailPanel.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DetailPanel
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom detail panel used to inspect and edit selected SysML model elements.
    /// </summary>
    public partial class DetailPanel : ComponentBase
    {
        /// <summary>
        /// Gets or sets the selected element stereotype text.
        /// </summary>
        [Parameter]
        public string Stereotype { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the selected element title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the selected element qualified name.
        /// </summary>
        [Parameter]
        public string QualifiedName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the owner label.
        /// </summary>
        [Parameter]
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the owner color.
        /// </summary>
        [Parameter]
        public string OwnerColor { get; set; } = "var(--mb-color-ownership-aocs)";

        /// <summary>
        /// Gets or sets the available detail panel tabs.
        /// </summary>
        [Parameter]
        public IReadOnlyList<TabItem> Tabs { get; set; } = [];

        /// <summary>
        /// Gets or sets the active tab value.
        /// </summary>
        [Parameter]
        public string ActiveTab { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the active tab change callback.
        /// </summary>
        [Parameter]
        public EventCallback<string> ActiveTabChanged { get; set; }

        /// <summary>
        /// Gets or sets whether the collapse button should be shown.
        /// </summary>
        [Parameter]
        public bool ShowCollapseButton { get; set; } = true;

        /// <summary>
        /// Gets or sets the collapse callback.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> OnCollapse { get; set; }

        /// <summary>
        /// Gets or sets the panel content.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the detail panel element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-detail-panel",
                this.Class);

            return cssClass;
        }
    }
}
