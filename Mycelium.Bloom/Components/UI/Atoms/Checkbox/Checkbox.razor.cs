// ------------------------------------------------------------------------------------------------
// <copyright file="Checkbox.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.Checkbox
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable native checkbox for Bloom forms and settings.
    /// </summary>
    public sealed partial class Checkbox : BloomFieldComponentBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the checkbox is checked.
        /// </summary>
        [Parameter]
        public bool Checked { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the checked state changes.
        /// </summary>
        [Parameter]
        public EventCallback<bool> CheckedChanged { get; set; }

        /// <summary>
        /// Gets or sets optional rich label content.
        /// </summary>
        [Parameter]
        public RenderFragment LabelContent { get; set; }

        /// <summary>
        /// Gets or sets optional rich description content.
        /// </summary>
        [Parameter]
        public RenderFragment DescriptionContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the checkbox wrapper.
        /// </summary>
        /// <returns>The checkbox CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-checkbox",
                CssClassBuilder.When("mb-checkbox--checked", this.Checked),
                CssClassBuilder.When("mb-checkbox--disabled", this.Disabled),
                CssClassBuilder.When("mb-checkbox--error", this.HasError));
        }

        /// <summary>
        /// Gets a value indicating whether a description is configured.
        /// </summary>
        /// <returns>True when text or rich description content is configured; otherwise, false.</returns>
        private bool HasDescription()
        {
            return this.HasHelpText || this.DescriptionContent is not null;
        }

        /// <summary>
        /// Gets a value indicating whether visible checkbox content is configured.
        /// </summary>
        /// <returns>True when label, description, or error content is configured; otherwise, false.</returns>
        private bool HasContent()
        {
            return this.HasLabel || this.LabelContent is not null || this.HasDescription() || this.HasError;
        }

        /// <summary>
        /// Gets the identifiers of the descriptions rendered for the checkbox.
        /// </summary>
        /// <returns>The rendered description identifiers, or null when no descriptions are rendered.</returns>
        private string GetDescribedBy()
        {
            var descriptionIds = new[]
            {
                this.HasDescription() ? this.HelpTextId : string.Empty,
                this.HasError ? this.ErrorTextId : string.Empty
            };

            var describedBy = string.Join(" ", descriptionIds.Where(descriptionId => !string.IsNullOrWhiteSpace(descriptionId)));

            return string.IsNullOrWhiteSpace(describedBy) ? null : describedBy;
        }

        /// <summary>
        /// Handles checked state changes and forwards the updated value to the parent component.
        /// </summary>
        /// <param name="args">The input change event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleCheckedChangedAsync(ChangeEventArgs args)
        {
            var isChecked = args.Value is bool value && value;

            this.Checked = isChecked;

            await this.CheckedChanged.InvokeAsync(isChecked);
        }
    }
}
