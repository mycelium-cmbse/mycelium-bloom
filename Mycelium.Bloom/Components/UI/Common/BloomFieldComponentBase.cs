// ------------------------------------------------------------------------------------------------
// <copyright file="BloomFieldComponentBase.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Common
{
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// Provides common parameters shared by reusable Bloom form field components.
    /// </summary>
    public class BloomFieldComponentBase : BloomComponentBase
    {
        /// <summary>
        /// The generated fallback identifier of the form field element.
        /// </summary>
        private readonly string generatedId = $"mb-field-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets the identifier of the form field element.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the form field element.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the label displayed for the form field.
        /// </summary>
        [Parameter]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the help text displayed for the form field.
        /// </summary>
        [Parameter]
        public string HelpText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error text displayed for the form field.
        /// </summary>
        [Parameter]
        public string ErrorText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the form field is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the form field is required.
        /// </summary>
        [Parameter]
        public bool Required { get; set; }

        /// <summary>
        /// Gets a value indicating whether the form field has an error.
        /// </summary>
        protected bool HasError => !string.IsNullOrWhiteSpace(this.ErrorText);

        /// <summary>
        /// Gets the effective identifier of the form field element.
        /// </summary>
        protected string FieldId => string.IsNullOrWhiteSpace(this.Id) ? this.generatedId : this.Id;

        /// <summary>
        /// Gets the effective name of the form field element.
        /// </summary>
        protected string FieldName => string.IsNullOrWhiteSpace(this.Name) ? null : this.Name;

        /// <summary>
        /// Gets a value indicating whether the form field has a visible label.
        /// </summary>
        protected bool HasLabel => !string.IsNullOrWhiteSpace(this.Label);

        /// <summary>
        /// Gets a value indicating whether the form field has help text.
        /// </summary>
        protected bool HasHelpText => !string.IsNullOrWhiteSpace(this.HelpText);

        /// <summary>
        /// Gets the identifier of the form field help text.
        /// </summary>
        protected string HelpTextId => $"{this.FieldId}-help";

        /// <summary>
        /// Gets the identifier of the form field error text.
        /// </summary>
        protected string ErrorTextId => $"{this.FieldId}-error";

        /// <summary>
        /// Gets the identifiers of the descriptions rendered for the form field.
        /// </summary>
        protected string DescribedBy
        {
            get
            {
                var descriptionIds = new[]
                {
                    this.HasHelpText ? this.HelpTextId : string.Empty,
                    this.HasError ? this.ErrorTextId : string.Empty
                };

                var describedBy = string.Join(" ", descriptionIds.Where(descriptionId => !string.IsNullOrWhiteSpace(descriptionId)));

                return string.IsNullOrWhiteSpace(describedBy) ? null : describedBy;
            }
        }

        /// <summary>
        /// Gets the accessible invalid state of the form field.
        /// </summary>
        protected string AriaInvalid => this.HasError ? "true" : null;
    }
}
