// ------------------------------------------------------------------------------------------------
// <copyright file="CommentThreadTestFixture.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.CommentThread
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;

    using CommentThreadComponent = Mycelium.Bloom.Components.UI.Organisms.CommentThread.CommentThread;

    /// <summary>
    /// Tests the <see cref="CommentThreadComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class CommentThreadTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that submit, resolve, and delete actions invoke their callbacks.
        /// </summary>
        [Test]
        public void VerifyActionsInvokeCallbacks()
        {
            var submittedComment = string.Empty;
            var resolvedCommentId = string.Empty;
            var deletedCommentId = string.Empty;

            var component = this.Render<CommentThreadComponent>(parameters => parameters
                .Add(component => component.Comments, GetComments())
                .Add(component => component.NewCommentValue, "Please verify the source multiplicity.")
                .Add(component => component.CommentSubmitted, value => submittedComment = value)
                .Add(component => component.CommentResolved, value => resolvedCommentId = value)
                .Add(component => component.CommentDeleted, value => deletedCommentId = value));

            component.FindAll(".mb-comment-thread__actions .mb-button")[0].Click();
            component.FindAll(".mb-comment-thread__actions .mb-button")[2].Click();
            component.Find(".mb-comment-thread__composer-actions .mb-button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolvedCommentId, Is.EqualTo("comment-multiplicity"));
                Assert.That(deletedCommentId, Is.EqualTo("comment-sysml-check"));
                Assert.That(submittedComment, Is.EqualTo("Please verify the source multiplicity."));
            }
        }

        /// <summary>
        /// Verifies that comments render their content, state classes, and available actions.
        /// </summary>
        [Test]
        public void VerifyRenderComments()
        {
            var component = this.Render<CommentThreadComponent>(parameters => parameters
                .Add(component => component.Comments, GetComments())
                .Add(component => component.Class, "custom-thread")
                .AddUnmatched("data-testid", "thread"));

            var root = component.Find(".mb-comment-thread");
            var comments = component.FindAll(".mb-comment-thread__item");
            var actions = component.FindAll(".mb-comment-thread__actions .mb-button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetAttribute("class"), Does.Contain("custom-thread"));
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("thread"));
                Assert.That(comments, Has.Count.EqualTo(3));
                Assert.That(comments[1].GetAttribute("class"), Does.Contain("mb-comment-thread__item--current-user"));
                Assert.That(comments[2].GetAttribute("class"), Does.Contain("mb-comment-thread__item--resolved"));
                Assert.That(component.Markup, Does.Contain("Verify multiplicity"));
                Assert.That(component.Markup, Does.Contain("edited"));
                Assert.That(actions, Has.Count.EqualTo(3));
            }
        }

        /// <summary>
        /// Verifies that the compact empty state renders when no comments are provided.
        /// </summary>
        [Test]
        public void VerifyRenderEmptyState()
        {
            var component = this.Render<CommentThreadComponent>(parameters => parameters
                .Add(component => component.EmptyText, "No review notes yet."));

            var emptyState = component.Find(".mb-comment-thread__empty");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(emptyState.TextContent.Trim(), Is.EqualTo("No review notes yet."));
                Assert.That(component.FindAll(".mb-comment-thread__item"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that submitting whitespace comments is disabled.
        /// </summary>
        [Test]
        public void VerifyWhitespaceSubmitIsDisabled()
        {
            var submittedComment = string.Empty;

            var component = this.Render<CommentThreadComponent>(parameters => parameters
                .Add(component => component.NewCommentValue, "   ")
                .Add(component => component.CommentSubmitted, value => submittedComment = value));

            var submitButton = component.Find(".mb-comment-thread__composer-actions .mb-button");

            submitButton.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(submitButton.HasAttribute("disabled"), Is.True);
                Assert.That(submittedComment, Is.Empty);
            }
        }

        /// <summary>
        /// Gets sample comment thread items.
        /// </summary>
        /// <returns>The sample comment thread items.</returns>
        private static IReadOnlyList<CommentThreadItem> GetComments()
        {
            return
            [
                new CommentThreadItem
                {
                    Id = "comment-multiplicity",
                    AuthorName = "Antoine",
                    AuthorInitials = "AT",
                    AuthorColor = "var(--mb-color-collaborator-c10)",
                    CreatedAtText = "12 min ago",
                    Body = "Verify multiplicity before commit.",
                    IsEdited = true
                },
                new CommentThreadItem
                {
                    Id = "comment-sysml-check",
                    AuthorName = "Omrane",
                    AuthorInitials = "OH",
                    AuthorColor = "var(--mb-color-collaborator-c08)",
                    CreatedAtText = "8 min ago",
                    Body = "I will check it against the SysML model.",
                    IsCurrentUser = true
                },
                new CommentThreadItem
                {
                    Id = "comment-resolved",
                    AuthorName = "Antoine",
                    AuthorInitials = "AT",
                    AuthorColor = "var(--mb-color-collaborator-c10)",
                    CreatedAtText = "Yesterday",
                    Body = "Resolved trace discussion.",
                    IsResolved = true
                }
            ];
        }
    }
}
