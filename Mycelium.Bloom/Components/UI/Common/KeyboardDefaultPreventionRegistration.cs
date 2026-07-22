// ------------------------------------------------------------------------------------------------
// <copyright file="KeyboardDefaultPreventionRegistration.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Common
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    /// <summary>
    /// Owns an element-scoped registration that prevents browser defaults for explicitly handled keys.
    /// </summary>
    internal sealed class KeyboardDefaultPreventionRegistration : IAsyncDisposable
    {
        /// <summary>
        /// The JavaScript runtime used to import the shared helper.
        /// </summary>
        private readonly IJSRuntime jsRuntime;

        /// <summary>
        /// The imported JavaScript module.
        /// </summary>
        private IJSObjectReference module;

        /// <summary>
        /// The element that owns the keyboard listener.
        /// </summary>
        private ElementReference rootElement;

        /// <summary>
        /// A value indicating whether the browser registration is active.
        /// </summary>
        private bool isRegistered;

        /// <summary>
        /// A value indicating whether this registration has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardDefaultPreventionRegistration" /> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime used to import the helper.</param>
        internal KeyboardDefaultPreventionRegistration(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Registers keyboard rules against one component-owned root element.
        /// </summary>
        /// <param name="rootElement">The element that owns the keyboard listener.</param>
        /// <param name="rules">The target selectors and keys whose browser defaults should be prevented.</param>
        /// <returns>A task representing the asynchronous registration.</returns>
        internal async Task RegisterAsync(
            ElementReference rootElement,
            IReadOnlyCollection<KeyboardDefaultPreventionRule> rules)
        {
            if (this.isDisposed || this.isRegistered)
            {
                return;
            }

            this.module = await this.jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/keyboard-defaults.js");
            this.rootElement = rootElement;

            try
            {
                await this.module.InvokeVoidAsync("registerKeyPrevention", this.rootElement, rules);
                this.isRegistered = true;
            }
            catch
            {
                await this.module.DisposeAsync();
                this.module = null;
                this.rootElement = default;

                throw;
            }
        }

        /// <summary>
        /// Releases only this component's element-scoped keyboard registration.
        /// </summary>
        /// <returns>A value task representing the asynchronous cleanup.</returns>
        public async ValueTask DisposeAsync()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            try
            {
                if (this.module is not null && this.isRegistered)
                {
                    await this.module.InvokeVoidAsync("disposeKeyPrevention", this.rootElement);
                }

                if (this.module is not null)
                {
                    await this.module.DisposeAsync();
                }
            }
            catch (JSDisconnectedException)
            {
                // The circuit is disconnected, so the browser no longer owns a usable registration.
            }
            finally
            {
                this.module = null;
                this.rootElement = default;
                this.isRegistered = false;
            }
        }
    }

    /// <summary>
    /// Defines the keys handled by one target within an element-scoped keyboard registration.
    /// </summary>
    internal sealed class KeyboardDefaultPreventionRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardDefaultPreventionRule" /> class.
        /// </summary>
        /// <param name="selector">The optional selector identifying matching event targets.</param>
        /// <param name="keys">The handled keys whose browser defaults should be prevented.</param>
        internal KeyboardDefaultPreventionRule(string selector, params string[] keys)
        {
            this.Selector = selector;
            this.Keys = keys;
        }

        /// <summary>
        /// Gets the optional selector identifying matching event targets beneath the registration root.
        /// </summary>
        public string Selector { get; }

        /// <summary>
        /// Gets the handled keys whose browser defaults should be prevented.
        /// </summary>
        public IReadOnlyCollection<string> Keys { get; }
    }
}
