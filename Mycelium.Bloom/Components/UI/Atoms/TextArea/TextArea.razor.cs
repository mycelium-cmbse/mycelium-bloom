// ------------------------------------------------------------------------------------------------
// <copyright file="TextArea.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.TextArea
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable multi-line text input for Bloom forms and dialogs.
    /// </summary>
    public sealed partial class TextArea : BloomFieldComponentBase
    {
        /// <summary>
        /// Gets or sets the current text area value.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the text area value changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder displayed when the text area is empty.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible number of text rows.
        /// </summary>
        [Parameter]
        public int Rows { get; set; } = 3;

        /// <summary>
        /// Gets or sets the maximum number of characters accepted by the text area.
        /// </summary>
        [Parameter]
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the text area is read-only.
        /// </summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the text area can be resized vertically.
        /// </summary>
        [Parameter]
        public bool Resizable { get; set; } = true;

        /// <summary>
        /// Gets the final CSS class list applied to the text area wrapper.
        /// </summary>
        /// <returns>The text area CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-text-area",
                CssClassBuilder.When("mb-text-area--disabled", this.Disabled),
                CssClassBuilder.When("mb-text-area--readonly", this.ReadOnly),
                CssClassBuilder.When("mb-text-area--fixed", !this.Resizable),
                CssClassBuilder.When("mb-text-area--error", this.HasError));
        }

        /// <summary>
        /// Gets the final CSS class list applied to the native text area element.
        /// </summary>
        /// <returns>The native text area CSS class list.</returns>
        private string GetFieldCssClass()
        {
            return CssClassBuilder.Build(
                "mb-text-area__field",
                "mb-field-control",
                "mb-field-control__native",
                CssClassBuilder.When("mb-text-area__field--fixed", !this.Resizable));
        }

        /// <summary>
        /// Gets the visible row count, falling back to the default when invalid.
        /// </summary>
        /// <returns>The effective visible row count.</returns>
        private int GetRows()
        {
            return this.Rows > 0 ? this.Rows : 3;
        }

        /// <summary>
        /// Gets the maximum length attribute when configured.
        /// </summary>
        /// <returns>The configured maximum length, or null when omitted.</returns>
        private string GetMaxLength()
        {
            return this.MaxLength > 0 ? this.MaxLength.ToString() : null;
        }

        /// <summary>
        /// Handles input changes and forwards the updated value to the parent component.
        /// </summary>
        /// <param name="args">The input change event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleInputAsync(ChangeEventArgs args)
        {
            var value = args.Value?.ToString() ?? string.Empty;

            this.Value = value;

            await this.ValueChanged.InvokeAsync(value);
        }
    }
}
