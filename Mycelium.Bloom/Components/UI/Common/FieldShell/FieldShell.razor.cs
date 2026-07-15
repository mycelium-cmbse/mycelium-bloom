// ------------------------------------------------------------------------------------------------
// <copyright file="FieldShell.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Common.FieldShell
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Provides shared rendering and presentation for labelled Bloom form fields.
    /// </summary>
    public sealed partial class FieldShell : BloomComponentBase
    {
        /// <summary>
        /// Gets or sets the identifier of the native control associated with the label.
        /// </summary>
        [Parameter]
        public string ControlId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible field label.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the help text displayed for the field.
        /// </summary>
        [Parameter]
        public string HelpText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the help text element.
        /// </summary>
        [Parameter]
        public string HelpTextId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error text displayed for the field.
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the error text element.
        /// </summary>
        [Parameter]
        public string ErrorTextId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the field is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is required.
        /// </summary>
        [Parameter]
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is read-only.
        /// </summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the native control content rendered by the field shell.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets the final CSS class list applied to the field shell.
        /// </summary>
        /// <returns>The field shell CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-field-shell",
                CssClassBuilder.When("mb-field-shell--disabled", this.Disabled),
                CssClassBuilder.When("mb-field-shell--readonly", this.ReadOnly),
                CssClassBuilder.When("mb-field-shell--error", this.HasError()));
        }

        /// <summary>
        /// Gets a value indicating whether a visible label is configured.
        /// </summary>
        /// <returns>True when a label is configured; otherwise, false.</returns>
        private bool HasLabel()
        {
            return !string.IsNullOrWhiteSpace(this.Label);
        }

        /// <summary>
        /// Gets a value indicating whether help text is configured.
        /// </summary>
        /// <returns>True when help text is configured; otherwise, false.</returns>
        private bool HasHelpText()
        {
            return !string.IsNullOrWhiteSpace(this.HelpText);
        }

        /// <summary>
        /// Gets a value indicating whether error text is configured.
        /// </summary>
        /// <returns>True when error text is configured; otherwise, false.</returns>
        private bool HasError()
        {
            return !string.IsNullOrWhiteSpace(this.ErrorText);
        }
    }
}
