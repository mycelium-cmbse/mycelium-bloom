// ------------------------------------------------------------------------------------------------
// <copyright file="TextInput.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.TextInput
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable text input for Bloom forms and dialogs.
    /// </summary>
    public sealed partial class TextInput : BloomFieldComponentBase
    {
        /// <summary>
        /// Gets or sets the current input value.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the input value changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder displayed when the input is empty.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the native HTML input type.
        /// </summary>
        [Parameter]
        public string InputType { get; set; } = "text";

        /// <summary>
        /// Gets or sets the browser autocomplete hint.
        /// </summary>
        [Parameter]
        public string Autocomplete { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the input is read-only.
        /// </summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of characters accepted by the input.
        /// </summary>
        [Parameter]
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets optional content rendered before the input field.
        /// </summary>
        [Parameter]
        public RenderFragment LeadingContent { get; set; }

        /// <summary>
        /// Gets or sets optional content rendered after the input field.
        /// </summary>
        [Parameter]
        public RenderFragment TrailingContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the text input wrapper.
        /// </summary>
        /// <returns>The text input CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-text-input",
                CssClassBuilder.When("mb-text-input--disabled", this.Disabled),
                CssClassBuilder.When("mb-text-input--readonly", this.ReadOnly),
                CssClassBuilder.When("mb-text-input--error", this.HasError));
        }

        /// <summary>
        /// Gets the native input type, falling back to text when no type is configured.
        /// </summary>
        /// <returns>The effective native input type.</returns>
        private string GetInputType()
        {
            return string.IsNullOrWhiteSpace(this.InputType) ? "text" : this.InputType;
        }

        /// <summary>
        /// Gets the autocomplete attribute when configured.
        /// </summary>
        /// <returns>The configured autocomplete hint, or null when omitted.</returns>
        private string GetAutocomplete()
        {
            return string.IsNullOrWhiteSpace(this.Autocomplete) ? null : this.Autocomplete;
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
