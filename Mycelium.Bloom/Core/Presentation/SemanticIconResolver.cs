// ------------------------------------------------------------------------------------------------
// <copyright file="SemanticIconResolver.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Presentation
{
    /// <summary>
    /// Resolves established Lucide icon names from semantic labels without classifying or rendering model elements.
    /// </summary>
    public static class SemanticIconResolver
    {
        /// <summary>
        /// Gets the established element symbol using an exact, case-sensitive semantic-kind label.
        /// </summary>
        /// <param name="semanticKind">The already-classified semantic kind, which may be null.</param>
        /// <returns>
        /// The Lucide symbol name, or "shapes" for an unrecognized label, including null, empty or whitespace input.
        /// </returns>
        /// <remarks>Exported icon selection remains the responsibility of the consumer.</remarks>
        public static string GetElementSymbol(string semanticKind)
        {
            return semanticKind switch
            {
                "Attribute" or "Attribute definition" => "hash",
                "Port definition" => "circle-dot",
                "Item" => "box",
                "Interface" or "Interface definition" => "plug",
                "Connection" or "Connection definition" or "Binding connector" or "Association" => "link",
                "Flow" or "Flow definition" or "Succession flow" => "move-right",
                "Allocation" or "Allocation definition" => "git-merge",
                "View definition" => "network",
                "Metadata definition" => "tags",
                "Enumeration definition" => "list",
                "Verification case" or "Verification case definition" => "clipboard-check",
                "Analysis case" or "Analysis case definition" => "chart-no-axes-combined",
                "Satisfy requirement" => "list-checks",
                "Requirement" or "Requirement definition" => "file-check",
                "State" or "State definition" or "Exhibit state" => "circle-dot",
                "Calculation" or "Calculation definition" => "calculator",
                "Action" or "Action definition" or "Perform action" => "activity",
                "Constraint" or "Constraint definition" or "Constraint assertion" => "square-equal",
                "Feature" => "diamond",
                "Namespace" => "folder",
                _ => "shapes"
            };
        }

        /// <summary>
        /// Gets the established indicator icon using an exact, case-sensitive semantic-indicator label.
        /// </summary>
        /// <param name="indicator">The semantic indicator label, which may be null.</param>
        /// <returns>
        /// The Lucide icon name, or null for an unrecognized label, including null, empty or whitespace input,
        /// so the consumer can retain its text fallback or special rendering.
        /// </returns>
        public static string GetIndicatorIcon(string indicator)
        {
            return indicator switch
            {
                "Imported" => "arrow-down-to-line",
                "Library" => "library",
                "Standard library" => "badge-check",
                "Metadata" => "tag",
                "Variation" => "git-branch",
                "Conjugated" => "rotate-cw",
                "Parallel" => "columns-2",
                _ => null
            };
        }
    }
}
