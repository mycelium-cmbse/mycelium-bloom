// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceEditorOptions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Configuration
{
    /// <summary>
    /// Provides strongly typed application configuration for rendering-independent workspace editor state.
    /// </summary>
    public sealed class WorkspaceEditorOptions
    {
        /// <summary>
        /// The configuration section containing workspace editor settings.
        /// </summary>
        public const string SectionName = "WorkspaceEditor";

        /// <summary>
        /// Gets or sets the maximum number of editor groups supported by one workspace.
        /// </summary>
        public int MaximumGroupCount { get; set; }
    }
}
