// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaClientTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Hypha
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using AGUI.Abstractions;
    using AGUI.Formatting;

    using Microsoft.Extensions.Logging.Abstractions;

    using Mycelium.Bloom.Core.Hypha;
    using Mycelium.Bloom.Model.Enum;

    [TestFixture]
    public sealed class HyphaClientTestFixture
    {
        private static readonly Uri FabricAddress = new("http://fabric.test/");

        private static readonly HyphaContext Context = new("sample", "user-1", "Demo Account", "aocs (PartUsage, id 42)");

        [Test]
        public async Task VerifyAskPostsOneQuestionWithThreadAndContext()
        {
            var handler = new StubHandler(_ => CreateEventStream(new RunFinishedEvent { ThreadId = Context.ThreadId, RunId = "run" }));
            var client = CreateClient(handler);

            await ReadAllAsync(client.AskAsync("Give me the mass", Context, CancellationToken.None));

            using var body = JsonDocument.Parse(handler.Bodies.Single());
            var root = body.RootElement;
            var messages = root.GetProperty("messages").EnumerateArray().ToList();
            var context = root.GetProperty("context").EnumerateArray()
                .ToDictionary(entry => entry.GetProperty("description").GetString(), entry => entry.GetProperty("value").GetString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(handler.Requests.Single().RequestUri, Is.EqualTo(new Uri(FabricAddress, "hypha/agui")));
                Assert.That(root.GetProperty("threadId").GetString(), Is.EqualTo("user-1:sample"));
                Assert.That(root.GetProperty("runId").GetString(), Is.Not.Empty);
                Assert.That(messages, Has.Count.EqualTo(1));
                Assert.That(messages[0].GetProperty("role").GetString(), Is.EqualTo("user"));
                Assert.That(messages[0].GetProperty("content").GetString(), Is.EqualTo("Give me the mass"));
                Assert.That(context["Project"], Is.EqualTo("sample"));
                Assert.That(context["User"], Is.EqualTo("Demo Account (id user-1)"));
                Assert.That(context["Selected element"], Is.EqualTo("aocs (PartUsage, id 42)"));
            }
        }

        [Test]
        public async Task VerifyAskStreamsToolCallsAndTextUntilTheEnd()
        {
            var handler = new StubHandler(_ => CreateEventStream(
                new RunStartedEvent { ThreadId = Context.ThreadId, RunId = "run" },
                new ToolCallStartEvent { ToolCallId = "call-1", ToolCallName = "sum_attribute" },
                new ToolCallArgsEvent { ToolCallId = "call-1", Delta = "{}" },
                new ToolCallEndEvent { ToolCallId = "call-1" },
                new ToolCallResultEvent { MessageId = "tool-1", ToolCallId = "call-1", Content = "125.8 kg" },
                new TextMessageStartEvent { MessageId = "answer", Role = "assistant" },
                new TextMessageContentEvent { MessageId = "answer", Delta = "The dry mass " },
                new TextMessageContentEvent { MessageId = "answer", Delta = "is **125.8 kg**." },
                new TextMessageEndEvent { MessageId = "answer" },
                new RunFinishedEvent { ThreadId = Context.ThreadId, RunId = "run" }));

            var updates = await ReadAllAsync(CreateClient(handler).AskAsync("Give me the mass", Context, CancellationToken.None));

            Assert.That(updates, Is.EqualTo(new[]
            {
                new HyphaUpdate(HyphaUpdateKind.ToolCallStarted, ToolCallId: "call-1", ToolName: "sum_attribute"),
                new HyphaUpdate(HyphaUpdateKind.ToolCallFinished, ToolCallId: "call-1", ToolName: "sum_attribute"),
                new HyphaUpdate(HyphaUpdateKind.Text, "The dry mass "),
                new HyphaUpdate(HyphaUpdateKind.Text, "is **125.8 kg**."),
                new HyphaUpdate(HyphaUpdateKind.Finished)
            }));
        }

        [Test]
        public async Task VerifyAskStreamsTheReasoningApartFromTheAnswer()
        {
            var handler = new StubHandler(_ => CreateEventStream(
                new RunStartedEvent { ThreadId = Context.ThreadId, RunId = "run" },
                new ReasoningStartEvent { MessageId = "reasoning" },
                new ReasoningMessageContentEvent { MessageId = "reasoning", Delta = "The user wants " },
                new ReasoningMessageContentEvent { MessageId = "reasoning", Delta = "the dry mass." },
                new ReasoningEndEvent { MessageId = "reasoning" },
                new TextMessageContentEvent { MessageId = "answer", Delta = "125.8 kg." },
                new RunFinishedEvent { ThreadId = Context.ThreadId, RunId = "run" }));

            var updates = await ReadAllAsync(CreateClient(handler).AskAsync("Give me the mass", Context, CancellationToken.None));

            Assert.That(updates, Is.EqualTo(new[]
            {
                new HyphaUpdate(HyphaUpdateKind.Reasoning, "The user wants "),
                new HyphaUpdate(HyphaUpdateKind.Reasoning, "the dry mass."),
                new HyphaUpdate(HyphaUpdateKind.Text, "125.8 kg."),
                new HyphaUpdate(HyphaUpdateKind.Finished)
            }));
        }

        [Test]
        public async Task VerifyAskEndsWithTheErrorThatFabricReports()
        {
            var handler = new StubHandler(_ => CreateEventStream(
                new RunStartedEvent { ThreadId = Context.ThreadId, RunId = "run" },
                new RunErrorEvent { Code = "llm_unreachable", Message = "The LLM cannot be reached: connection refused" }));

            var updates = await ReadAllAsync(CreateClient(handler).AskAsync("Give me the mass", Context, CancellationToken.None));

            Assert.That(updates, Is.EqualTo(new[] { HyphaUpdate.Error("The LLM cannot be reached: connection refused") }));
        }

        [Test]
        public async Task VerifyAskEndsWithAnErrorWhenFabricCannotBeReached()
        {
            var handler = new StubHandler(_ => throw new HttpRequestException("Connection refused"));

            var updates = await ReadAllAsync(CreateClient(handler).AskAsync("Give me the mass", Context, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updates, Has.Count.EqualTo(1));
                Assert.That(updates[0].Kind, Is.EqualTo(HyphaUpdateKind.Error));
                Assert.That(updates[0].Text, Does.Contain("cannot reach Fabric at http://fabric.test/"));
            }
        }

        [Test]
        public async Task VerifyAskEndsWithAnErrorWhenFabricRefusesTheQuestion()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("The run has no threadId.") });

            var updates = await ReadAllAsync(CreateClient(handler).AskAsync("Give me the mass", Context, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updates, Has.Count.EqualTo(1));
                Assert.That(updates[0].Kind, Is.EqualTo(HyphaUpdateKind.Error));
            }
        }

        [Test]
        public async Task VerifyAskEndsQuietlyWhenTheUserStopsTheAnswer()
        {
            using var cancellation = new CancellationTokenSource();
            var handler = new StubHandler(_ => CreateEventStream(
                new TextMessageContentEvent { MessageId = "answer", Delta = "The dry mass" },
                new TextMessageContentEvent { MessageId = "answer", Delta = " is" },
                new RunFinishedEvent { ThreadId = Context.ThreadId, RunId = "run" }));
            var updates = new List<HyphaUpdate>();

            await foreach (var update in CreateClient(handler).AskAsync("Give me the mass", Context, cancellation.Token))
            {
                updates.Add(update);
                await cancellation.CancelAsync();
            }

            Assert.That(updates, Is.EqualTo(new[] { new HyphaUpdate(HyphaUpdateKind.Text, "The dry mass") }));
        }

        [Test]
        public void VerifyAskRejectsAnEmptyQuestion()
        {
            var client = CreateClient(new StubHandler(_ => CreateEventStream()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => client.AskAsync(" ", Context, CancellationToken.None), Throws.ArgumentException);
                Assert.That(() => client.AskAsync("Give me the mass", null, CancellationToken.None), Throws.ArgumentNullException);
            }
        }

        [Test]
        public void VerifyClientNeedsTheFabricAddress()
        {
            using var httpClient = new HttpClient(new StubHandler(_ => CreateEventStream()));

            Assert.That(() => new HyphaClient(httpClient, NullLogger<HyphaClient>.Instance), Throws.ArgumentException);
        }

        [Test]
        public async Task VerifyHistoryReplaysTheConversationOfTheThread()
        {
            const string history = """
                [
                  { "id": "1", "role": "user", "content": "Give me the mass" },
                  { "id": "2", "role": "assistant", "content": "Let me sum it.", "toolCalls": [ { "id": "call-1", "type": "function", "function": { "name": "sum_attribute", "arguments": "{}" } } ] },
                  { "id": "3", "role": "tool", "toolCallId": "call-1", "content": "125.8 kg" },
                  { "id": "4", "role": "assistant", "content": "The dry mass is 125.8 kg." },
                  { "id": "5", "role": "user", "content": "And the power?" },
                  { "id": "6", "role": "assistant", "content": "227 W." }
                ]
                """;
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(history, Encoding.UTF8, "application/json") });

            var result = await CreateClient(handler).GetHistoryAsync("user-1:sample", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handler.Requests.Single().RequestUri, Is.EqualTo(new Uri(FabricAddress, "hypha/threads/user-1%3Asample")));
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.EqualTo(new[]
                {
                    new HyphaUpdate(HyphaUpdateKind.Question, "Give me the mass"),
                    new HyphaUpdate(HyphaUpdateKind.Text, "Let me sum it."),
                    new HyphaUpdate(HyphaUpdateKind.ToolCallStarted, ToolCallId: "call-1", ToolName: "sum_attribute"),
                    new HyphaUpdate(HyphaUpdateKind.ToolCallFinished, ToolCallId: "call-1", ToolName: "sum_attribute"),
                    new HyphaUpdate(HyphaUpdateKind.Text, "The dry mass is 125.8 kg."),
                    new HyphaUpdate(HyphaUpdateKind.Finished),
                    new HyphaUpdate(HyphaUpdateKind.Question, "And the power?"),
                    new HyphaUpdate(HyphaUpdateKind.Text, "227 W."),
                    new HyphaUpdate(HyphaUpdateKind.Finished)
                }));
            }
        }

        [Test]
        public async Task VerifyHistoryKeepsTheReasoningBeforeItsAnswer()
        {
            const string history = """
                [
                  { "id": "1", "role": "user", "content": "Give me the mass" },
                  { "id": "2", "role": "reasoning", "content": "The user wants the dry mass." },
                  { "id": "3", "role": "assistant", "content": "125.8 kg." }
                ]
                """;
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(history, Encoding.UTF8, "application/json") });

            var result = await CreateClient(handler).GetHistoryAsync("user-1:sample", CancellationToken.None);

            Assert.That(result.Value, Is.EqualTo(new[]
            {
                new HyphaUpdate(HyphaUpdateKind.Question, "Give me the mass"),
                new HyphaUpdate(HyphaUpdateKind.Reasoning, "The user wants the dry mass."),
                new HyphaUpdate(HyphaUpdateKind.Text, "125.8 kg."),
                new HyphaUpdate(HyphaUpdateKind.Finished)
            }));
        }

        [Test]
        public async Task VerifyHistoryOfANewThreadIsEmpty()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") });

            var result = await CreateClient(handler).GetHistoryAsync("user-1:sample", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.Empty);
            }
        }

        [Test]
        public async Task VerifyHistoryReportsAnErrorWhenFabricCannotBeRead()
        {
            var unreachable = new StubHandler(_ => throw new HttpRequestException("Connection refused"));
            var failing = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var unreadable = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not json", Encoding.UTF8, "application/json") });

            var unreachableResult = await CreateClient(unreachable).GetHistoryAsync("user-1:sample", CancellationToken.None);
            var failingResult = await CreateClient(failing).GetHistoryAsync("user-1:sample", CancellationToken.None);
            var unreadableResult = await CreateClient(unreadable).GetHistoryAsync("user-1:sample", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unreachableResult.FirstError.Description, Does.Contain("cannot reach Fabric"));
                Assert.That(failingResult.FirstError.Description, Does.Contain("500"));
                Assert.That(unreadableResult.FirstError.Description, Does.Contain("cannot read"));
            }
        }

        private static HyphaClient CreateClient(StubHandler handler)
        {
            return new HyphaClient(new HttpClient(handler) { BaseAddress = FabricAddress }, NullLogger<HyphaClient>.Instance);
        }

        private static HttpResponseMessage CreateEventStream(params BaseEvent[] events)
        {
            var formatter = new SseEventStreamFormatter();
            var stream = new MemoryStream();
            formatter.WriteAsync(events.ToAsyncEnumerable(), stream, CancellationToken.None).GetAwaiter().GetResult();
            stream.Position = 0;

            var content = new StreamContent(stream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(formatter.MediaType);

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }

        private static async Task<List<HyphaUpdate>> ReadAllAsync(IAsyncEnumerable<HyphaUpdate> updates)
        {
            var result = new List<HyphaUpdate>();

            await foreach (var update in updates)
            {
                result.Add(update);
            }

            return result;
        }

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public List<HttpRequestMessage> Requests { get; } = [];

            public List<string> Bodies { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.Requests.Add(request);

                if (request.Content is not null)
                {
                    this.Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
                }

                return respond(request);
            }
        }
    }
}
