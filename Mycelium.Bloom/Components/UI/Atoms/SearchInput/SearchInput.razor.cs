namespace Mycelium.Bloom.Components.UI.Atoms.SearchInput
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Represents a reusable search input component with optional shortcut and icon support.
    /// </summary>
    public sealed partial class SearchInput : ComponentBase, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The generated fallback identifier of the search input element.
        /// </summary>
        private readonly string generatedId = $"mb-search-input-{Guid.NewGuid():N}";

        /// <summary>
        /// The JavaScript module used to manage the search shortcut.
        /// </summary>
        private IJSObjectReference module;

        /// <summary>
        /// Gets or sets the identifier of the search input element.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

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
        /// Gets or sets a value indicating whether the search input is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the search input wrapper.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

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
        /// Gets or sets additional unmatched attributes applied to the search input element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the effective identifier of the search input element.
        /// </summary>
        private string GetInputId()
        {
            var inputId = string.IsNullOrWhiteSpace(this.Id)
                ? this.generatedId
                : this.Id;

            return inputId;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the search input wrapper.
        /// </summary>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-search-input",
                CssClassBuilder.When("mb-search-input--full-width", this.FullWidth),
                CssClassBuilder.When("mb-search-input--disabled", this.Disabled),
                this.Class);

            return cssClass;
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender && this.EnableShortcut)
            {
                var shortcutKey = string.IsNullOrWhiteSpace(this.ShortcutKey)
                    ? "k"
                    : this.ShortcutKey;

                this.module = await this.JsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Components/UI/Atoms/SearchInput/SearchInput.razor.js");

                await this.module.InvokeVoidAsync(
                    "registerSearchShortcut",
                    this.GetInputId(),
                    new
                    {
                        key = shortcutKey,
                        requiresControlOrMeta = this.ShortcutRequiresControlOrMeta,
                        requiresAlt = this.ShortcutRequiresAlt,
                        requiresShift = this.ShortcutRequiresShift
                    });
            }
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
        /// Releases synchronous resources used by the search input component.
        /// </summary>
        public void Dispose()
        {
            // The component owns no synchronous resources. JavaScript cleanup is handled by DisposeAsync.
        }

        /// <summary>
        /// Releases asynchronous resources used by the search input component.
        /// </summary>
        /// <returns>A value task representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();
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
                    await this.module.InvokeVoidAsync("disposeSearchShortcut");
                    await this.module.DisposeAsync();

                    this.module = null;
                }
                catch (JSDisconnectedException)
                {
                    // The circuit is already disconnected, so there is nothing left to clean up on the client.
                }
            }
        }
    }
}
