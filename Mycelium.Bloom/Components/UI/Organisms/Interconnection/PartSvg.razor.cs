// ------------------------------------------------------------------------------------------------
// <copyright file="PartSvg.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Interconnection
{
    using Microsoft.AspNetCore.Components;
    using Blazor.Diagrams.Core.Models;
    using SysML2.NET.Core.Core.Types;

    /// <summary>Renders canonical part notation using the mounted node geometry.</summary>
    public partial class PartSvg
    {
        /// <summary>Retains the embedded direction glyphs shared by mounted parts.</summary>
        private static readonly IReadOnlyDictionary<FeatureDirectionKind, string> PortImages =
            new Dictionary<FeatureDirectionKind, string>
            {
                [FeatureDirectionKind.In] = LoadPortImage("in"),
                [FeatureDirectionKind.Out] = LoadPortImage("out"),
                [FeatureDirectionKind.Inout] = LoadPortImage("inout")
            };

        /// <summary>Gets or sets the mounted library node.</summary>
        [Parameter]
        public NodeModel Node { get; set; }

        /// <summary>Gets the native node or container presentation.</summary>
        private Mycelium.Bloom.Model.Diagram.DiagramPart Part => this.Node switch
        {
            DiagramNodeModel node => node.Part,
            DiagramGroupModel group => group.Part,
            _ => throw new InvalidOperationException("Unsupported part renderer.")
        };

        /// <summary>Gets the header shape using model-space geometry.</summary>
        private string HeaderPath => string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"M6 0 H{this.Node.Size.Width - 6} Q{this.Node.Size.Width} 0 {this.Node.Size.Width} 6 V40 H0 V6 Q0 0 6 0");

        /// <summary>Formats native SVG scalar values independently of the circuit culture.</summary>
        /// <param name="value">The model-space scalar.</param>
        /// <returns>The invariant SVG number.</returns>
        private static string SvgNumber(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Embeds the approved glyph so rendered SVG has no external asset dependency.</summary>
        /// <param name="direction">The direction identifying the embedded port glyph.</param>
        /// <returns>The glyph encoded as an SVG data URI.</returns>
        private static string LoadPortImage(string direction)
        {
            using var stream = typeof(PartSvg).Assembly.GetManifestResourceStream($"Mycelium.Bloom.Resources.Diagram.port-{direction}.svg")
                ?? throw new InvalidOperationException("Missing diagram port asset.");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return "data:image/svg+xml;base64," + Convert.ToBase64String(buffer.ToArray());
        }
    }
}
