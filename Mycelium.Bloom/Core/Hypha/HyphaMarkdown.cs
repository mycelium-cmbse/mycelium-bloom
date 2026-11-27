// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaMarkdown.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Hypha
{
    using Ganss.Xss;

    using Markdig;

    /// <summary>
    /// Renders the Markdown answers of Hypha as HTML that is safe to show: the language model writes the text, so raw HTML is
    /// not rendered and the result is sanitized.
    /// </summary>
    public static class HyphaMarkdown
    {
        /// <summary>
        /// The Markdown features of an answer: tables for budgets, emphasis, links; raw HTML is shown as text.
        /// </summary>
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .DisableHtml()
            .Build();

        /// <summary>
        /// Removes scripts, event handlers and unsafe links; thread-safe for static use.
        /// </summary>
        private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

        /// <summary>
        /// Converts Markdown to sanitized HTML.
        /// </summary>
        /// <param name="markdown">The Markdown written by Hypha.</param>
        /// <returns>The sanitized HTML, or an empty string.</returns>
        public static string ToSafeHtml(string markdown)
        {
            return string.IsNullOrWhiteSpace(markdown) ? string.Empty : Sanitizer.Sanitize(Markdown.ToHtml(markdown, Pipeline));
        }

        /// <summary>
        /// Creates the sanitizer; links of an answer open in a new tab so that the workspace stays open.
        /// </summary>
        /// <returns>The configured sanitizer.</returns>
        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("target");
            sanitizer.PostProcessNode += (_, arguments) =>
            {
                if (arguments.Node is AngleSharp.Dom.IElement { LocalName: "a" } link)
                {
                    link.SetAttribute("target", "_blank");
                    link.SetAttribute("rel", "noopener noreferrer");
                }
            };

            return sanitizer;
        }
    }
}
