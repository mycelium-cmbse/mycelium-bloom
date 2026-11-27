// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaPanel.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.HyphaPanel
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.Hypha;

    /// <summary>
    /// Shows the conversation with Hypha, the assistant of Fabric, in the right panel of the workspace.
    /// </summary>
    public sealed partial class HyphaPanel : BloomReactiveComponentBase<IHyphaPanelViewModel>
    {
        /// <summary>
        /// The stable identifier of the panel heading.
        /// </summary>
        private readonly string headingId = $"mb-hypha-panel-title-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets the composition-owned close action.
        /// </summary>
        [Parameter]
        public EventCallback CloseRequested { get; set; }

        /// <summary>
        /// Gets or sets the conversation of the circuit, used when no ViewModel is supplied; the circuit owns its lifetime.
        /// </summary>
        [Inject]
        private IHyphaPanelViewModel CircuitViewModel { get; set; }

        /// <summary>
        /// Describes a reasoning row in one line, shown while its text stays folded.
        /// </summary>
        /// <param name="item">The reasoning row.</param>
        /// <returns>"Thinking…" while the model reasons, "Thought for 6 seconds" once it is done, "Thought" when read back.</returns>
        private static string GetReasoningLabel(HyphaChatItem item)
        {
            if (!item.IsDone)
            {
                return "Thinking…";
            }

            if (item.Duration is not { } duration)
            {
                return "Thought";
            }

            var seconds = (int)Math.Max(1, Math.Round(duration.TotalSeconds));

            return $"Thought for {"second".ToQuantity(seconds)}";
        }

        /// <summary>Uses the conversation of the circuit unless a caller supplies one.</summary>
        protected override void OnInitialized()
        {
            this.ViewModel ??= this.CircuitViewModel
                               ?? throw new InvalidOperationException("No Hypha conversation is registered for this circuit.");

            base.OnInitialized();
        }

        /// <summary>Reads the conversation back from Fabric the first time the panel opens.</summary>
        /// <returns>The read operation.</returns>
        protected override Task OnInitializedAsync()
        {
            return this.ViewModel.OpenAsync();
        }

        /// <summary>Sends the typed question, or queues it while Hypha is answering.</summary>
        /// <returns>The answer.</returns>
        private Task SendAsync()
        {
            return this.ViewModel.SendAsync();
        }

        /// <summary>Sends the question on Enter; a key that composes a character (an input method) sends nothing.</summary>
        /// <param name="arguments">The key pressed in the question field.</param>
        /// <returns>The answer, or a completed task for other keys.</returns>
        private Task HandleKeyDownAsync(KeyboardEventArgs arguments)
        {
            return arguments.Key == "Enter" && !arguments.IsComposing ? this.ViewModel.SendAsync() : Task.CompletedTask;
        }

        /// <summary>Stops the current answer and sends the queued question.</summary>
        private void SendNow()
        {
            this.ViewModel.SendNow();
        }

        /// <summary>Stops the current answer.</summary>
        private void Stop()
        {
            this.ViewModel.Stop();
        }

        /// <summary>Captures the header close event before delegating workspace visibility.</summary>
        /// <returns>The composition callback completion.</returns>
        private Task ClosePanel()
        {
            return this.CloseRequested.InvokeAsync();
        }

        /// <summary>
        /// Gets the final CSS class list applied to the panel.
        /// </summary>
        /// <returns>The panel CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-hypha-panel");
        }
    }
}
