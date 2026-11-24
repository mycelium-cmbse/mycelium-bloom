// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeWidget.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Blazor.Diagrams.Extensions;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Adapts a native occurrence to the shared node components and captures selection.</summary>
    public partial class DiagramNodeWidget : SelectableDiagramWidgetBase<DiagramNodeViewModel>
    {
        /// <summary>The immutable presentation used by the cached component parameters.</summary>
        private DiagramNodePresentation renderedPresentation;

        /// <summary>The native DynamicComponent parameters for the current presentation.</summary>
        private IDictionary<string, object> componentParameters;

        /// <summary>Gets or sets the registered presentation-to-component map.</summary>
        [Inject]
        private DiagramNodeComponentRegistry Components { get; set; }

        /// <summary>Gets or sets the native occurrence adapter.</summary>
        [Parameter, EditorRequired]
        public DiagramNodeModel Node { get; set; }

        /// <summary>Gets or sets the mount-owned pointer dispatcher.</summary>
        [CascadingParameter]
        private IDiagramPointerBehavior ResizePointer { get; set; }

        /// <summary>Gets invariant occurrence dimensions without changing canonical geometry.</summary>
        private string Size => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"width:{this.Node.Size.Width}px;height:{this.Node.Size.Height}px");

        /// <summary>Gets a mount-local association for accessible resize instructions.</summary>
        private string ResizeHelpId { get; } = $"diagram-resize-{Guid.NewGuid():N}";

        /// <summary>Supplies occurrence state before the reactive base attaches its initial observation.</summary>
        protected override void OnInitialized()
        {
            this.ViewModel = this.Node.ViewModel;
            base.OnInitialized();
        }

        /// <summary>Borrows occurrence state from the native adapter without subscribing to native refresh events.</summary>
        protected override void OnParametersSet()
        {
            this.ViewModel = this.Node.ViewModel;
            base.OnParametersSet();
        }

        /// <summary>Reuses framework parameters until the immutable presentation changes.</summary>
        /// <returns>The parameters for the typed notation component.</returns>
        private IDictionary<string, object> GetComponentParameters()
        {
            if (!ReferenceEquals(this.renderedPresentation, this.ViewModel.Presentation))
            {
                this.renderedPresentation = this.ViewModel.Presentation;
                this.componentParameters = new Dictionary<string, object> { ["Presentation"] = this.renderedPresentation };
            }

            return this.componentParameters;
        }

        /// <summary>Captures the resize handle before native node movement can begin.</summary>
        /// <param name="args">The browser pointer event delivered by Blazor.</param>
        private void BeginResize(PointerEventArgs args) => this.ResizePointer.BeginResize(this.ViewModel.Presentation.Occurrence.Id, args.ToCore());

        /// <summary>Captures keyboard resizing before delegating to the presentation authority.</summary>
        /// <param name="args">The directional key and precision modifier.</param>
        private void ResizeWithKeyboard(KeyboardEventArgs args)
        {
            var step = args.ShiftKey ? 1 : 10;
            var size = this.ViewModel.Size;

            switch (args.Key)
            {
                case "ArrowLeft":
                    this.ViewModel.Resize(size.Width - step, size.Height);
                    break;
                case "ArrowRight":
                    this.ViewModel.Resize(size.Width + step, size.Height);
                    break;
                case "ArrowUp":
                    this.ViewModel.Resize(size.Width, size.Height - step);
                    break;
                case "ArrowDown":
                    this.ViewModel.Resize(size.Width, size.Height + step);
                    break;
            }
        }
    }
}
