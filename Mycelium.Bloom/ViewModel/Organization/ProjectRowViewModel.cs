// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectRowViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Holds what one row of the projects list shows, already turned into words.
    /// </summary>
    public sealed class ProjectRowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectRowViewModel" /> class.
        /// </summary>
        /// <param name="project">The project that the row shows.</param>
        /// <param name="now">The current moment, from which the last update is told.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="project" /> is <see langword="null" />.</exception>
        public ProjectRowViewModel(ProjectSummary project, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(project);

            this.Name = project.Name;
            this.Description = project.Description;
            this.VisibilityLabel = ProjectLabelFormatter.GetVisibilityLabel(project.Visibility);
            this.VisibilityIcon = ProjectLabelFormatter.GetVisibilityIcon(project.Visibility);
            this.ModeLabel = ProjectLabelFormatter.GetModeLabel(project.Mode);
            this.IsConcurrentDesign = project.Mode == ProjectMode.ConcurrentDesign;
            this.MemberCount = project.MemberCount;
            this.StatusLabel = ProjectLabelFormatter.GetLifecycleLabel(project.LifecycleState);
            this.LifecycleState = project.LifecycleState;
            this.UpdatedText = RelativeTimeFormatter.Format(project.UpdatedAt, now);
        }

        /// <summary>
        /// Gets the name of the project.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the project.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the label of the visibility of the project.
        /// </summary>
        public string VisibilityLabel { get; }

        /// <summary>
        /// Gets the name of the icon of the visibility of the project.
        /// </summary>
        public string VisibilityIcon { get; }

        /// <summary>
        /// Gets the label of the mode of the project.
        /// </summary>
        public string ModeLabel { get; }

        /// <summary>
        /// Gets a value indicating whether the mode of the project is concurrent design, which has its own colour.
        /// </summary>
        public bool IsConcurrentDesign { get; }

        /// <summary>
        /// Gets the number of members of the project.
        /// </summary>
        public int MemberCount { get; }

        /// <summary>
        /// Gets the label of the lifecycle state of the project.
        /// </summary>
        public string StatusLabel { get; }

        /// <summary>
        /// Gets the lifecycle state of the project, which gives its status dot its colour.
        /// </summary>
        public ProjectLifecycleState LifecycleState { get; }

        /// <summary>
        /// Gets how long ago the project was updated, in words.
        /// </summary>
        public string UpdatedText { get; }
    }
}
