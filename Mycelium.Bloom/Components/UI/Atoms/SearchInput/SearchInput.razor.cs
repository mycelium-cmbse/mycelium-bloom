// ------------------------------------------------------------------------------------------------
// <copyright file="SearchInput.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.SearchInput
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;

    /// <summary>
    /// Represents a reusable search input component with optional shortcut and icon support.
    /// </summary>
    public sealed partial class SearchInput : BloomFieldComponentBase, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The identifier that scopes JavaScript shortcut cleanup to this component instance.
        /// </summary>
        private readonly string shortcutRegistrationId = CreateGeneratedId("mb-search-shortcut");

        /// <summary>
        /// The JavaScript module used to manage the search shortcut.
        /// </summary>
        private IJSObjectReference module;

        /// <summary>
        /// The signature of the shortcut configuration currently registered in JavaScript.
        /// </summary>
        private string registeredShortcutSignature;

        /// <summary>
        /// A value indicating whether this component currently owns a shortcut registration.
        /// </summary>
        private bool shortcutRegistered;

        /// <summary>
        /// Gets or sets the current search value.
        /// </summary>
        [Parameter]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the search value changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text displayed when the search input is empty.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = "Search…";

        /// <summary>
        /// Gets or sets the shortcut text displayed next to the search input.
        /// </summary>
        [Parameter]
        public string ShortcutText { get; set; } = "Ctrl K";

        /// <summary>
        /// Gets or sets the keyboard key used for the search shortcut.
        /// </summary>
        [Parameter]
        public string ShortcutKey { get; set; } = "k";

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut requires Control or Command.
        /// </summary>
        [Parameter]
        public bool ShortcutRequiresControlOrMeta { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut requires Alt.
        /// </summary>
        [Parameter]
        public bool ShortcutRequiresAlt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut requires Shift.
        /// </summary>
        [Parameter]
        public bool ShortcutRequiresShift { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shortcut text should be displayed.
        /// </summary>
        [Parameter]
        public bool ShowShortcut { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the global search shortcut should focus this input.
        /// </summary>
        [Parameter]
        public bool EnableShortcut { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the search input should take the full available width.
        /// </summary>
        [Parameter]
        public bool FullWidth { get; set; }

        /// <summary>
        /// Gets or sets optional content rendered before the search input.
        /// </summary>
        [Parameter]
        public RenderFragment StartIcon { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a key is pressed while the search input is focused.
        /// </summary>
        [Parameter]
        public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

        /// <summary>
        /// Releases asynchronous resources used by the search input component.
        /// </summary>
        /// <returns>A value task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();
        }

        /// <summary>
        /// Releases synchronous resources used by the search input component.
        /// </summary>
        public void Dispose()
        {
            // The component owns no synchronous resources. JavaScript cleanup is handled by DisposeAsync.
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (this.EnableShortcut)
            {
                await this.RegisterSearchShortcutAsync();
            }
            else
            {
                await this.DisposeSearchShortcutAsync();
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the search input wrapper.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass(
                "mb-search-input",
                CssClassBuilder.When("mb-search-input--full-width", this.FullWidth),
                CssClassBuilder.When("mb-search-input--with-shortcut", this.ShouldShowShortcut()),
                CssClassBuilder.When("mb-search-input--disabled", this.Disabled));

            return cssClass;
        }

        /// <summary>
        /// Gets a value indicating whether the visual shortcut hint should be rendered.
        /// </summary>
        /// <returns>True when the shortcut is enabled and has a visible label; otherwise, false.</returns>
        private bool ShouldShowShortcut()
        {
            return this.EnableShortcut
                   && this.ShowShortcut
                   && !string.IsNullOrWhiteSpace(this.ShortcutText);
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

        /// <summary>
        /// Handles key down events and forwards them to the parent component.
        /// </summary>
        /// <param name="args">The keyboard event arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleKeyDownAsync(KeyboardEventArgs args)
        {
            await this.OnKeyDown.InvokeAsync(args);
        }

        /// <summary>
        /// Registers or refreshes this component's shortcut when its effective configuration changes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RegisterSearchShortcutAsync()
        {
            var shortcutKey = string.IsNullOrWhiteSpace(this.ShortcutKey)
                ? "k"
                : this.ShortcutKey;
            var registrationSignature = this.GetShortcutRegistrationSignature(shortcutKey);

            if (this.shortcutRegistered
                && string.Equals(this.registeredShortcutSignature, registrationSignature, StringComparison.Ordinal))
            {
                return;
            }

            this.module ??= await this.JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/UI/Atoms/SearchInput/SearchInput.razor.js");

            await this.module.InvokeVoidAsync(
                "registerSearchShortcut",
                this.shortcutRegistrationId,
                this.FieldId,
                new
                {
                    key = shortcutKey,
                    requiresControlOrMeta = this.ShortcutRequiresControlOrMeta,
                    requiresAlt = this.ShortcutRequiresAlt,
                    requiresShift = this.ShortcutRequiresShift
                });

            this.registeredShortcutSignature = registrationSignature;
            this.shortcutRegistered = true;
        }

        /// <summary>
        /// Gets a signature representing the effective shortcut registration settings.
        /// </summary>
        /// <param name="shortcutKey">The effective shortcut key.</param>
        /// <returns>The shortcut registration signature.</returns>
        private string GetShortcutRegistrationSignature(string shortcutKey)
        {
            return $"{this.FieldId}\u001f{shortcutKey}\u001f{this.ShortcutRequiresControlOrMeta}"
                   + $"\u001f{this.ShortcutRequiresAlt}\u001f{this.ShortcutRequiresShift}";
        }

        /// <summary>
        /// Disposes only the shortcut registration owned by this component instance.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DisposeSearchShortcutAsync()
        {
            if (this.module is null || !this.shortcutRegistered)
            {
                return;
            }

            await this.module.InvokeVoidAsync("disposeSearchShortcut", this.shortcutRegistrationId);

            this.registeredShortcutSignature = null;
            this.shortcutRegistered = false;
        }

        /// <summary>
        /// Asynchronously disposes the JavaScript shortcut registration.
        /// </summary>
        /// <returns>A value task representing the asynchronous dispose operation.</returns>
        private async ValueTask DisposeAsyncCore()
        {
            if (this.module is not null)
            {
                try
                {
                    await this.DisposeSearchShortcutAsync();
                    await this.module.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // The circuit is already disconnected, so there is nothing left to clean up on the client.
                }
                finally
                {
                    this.module = null;
                    this.registeredShortcutSignature = null;
                    this.shortcutRegistered = false;
                }
            }
        }
    }
}
