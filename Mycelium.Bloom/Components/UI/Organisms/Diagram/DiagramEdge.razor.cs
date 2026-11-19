// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEdge.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams.Core.Models.Base;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Renders shared relationship paths, markers and labels and captures selection intent.</summary>
    public partial class DiagramEdge : SelectableDiagramWidgetBase<DiagramEdgeViewModel>
    {
        /// <summary>The SVG paint that follows the relationship's inherited color.</summary>
        private const string CurrentColor = "currentColor";

        /// <summary>Gets or sets the native link adapted from caller-owned presentation.</summary>
        [Parameter, EditorRequired]
        public BaseLinkModel Link { get; set; }

        /// <summary>Gets the combined relationship name exposed to assistive technology.</summary>
        private string AccessibleName => this.GetAccessibleName();

        /// <summary>Supplies relationship state before the reactive base attaches its initial observation.</summary>
        protected override void OnInitialized()
        {
            this.ViewModel = ((DiagramLinkModel)this.Link).ViewModel;
            base.OnInitialized();
        }

        /// <summary>Borrows relationship state while the library continues to own native path geometry.</summary>
        protected override void OnParametersSet()
        {
            this.ViewModel = ((DiagramLinkModel)this.Link).ViewModel;
            base.OnParametersSet();
        }

        /// <summary>Combines nonempty relationship labels for assistive technology.</summary>
        /// <returns>The relationship label and supplied endpoint labels.</returns>
        private string GetAccessibleName()
        {
            var edge = this.ViewModel.Presentation;

            return string.Join(" ", new[] { edge.Label ?? edge.Kind.ToString(), edge.SourceLabel?.ToString(), edge.TargetLabel?.ToString() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        /// <summary>Formats SVG coordinates independently of the circuit culture.</summary>
        /// <param name="value">The coordinate to format.</param>
        /// <returns>The invariant numeric coordinate text.</returns>
        private static string Number(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Formats the descriptor's SVG viewport independently of the circuit culture.</summary>
        /// <param name="marker">The validated marker geometry.</param>
        /// <returns>The four invariant SVG viewport measurements.</returns>
        private static string MarkerViewBox(DiagramMarker marker) =>
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{marker.ViewBox.X} {marker.ViewBox.Y} {marker.ViewBox.Width} {marker.ViewBox.Height}");

        /// <summary>Builds the SVG endpoint symbol requested by shared notation.</summary>
        /// <param name="marker">The endpoint symbol to draw.</param>
        /// <returns>The SVG fragment for the requested endpoint.</returns>
        private static RenderFragment Marker(DiagramMarker marker)
        {
            return builder =>
            {
                if (marker.Path is null)
                {
                    return;
                }

                builder.OpenElement(0, "path");
                builder.AddAttribute(1, "d", marker.Path);
                builder.AddAttribute(2, "fill", marker.Fill);
                builder.AddAttribute(3, "stroke", CurrentColor);
                builder.CloseElement();

                if (marker.DetailPath is not null)
                {
                    builder.OpenElement(4, "path");
                    builder.AddAttribute(5, "d", marker.DetailPath);
                    builder.AddAttribute(6, "fill", marker.DetailFill);
                    builder.AddAttribute(7, "stroke", marker.DetailStroke);
                    builder.CloseElement();
                }
            };
        }
    }
}
