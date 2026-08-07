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
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Presents a labelled Bloom field through the Blazor Blueprint select primitive.
    /// </summary>
    /// <remarks>
    /// Enabled named instances use a hidden input for form submission. Hidden inputs do not participate in native
    /// constraint validation, and this component does not derive from <see cref="Microsoft.AspNetCore.Components.Forms.InputBase{TValue}" />.
    /// Consumers remain responsible for model or <c>EditContext</c> validation and may supply validation feedback
    /// through <see cref="BloomFieldComponentBase.ErrorText" />. The required state is exposed through ARIA.
    /// </remarks>
    public sealed partial class SelectInput : BloomFieldComponentBase, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The identifier that scopes JavaScript compatibility cleanup to this component instance.
        /// </summary>
        private readonly string compatibilityRegistrationId = CreateGeneratedId("mb-select-compatibility");

        /// <summary>
        /// The indexed option view used by the Blueprint primitive.
        /// </summary>
        private IReadOnlyList<SelectInputOption> optionList = [];

        /// <summary>
        /// The JavaScript module that bridges verified Blueprint 3.15.0 browser-behavior gaps.
        /// </summary>
        private IJSObjectReference compatibilityModule;

        /// <summary>
        /// Gets or sets the JavaScript runtime used by the compatibility bridge.
        /// </summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>
        /// Gets or sets the selected option value owned by the parent component.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when an enabled option is requested.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text displayed when no option is selected.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the options rendered by the select input.
        /// </summary>
        [Parameter]
        public IReadOnlyCollection<SelectInputOption> Options { get; set; } = [];

        /// <summary>
        /// Gets or sets whether the uncontrolled Blueprint listbox starts open.
        /// </summary>
        [Parameter]
        public bool DefaultOpen { get; set; }

        /// <summary>
        /// Gets a value indicating whether the Blueprint listbox is open.
        /// </summary>
        private bool IsOpen { get; set; }

        /// <summary>
        /// Releases asynchronous browser resources owned by the select input.
        /// </summary>
        /// <returns>A value task representing the asynchronous operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();
        }

        /// <summary>
        /// Releases synchronous resources owned by the select input.
        /// </summary>
        public void Dispose()
        {
            // The component owns no synchronous resources. JavaScript cleanup is handled by DisposeAsync.
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.IsOpen = this.DefaultOpen && !this.Disabled;
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            this.optionList = this.Options as IReadOnlyList<SelectInputOption> ?? this.Options.ToArray();

            if (this.Disabled)
            {
                this.IsOpen = false;
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.compatibilityModule ??= await this.JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/UI/Atoms/SelectInput/SelectInput.razor.js");

            await this.compatibilityModule.InvokeVoidAsync(
                "registerSelectCompatibility",
                this.compatibilityRegistrationId,
                this.FieldId);
        }

        /// <summary>
        /// Gets the final CSS class list applied to the select input wrapper.
        /// </summary>
        /// <returns>The select input CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-select-input",
                CssClassBuilder.When("mb-select-input--open", this.IsOpen),
                CssClassBuilder.When("mb-select-input--disabled", this.Disabled),
                CssClassBuilder.When("mb-select-input--error", this.HasError));
        }

        /// <summary>
        /// Resolves visible text for a parent-owned value.
        /// </summary>
        /// <param name="value">The controlled value.</param>
        /// <returns>The option label, placeholder, or unmatched value.</returns>
        private string GetDisplayTextForValue(string value)
        {
            var selectedOption = this.optionList.FirstOrDefault(option =>
                string.Equals(option.Value, value, StringComparison.Ordinal));

            if (selectedOption is not null)
            {
                return selectedOption.Label;
            }

            return string.IsNullOrWhiteSpace(value) ? this.Placeholder : value;
        }

        /// <summary>
        /// Gets the CSS class applied to the trigger value or placeholder.
        /// </summary>
        /// <returns>The trigger text CSS class.</returns>
        private string GetValueCssClass()
        {
            return CssClassBuilder.Build(
                "mb-select-input__value",
                CssClassBuilder.When(
                    "mb-select-input__value--placeholder",
                    string.IsNullOrWhiteSpace(this.Value)));
        }

        /// <summary>
        /// Gets the stable identifier of the enabled form-submission proxy.
        /// </summary>
        /// <returns>The form-value input identifier.</returns>
        private string GetFormValueId()
        {
            return $"{this.FieldId}-form-value";
        }

        /// <summary>
        /// Gets the explicit ARIA selection state for a Blueprint option.
        /// </summary>
        /// <param name="option">The option to compare with the controlled value.</param>
        /// <returns>The string-valued ARIA selection state.</returns>
        private string IsOptionSelected(SelectInputOption option)
        {
            return string.Equals(option.Value, this.Value, StringComparison.Ordinal) ? "true" : "false";
        }

        /// <summary>
        /// Gets the accessible invalid state derived from validation feedback or a missing required value.
        /// </summary>
        /// <returns>True when the controlled value is invalid; otherwise, null.</returns>
        private string GetAriaInvalid()
        {
            return this.HasError || (this.Required && string.IsNullOrWhiteSpace(this.Value)) ? "true" : null;
        }

        /// <summary>
        /// Tracks the primitive's controlled open state.
        /// </summary>
        /// <param name="isOpen">The requested open state.</param>
        private void HandleOpenChanged(bool isOpen)
        {
            this.IsOpen = !this.Disabled && isOpen;
        }

        /// <summary>
        /// Forwards an enabled primitive selection without mutating the parent-owned value.
        /// </summary>
        /// <param name="value">The requested option value.</param>
        /// <returns>A task representing the callback.</returns>
        private async Task HandleValueChangedAsync(string value)
        {
            var option = this.optionList.FirstOrDefault(candidate =>
                string.Equals(candidate.Value, value, StringComparison.Ordinal));

            if (!this.Disabled && option is not null && !option.Disabled)
            {
                this.IsOpen = false;
                await this.ValueChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Releases the compatibility registration and its JavaScript module.
        /// </summary>
        /// <returns>A value task representing the asynchronous operation.</returns>
        private async ValueTask DisposeAsyncCore()
        {
            if (this.compatibilityModule is null)
            {
                return;
            }

            try
            {
                await this.compatibilityModule.InvokeVoidAsync(
                    "disposeSelectCompatibility",
                    this.compatibilityRegistrationId);
                await this.compatibilityModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit has already disconnected, so browser cleanup is no longer available.
            }
            finally
            {
                this.compatibilityModule = null;
            }
        }
    }
}
