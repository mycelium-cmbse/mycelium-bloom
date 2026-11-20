// ------------------------------------------------------------------------------------------------
// <copyright file="SelectableDiagramWidgetBase.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Shares keyboard selection for native buttons and focusable SVG diagram widgets.</summary>
    /// <typeparam name="TViewModel">The occurrence-owned reactive presentation.</typeparam>
    public abstract class SelectableDiagramWidgetBase<TViewModel> : BloomReactiveComponentBase<TViewModel>
        where TViewModel : DiagramElementViewModel
    {
        /// <summary>Gets or sets the diagram mount that owns keyboard gestures.</summary>
        [CascadingParameter]
        private DiagramPointerBehavior Pointer { get; set; }

        /// <summary>Renders one selectable surface with the shared keyboard event bindings.</summary>
        /// <param name="element">The element tag, either a native button or a focusable SVG element.</param>
        /// <param name="attributes">The widget's semantic and visual attributes.</param>
        /// <param name="content">The content of the selectable surface, when present.</param>
        /// <param name="forwardAdditionalAttributes">Whether caller-supplied attributes belong on this surface.</param>
        /// <returns>The selectable markup.</returns>
        protected RenderFragment SelectableSurface(string element, IEnumerable<KeyValuePair<string, object>> attributes, RenderFragment content = null,
            bool forwardAdditionalAttributes = true) => builder =>
        {
            builder.OpenElement(0, element);

            if (forwardAdditionalAttributes)
            {
                builder.AddMultipleAttributes(1, this.AdditionalAttributes);
            }

            builder.AddMultipleAttributes(2, attributes);
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, this.OnSelectionClick));
            builder.AddEventStopPropagationAttribute(4, "onclick", true);
            builder.AddAttribute(5, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, args => this.OnSelectionKeyDown(args, element == "button")));
            builder.AddEventPreventDefaultAttribute(6, "onkeyup", this.Pointer is not null);
            builder.AddAttribute(7, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, this.CancelSpaceActivation));
            builder.AddContent(8, content);
            builder.CloseElement();
        };

        /// <summary>Discards pending activation when this widget unmounts.</summary>
        /// <param name="disposing">Whether managed resources are being released.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.CancelSpaceActivation();
            }

            base.Dispose(disposing);
        }

        /// <summary>Handles only activation without a pointer click count.</summary>
        /// <param name="args">The browser activation event.</param>
        private void OnSelectionClick(MouseEventArgs args)
        {
            if (args.Detail == 0)
            {
                this.ViewModel.Select();
            }
        }

        /// <summary>Uses native button Enter activation and delegates a possible Space tap to the dispatcher.</summary>
        /// <param name="args">The key pressed on this widget.</param>
        /// <param name="nativeButton">Whether the browser generates the Enter activation click.</param>
        private void OnSelectionKeyDown(KeyboardEventArgs args, bool nativeButton)
        {
            if (!nativeButton && args.Key == "Enter" && !args.Repeat)
            {
                this.ViewModel.Select();
            }
            else
            {
                this.Pointer?.PrepareSpaceActivation(args, this.ViewModel);
            }
        }

        /// <summary>Discards pending activation when this widget loses focus.</summary>
        private void CancelSpaceActivation() => this.Pointer?.CancelSpaceActivation(this.ViewModel);
    }
}
