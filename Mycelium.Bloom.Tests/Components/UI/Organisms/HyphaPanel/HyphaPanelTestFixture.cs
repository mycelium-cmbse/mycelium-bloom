// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaPanelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.HyphaPanel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Bunit;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Time.Testing;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Hypha;
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Hypha;
    using Mycelium.SDK.POCO;

    using HyphaPanelComponent = Mycelium.Bloom.Components.UI.Organisms.HyphaPanel.HyphaPanel;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class HyphaPanelTestFixture : BunitContext
    {
        private readonly Mock<IHyphaClient> client = new();

        private readonly ContextAwareService selection = new();

        private readonly FakeTimeProvider timeProvider = new();

        public HyphaPanelTestFixture()
        {
            BlueprintTestSetup.Configure(this);

            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ErrorOr<IReadOnlyList<HyphaUpdate>>)new List<HyphaUpdate>());

            var mockedData = new Mock<IMockedDataService>();
            mockedData.SetupGet(data => data.CurrentUser).Returns(new User { Id = Guid.NewGuid(), Name = "Demo Account" });

            this.Services.AddScoped<IHyphaPanelViewModel>(provider =>
                new HyphaPanelViewModel(this.client.Object, this.selection, mockedData.Object, provider.GetRequiredService<NavigationManager>(), this.timeProvider));
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.DisposeAsync();
            this.selection.Dispose();
        }

        [Test]
        public async Task VerifyAQuestionShowsTheDotsThenTheMarkdownAnswer()
        {
            var firstWord = new TaskCompletionSource();
            this.client.Setup(client => client.AskAsync("Give me the dry mass", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => AnswerAfter(firstWord.Task, token, Text("The dry mass is **125.8 kg**."), Finished()));
            var panel = this.Render<HyphaPanelComponent>();

            panel.Find("[data-testid='hypha-input']").Input("Give me the dry mass");
            panel.Find("[data-testid='hypha-input']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            panel.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.Find("[data-testid='hypha-question']").TextContent, Is.EqualTo("Give me the dry mass"));
                    Assert.That(panel.FindAll("[data-testid='hypha-thinking']"), Has.Count.EqualTo(1));
                    Assert.That(panel.FindAll("[data-testid='hypha-answer']"), Is.Empty);
                    Assert.That(panel.FindAll("[data-testid='hypha-stop']"), Has.Count.EqualTo(1));
                }
            });

            await panel.InvokeAsync(firstWord.SetResult);

            panel.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.Find("[data-testid='hypha-answer'] strong").TextContent, Is.EqualTo("125.8 kg"));
                    Assert.That(panel.FindAll("[data-testid='hypha-thinking']"), Is.Empty);
                    Assert.That(panel.FindAll("[data-testid='hypha-stop']"), Is.Empty);
                    Assert.That(panel.Find("[data-testid='hypha-input']").GetAttribute("value"), Is.Empty);
                }
            });
        }

        [Test]
        public async Task VerifyTheReasoningIsFoldedUnderOneLineThatGivesItsDuration()
        {
            var answer = new TaskCompletionSource();
            this.client.Setup(client => client.AskAsync("Give me the dry mass", It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => ReasoningThenAnswer(answer.Task, token));
            var panel = this.Render<HyphaPanelComponent>();

            panel.Find("[data-testid='hypha-input']").Input("Give me the dry mass");
            panel.Find("[data-testid='hypha-input']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            panel.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.Find("[data-testid='hypha-reasoning-label']").TextContent, Is.EqualTo("Thinking…"));
                    Assert.That(panel.Find("[data-testid='hypha-reasoning-text']").TextContent, Is.EqualTo("The user wants the dry mass."));
                    Assert.That(panel.Find("[data-testid='hypha-reasoning']").HasAttribute("open"), Is.False, "The reasoning starts folded.");
                    Assert.That(panel.FindAll("[data-testid='hypha-thinking']"), Is.Empty);
                }
            });

            this.timeProvider.Advance(TimeSpan.FromSeconds(6.4));
            await panel.InvokeAsync(answer.SetResult);

            panel.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.Find("[data-testid='hypha-reasoning-label']").TextContent, Is.EqualTo("Thought for 6 seconds"));
                    Assert.That(panel.Find("[data-testid='hypha-answer']").TextContent.Trim(), Is.EqualTo("125.8 kg."));
                }
            });
        }

        [Test]
        public void VerifyTheReasoningReadBackSaysOnlyThatHyphaThought()
        {
            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ErrorOr<IReadOnlyList<HyphaUpdate>>)new List<HyphaUpdate>
                {
                    new(HyphaUpdateKind.Question, "Give me the dry mass"),
                    new(HyphaUpdateKind.Reasoning, "The user wants the dry mass."),
                    Text("125.8 kg."),
                    Finished()
                });

            var panel = this.Render<HyphaPanelComponent>();

            panel.WaitForAssertion(() => Assert.That(panel.Find("[data-testid='hypha-reasoning-label']").TextContent, Is.EqualTo("Thought")));
        }

        [Test]
        public void VerifyDangerousHtmlOfAnAnswerIsRemoved()
        {
            this.SetUpAnswer(Text("Done <script>alert('x')</script><img src=x onerror=alert(1)>"), Finished());
            var panel = this.Render<HyphaPanelComponent>();

            panel.Find("[data-testid='hypha-input']").Input("Say hello");
            panel.Find("[data-testid='hypha-input']").KeyDown(new KeyboardEventArgs { Key = "a" });

            Assert.That(panel.FindAll("[data-testid='hypha-question']"), Is.Empty, "Only Enter sends from the field.");

            panel.Find("[data-testid='hypha-send']").Click();

            panel.WaitForAssertion(() =>
            {
                var answer = panel.Find("[data-testid='hypha-answer']");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answer.TextContent, Does.Contain("Done"));
                    Assert.That(answer.QuerySelectorAll("script, img"), Is.Empty);
                }
            });
        }

        [Test]
        public async Task VerifyAToolCallIsOneLineThatEndsWithACheck()
        {
            var result = new TaskCompletionSource();
            this.client.Setup(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => ToolCallThenAnswer(result.Task, token));
            var panel = this.Render<HyphaPanelComponent>();

            panel.Find("[data-testid='hypha-input']").Input("Give me the dry mass");
            panel.Find("[data-testid='hypha-input']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            panel.WaitForAssertion(() =>
            {
                var line = panel.Find("[data-testid='hypha-tool-call']");

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(line.TextContent.Trim(), Is.EqualTo("Sum attribute"));
                    Assert.That(line.GetAttribute("data-done"), Is.EqualTo("false"));
                }
            });

            await panel.InvokeAsync(result.SetResult);

            panel.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.FindAll("[data-testid='hypha-tool-call']"), Has.Count.EqualTo(1));
                    Assert.That(panel.Find("[data-testid='hypha-tool-call']").GetAttribute("data-done"), Is.EqualTo("true"));
                    Assert.That(panel.Find("[data-testid='hypha-answer']").TextContent, Does.Contain("125.8 kg"));
                }
            });
        }

        [Test]
        public void VerifyStoppedFabricShowsAMessageAndTheQuestionCanBeAskedAgain()
        {
            this.SetUpAnswer(HyphaUpdate.Error("Hypha cannot reach Fabric at http://localhost:5180/. Check that the Fabric host runs."));
            var panel = this.Render<HyphaPanelComponent>();

            panel.Find("[data-testid='hypha-input']").Input("Give me the dry mass");
            panel.Find("[data-testid='hypha-input']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            panel.WaitForAssertion(() => Assert.That(panel.Find("[data-testid='hypha-error']").TextContent, Does.Contain("cannot reach Fabric")));
            panel.Find("[data-testid='hypha-input']").Input("Give me the dry mass");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(panel.Find("[data-testid='hypha-error']").GetAttribute("role"), Is.EqualTo("alert"));
                Assert.That(panel.Find("[data-testid='hypha-input']").HasAttribute("disabled"), Is.False);
                Assert.That(panel.Find("[data-testid='hypha-send']").HasAttribute("disabled"), Is.False);
            }
        }

        [Test]
        public void VerifyTheConversationComesBackWhenThePanelOpens()
        {
            this.client.Setup(client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ErrorOr<IReadOnlyList<HyphaUpdate>>)new List<HyphaUpdate> { new(HyphaUpdateKind.Question, "Give me the dry mass"), Text("125.8 kg."), Finished() });

            var panel = this.Render<HyphaPanelComponent>();

            panel.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(panel.Find("[data-testid='hypha-question']").TextContent, Is.EqualTo("Give me the dry mass"));
                    Assert.That(panel.Find("[data-testid='hypha-answer']").TextContent.Trim(), Is.EqualTo("125.8 kg."));
                }
            });
        }

        [Test]
        public void VerifyTheCloseButtonAsksTheWorkspaceToFoldThePanel()
        {
            var closed = 0;
            var panel = this.Render<HyphaPanelComponent>(parameters => parameters.Add(component => component.CloseRequested, () => closed++));

            panel.Find("[data-testid='hypha-close']").Click();

            Assert.That(closed, Is.EqualTo(1));
        }

        private static HyphaUpdate Text(string text) => new(HyphaUpdateKind.Text, text);

        private static HyphaUpdate Finished() => new(HyphaUpdateKind.Finished);

        private static async IAsyncEnumerable<HyphaUpdate> AnswerAfter(Task gate, [EnumeratorCancellation] CancellationToken token, params HyphaUpdate[] updates)
        {
            await gate.WaitAsync(token);

            foreach (var update in updates)
            {
                yield return update;
            }
        }

        private static async IAsyncEnumerable<HyphaUpdate> ReasoningThenAnswer(Task answer, [EnumeratorCancellation] CancellationToken token)
        {
            yield return new HyphaUpdate(HyphaUpdateKind.Reasoning, "The user wants ");
            yield return new HyphaUpdate(HyphaUpdateKind.Reasoning, "the dry mass.");
            await answer.WaitAsync(token);
            yield return Text("125.8 kg.");
            yield return Finished();
        }

        private static async IAsyncEnumerable<HyphaUpdate> ToolCallThenAnswer(Task result, [EnumeratorCancellation] CancellationToken token)
        {
            yield return new HyphaUpdate(HyphaUpdateKind.ToolCallStarted, ToolCallId: "call-1", ToolName: "sum_attribute");
            await result.WaitAsync(token);
            yield return new HyphaUpdate(HyphaUpdateKind.ToolCallFinished, ToolCallId: "call-1", ToolName: "sum_attribute");
            yield return Text("The dry mass is 125.8 kg.");
            yield return Finished();
        }

        private void SetUpAnswer(params HyphaUpdate[] updates)
        {
            this.client.Setup(client => client.AskAsync(It.IsAny<string>(), It.IsAny<HyphaContext>(), It.IsAny<CancellationToken>()))
                .Returns((string _, HyphaContext _, CancellationToken token) => AnswerAfter(Task.CompletedTask, token, updates));
        }
    }
}
