// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserFilterPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using System.Collections.Immutable;

    /// <summary>
    /// Provides one immutable visibility snapshot over the canonical project browser tree.
    /// </summary>
    public sealed class ProjectBrowserFilterPresentation
    {
        /// <summary>
        /// The canonical nodes visible in an active filter presentation.
        /// </summary>
        private readonly ImmutableHashSet<ProjectBrowserNodeViewModel> visibleNodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBrowserFilterPresentation" /> class.
        /// </summary>
        /// <param name="isActive">Whether filtering is active.</param>
        /// <param name="visibleNodes">The reference-identity set of visible canonical nodes.</param>
        private ProjectBrowserFilterPresentation(
            bool isActive,
            ImmutableHashSet<ProjectBrowserNodeViewModel> visibleNodes)
        {
            this.IsActive = isActive;
            this.visibleNodes = visibleNodes;
        }

        /// <summary>
        /// Gets the shared inactive presentation, for which every canonical node is visible.
        /// </summary>
        internal static ProjectBrowserFilterPresentation Inactive { get; } =
            new(
                false,
                ImmutableHashSet.Create<ProjectBrowserNodeViewModel>(ReferenceEqualityComparer.Instance));

        /// <summary>
        /// Gets a value indicating whether filtering is active.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Determines whether a canonical project browser node is visible.
        /// </summary>
        /// <param name="node">The canonical node.</param>
        /// <returns>
        /// <see langword="true" /> when filtering is inactive or the node belongs to the active visibility set;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool IsVisible(ProjectBrowserNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return !this.IsActive || this.visibleNodes.Contains(node);
        }

        /// <summary>
        /// Creates an active immutable presentation from canonical node identities.
        /// </summary>
        /// <param name="visibleNodes">The canonical nodes visible under the active filter.</param>
        /// <returns>The active immutable presentation.</returns>
        internal static ProjectBrowserFilterPresentation CreateActive(
            IEnumerable<ProjectBrowserNodeViewModel> visibleNodes)
        {
            ArgumentNullException.ThrowIfNull(visibleNodes);

            return new ProjectBrowserFilterPresentation(
                true,
                ImmutableHashSet.CreateRange<ProjectBrowserNodeViewModel>(
                    ReferenceEqualityComparer.Instance,
                    visibleNodes));
        }

        /// <summary>
        /// Determines whether another presentation has the same visibility semantics.
        /// </summary>
        /// <param name="other">The other presentation.</param>
        /// <returns><see langword="true" /> when both presentations expose the same canonical nodes.</returns>
        internal bool HasSameVisibilityAs(ProjectBrowserFilterPresentation other)
        {
            return ReferenceEquals(this, other)
                   || (other != null
                       && this.IsActive == other.IsActive
                       && (!this.IsActive || this.visibleNodes.SetEquals(other.visibleNodes)));
        }
    }
}
