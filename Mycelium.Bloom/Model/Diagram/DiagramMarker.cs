// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramMarker.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Describes native-independent SVG endpoint geometry using inherited diagram paints.</summary>
    /// <param name="Path">The primary SVG path, or null for no marker.</param>
    /// <param name="Fill">The primary path paint.</param>
    /// <param name="DetailPath">The optional additional path.</param>
    /// <param name="DetailFill">The additional path paint.</param>
    /// <param name="DetailStroke">The optional additional path stroke paint.</param>
    public sealed record DiagramMarker(string Path, string Fill = "none", string DetailPath = null, string DetailFill = "none", string DetailStroke = "currentColor")
    {
        /// <summary>Gets the finite SVG viewport (X, Y, Width, Height), also used as the marker's model-space dimensions.</summary>
        public (double X, double Y, double Width, double Height) ViewBox { get; init; } = (-23, -8, 24, 16);

        /// <summary>Gets the absence of an endpoint symbol.</summary>
        public static DiagramMarker None { get; } = new(Path: null);

        /// <summary>Gets the composite feature-membership symbol.</summary>
        public static DiagramMarker FilledDiamond { get; } = new("M0 0 L-8 -5 L-16 0 L-8 5 Z", "currentColor");

        /// <summary>Gets the noncomposite feature-membership symbol.</summary>
        public static DiagramMarker HollowDiamond { get; } = new("M0 0 L-8 -5 L-16 0 L-8 5 Z", "var(--background)");

        /// <summary>Gets the specialization triangle.</summary>
        public static DiagramMarker Triangle { get; } = new("M0 0 L-12 -6 L-12 6 Z", "var(--background)");

        /// <summary>Gets the open dependency arrow.</summary>
        public static DiagramMarker OpenArrow { get; } = new("M-12 -5 L0 0 L-12 5");

        /// <summary>Gets the crossed open arrow.</summary>
        public static DiagramMarker CrossedArrow { get; } = new("M-12 -5 L0 0 L-12 5 M-6 -5 L-6 5");

        /// <summary>Gets the feature-typing triangle and dots.</summary>
        public static DiagramMarker TypingTriangle { get; } = new(Triangle.Path, Triangle.Fill,
            "M-18 -4 a1.5 1.5 0 1 0 0.1 0 M-18 4 a1.5 1.5 0 1 0 0.1 0", "currentColor", DetailStroke: null);

        /// <summary>Gets the redefinition triangle and bar.</summary>
        public static DiagramMarker BarredTriangle { get; } = new(Triangle.Path, Triangle.Fill, "M-16 -6 L-16 6");
    }
}
