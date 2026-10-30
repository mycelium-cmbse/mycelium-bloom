// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserSearchAnchor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser
{
    using System.Threading.Tasks;
    using BlazorBlueprint.Primitives.Popover;
    using Microsoft.AspNetCore.Components;

    /// <summary>Anchors a transient Blueprint popover to the Browser's persistent search field.</summary>
    public sealed class ProjectBrowserSearchAnchor : ComponentBase
    {
        /// <summary>Gets or sets the stable search-field container.</summary>
        [Parameter]
        public ElementReference Anchor { get; set; }

        /// <summary>Gets or sets the context for this popover opening.</summary>
        [CascadingParameter]
        public PopoverContext Popover { get; set; }

        /// <summary>Gets or sets the callback that exposes this opening's trigger ID to the persistent field.</summary>
        [Parameter]
        public EventCallback<string> TriggerIdChanged { get; set; }

        /// <summary>Connects outside-click exclusion to the same DOM element used for positioning.</summary>
        /// <returns>The trigger identity update.</returns>
        protected override Task OnInitializedAsync()
        {
            return this.TriggerIdChanged.InvokeAsync(this.Popover.TriggerId);
        }

        /// <summary>Registers the existing field without rendering a second trigger or remounting the input.</summary>
        /// <param name="firstRender">Whether this opening's anchor is being rendered for the first time.</param>
        protected override void OnAfterRender(bool firstRender)
        {
            if (!string.IsNullOrEmpty(this.Anchor.Id))
            {
                this.Popover?.SetTriggerElement(this.Anchor);
            }
        }
    }
}
