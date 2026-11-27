// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaPanelViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Hypha
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.WebUtilities;

    using Mycelium.Bloom.Core.Hypha;
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    /// <summary>
    /// Owns the Hypha conversation of one circuit: questions, streamed answers, tool calls, the queued question and
    /// failures.
    /// </summary>
    public sealed class HyphaPanelViewModel : ReactiveObject, IHyphaPanelViewModel
    {
        /// <summary>
        /// The query parameter of the open project in the workspace address.
        /// </summary>
        public const string ProjectParameterName = "project";

        /// <summary>
        /// The project sent while the workspace shows the sample model of Bloom.
        /// </summary>
        public const string SampleProjectId = "sample";

        /// <summary>The client of the Fabric agent.</summary>
        private readonly IHyphaClient client;

        /// <summary>The selected-element authority of the workspace.</summary>
        private readonly IElementSelectionService selection;

        /// <summary>The source of the current user until Bloom signs users in.</summary>
        private readonly IMockedDataService mockedData;

        /// <summary>The address of the workspace, which carries the open project.</summary>
        private readonly NavigationManager navigation;

        /// <summary>The clock that times the reasoning of the model.</summary>
        private readonly TimeProvider timeProvider;

        /// <summary>Cancels every exchange with Fabric when the circuit ends.</summary>
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>The rows of the conversation.</summary>
        private IReadOnlyList<HyphaChatItem> items = [];

        /// <summary>The question being typed.</summary>
        private string draft = string.Empty;

        /// <summary>Whether Hypha is answering.</summary>
        private bool isAnswering;

        /// <summary>Whether Hypha is answering but has shown nothing new yet.</summary>
        private bool isThinking;

        /// <summary>The question waiting for the current answer.</summary>
        private string queuedQuestion;

        /// <summary>Whether the conversation is being read back.</summary>
        private bool isLoadingHistory;

        /// <summary>The index of the answer row that receives text, or -1 when the next text starts a new row.</summary>
        private int currentAnswerIndex = -1;

        /// <summary>The index of the reasoning row that receives reasoning, or -1 when the next reasoning starts a new row.</summary>
        private int currentReasoningIndex = -1;

        /// <summary>The timestamp of the start of the current reasoning, from <see cref="timeProvider" />.</summary>
        private long reasoningStart;

        /// <summary>Stops the current answer.</summary>
        private CancellationTokenSource answerCancellation;

        /// <summary>The conversation already read back, so that reopening the panel reads nothing.</summary>
        private string loadedThreadId;

        /// <summary>The read of the conversation in progress, awaited by a question sent meanwhile.</summary>
        private Task historyLoad = Task.CompletedTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyphaPanelViewModel" /> class.
        /// </summary>
        /// <param name="client">The client of the Fabric agent.</param>
        /// <param name="selection">The selected-element authority of the workspace.</param>
        /// <param name="mockedData">The source of the current user.</param>
        /// <param name="navigation">The address of the workspace.</param>
        /// <param name="timeProvider">The clock that times the reasoning of the model.</param>
        public HyphaPanelViewModel(IHyphaClient client, IElementSelectionService selection, IMockedDataService mockedData, NavigationManager navigation, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(selection);
            ArgumentNullException.ThrowIfNull(mockedData);
            ArgumentNullException.ThrowIfNull(navigation);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.client = client;
            this.selection = selection;
            this.mockedData = mockedData;
            this.navigation = navigation;
            this.timeProvider = timeProvider;
        }

        /// <inheritdoc />
        public IReadOnlyList<HyphaChatItem> Items
        {
            get => this.items;
            private set => this.RaiseAndSetIfChanged(ref this.items, value);
        }

        /// <inheritdoc />
        public string Draft
        {
            get => this.draft;
            set => this.RaiseAndSetIfChanged(ref this.draft, value ?? string.Empty);
        }

        /// <inheritdoc />
        public bool IsAnswering
        {
            get => this.isAnswering;
            private set => this.RaiseAndSetIfChanged(ref this.isAnswering, value);
        }

        /// <inheritdoc />
        public bool IsThinking
        {
            get => this.isThinking;
            private set => this.RaiseAndSetIfChanged(ref this.isThinking, value);
        }

        /// <inheritdoc />
        public string QueuedQuestion
        {
            get => this.queuedQuestion;
            private set => this.RaiseAndSetIfChanged(ref this.queuedQuestion, value);
        }

        /// <inheritdoc />
        public bool IsLoadingHistory
        {
            get => this.isLoadingHistory;
            private set => this.RaiseAndSetIfChanged(ref this.isLoadingHistory, value);
        }

        /// <inheritdoc />
        public Task OpenAsync()
        {
            var threadId = this.CreateContext().ThreadId;

            if (!string.Equals(threadId, this.loadedThreadId, StringComparison.Ordinal))
            {
                this.loadedThreadId = threadId;
                this.historyLoad = this.LoadHistoryAsync(threadId);
            }

            return this.historyLoad;
        }

        /// <inheritdoc />
        public async Task SendAsync()
        {
            var question = this.Draft.Trim();

            if (question.Length == 0)
            {
                return;
            }

            this.Draft = string.Empty;

            if (this.IsAnswering)
            {
                this.QueuedQuestion = this.QueuedQuestion is null ? question : $"{this.QueuedQuestion}\n{question}";
                return;
            }

            // Answering from now on: a question sent while the history is still read waits in the queue.
            this.IsAnswering = true;

            try
            {
                await this.historyLoad;
                await this.AnswerAsync(question);
            }
            finally
            {
                this.IsAnswering = false;
            }
        }

        /// <inheritdoc />
        public void SendNow()
        {
            if (this.QueuedQuestion is not null)
            {
                this.answerCancellation?.Cancel();
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (this.QueuedQuestion is not null)
            {
                this.Draft = this.Draft.Length == 0 ? this.QueuedQuestion : $"{this.QueuedQuestion}\n{this.Draft}";
                this.QueuedQuestion = null;
            }

            this.answerCancellation?.Cancel();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.lifetime.Cancel();
            this.lifetime.Dispose();
        }

        /// <summary>
        /// Describes a Fabric tool to the user.
        /// </summary>
        /// <param name="toolName">The name of the tool, such as <c>sum_attribute</c>.</param>
        /// <returns>A readable name, such as "Sum attribute".</returns>
        private static string GetReadableToolName(string toolName)
        {
            return string.IsNullOrWhiteSpace(toolName) ? "Fabric tool" : toolName.Humanize(LetterCasing.Sentence);
        }

        /// <summary>
        /// Sends one question, applies its answer, then sends the question queued meanwhile.
        /// </summary>
        /// <param name="question">The question of the user.</param>
        /// <returns>The answer and the queued answers after it.</returns>
        private async Task AnswerAsync(string question)
        {
            var nextQuestion = question;

            while (nextQuestion is not null && !this.lifetime.IsCancellationRequested)
            {
                var context = this.CreateContext();
                this.Add(new HyphaChatItem(HyphaChatItemKind.Question, nextQuestion));
                this.currentAnswerIndex = -1;
                this.IsAnswering = true;
                this.IsThinking = true;

                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetime.Token);
                this.answerCancellation = cancellation;

                try
                {
                    await foreach (var update in this.client.AskAsync(nextQuestion, context, cancellation.Token))
                    {
                        this.Apply(update, isLive: true);
                    }
                }
                finally
                {
                    this.answerCancellation = null;
                    this.CloseAnswer();
                    this.IsThinking = false;
                    this.IsAnswering = false;
                }

                nextQuestion = this.QueuedQuestion;
                this.QueuedQuestion = null;
            }
        }

        /// <summary>
        /// Reads a conversation back from Fabric and shows it in place of the current rows.
        /// </summary>
        /// <param name="threadId">The conversation identifier.</param>
        /// <returns>The read operation.</returns>
        private async Task LoadHistoryAsync(string threadId)
        {
            this.IsLoadingHistory = true;

            try
            {
                var history = await this.client.GetHistoryAsync(threadId, this.lifetime.Token);

                if (history.IsError)
                {
                    this.loadedThreadId = null;
                    this.Add(new HyphaChatItem(HyphaChatItemKind.Error, history.FirstError.Description));
                    return;
                }

                this.Items = [];
                this.currentAnswerIndex = -1;
                this.currentReasoningIndex = -1;

                foreach (var update in history.Value)
                {
                    this.Apply(update, isLive: false);
                }
            }
            catch (OperationCanceledException) when (this.lifetime.IsCancellationRequested)
            {
                // The circuit ended while the conversation was being read.
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !this.lifetime.IsCancellationRequested)
            {
                // A failed read must not block the questions that wait for it.
                this.loadedThreadId = null;
                this.Add(new HyphaChatItem(HyphaChatItemKind.Error, "Hypha could not read the conversation back from Fabric."));
            }
            finally
            {
                this.IsLoadingHistory = false;
            }
        }

        /// <summary>
        /// Applies one step of a conversation to the rows.
        /// </summary>
        /// <param name="update">The step, live or read back.</param>
        /// <param name="isLive">Whether the step comes from the answer being written, so that its reasoning is timed.</param>
        private void Apply(HyphaUpdate update, bool isLive)
        {
            switch (update.Kind)
            {
                case HyphaUpdateKind.Question:
                    this.CloseAnswer();
                    this.Add(new HyphaChatItem(HyphaChatItemKind.Question, update.Text));
                    break;
                case HyphaUpdateKind.Reasoning:
                    this.AppendReasoning(update.Text, isLive);
                    this.IsThinking = false;
                    break;
                case HyphaUpdateKind.Text:
                    this.CloseReasoning();
                    this.AppendAnswer(update.Text);
                    this.IsThinking = false;
                    break;
                case HyphaUpdateKind.ToolCallStarted:
                    this.CloseReasoning();
                    this.currentAnswerIndex = -1;
                    this.Add(new HyphaChatItem(HyphaChatItemKind.ToolCall, GetReadableToolName(update.ToolName), update.ToolCallId));
                    this.IsThinking = false;
                    break;
                case HyphaUpdateKind.ToolCallFinished:
                    this.Items = [.. this.Items.Select(item => item.Kind == HyphaChatItemKind.ToolCall && item.ToolCallId == update.ToolCallId ? item with { IsDone = true } : item)];
                    this.IsThinking = this.IsAnswering;
                    break;
                case HyphaUpdateKind.Finished:
                    this.CloseAnswer();
                    break;
                case HyphaUpdateKind.Error:
                    this.CloseAnswer();
                    this.Add(new HyphaChatItem(HyphaChatItemKind.Error, update.Text));
                    break;
            }
        }

        /// <summary>
        /// Appends answer text to the current answer row, or starts one.
        /// </summary>
        /// <param name="text">The text to append.</param>
        private void AppendAnswer(string text)
        {
            if (this.currentAnswerIndex < 0)
            {
                this.Add(new HyphaChatItem(HyphaChatItemKind.Answer, text));
                this.currentAnswerIndex = this.Items.Count - 1;
                return;
            }

            var rows = this.Items.ToArray();
            rows[this.currentAnswerIndex] = rows[this.currentAnswerIndex] with { Text = rows[this.currentAnswerIndex].Text + text };
            this.Items = rows;
        }

        /// <summary>
        /// Appends reasoning to the current reasoning row, or starts one below the rows already shown.
        /// </summary>
        /// <param name="text">The reasoning to append.</param>
        /// <param name="isLive">Whether the reasoning is being written, so that it stays open and is timed.</param>
        private void AppendReasoning(string text, bool isLive)
        {
            if (this.currentReasoningIndex < 0)
            {
                // Text written after this reasoning starts a new answer row below it.
                this.currentAnswerIndex = -1;
                this.reasoningStart = this.timeProvider.GetTimestamp();
                this.Add(new HyphaChatItem(HyphaChatItemKind.Reasoning, text, IsDone: !isLive));
                this.currentReasoningIndex = this.Items.Count - 1;
                return;
            }

            var rows = this.Items.ToArray();
            rows[this.currentReasoningIndex] = rows[this.currentReasoningIndex] with { Text = rows[this.currentReasoningIndex].Text + text };
            this.Items = rows;
        }

        /// <summary>
        /// Ends the current reasoning: a live one is marked as ended with its duration.
        /// </summary>
        private void CloseReasoning()
        {
            if (this.currentReasoningIndex < 0)
            {
                return;
            }

            var index = this.currentReasoningIndex;
            this.currentReasoningIndex = -1;

            if (!this.Items[index].IsDone)
            {
                var rows = this.Items.ToArray();
                rows[index] = rows[index] with { IsDone = true, Duration = this.timeProvider.GetElapsedTime(this.reasoningStart) };
                this.Items = rows;
            }
        }

        /// <summary>
        /// Ends the current answer: the next text starts a new row, and the reasoning and tool calls still open are shown as ended.
        /// </summary>
        private void CloseAnswer()
        {
            this.CloseReasoning();
            this.currentAnswerIndex = -1;

            if (this.Items.Any(item => item.Kind == HyphaChatItemKind.ToolCall && !item.IsDone))
            {
                this.Items = [.. this.Items.Select(item => item.Kind == HyphaChatItemKind.ToolCall ? item with { IsDone = true } : item)];
            }
        }

        /// <summary>
        /// Adds one row at the end of the conversation.
        /// </summary>
        /// <param name="item">The row to add.</param>
        private void Add(HyphaChatItem item)
        {
            this.Items = [.. this.Items, item];
        }

        /// <summary>
        /// Describes the project, the user and the selection sent with a question.
        /// </summary>
        /// <returns>The context of the next question.</returns>
        private HyphaContext CreateContext()
        {
            var query = QueryHelpers.ParseQuery(this.navigation.ToAbsoluteUri(this.navigation.Uri).Query);
            var projectId = query.TryGetValue(ProjectParameterName, out var project) && !string.IsNullOrWhiteSpace(project) ? project.ToString() : SampleProjectId;
            var user = this.mockedData.CurrentUser;
            var element = this.selection.SelectedElement;
            var selected = element is null ? null : $"{ElementPresentation.GetDisplayName(element)} ({ElementPresentation.GetKindName(element.GetType())}, id {element.Id})";

            return new HyphaContext(projectId, user.Id.ToString(), user.Name, selected);
        }
    }
}
