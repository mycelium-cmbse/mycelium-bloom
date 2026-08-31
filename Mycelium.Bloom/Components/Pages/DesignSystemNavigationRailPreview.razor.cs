// ------------------------------------------------------------------------------------------------
// <copyright file="DesignSystemNavigationRailPreview.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.NavigationRail;

    /// <summary>
    /// Provides the Design System navigation preview with lifecycle-correct reactive observation.
    /// </summary>
    public sealed partial class DesignSystemNavigationRailPreview :
        BloomReactiveComponentBase<INavigationRailViewModel>
    {
        /// <summary>
        /// Gets or sets the preview fragment rendered for the observed caller-owned ViewModel.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public RenderFragment<INavigationRailViewModel> ChildContent { get; set; }
    }
}
