// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramViewport.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{

    /// <summary>Captures one atomic, renderer-independent viewport snapshot.</summary>
    /// <param name="PanX">The horizontal canvas translation.</param>
    /// <param name="PanY">The vertical canvas translation.</param>
    /// <param name="Zoom">The model-to-canvas scale.</param>
    /// <param name="IsInitialized">Whether initial canvas fitting has completed.</param>
    public sealed record DiagramViewport(double PanX = 0, double PanY = 0, double Zoom = 1, bool IsInitialized = false)
    {
        /// <summary>The minimum supported scale (0.1%), shared by fit, navigation controls and the native adapter.</summary>
        public const double MinimumZoom = 0.001;

        /// <summary>The maximum supported scale (400%).</summary>
        public const double MaximumZoom = 4;
    }
}
