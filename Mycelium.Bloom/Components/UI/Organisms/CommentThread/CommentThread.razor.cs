// ------------------------------------------------------------------------------------------------
// <copyright file="CommentThread.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.CommentThread
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a reusable collaboration comment thread.
    /// </summary>
    public partial class CommentThread : ComponentBase
    {
        /// <summary>
        /// Gets or sets the comments rendered in the thread.
        /// </summary>
        [Parameter]
        public IReadOnlyList<CommentThreadItem> Comments { get; set; } = [];

        /// <summary>
        /// Gets or sets the current new comment value.
        /// </summary>
        [Parameter]
        public string NewCommentValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the new comment value changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> NewCommentValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text rendered in the composer.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = "Write a comment...";

        /// <summary>
        /// Gets or sets the text rendered when the thread has no comments.
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; } = "No comments yet.";

        /// <summary>
        /// Gets or sets a value indicating whether comment actions and composing are disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether submitting a new comment is allowed.
        /// </summary>
        [Parameter]
        public bool AllowSubmit { get; set; } = true;

        /// <summary>
        /// Gets or sets the callback invoked when a comment is submitted.
        /// </summary>
        [Parameter]
        public EventCallback<string> CommentSubmitted { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a comment is resolved.
        /// </summary>
        [Parameter]
        public EventCallback<string> CommentResolved { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a comment is deleted.
        /// </summary>
        [Parameter]
        public EventCallback<string> CommentDeleted { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes applied to the thread.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the thread.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the comment thread.
        /// </summary>
        /// <returns>The comment thread CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-comment-thread",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to a comment item.
        /// </summary>
        /// <param name="comment">The comment item.</param>
        /// <returns>The comment item CSS class list.</returns>
        private static string GetCommentCssClass(CommentThreadItem comment)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-comment-thread__item",
                CssClassBuilder.When("mb-comment-thread__item--current-user", comment.IsCurrentUser),
                CssClassBuilder.When("mb-comment-thread__item--resolved", comment.IsResolved));

            return cssClass;
        }

        /// <summary>
        /// Gets a value indicating whether comment actions should render.
        /// </summary>
        /// <param name="comment">The comment item.</param>
        /// <returns>True when an action is available; otherwise, false.</returns>
        private static bool ShouldShowActions(CommentThreadItem comment)
        {
            var shouldShowActions = !comment.IsResolved || comment.IsCurrentUser;

            return shouldShowActions;
        }

        /// <summary>
        /// Gets a value indicating whether the submit action is disabled.
        /// </summary>
        /// <returns>True when submitting should be disabled; otherwise, false.</returns>
        private bool IsSubmitDisabled()
        {
            var isSubmitDisabled = this.Disabled
                                   || !this.AllowSubmit
                                   || string.IsNullOrWhiteSpace(this.NewCommentValue);

            return isSubmitDisabled;
        }

        /// <summary>
        /// Handles submitting the current composer value.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleSubmitAsync()
        {
            if (this.IsSubmitDisabled())
            {
                return Task.CompletedTask;
            }

            var task = this.CommentSubmitted.InvokeAsync(this.NewCommentValue);

            return task;
        }

        /// <summary>
        /// Handles resolving a comment.
        /// </summary>
        /// <param name="commentId">The comment identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleResolveAsync(string commentId)
        {
            if (this.Disabled)
            {
                return Task.CompletedTask;
            }

            var task = this.CommentResolved.InvokeAsync(commentId);

            return task;
        }

        /// <summary>
        /// Handles deleting a comment.
        /// </summary>
        /// <param name="commentId">The comment identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleDeleteAsync(string commentId)
        {
            if (this.Disabled)
            {
                return Task.CompletedTask;
            }

            var task = this.CommentDeleted.InvokeAsync(commentId);

            return task;
        }
    }
}
