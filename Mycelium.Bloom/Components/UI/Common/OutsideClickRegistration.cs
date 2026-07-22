// ------------------------------------------------------------------------------------------------
// <copyright file="OutsideClickRegistration.cs" company="Starion Group S.A.">
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
    /// Owns one instance-specific outside-click registration for a popup component.
    /// </summary>
    /// <typeparam name="TComponent">The component receiving the dismissal callback.</typeparam>
    internal sealed class OutsideClickRegistration<TComponent> : IAsyncDisposable
        where TComponent : class
    {
        /// <summary>
        /// The stable identifier that prevents one component from unregistering another.
        /// </summary>
        private readonly string registrationId = $"mb-outside-click-{Guid.NewGuid():N}";

        /// <summary>
        /// The JavaScript runtime used to import the shared helper.
        /// </summary>
        private readonly IJSRuntime jsRuntime;

        /// <summary>
        /// The imported JavaScript module.
        /// </summary>
        private IJSObjectReference module;

        /// <summary>
        /// The component reference invoked for outside pointer interaction.
        /// </summary>
        private DotNetObjectReference<TComponent> componentReference;

        /// <summary>
        /// A value indicating whether the browser registration is active.
        /// </summary>
        private bool isRegistered;

        /// <summary>
        /// A value indicating whether this registration has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutsideClickRegistration{TComponent}" /> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime used to import the helper.</param>
        internal OutsideClickRegistration(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Registers the component root with the shared outside-click helper.
        /// </summary>
        /// <param name="rootElement">The root containing the trigger and popup surface.</param>
        /// <param name="component">The component receiving dismissal callbacks.</param>
        /// <returns>A task representing the asynchronous registration.</returns>
        internal async Task RegisterAsync(ElementReference rootElement, TComponent component)
        {
            if (this.isDisposed || this.isRegistered)
            {
                return;
            }

            this.module = await this.jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/outside-click.js");
            this.componentReference = DotNetObjectReference.Create(component);

            try
            {
                await this.module.InvokeVoidAsync(
                    "registerOutsideClick",
                    this.registrationId,
                    rootElement,
                    this.componentReference);

                this.isRegistered = true;
            }
            catch
            {
                this.componentReference.Dispose();
                this.componentReference = null;
                await this.module.DisposeAsync();
                this.module = null;

                throw;
            }
        }

        /// <summary>
        /// Releases only this component's browser registration and managed callback reference.
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
                    await this.module.InvokeVoidAsync("disposeOutsideClick", this.registrationId);
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
                this.componentReference?.Dispose();
                this.componentReference = null;
                this.module = null;
                this.isRegistered = false;
            }
        }
    }
}
