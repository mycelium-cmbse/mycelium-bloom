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

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Renders shared relationship paths, markers and labels and captures selection intent.</summary>
    public partial class DiagramEdge : BloomReactiveComponentBase<DiagramEdgeViewModel>
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
            this.ViewModel = ((GeneralDiagramLinkModel)this.Link).ViewModel;
            base.OnInitialized();
        }

        /// <summary>Borrows relationship state while the library continues to own native path geometry.</summary>
        protected override void OnParametersSet()
        {
            this.ViewModel = ((GeneralDiagramLinkModel)this.Link).ViewModel;
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

        /// <summary>Delegates relationship selection to its presentation owner.</summary>
        private void Select() => this.ViewModel.Select();

        /// <summary>Captures supported keyboard activation for relationship selection.</summary>
        /// <param name="args">The keyboard event captured by the UI.</param>
        private void KeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
        {
            if (args.Key is "Enter" or " ")
            {
                this.Select();
            }
        }

        /// <summary>Builds the SVG endpoint symbol requested by shared notation.</summary>
        /// <param name="marker">The endpoint symbol to draw.</param>
        /// <returns>The SVG fragment for the requested endpoint.</returns>
        private static RenderFragment Marker(DiagramMarker marker) => builder =>
        {
            if (marker == DiagramMarker.None)
            {
                return;
            }
            builder.OpenElement(0, "path");
            builder.AddAttribute(1, "d", marker switch
            {
                DiagramMarker.FilledDiamond or DiagramMarker.HollowDiamond => "M0 0 L-8 -5 L-16 0 L-8 5 Z",
                DiagramMarker.Triangle or DiagramMarker.TypingTriangle or DiagramMarker.BarredTriangle => "M0 0 L-12 -6 L-12 6 Z",
                DiagramMarker.CrossedArrow => "M-12 -5 L0 0 L-12 5 M-6 -5 L-6 5",
                _ => "M-12 -5 L0 0 L-12 5"
            });
            builder.AddAttribute(2, "fill", marker switch
            {
                DiagramMarker.FilledDiamond => CurrentColor,
                DiagramMarker.HollowDiamond or DiagramMarker.Triangle or DiagramMarker.TypingTriangle or DiagramMarker.BarredTriangle => "var(--background)",
                _ => "none"
            });
            builder.AddAttribute(3, "stroke", CurrentColor);
            builder.CloseElement();

            if (marker == DiagramMarker.BarredTriangle)
            {
                builder.OpenElement(4, "path");
                builder.AddAttribute(5, "d", "M-16 -6 L-16 6");
                builder.AddAttribute(6, "stroke", CurrentColor);
                builder.CloseElement();
            }

            if (marker == DiagramMarker.TypingTriangle)
            {
                builder.OpenElement(7, "path");
                builder.AddAttribute(8, "d", "M-18 -4 a1.5 1.5 0 1 0 0.1 0 M-18 4 a1.5 1.5 0 1 0 0.1 0");
                builder.AddAttribute(9, "fill", CurrentColor);
                builder.CloseElement();
            }
        };
    }
}
