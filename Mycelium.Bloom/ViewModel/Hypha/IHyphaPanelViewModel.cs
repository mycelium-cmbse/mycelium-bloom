// ------------------------------------------------------------------------------------------------
// <copyright file="IHyphaPanelViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Hypha
{
    using ReactiveUI;

    /// <summary>
    /// Owns the Hypha conversation of one circuit: it outlives the panel, so an answer keeps streaming while
    /// Properties is shown.
    /// </summary>
    public interface IHyphaPanelViewModel : IReactiveObject, IDisposable
    {
        /// <summary>Gets the rows of the conversation, oldest first.</summary>
        IReadOnlyList<HyphaChatItem> Items { get; }

        /// <summary>Gets or sets the question being typed.</summary>
        string Draft { get; set; }

        /// <summary>Gets a value indicating whether Hypha is answering.</summary>
        bool IsAnswering { get; }

        /// <summary>Gets a value indicating whether Hypha is answering but has shown nothing new yet (the dots).</summary>
        bool IsThinking { get; }

        /// <summary>Gets the question waiting for the current answer to end, or <see langword="null" />.</summary>
        string QueuedQuestion { get; }

        /// <summary>Gets a value indicating whether the conversation is being read back from Fabric.</summary>
        bool IsLoadingHistory { get; }

        /// <summary>Reads the conversation of the current user and project back from Fabric, once per conversation.</summary>
        /// <returns>The read operation.</returns>
        Task OpenAsync();

        /// <summary>Sends the draft, or queues it while Hypha is answering.</summary>
        /// <returns>The answer, and the queued answers after it.</returns>
        Task SendAsync();

        /// <summary>Stops the current answer and sends the queued question at once.</summary>
        void SendNow();

        /// <summary>Stops the current answer; a queued question goes back to the draft.</summary>
        void Stop();
    }
}
