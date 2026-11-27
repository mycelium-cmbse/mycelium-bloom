// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaPanelViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.Time.Testing;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Hypha;
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Hypha;
    using Mycelium.SDK.POCO;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class HyphaPanelViewModelTestFixture
    {
        private static readonly Guid UserId = Guid.Parse("6c0d1f43-6a8e-4b6c-9a51-3b3f2b1f9a10");

        private Mock<IHyphaClient> client;

        private ContextAwareService selection;

        private TestNavigationManager navigation;

        private FakeTimeProvider timeProvider;

        private HyphaPanelViewModel viewModel;

        [SetUp]
        public void SetUp()
        {
            this.client = new Mock<IHyphaClient>();
            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ErrorOr<IReadOnlyList<HyphaUpdate>>)new List<HyphaUpdate>());
            this.selection = new ContextAwareService();
            this.navigation = new TestNavigationManager("http://localhost/workspace/modeling");

            var mockedData = new Mock<IMockedDataService>();
            mockedData.SetupGet(data => data.CurrentUser).Returns(new User { Id = UserId, Name = "Demo Account" });

            this.timeProvider = new FakeTimeProvider();
            this.viewModel = new HyphaPanelViewModel(this.client.Object, this.selection, mockedData.Object, this.navigation, this.timeProvider);
        }

        [TearDown]
        public void TearDown()
        {
            this.viewModel.Dispose();
            this.selection.Dispose();
        }

        [Test]
        public async Task VerifyAQuestionShowsTheDotsUntilTheFirstWordThenTheStreamedAnswer()
        {
            this.SetUpAnswer(Text("The dry mass "), Text("is **125.8 kg**."), Finished());
            var dots = new List<(bool IsThinking, int Rows)>();
            this.viewModel.PropertyChanged += (_, arguments) =>
            {
                if (arguments.PropertyName == nameof(IHyphaPanelViewModel.IsThinking))
                {
                    dots.Add((this.viewModel.IsThinking, this.viewModel.Items.Count));
                }
            };

            this.viewModel.Draft = "  Give me the dry mass  ";
            await this.viewModel.SendAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dots, Is.EqualTo(new[] { (true, 1), (false, 2) }), "The dots show from the question to the first word.");
                Assert.That(this.viewModel.Items, Is.EqualTo(new[]
                {
                    new HyphaChatItem(HyphaChatItemKind.Question, "Give me the dry mass"),
                    new HyphaChatItem(HyphaChatItemKind.Answer, "The dry mass is **125.8 kg**.")
                }));
                Assert.That(this.viewModel.IsAnswering, Is.False);
                Assert.That(this.viewModel.IsThinking, Is.False);
                Assert.That(this.viewModel.Draft, Is.Empty);
            }

            this.client.Verify(client => client.AskAsync("Give me the dry mass", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task VerifyTheReasoningIsOneRowOpenUntilTheAnswerStartsThenTimed()
        {
            var reasoningWhileWritten = new List<HyphaChatItem>();
            var dots = new List<bool>();
            this.client.Setup(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => this.ReasonThenAnswer(token, TimeSpan.FromSeconds(6.4)));
            this.viewModel.PropertyChanged += (_, arguments) =>
            {
                if (arguments.PropertyName == nameof(IHyphaPanelViewModel.Items) && this.viewModel.Items.Count == 2)
                {
                    reasoningWhileWritten.Add(this.viewModel.Items[1]);
                }

                if (arguments.PropertyName == nameof(IHyphaPanelViewModel.IsThinking))
                {
                    dots.Add(this.viewModel.IsThinking);
                }
            };

            this.viewModel.Draft = "Give me the dry mass";
            await this.viewModel.SendAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reasoningWhileWritten.Take(2), Is.EqualTo(new[]
                {
                    new HyphaChatItem(HyphaChatItemKind.Reasoning, "The user wants "),
                    new HyphaChatItem(HyphaChatItemKind.Reasoning, "The user wants the dry mass.")
                }), "The reasoning row stays open while it is written.");
                Assert.That(dots, Is.EqualTo(new[] { true, false }), "The dots give way to the reasoning row.");
                Assert.That(this.viewModel.Items, Is.EqualTo(new[]
                {
                    new HyphaChatItem(HyphaChatItemKind.Question, "Give me the dry mass"),
                    new HyphaChatItem(HyphaChatItemKind.Reasoning, "The user wants the dry mass.", IsDone: true, Duration: TimeSpan.FromSeconds(6.4)),
                    new HyphaChatItem(HyphaChatItemKind.Answer, "125.8 kg.")
                }));
            }
        }

        [Test]
        public async Task VerifyReasoningBetweenTwoTextsSplitsTheAnswer()
        {
            this.SetUpAnswer(
                Reasoning("Find the element first."),
                Text("Let me look."),
                new HyphaUpdate(HyphaUpdateKind.ToolCallStarted, ToolCallId: "call-1", ToolName: "find_elements_by_name"),
                new HyphaUpdate(HyphaUpdateKind.ToolCallFinished, ToolCallId: "call-1", ToolName: "find_elements_by_name"),
                Text("Found it."),
                Reasoning("Now the mass."),
                Text("125.8 kg."),
                Finished());

            this.viewModel.Draft = "Give me the dry mass";
            await this.viewModel.SendAsync();

            Assert.That(this.viewModel.Items, Is.EqualTo(new[]
            {
                new HyphaChatItem(HyphaChatItemKind.Question, "Give me the dry mass"),
                new HyphaChatItem(HyphaChatItemKind.Reasoning, "Find the element first.", IsDone: true, Duration: TimeSpan.Zero),
                new HyphaChatItem(HyphaChatItemKind.Answer, "Let me look."),
                new HyphaChatItem(HyphaChatItemKind.ToolCall, "Find elements by name", "call-1", true),
                new HyphaChatItem(HyphaChatItemKind.Answer, "Found it."),
                new HyphaChatItem(HyphaChatItemKind.Reasoning, "Now the mass.", IsDone: true, Duration: TimeSpan.Zero),
                new HyphaChatItem(HyphaChatItemKind.Answer, "125.8 kg.")
            }));
        }

        [Test]
        public async Task VerifyAnAnswerStoppedWhileReasoningEndsTheReasoning()
        {
            this.client.Setup(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => StreamUntilStopped(token, Reasoning("The user wants")));

            this.viewModel.Draft = "Give me the dry mass";
            var answer = this.viewModel.SendAsync();
            this.viewModel.Stop();
            await answer;

            Assert.That(this.viewModel.Items[1], Is.EqualTo(new HyphaChatItem(HyphaChatItemKind.Reasoning, "The user wants", IsDone: true, Duration: TimeSpan.Zero)));
        }

        [Test]
        public async Task VerifyTheReasoningReadBackIsFoldedWithoutDuration()
        {
            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ErrorOr<IReadOnlyList<HyphaUpdate>>)new List<HyphaUpdate>
                {
                    new(HyphaUpdateKind.Question, "Give me the dry mass"),
                    Reasoning("The user wants the dry mass."),
                    Text("125.8 kg."),
                    Finished()
                });

            await this.viewModel.OpenAsync();

            Assert.That(this.viewModel.Items, Is.EqualTo(new[]
            {
                new HyphaChatItem(HyphaChatItemKind.Question, "Give me the dry mass"),
                new HyphaChatItem(HyphaChatItemKind.Reasoning, "The user wants the dry mass.", IsDone: true),
                new HyphaChatItem(HyphaChatItemKind.Answer, "125.8 kg.")
            }));
        }

        [Test]
        public async Task VerifyAnEmptyDraftSendsNothing()
        {
            this.viewModel.Draft = "   ";

            await this.viewModel.SendAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Items, Is.Empty);
                this.client.Verify(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public async Task VerifyEachToolCallIsOneLineThatEndsWithItsResult()
        {
            var linesWhileRunning = new List<HyphaChatItem>();
            this.client.Setup(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => Stream(token,
                    new HyphaUpdate(HyphaUpdateKind.ToolCallStarted, ToolCallId: "call-1", ToolName: "sum_attribute"),
                    Text("Reading the budget."),
                    new HyphaUpdate(HyphaUpdateKind.ToolCallFinished, ToolCallId: "call-1", ToolName: "sum_attribute"),
                    Text("The dry mass is 125.8 kg."),
                    Finished()));
            this.viewModel.PropertyChanged += (_, arguments) =>
            {
                if (arguments.PropertyName == nameof(IHyphaPanelViewModel.Items) && this.viewModel.Items.Count == 2)
                {
                    linesWhileRunning.Add(this.viewModel.Items[1]);
                }
            };

            this.viewModel.Draft = "Give me the dry mass";
            await this.viewModel.SendAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(linesWhileRunning.First(), Is.EqualTo(new HyphaChatItem(HyphaChatItemKind.ToolCall, "Sum attribute", "call-1")));
                Assert.That(this.viewModel.Items, Is.EqualTo(new[]
                {
                    new HyphaChatItem(HyphaChatItemKind.Question, "Give me the dry mass"),
                    new HyphaChatItem(HyphaChatItemKind.ToolCall, "Sum attribute", "call-1", true),
                    new HyphaChatItem(HyphaChatItemKind.Answer, "Reading the budget.The dry mass is 125.8 kg.")
                }));
            }
        }

        [Test]
        public async Task VerifyAnErrorIsShownAndTheNextQuestionStillGoes()
        {
            this.SetUpAnswer(HyphaUpdate.Error("Hypha cannot reach Fabric at http://localhost:5180/."));

            this.viewModel.Draft = "Give me the dry mass";
            await this.viewModel.SendAsync();
            this.viewModel.Draft = "Try again";
            await this.viewModel.SendAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Items.Select(item => item.Kind), Is.EqualTo(new[]
                {
                    HyphaChatItemKind.Question, HyphaChatItemKind.Error, HyphaChatItemKind.Question, HyphaChatItemKind.Error
                }));
                Assert.That(this.viewModel.Items[1].Text, Does.Contain("cannot reach Fabric"));
                Assert.That(this.viewModel.IsAnswering, Is.False);
            }

            this.client.Verify(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task VerifyAQuestionSentDuringAnAnswerWaitsThenSendNowStopsTheAnswerAndSendsIt()
        {
            this.client.Setup(client => client.AskAsync("First", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => StreamUntilStopped(token, Text("The first answer")));
            this.client.Setup(client => client.AskAsync("Second", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => Stream(token, Text("The second answer"), Finished()));

            this.viewModel.Draft = "First";
            var first = this.viewModel.SendAsync();
            this.viewModel.Draft = "Second";
            await this.viewModel.SendAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.IsAnswering, Is.True);
                Assert.That(this.viewModel.QueuedQuestion, Is.EqualTo("Second"));
                this.client.Verify(client => client.AskAsync("Second", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()), Times.Never);
            }

            this.viewModel.SendNow();
            await first;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Items, Is.EqualTo(new[]
                {
                    new HyphaChatItem(HyphaChatItemKind.Question, "First"),
                    new HyphaChatItem(HyphaChatItemKind.Answer, "The first answer"),
                    new HyphaChatItem(HyphaChatItemKind.Question, "Second"),
                    new HyphaChatItem(HyphaChatItemKind.Answer, "The second answer")
                }));
                Assert.That(this.viewModel.QueuedQuestion, Is.Null);
                Assert.That(this.viewModel.IsAnswering, Is.False);
            }
        }

        [Test]
        public async Task VerifyStopEndsTheAnswerAndGivesTheQueuedQuestionBack()
        {
            this.client.Setup(client => client.AskAsync("First", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => StreamUntilStopped(token, Text("The first answer")));

            this.viewModel.Draft = "First";
            var first = this.viewModel.SendAsync();
            this.viewModel.Draft = "Second";
            await this.viewModel.SendAsync();
            this.viewModel.Stop();
            await first;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.IsAnswering, Is.False);
                Assert.That(this.viewModel.QueuedQuestion, Is.Null);
                Assert.That(this.viewModel.Draft, Is.EqualTo("Second"));
                Assert.That(this.viewModel.Items.Last(), Is.EqualTo(new HyphaChatItem(HyphaChatItemKind.Answer, "The first answer")));
                this.client.Verify(client => client.AskAsync("Second", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public async Task VerifyOpenReadsTheConversationBackOnce()
        {
            this.client.Setup(client => client.GetHistoryAsync($"{UserId}:sample", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ErrorOr<IReadOnlyList<HyphaUpdate>>)new List<HyphaUpdate>
                {
                    new(HyphaUpdateKind.Question, "Give me the dry mass"),
                    new(HyphaUpdateKind.ToolCallStarted, ToolCallId: "call-1", ToolName: "sum_attribute"),
                    new(HyphaUpdateKind.ToolCallFinished, ToolCallId: "call-1", ToolName: "sum_attribute"),
                    Text("125.8 kg."),
                    Finished()
                });

            await this.viewModel.OpenAsync();
            await this.viewModel.OpenAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Items, Is.EqualTo(new[]
                {
                    new HyphaChatItem(HyphaChatItemKind.Question, "Give me the dry mass"),
                    new HyphaChatItem(HyphaChatItemKind.ToolCall, "Sum attribute", "call-1", true),
                    new HyphaChatItem(HyphaChatItemKind.Answer, "125.8 kg.")
                }));
                Assert.That(this.viewModel.IsLoadingHistory, Is.False);
                this.client.Verify(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Test]
        public async Task VerifyOpenExplainsAnUnreadableConversationAndTriesAgainNextTime()
        {
            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Error.Unexpected("Hypha.Unreachable", "Hypha cannot reach Fabric at http://localhost:5180/."));

            await this.viewModel.OpenAsync();
            await this.viewModel.OpenAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Items.First(), Is.EqualTo(new HyphaChatItem(HyphaChatItemKind.Error, "Hypha cannot reach Fabric at http://localhost:5180/.")));
                this.client.Verify(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
        }

        [Test]
        public async Task VerifyAQuestionSentWhileTheHistoryIsReadWaitsForTheFirstOne()
        {
            var history = new TaskCompletionSource<ErrorOr<IReadOnlyList<HyphaUpdate>>>();
            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(history.Task);
            this.SetUpAnswer(Text("Answer"), Finished());

            var open = this.viewModel.OpenAsync();
            this.viewModel.Draft = "First";
            var first = this.viewModel.SendAsync();
            this.viewModel.Draft = "Second";
            await this.viewModel.SendAsync();

            Assert.That(this.viewModel.QueuedQuestion, Is.EqualTo("Second"));

            history.SetResult(new List<HyphaUpdate>());
            await open;
            await first;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.viewModel.Items.Select(item => item.Text), Is.EqualTo(new[] { "First", "Answer", "Second", "Answer" }));
                Assert.That(this.viewModel.IsAnswering, Is.False);
            }
        }

        [Test]
        public async Task VerifyAFailedReadOfTheHistoryDoesNotBlockTheQuestions()
        {
            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotSupportedException("Unknown message"));
            this.SetUpAnswer(Text("Answer"), Finished());

            await this.viewModel.OpenAsync();
            this.viewModel.Draft = "First";
            await this.viewModel.SendAsync();

            Assert.That(this.viewModel.Items.Select(item => item.Kind), Is.EqualTo(new[]
            {
                HyphaChatItemKind.Error, HyphaChatItemKind.Question, HyphaChatItemKind.Answer
            }));
        }

        [Test]
        public async Task VerifyTheQuestionCarriesTheProjectTheUserAndTheSelection()
        {
            var contexts = new List<HyphaContext>();
            this.client.Setup(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Callback((string _, HyphaContext context, CancellationToken _) => contexts.Add(context))
                .Returns((string _, HyphaContext _, CancellationToken token) => Stream(token, Finished()));
            var element = new Namespace { Id = Guid.Parse("aed7ac47-7926-ea14-6637-ac50ef557797"), DeclaredName = "aocs" };

            this.viewModel.Draft = "Without selection";
            await this.viewModel.SendAsync();
            this.selection.SelectedElement = element;
            this.navigation.NavigateTo("http://localhost/workspace/modeling?project=kestrel-1");
            this.viewModel.Draft = "With selection";
            await this.viewModel.SendAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(contexts[0].ThreadId, Is.EqualTo($"{UserId}:sample"));
                Assert.That(contexts[0].UserName, Is.EqualTo("Demo Account"));
                Assert.That(contexts[0].SelectedElement, Is.Null);
                Assert.That(contexts[1].ThreadId, Is.EqualTo($"{UserId}:kestrel-1"));
                Assert.That(contexts[1].SelectedElement, Does.StartWith("aocs (").And.EndWith("id aed7ac47-7926-ea14-6637-ac50ef557797)"));
            }
        }

        private static HyphaUpdate Text(string text) => new(HyphaUpdateKind.Text, text);

        private static HyphaUpdate Reasoning(string text) => new(HyphaUpdateKind.Reasoning, text);

        private static HyphaUpdate Finished() => new(HyphaUpdateKind.Finished);

        private static async IAsyncEnumerable<HyphaUpdate> Stream([EnumeratorCancellation] CancellationToken token, params HyphaUpdate[] updates)
        {
            foreach (var update in updates)
            {
                token.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return update;
            }
        }

        private static async IAsyncEnumerable<HyphaUpdate> StreamUntilStopped([EnumeratorCancellation] CancellationToken token, HyphaUpdate first)
        {
            yield return first;

            var stopped = false;

            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
                stopped = true;
            }

            if (!stopped)
            {
                yield return Finished();
            }
        }

        private async IAsyncEnumerable<HyphaUpdate> ReasonThenAnswer([EnumeratorCancellation] CancellationToken token, TimeSpan reasoningTime)
        {
            await Task.Yield();
            yield return Reasoning("The user wants ");
            token.ThrowIfCancellationRequested();
            yield return Reasoning("the dry mass.");
            this.timeProvider.Advance(reasoningTime);
            yield return Text("125.8 kg.");
            yield return Finished();
        }

        private void SetUpAnswer(params HyphaUpdate[] updates)
        {
            this.client.Setup(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => Stream(token, updates));
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager(string uri)
            {
                this.Initialize("http://localhost/", uri);
            }

            protected override void NavigateToCore(string uri, NavigationOptions options)
            {
                this.Uri = this.ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
