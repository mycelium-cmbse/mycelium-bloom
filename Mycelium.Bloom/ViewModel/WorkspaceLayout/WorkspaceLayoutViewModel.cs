// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceLayoutViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.WorkspaceLayout
{
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    /// <summary>Owns renderer-affine region preferences for one workspace layout lifetime.</summary>
    public sealed class WorkspaceLayoutViewModel : ReactiveObject
    {
        /// <summary>The controlled Properties fold state.</summary>
        private bool propertiesCollapsed;

        /// <summary>The preferred width retained independently of responsive geometry.</summary>
        private double propertiesWidth = DetailsPanelSizing.MinimumWidth;

        /// <summary>What the right panel shows when it is unfolded.</summary>
        private WorkspaceRightPanelContent rightPanelContent = WorkspaceRightPanelContent.Properties;

        /// <summary>Gets or sets what the right panel shows when it is unfolded: Properties or Hypha.</summary>
        public WorkspaceRightPanelContent RightPanelContent
        {
            get => this.rightPanelContent;
            set => this.RaiseAndSetIfChanged(ref this.rightPanelContent, value);
        }

        /// <summary>Gets or sets whether Properties is folded without clearing selection.</summary>
        public bool PropertiesCollapsed
        {
            get => this.propertiesCollapsed;
            set => this.RaiseAndSetIfChanged(ref this.propertiesCollapsed, value);
        }

        /// <summary>Gets or sets the preferred Properties width in CSS pixels.</summary>
        public double PropertiesWidth
        {
            get => this.propertiesWidth;
            set => this.RaiseAndSetIfChanged(ref this.propertiesWidth, value);
        }
    }
}
