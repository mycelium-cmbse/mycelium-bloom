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
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable text area component for Bloom forms and dialogs.
    /// </summary>
    public partial class TextArea : ComponentBase
    {
        /// <summary>
        /// The generated fallback identifier of the text area element.
        /// </summary>
        private readonly string generatedId = $"mb-text-area-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets the identifier of the text area element.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the text area element.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional text area label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

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
        /// Gets or sets the placeholder text displayed when the text area is empty.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional help text rendered under the text area when no error is present.
        /// </summary>
        [Parameter]
        public string HelpText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional error text rendered under the text area.
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the text area is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the text area is required.
        /// </summary>
        [Parameter]
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the text area is read-only.
        /// </summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the visible row count of the text area.
        /// </summary>
        [Parameter]
        public int Rows { get; set; } = 4;

        /// <summary>
        /// Gets or sets the maximum allowed character count.
        /// </summary>
        [Parameter]
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current character count should be rendered.
        /// </summary>
        [Parameter]
        public bool ShowCharacterCount { get; set; }

        /// <summary>
        /// Gets or sets the visual size of the text area.
        /// </summary>
        [Parameter]
        public TextAreaSize Size { get; set; } = TextAreaSize.Medium;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the text area wrapper.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the text area element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the text area wrapper.
        /// </summary>
        /// <returns>The text area CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-text-area",
                this.GetSizeClass(),
                CssClassBuilder.When("mb-text-area--disabled", this.Disabled),
                CssClassBuilder.When("mb-text-area--readonly", this.ReadOnly),
                CssClassBuilder.When("mb-text-area--error", this.HasError()),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected text area size.
        /// </summary>
        /// <returns>The text area size CSS class.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                TextAreaSize.Small => "mb-text-area--small",
                TextAreaSize.Large => "mb-text-area--large",
                _ => "mb-text-area--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Gets the effective identifier of the text area element.
        /// </summary>
        /// <returns>The explicit or generated text area identifier.</returns>
        private string GetTextAreaId()
        {
            var textAreaId = string.IsNullOrWhiteSpace(this.Id)
                ? this.generatedId
                : this.Id;

            return textAreaId;
        }

        /// <summary>
        /// Gets the effective text area name when provided.
        /// </summary>
        /// <returns>The text area name, or null when no name is configured.</returns>
        private string GetTextAreaName()
        {
            var textAreaName = string.IsNullOrWhiteSpace(this.Name)
                ? null
                : this.Name;

            return textAreaName;
        }

        /// <summary>
        /// Gets the maximum allowed character count attribute when configured.
        /// </summary>
        /// <returns>The maximum allowed character count, or null when no limit is configured.</returns>
        private int? GetMaxLength()
        {
            var maxLength = this.MaxLength > 0
                ? this.MaxLength
                : (int?)null;

            return maxLength;
        }

        /// <summary>
        /// Gets the help text identifier.
        /// </summary>
        /// <returns>The help text identifier.</returns>
        private string GetHelpId()
        {
            var helpId = $"{this.GetTextAreaId()}-help";

            return helpId;
        }

        /// <summary>
        /// Gets the error text identifier.
        /// </summary>
        /// <returns>The error text identifier.</returns>
        private string GetErrorId()
        {
            var errorId = $"{this.GetTextAreaId()}-error";

            return errorId;
        }

        /// <summary>
        /// Gets the character count identifier.
        /// </summary>
        /// <returns>The character count identifier.</returns>
        private string GetCharacterCountId()
        {
            var characterCountId = $"{this.GetTextAreaId()}-count";

            return characterCountId;
        }

        /// <summary>
        /// Gets the identifier of the active text area description.
        /// </summary>
        /// <returns>The error, help, or count text identifiers when description text is rendered.</returns>
        private string GetDescriptionId()
        {
            var descriptionIds = new List<string>();

            if (this.HasError())
            {
                descriptionIds.Add(this.GetErrorId());
            }
            else if (this.HasHelpText())
            {
                descriptionIds.Add(this.GetHelpId());
            }

            if (this.ShouldShowCharacterCount())
            {
                descriptionIds.Add(this.GetCharacterCountId());
            }

            var descriptionId = descriptionIds.Count > 0
                ? string.Join(" ", descriptionIds)
                : null;

            return descriptionId;
        }

        /// <summary>
        /// Gets the aria-invalid value when the text area is in an error state.
        /// </summary>
        /// <returns>The aria-invalid value when an error is present; otherwise, null.</returns>
        private string GetAriaInvalid()
        {
            var ariaInvalid = this.HasError()
                ? "true"
                : null;

            return ariaInvalid;
        }

        /// <summary>
        /// Gets the current text value length.
        /// </summary>
        /// <returns>The current text value length.</returns>
        private int GetCurrentLength()
        {
            var currentLength = this.Value?.Length ?? 0;

            return currentLength;
        }

        /// <summary>
        /// Gets a value indicating whether the text area has a visible label.
        /// </summary>
        /// <returns>True when a label is provided; otherwise, false.</returns>
        private bool HasLabel()
        {
            var hasLabel = !string.IsNullOrWhiteSpace(this.Label);

            return hasLabel;
        }

        /// <summary>
        /// Gets a value indicating whether help text should be rendered.
        /// </summary>
        /// <returns>True when help text is provided; otherwise, false.</returns>
        private bool HasHelpText()
        {
            var hasHelpText = !string.IsNullOrWhiteSpace(this.HelpText);

            return hasHelpText;
        }

        /// <summary>
        /// Gets a value indicating whether the text area has an error.
        /// </summary>
        /// <returns>True when error text is provided; otherwise, false.</returns>
        private bool HasError()
        {
            var hasError = !string.IsNullOrWhiteSpace(this.ErrorText);

            return hasError;
        }

        /// <summary>
        /// Gets a value indicating whether character count should be rendered.
        /// </summary>
        /// <returns>True when character count is enabled and a maximum length is configured; otherwise, false.</returns>
        private bool ShouldShowCharacterCount()
        {
            var shouldShowCharacterCount = this.ShowCharacterCount && this.MaxLength > 0;

            return shouldShowCharacterCount;
        }

        /// <summary>
        /// Gets a value indicating whether the footer should be rendered.
        /// </summary>
        /// <returns>True when the footer has help, error, or count content; otherwise, false.</returns>
        private bool HasFooter()
        {
            var hasFooter = this.HasError() || this.HasHelpText() || this.ShouldShowCharacterCount();

            return hasFooter;
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
