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
    using System.Globalization;

    using Humanizer;

    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Holds one row of the projects list: the name, description, visibility, mode, lifecycle and number of members of a
    /// project, and how long ago it was updated. The words and icons of the lifecycle, visibility and mode are the view's.
    /// </summary>
    public sealed class ProjectRowViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectRowViewModel" /> class.
        /// </summary>
        /// <param name="project">The project to show.</param>
        /// <param name="now">The current time, from which the last update is told.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="project" /> is <see langword="null" />.</exception>
        public ProjectRowViewModel(FunctionalProject project, DateTimeOffset now)
        {
            ArgumentNullException.ThrowIfNull(project);

            this.EngineeringProjectId = project.EngineeringProjectId;
            this.Name = project.Name;
            this.Description = project.Description;
            this.Visibility = project.Visibility;
            this.Mode = project.CurrentMode;
            this.MemberCount = project.Involves.Count;
            this.Lifecycle = project.Lifecycle;
            this.UpdatedText = new DateTimeOffset(DateTime.SpecifyKind(project.UpdatedOn, DateTimeKind.Utc)).Humanize(now, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the identifier of the project on the model server, which the modelling workspace opens.
        /// </summary>
        public Guid EngineeringProjectId { get; }

        /// <summary>
        /// Gets the name of the project.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the short description of the project.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets who can see the project.
        /// </summary>
        public ProjectVisibility Visibility { get; }

        /// <summary>
        /// Gets how the members work on the project.
        /// </summary>
        public ProjectMode Mode { get; }

        /// <summary>
        /// Gets the number of members of the project.
        /// </summary>
        public int MemberCount { get; }

        /// <summary>
        /// Gets the lifecycle state of the project.
        /// </summary>
        public ProjectLifecycleKind Lifecycle { get; }

        /// <summary>
        /// Gets how long ago the project was updated, in words.
        /// </summary>
        public string UpdatedText { get; }
    }
}
