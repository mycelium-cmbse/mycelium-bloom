// ------------------------------------------------------------------------------------------------
// <copyright file="ExpressionGroup.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DetailsPanel
{
    using Microsoft.AspNetCore.Components;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Renders one bounded expression group using Blueprint-owned disclosure state.</summary>
    public partial class ExpressionGroup : ComponentBase
    {
        /// <summary>Gets or sets the immutable canonical expression projection.</summary>
        [Parameter, EditorRequired]
        public ExpressionPresentation Node { get; set; }

        /// <summary>Gets or sets the canonical selection to omit navigation back to the displayed object.</summary>
        [Parameter]
        public IElement SelectedElement { get; set; }

        /// <summary>Gets or sets the nesting level used for initial disclosure.</summary>
        [Parameter]
        public int Depth { get; set; }

        /// <summary>Gets or sets the request to inspect an exact canonical element.</summary>
        [Parameter]
        public EventCallback<IElement> InspectRequested { get; set; }

        /// <summary>Gets or sets selected-element metadata included once alongside the expression's identities.</summary>
        [Parameter]
        public IReadOnlyList<KeyValuePair<string, string>> SelectedProperties { get; set; } = [];

        /// <summary>Forwards resolvable targets without changing browser focus.</summary>
        private Task InspectAsync(IElement element)
        {
            return element is null ? Task.CompletedTask : this.InspectRequested.InvokeAsync(element);
        }
    }
}
