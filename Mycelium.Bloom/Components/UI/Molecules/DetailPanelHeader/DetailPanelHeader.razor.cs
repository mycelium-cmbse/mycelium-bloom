// ------------------------------------------------------------------------------------------------
// <copyright file="DetailPanelHeader.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.DetailPanelHeader
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Reusable Bloom detail panel header used for selected SysML model elements.
    /// </summary>
    public partial class DetailPanelHeader : ComponentBase
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
        /// Gets or sets the qualified name of the selected element.
        /// </summary>
        [Parameter]
        public string QualifiedName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the owner label.
        /// </summary>
        [Parameter]
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the owner color used by the ownership chip.
        /// </summary>
        [Parameter]
        public string OwnerColor { get; set; } = "var(--mb-color-ownership-aocs)";

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
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the header element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build("mb-detail-panel-header",
                this.Class);

            return cssClass;
        }
    }
}
