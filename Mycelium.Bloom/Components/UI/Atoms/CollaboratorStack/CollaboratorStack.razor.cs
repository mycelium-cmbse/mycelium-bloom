// ------------------------------------------------------------------------------------------------
// <copyright file="CollaboratorStack.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Atoms.CollaboratorStack
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a compact overlapping collaborator avatar stack.
    /// </summary>
    public partial class CollaboratorStack : ComponentBase
    {
        /// <summary>
        /// Gets or sets the collaborators shown in the stack.
        /// </summary>
        [Parameter]
        public IReadOnlyList<CollaboratorStackItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the maximum number of collaborators shown before the overflow avatar.
        /// </summary>
        [Parameter]
        public int MaxVisible { get; set; } = 4;

        /// <summary>
        /// Gets or sets a value indicating whether online/offline status indicators should be shown.
        /// </summary>
        [Parameter]
        public bool ShowOnlineIndicator { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the current user avatar should use a brand ring.
        /// </summary>
        [Parameter]
        public bool ShowCurrentUserRing { get; set; } = true;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the stack.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the stack element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the number of hidden collaborators represented by the overflow avatar.
        /// </summary>
        protected int HiddenCount => Math.Max(0, this.Items.Count - this.VisibleCount);

        /// <summary>
        /// Gets the clamped visible collaborator count.
        /// </summary>
        private int VisibleCount => Math.Min(Math.Max(0, this.MaxVisible), this.Items.Count);

        /// <summary>
        /// Gets the collaborators visible in the stack.
        /// </summary>
        /// <returns>The visible collaborators.</returns>
        protected IEnumerable<CollaboratorStackItem> GetVisibleItems()
        {
            return this.Items.Take(this.VisibleCount);
        }

        /// <summary>
        /// Gets the final CSS class list applied to the stack.
        /// </summary>
        /// <returns>The stack CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-collaborator-stack",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to a collaborator item.
        /// </summary>
        /// <param name="item">The collaborator stack item.</param>
        /// <returns>The collaborator item CSS class list.</returns>
        private string GetItemCssClass(CollaboratorStackItem item)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-collaborator-stack__item",
                CssClassBuilder.When("mb-collaborator-stack__item--current-user",
                    this.ShowCurrentUserRing && item.IsCurrentUser));

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to a collaborator status indicator.
        /// </summary>
        /// <param name="item">The collaborator stack item.</param>
        /// <returns>The collaborator status CSS class list.</returns>
        private string GetStatusCssClass(CollaboratorStackItem item)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-collaborator-stack__status",
                item.IsOnline
                    ? "mb-collaborator-stack__status--online"
                    : "mb-collaborator-stack__status--offline");

            return cssClass;
        }

        /// <summary>
        /// Gets the tooltip text for a collaborator.
        /// </summary>
        /// <param name="item">The collaborator stack item.</param>
        /// <returns>The collaborator tooltip text.</returns>
        private string GetItemTitle(CollaboratorStackItem item)
        {
            var title = string.IsNullOrWhiteSpace(item.Role)
                ? item.Name
                : $"{item.Name} - {item.Role}";

            return title;
        }

        /// <summary>
        /// Gets the title for a collaborator status indicator.
        /// </summary>
        /// <param name="item">The collaborator stack item.</param>
        /// <returns>The collaborator status title.</returns>
        private string GetStatusTitle(CollaboratorStackItem item)
        {
            var status = item.IsOnline ? "Online" : "Offline";

            return $"{item.Name} is {status.ToLowerInvariant()}";
        }

        /// <summary>
        /// Gets the overflow avatar text.
        /// </summary>
        /// <returns>The overflow avatar text.</returns>
        private string GetMoreText()
        {
            return $"+{this.HiddenCount}";
        }

        /// <summary>
        /// Gets the overflow avatar title.
        /// </summary>
        /// <returns>The overflow avatar title.</returns>
        private string GetMoreTitle()
        {
            var collaboratorText = this.HiddenCount == 1 ? "collaborator" : "collaborators";

            return $"{this.HiddenCount} more {collaboratorText}";
        }
    }
}
