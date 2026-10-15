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
    using ReactiveUI;

    /// <summary>Owns renderer-affine region preferences for one workspace layout lifetime.</summary>
    public sealed class WorkspaceLayoutViewModel : ReactiveObject
    {
        /// <summary>The controlled Properties fold state.</summary>
        private bool propertiesCollapsed;

        /// <summary>The preferred width retained independently of responsive geometry.</summary>
        private double propertiesWidth = 380;

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
