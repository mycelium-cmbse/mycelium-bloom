// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaClient.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Hypha
{
    using System.Net.Http.Json;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization.Metadata;

    using AGUI.Abstractions;
    using AGUI.Client;

    using ErrorOr;

    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Talks to the Hypha agent of Fabric: <c>POST hypha/agui</c> streams an answer, <c>GET hypha/threads/{id}</c> reads a
    /// conversation back.
    /// </summary>
    public sealed class HyphaClient : IHyphaClient
    {
        /// <summary>
        /// The path of the AG-UI endpoint, relative to the Fabric address.
        /// </summary>
        public const string RunPath = "hypha/agui";

        /// <summary>
        /// The path of the conversation history, relative to the Fabric address, followed by the thread identifier.
        /// </summary>
        public const string ThreadPath = "hypha/threads/";

        /// <summary>
        /// The message shown when Fabric sends something that is not an AG-UI answer.
        /// </summary>
        private const string UnreadableAnswer = "Fabric sent an answer that Bloom cannot read.";

        /// <summary>
        /// Reads the AG-UI messages of a conversation history, as Fabric writes them.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

        /// <summary>
        /// The HTTP client whose base address is the Fabric host.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// The AG-UI transport that posts a run and reads its event stream.
        /// </summary>
        private readonly IAGUITransport transport;

        /// <summary>
        /// The logger of failed exchanges with Fabric.
        /// </summary>
        private readonly ILogger<HyphaClient> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyphaClient" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client whose base address is the Fabric host.</param>
        /// <param name="logger">The logger of failed exchanges with Fabric.</param>
        public HyphaClient(HttpClient httpClient, ILogger<HyphaClient> logger)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(logger);

            if (httpClient.BaseAddress is null)
            {
                throw new ArgumentException("The HTTP client must have the Fabric address as base address.", nameof(httpClient));
            }

            this.httpClient = httpClient;
            this.logger = logger;
            this.transport = new AGUIChatClientOptions(httpClient, new Uri(httpClient.BaseAddress, RunPath).ToString()).Transport;
        }

        /// <summary>
        /// Sends one question and streams the answer. Failures end the stream with an error step, never an exception;
        /// cancelling the token closes the stream, which makes Fabric stop the answer.
        /// </summary>
        /// <param name="question">The question of the user.</param>
        /// <param name="context">The project, user and selection sent with the question.</param>
        /// <param name="cancellationToken">Stops the answer.</param>
        /// <returns>The steps of the answer, ending with <see cref="HyphaUpdateKind.Finished" /> or an error.</returns>
        public IAsyncEnumerable<HyphaUpdate> AskAsync(string question, HyphaContext context, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(question);
            ArgumentNullException.ThrowIfNull(context);

            var input = new RunAgentInput
            {
                ThreadId = context.ThreadId,
                RunId = Guid.NewGuid().ToString("N"),
                Messages = [new AGUIUserMessage { Id = Guid.NewGuid().ToString("N"), Content = question }],
                Tools = [],
                Context = context.ToAGUIContext()
            };

            return this.StreamAsync(input, cancellationToken);
        }

        /// <summary>
        /// Reads back the conversation Fabric keeps for one thread.
        /// </summary>
        /// <param name="threadId">The conversation identifier.</param>
        /// <param name="cancellationToken">Cancels the request.</param>
        /// <returns>The steps of the conversation, or an error when Fabric cannot be read.</returns>
        public async Task<ErrorOr<IReadOnlyList<HyphaUpdate>>> GetHistoryAsync(string threadId, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(threadId);

            try
            {
                using var response = await this.httpClient.GetAsync(ThreadPath + Uri.EscapeDataString(threadId), cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    this.logger.LogWarning("Fabric answered {StatusCode} to the history of Hypha thread {ThreadId}", (int)response.StatusCode, threadId);
                    return Error.Unexpected("Hypha.History", $"Fabric answered {(int)response.StatusCode} when asked for the conversation.");
                }

                var messages = await response.Content.ReadFromJsonAsync<List<AGUIMessage>>(SerializerOptions, cancellationToken) ?? [];

                return ToUpdates(messages);
            }
            catch (HttpRequestException exception)
            {
                this.logger.LogWarning(exception, "Hypha could not read thread {ThreadId} from Fabric", threadId);
                return Error.Unexpected("Hypha.Unreachable", this.DescribeUnreachable());
            }
            catch (JsonException exception)
            {
                this.logger.LogError(exception, "Hypha could not read the history of thread {ThreadId}", threadId);
                return Error.Unexpected("Hypha.Unreadable", UnreadableAnswer);
            }
        }

        /// <summary>
        /// Converts the AG-UI messages of a history to conversation steps, closing each answer.
        /// </summary>
        /// <param name="messages">The messages of the history, oldest first.</param>
        /// <returns>The steps, oldest first.</returns>
        private static List<HyphaUpdate> ToUpdates(List<AGUIMessage> messages)
        {
            var updates = new List<HyphaUpdate>();
            var toolNames = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var message in messages)
            {
                switch (message)
                {
                    case AGUIUserMessage user:
                        if (updates.Count > 0)
                        {
                            updates.Add(new HyphaUpdate(HyphaUpdateKind.Finished));
                        }

                        updates.Add(new HyphaUpdate(HyphaUpdateKind.Question, user.Content.ToString()));
                        break;
                    case AGUIReasoningMessage reasoning when !string.IsNullOrWhiteSpace(reasoning.Content):
                        updates.Add(new HyphaUpdate(HyphaUpdateKind.Reasoning, reasoning.Content));
                        break;
                    case AGUIAssistantMessage assistant:
                        // The text of a message was written before the tools it calls, as in the live answer.
                        if (!string.IsNullOrEmpty(assistant.Content))
                        {
                            updates.Add(new HyphaUpdate(HyphaUpdateKind.Text, assistant.Content));
                        }

                        foreach (var toolCall in assistant.ToolCalls ?? [])
                        {
                            toolNames[toolCall.Id] = toolCall.Function?.Name;
                            updates.Add(new HyphaUpdate(HyphaUpdateKind.ToolCallStarted, ToolCallId: toolCall.Id, ToolName: toolCall.Function?.Name));
                        }

                        break;
                    case AGUIToolMessage tool:
                        updates.Add(new HyphaUpdate(HyphaUpdateKind.ToolCallFinished, ToolCallId: tool.ToolCallId, ToolName: toolNames.GetValueOrDefault(tool.ToolCallId ?? string.Empty)));
                        break;
                }
            }

            if (updates.Count > 0)
            {
                updates.Add(new HyphaUpdate(HyphaUpdateKind.Finished));
            }

            return updates;
        }

        /// <summary>
        /// Converts one AG-UI event to a conversation step.
        /// </summary>
        /// <param name="runEvent">The event read from the stream.</param>
        /// <param name="toolNames">The names of the tool calls started in this run, by call identifier.</param>
        /// <returns>The step, or <see langword="null" /> for an event the panel does not show.</returns>
        private static HyphaUpdate ToUpdate(BaseEvent runEvent, Dictionary<string, string> toolNames)
        {
            switch (runEvent)
            {
                case TextMessageContentEvent content when !string.IsNullOrEmpty(content.Delta):
                    return new HyphaUpdate(HyphaUpdateKind.Text, content.Delta);
                case ReasoningMessageContentEvent reasoning when !string.IsNullOrEmpty(reasoning.Delta):
                    return new HyphaUpdate(HyphaUpdateKind.Reasoning, reasoning.Delta);
                case ReasoningMessageChunkEvent chunk when !string.IsNullOrEmpty(chunk.Delta):
                    return new HyphaUpdate(HyphaUpdateKind.Reasoning, chunk.Delta);
                case ToolCallStartEvent start:
                    toolNames[start.ToolCallId] = start.ToolCallName;
                    return new HyphaUpdate(HyphaUpdateKind.ToolCallStarted, ToolCallId: start.ToolCallId, ToolName: start.ToolCallName);
                case ToolCallResultEvent result:
                    return new HyphaUpdate(HyphaUpdateKind.ToolCallFinished, ToolCallId: result.ToolCallId, ToolName: toolNames.GetValueOrDefault(result.ToolCallId ?? string.Empty));
                case RunErrorEvent error:
                    return HyphaUpdate.Error(string.IsNullOrWhiteSpace(error.Message) ? "Hypha could not answer." : error.Message);
                case RunFinishedEvent:
                    return new HyphaUpdate(HyphaUpdateKind.Finished);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates the serializer options that read the AG-UI messages of a history.
        /// </summary>
        /// <returns>The read-only serializer options.</returns>
        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.TypeInfoResolverChain.Add(AGUIJsonUtilities.DefaultTypeInfoResolver);
            options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
            options.MakeReadOnly();

            return options;
        }

        /// <summary>
        /// Posts the run and converts its event stream until the answer ends, fails or is cancelled.
        /// </summary>
        /// <param name="input">The AG-UI run to post.</param>
        /// <param name="cancellationToken">Stops the answer.</param>
        /// <returns>The steps of the answer.</returns>
        private async IAsyncEnumerable<HyphaUpdate> StreamAsync(RunAgentInput input, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var toolNames = new Dictionary<string, string>(StringComparer.Ordinal);
            await using var events = this.transport.SendAsync(input, cancellationToken).GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                var (hasEvent, failure) = await this.TryMoveNextAsync(events, input.ThreadId, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (failure is not null)
                {
                    yield return failure;
                    yield break;
                }

                if (!hasEvent)
                {
                    yield return new HyphaUpdate(HyphaUpdateKind.Finished);
                    yield break;
                }

                var update = ToUpdate(events.Current, toolNames);

                if (update is not null)
                {
                    yield return update;

                    if (update.Kind is HyphaUpdateKind.Finished or HyphaUpdateKind.Error)
                    {
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Reads the next event and turns a failed exchange into an error step.
        /// </summary>
        /// <param name="events">The event stream of the run.</param>
        /// <param name="threadId">The conversation identifier, for the log.</param>
        /// <param name="cancellationToken">Stops the answer.</param>
        /// <returns>Whether an event was read, and the error step when the exchange failed.</returns>
        private async Task<(bool HasEvent, HyphaUpdate Failure)> TryMoveNextAsync(IAsyncEnumerator<BaseEvent> events, string threadId, CancellationToken cancellationToken)
        {
            try
            {
                return (await events.MoveNextAsync(), null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return (false, null);
            }
            catch (HttpRequestException exception)
            {
                this.logger.LogWarning(exception, "Hypha could not reach Fabric for thread {ThreadId}", threadId);
                return (false, HyphaUpdate.Error(this.DescribeUnreachable()));
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or IOException)
            {
                this.logger.LogError(exception, "Hypha could not read the answer of Fabric for thread {ThreadId}", threadId);
                return (false, HyphaUpdate.Error(UnreadableAnswer));
            }
        }

        /// <summary>
        /// Describes an unreachable Fabric host to the user.
        /// </summary>
        /// <returns>The message shown in the panel.</returns>
        private string DescribeUnreachable() => $"Hypha cannot reach Fabric at {this.httpClient.BaseAddress}. Check that the Fabric host runs.";
    }
}
