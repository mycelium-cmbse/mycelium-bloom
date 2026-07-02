// ------------------------------------------------------------------------------------------------
// <copyright file="ActivityFeed.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ActivityFeed
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable project or workspace activity feed.
    /// </summary>
    public partial class ActivityFeed : ComponentBase
    {
        /// <summary>
        /// Gets or sets the activity items rendered in the feed.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ActivityFeedItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the text rendered when the feed has no activity items.
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; } = "No activity yet.";

        /// <summary>
        /// Gets or sets a value indicating whether the compact feed layout is used.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; } = true;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the feed.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the feed element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the feed.
        /// </summary>
        /// <returns>The feed CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-activity-feed",
                CssClassBuilder.When("mb-activity-feed--compact", this.Compact),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to an activity marker.
        /// </summary>
        /// <param name="item">The activity feed item.</param>
        /// <returns>The activity marker CSS class list.</returns>
        private string GetMarkerCssClass(ActivityFeedItem item)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-activity-feed__marker",
                this.GetMarkerVariantCssClass(item.Variant));

            return cssClass;
        }

        /// <summary>
        /// Gets the marker CSS class matching the selected activity item variant.
        /// </summary>
        /// <param name="variant">The activity item variant.</param>
        /// <returns>The marker variant CSS class.</returns>
        private string GetMarkerVariantCssClass(ActivityFeedItemVariant variant)
        {
            var cssClass = variant switch
            {
                ActivityFeedItemVariant.Created => "mb-activity-feed__marker--created",
                ActivityFeedItemVariant.Updated => "mb-activity-feed__marker--updated",
                ActivityFeedItemVariant.Deleted => "mb-activity-feed__marker--deleted",
                ActivityFeedItemVariant.Commented => "mb-activity-feed__marker--commented",
                ActivityFeedItemVariant.Reviewed => "mb-activity-feed__marker--reviewed",
                ActivityFeedItemVariant.Synced => "mb-activity-feed__marker--synced",
                ActivityFeedItemVariant.Joined => "mb-activity-feed__marker--joined",
                ActivityFeedItemVariant.Left => "mb-activity-feed__marker--left",
                _ => "mb-activity-feed__marker--neutral"
            };

            return cssClass;
        }
    }
}
