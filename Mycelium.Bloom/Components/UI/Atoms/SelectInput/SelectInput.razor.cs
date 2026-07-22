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
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a reusable controlled select input with a custom accessible listbox.
    /// </summary>
    /// <remarks>
    /// Enabled named instances use a hidden input for form submission. Hidden inputs do not participate in native
    /// constraint validation, and this component does not derive from <see cref="Microsoft.AspNetCore.Components.Forms.InputBase{TValue}" />.
    /// Consumers remain responsible for model or <c>EditContext</c> validation and may supply validation feedback
    /// through <see cref="BloomFieldComponentBase.ErrorText" />. The required state is exposed through ARIA.
    /// </remarks>
    public sealed partial class SelectInput : BloomFieldComponentBase, IAsyncDisposable
    {
        /// <summary>
        /// The rendered trigger used by the narrowly scoped keyboard-default handler.
        /// </summary>
        private ElementReference triggerElement;

        /// <summary>
        /// The root containing the trigger and listbox for outside-click detection.
        /// </summary>
        private ElementReference popupRootElement;

        /// <summary>
        /// The indexed option view used for stable navigation and option identifiers.
        /// </summary>
        private IReadOnlyList<SelectInputOption> optionList = [];

        /// <summary>
        /// The element-scoped keyboard-default registration.
        /// </summary>
        private KeyboardDefaultPreventionRegistration keyboardDefaultPreventionRegistration;

        /// <summary>
        /// The instance-specific outside-click registration.
        /// </summary>
        private OutsideClickRegistration<SelectInput> outsideClickRegistration;

        /// <summary>
        /// A value indicating whether an option callback is currently running.
        /// </summary>
        private bool isSelecting;

        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

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
        /// Gets a value indicating whether the listbox is open for this component instance.
        /// </summary>
        private bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets the enabled option participating in active keyboard navigation.
        /// </summary>
        private int ActiveOptionIndex { get; set; } = -1;

        /// <summary>
        /// Gets the stable listbox identifier derived from the existing field identifier foundation.
        /// </summary>
        private string ListboxId => $"{this.FieldId}-listbox";

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            this.optionList = this.Options as IReadOnlyList<SelectInputOption> ?? this.Options.ToArray();

            if (this.Disabled || this.optionList.Count == 0)
            {
                this.CloseMenu();
                return;
            }

            if (this.IsOpen)
            {
                var selectedIndex = this.FindSelectedEnabledIndex();

                if (selectedIndex >= 0)
                {
                    this.ActiveOptionIndex = selectedIndex;
                }
                else if (!this.IsEnabledIndex(this.ActiveOptionIndex))
                {
                    this.ActiveOptionIndex = this.FindEnabledIndex(0, 1);
                }
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender || this.isDisposed)
            {
                return;
            }

            this.keyboardDefaultPreventionRegistration = new KeyboardDefaultPreventionRegistration(this.JsRuntime);

            await this.keyboardDefaultPreventionRegistration.RegisterAsync(
                this.triggerElement,
                [
                    new KeyboardDefaultPreventionRule(
                        null,
                        "Enter",
                        " ",
                        "Space",
                        "Spacebar",
                        "ArrowDown",
                        "Down",
                        "ArrowUp",
                        "Up",
                        "Home",
                        "End")
                ]);

            this.outsideClickRegistration = new OutsideClickRegistration<SelectInput>(this.JsRuntime);
            await this.outsideClickRegistration.RegisterAsync(this.popupRootElement, this);
        }

        /// <summary>
        /// Releases the per-instance keyboard registration and JavaScript module reference.
        /// </summary>
        /// <returns>A value task representing the asynchronous operation.</returns>
        public async ValueTask DisposeAsync()
        {
            this.isDisposed = true;
            this.IsOpen = false;
            this.ActiveOptionIndex = -1;

            if (this.outsideClickRegistration is not null)
            {
                await this.outsideClickRegistration.DisposeAsync();
                this.outsideClickRegistration = null;
            }

            if (this.keyboardDefaultPreventionRegistration is not null)
            {
                await this.keyboardDefaultPreventionRegistration.DisposeAsync();
                this.keyboardDefaultPreventionRegistration = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes an open listbox after a pointer interaction outside this component instance.
        /// </summary>
        /// <returns>A task representing the render update.</returns>
        [JSInvokable]
        public Task DismissFromOutsideClickAsync()
        {
            if (this.isDisposed || !this.IsOpen)
            {
                return Task.CompletedTask;
            }

            this.CloseMenu();

            return this.InvokeAsync(this.StateHasChanged);
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
        /// Gets the visible text derived only from the parent-owned value.
        /// </summary>
        /// <returns>The matching option label, placeholder, or unmatched controlled value.</returns>
        private string GetDisplayText()
        {
            var selectedOption = this.optionList.FirstOrDefault(this.IsSelected);

            if (selectedOption is not null)
            {
                return selectedOption.Label;
            }

            return string.IsNullOrWhiteSpace(this.Value) ? this.Placeholder : this.Value;
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
        /// Gets the stable identifier of an option at the provided index.
        /// </summary>
        /// <param name="optionIndex">The option index.</param>
        /// <returns>The instance-specific option identifier.</returns>
        private string GetOptionId(int optionIndex)
        {
            return $"{this.FieldId}-option-{optionIndex}";
        }

        /// <summary>
        /// Gets the active-descendant identifier while an enabled option is active.
        /// </summary>
        /// <returns>The active option identifier, or null while the listbox is closed.</returns>
        private string GetActiveOptionId()
        {
            return this.IsOpen && this.IsEnabledIndex(this.ActiveOptionIndex)
                ? this.GetOptionId(this.ActiveOptionIndex)
                : null;
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
        /// Gets the accessible invalid state derived from explicit validation feedback or a missing required value.
        /// </summary>
        /// <returns>True when the controlled value is invalid; otherwise, null.</returns>
        private string GetAriaInvalid()
        {
            return this.HasError || this.IsRequiredValueMissing() ? "true" : null;
        }

        /// <summary>
        /// Gets a value indicating whether the controlled value is missing while the field is required.
        /// </summary>
        /// <returns>True when a required controlled value is blank; otherwise, false.</returns>
        private bool IsRequiredValueMissing()
        {
            return this.Required && string.IsNullOrWhiteSpace(this.Value);
        }

        /// <summary>
        /// Gets the final CSS class list applied to a listbox option.
        /// </summary>
        /// <param name="optionIndex">The option index.</param>
        /// <returns>The option CSS class list.</returns>
        private string GetOptionCssClass(int optionIndex)
        {
            var option = this.optionList[optionIndex];

            return CssClassBuilder.Build(
                "mb-select-input__option",
                CssClassBuilder.When("mb-select-input__option--active", optionIndex == this.ActiveOptionIndex),
                CssClassBuilder.When("mb-select-input__option--selected", this.IsSelected(option)),
                CssClassBuilder.When("mb-select-input__option--disabled", option.Disabled));
        }

        /// <summary>
        /// Gets a value indicating whether the provided option matches the parent-owned value.
        /// </summary>
        /// <param name="option">The option to inspect.</param>
        /// <returns>True when the option is selected; otherwise, false.</returns>
        private bool IsSelected(SelectInputOption option)
        {
            return string.Equals(option.Value, this.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Toggles the listbox from a pointer or native button activation.
        /// </summary>
        private void ToggleMenu()
        {
            if (this.Disabled || this.isSelecting || this.isDisposed)
            {
                return;
            }

            if (this.IsOpen)
            {
                this.CloseMenu();
            }
            else
            {
                this.OpenMenu(this.GetInitialActiveIndex(1));
            }
        }

        /// <summary>
        /// Handles custom-select keyboard interaction while focus remains on the combobox trigger.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleKeyDownAsync(KeyboardEventArgs args)
        {
            if (this.Disabled || this.isSelecting || this.isDisposed)
            {
                return;
            }

            switch (args.Key)
            {
                case "Enter":
                case " ":
                case "Space":
                case "Spacebar":
                    if (this.IsOpen && this.IsEnabledIndex(this.ActiveOptionIndex))
                    {
                        await this.SelectOptionAsync(this.ActiveOptionIndex);
                    }
                    else if (!this.IsOpen)
                    {
                        this.OpenMenu(this.GetInitialActiveIndex(1));
                    }

                    break;
                case "ArrowDown":
                case "Down":
                    this.MoveOrOpen(1);
                    break;
                case "ArrowUp":
                case "Up":
                    this.MoveOrOpen(-1);
                    break;
                case "Home":
                    this.OpenMenu(this.FindEnabledIndex(0, 1));
                    break;
                case "End":
                    this.OpenMenu(this.FindEnabledIndex(this.optionList.Count - 1, -1));
                    break;
                case "Escape":
                    this.CloseMenu();
                    break;
                case "Tab":
                    this.CloseMenu();
                    break;
            }
        }

        /// <summary>
        /// Opens the listbox or moves to the next enabled option without wrapping.
        /// </summary>
        /// <param name="direction">One to move down, or minus one to move up.</param>
        private void MoveOrOpen(int direction)
        {
            if (!this.IsOpen)
            {
                this.OpenMenu(this.GetInitialActiveIndex(direction));
                return;
            }

            var nextIndex = this.FindEnabledIndex(this.ActiveOptionIndex + direction, direction);

            if (nextIndex >= 0)
            {
                this.ActiveOptionIndex = nextIndex;
            }
        }

        /// <summary>
        /// Opens the listbox with the provided enabled option active.
        /// </summary>
        /// <param name="optionIndex">The option index to activate.</param>
        private void OpenMenu(int optionIndex)
        {
            if (this.Disabled || this.isSelecting || this.isDisposed)
            {
                return;
            }

            this.IsOpen = true;
            this.ActiveOptionIndex = this.IsEnabledIndex(optionIndex) ? optionIndex : -1;
        }

        /// <summary>
        /// Closes the listbox and clears its active option.
        /// </summary>
        private void CloseMenu()
        {
            this.IsOpen = false;
            this.ActiveOptionIndex = -1;
        }

        /// <summary>
        /// Selects an enabled option without mutating the parent-owned value.
        /// </summary>
        /// <param name="optionIndex">The selected option index.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectOptionAsync(int optionIndex)
        {
            if (!this.IsOpen || !this.IsEnabledIndex(optionIndex) || this.isSelecting || this.isDisposed)
            {
                return;
            }

            var option = this.optionList[optionIndex];
            this.isSelecting = true;
            this.CloseMenu();

            try
            {
                if (!this.isDisposed)
                {
                    await this.ValueChanged.InvokeAsync(option.Value);
                }
            }
            finally
            {
                this.isSelecting = false;
            }
        }

        /// <summary>
        /// Gets the selected enabled option or the directional enabled fallback.
        /// </summary>
        /// <param name="direction">The navigation direction used for the fallback.</param>
        /// <returns>The initial enabled option index, or negative one when none is available.</returns>
        private int GetInitialActiveIndex(int direction)
        {
            var selectedIndex = this.FindSelectedEnabledIndex();

            return selectedIndex >= 0
                ? selectedIndex
                : this.FindEnabledIndex(direction > 0 ? 0 : this.optionList.Count - 1, direction);
        }

        /// <summary>
        /// Finds the enabled option matching the parent-owned value.
        /// </summary>
        /// <returns>The selected enabled option index, or negative one.</returns>
        private int FindSelectedEnabledIndex()
        {
            for (var index = 0; index < this.optionList.Count; index++)
            {
                if (this.IsEnabledIndex(index) && this.IsSelected(this.optionList[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds the first enabled option from a starting index without wrapping.
        /// </summary>
        /// <param name="startIndex">The first index to inspect.</param>
        /// <param name="direction">The search direction.</param>
        /// <returns>The enabled option index, or negative one.</returns>
        private int FindEnabledIndex(int startIndex, int direction)
        {
            for (var index = startIndex; index >= 0 && index < this.optionList.Count; index += direction)
            {
                if (this.IsEnabledIndex(index))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets a value indicating whether an index identifies an enabled option.
        /// </summary>
        /// <param name="optionIndex">The option index.</param>
        /// <returns>True when the option exists and is enabled; otherwise, false.</returns>
        private bool IsEnabledIndex(int optionIndex)
        {
            return optionIndex >= 0
                   && optionIndex < this.optionList.Count
                   && !this.optionList[optionIndex].Disabled;
        }
    }
}
