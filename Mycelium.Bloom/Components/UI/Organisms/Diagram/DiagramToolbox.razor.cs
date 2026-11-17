// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolbox.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>Captures tool requests for the caller-owned diagram presentation.</summary>
    public partial class DiagramToolbox
    {
        /// <summary>Activates selection and permitted graphical movement.</summary>
        private void SelectMove() => this.ViewModel.SetTool(DiagramTool.Move);

        /// <summary>Activates viewport navigation without altering selection.</summary>
        private void SelectPan() => this.ViewModel.SetTool(DiagramTool.Pan);
    }
}
