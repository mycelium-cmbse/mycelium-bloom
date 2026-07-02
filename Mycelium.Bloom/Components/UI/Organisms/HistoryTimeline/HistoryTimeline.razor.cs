// ------------------------------------------------------------------------------------------------
// <copyright file="HistoryTimeline.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.HistoryTimeline
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable element or project history timeline.
    /// </summary>
    public partial class HistoryTimeline : ComponentBase
    {
        /// <summary>
        /// Gets or sets the history items rendered in the timeline.
        /// </summary>
        [Parameter]
        public IReadOnlyList<HistoryTimelineItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the text rendered when the timeline has no history items.
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; } = "No history available.";

        /// <summary>
        /// Gets or sets a value indicating whether the compact timeline layout is used.
        /// </summary>
        [Parameter]
        public bool Compact { get; set; } = true;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the timeline.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the timeline element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the timeline.
        /// </summary>
        /// <returns>The timeline CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-history-timeline",
                CssClassBuilder.When("mb-history-timeline--compact", this.Compact),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to a timeline marker.
        /// </summary>
        /// <param name="item">The history timeline item.</param>
        /// <returns>The timeline marker CSS class list.</returns>
        private string GetMarkerCssClass(HistoryTimelineItem item)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-history-timeline__marker",
                GetMarkerVariantCssClass(item.Variant));

            return cssClass;
        }

        /// <summary>
        /// Gets the marker CSS class matching the selected history item variant.
        /// </summary>
        /// <param name="variant">The history item variant.</param>
        /// <returns>The marker variant CSS class.</returns>
        private static string GetMarkerVariantCssClass(HistoryTimelineItemVariant variant)
        {
            var cssClass = variant switch
            {
                HistoryTimelineItemVariant.Created => "mb-history-timeline__marker--created",
                HistoryTimelineItemVariant.Updated => "mb-history-timeline__marker--updated",
                HistoryTimelineItemVariant.Deleted => "mb-history-timeline__marker--deleted",
                HistoryTimelineItemVariant.Commented => "mb-history-timeline__marker--commented",
                HistoryTimelineItemVariant.Reviewed => "mb-history-timeline__marker--reviewed",
                HistoryTimelineItemVariant.Synced => "mb-history-timeline__marker--synced",
                _ => "mb-history-timeline__marker--neutral"
            };

            return cssClass;
        }
    }
}
