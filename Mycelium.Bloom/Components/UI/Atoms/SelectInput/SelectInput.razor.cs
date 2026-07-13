// ------------------------------------------------------------------------------------------------
// <copyright file="SelectInput.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.SelectInput
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a reusable native select input for Bloom forms and dialogs.
    /// </summary>
    public sealed partial class SelectInput : BloomFieldComponentBase
    {
        /// <summary>
        /// Gets or sets the selected option value.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the selected value changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder option text.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the options rendered by the select input.
        /// </summary>
        [Parameter]
        public IReadOnlyCollection<SelectInputOption> Options { get; set; } = [];

        /// <summary>
        /// Gets the final CSS class list applied to the select input wrapper.
        /// </summary>
        /// <returns>The select input CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-select-input",
                CssClassBuilder.When("mb-select-input--disabled", this.Disabled),
                CssClassBuilder.When("mb-select-input--error", this.HasError));
        }

        /// <summary>
        /// Gets a value indicating whether a placeholder option should be rendered.
        /// </summary>
        /// <returns>True when placeholder text is configured; otherwise, false.</returns>
        private bool HasPlaceholder()
        {
            return !string.IsNullOrWhiteSpace(this.Placeholder);
        }

        /// <summary>
        /// Gets a value indicating whether the placeholder is selected.
        /// </summary>
        /// <returns>True when the selected value is blank; otherwise, false.</returns>
        private bool IsPlaceholderSelected()
        {
            return string.IsNullOrWhiteSpace(this.Value);
        }

        /// <summary>
        /// Gets a value indicating whether the provided option is selected.
        /// </summary>
        /// <param name="option">The option to inspect.</param>
        /// <returns>True when the option matches the selected value; otherwise, false.</returns>
        private bool IsSelected(SelectInputOption option)
        {
            return string.Equals(option.Value, this.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Handles selection changes and forwards the updated value to the parent component.
        /// </summary>
        /// <param name="args">The selection change event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleValueChangedAsync(ChangeEventArgs args)
        {
            var value = args.Value?.ToString() ?? string.Empty;

            this.Value = value;

            await this.ValueChanged.InvokeAsync(value);
        }
    }
}
