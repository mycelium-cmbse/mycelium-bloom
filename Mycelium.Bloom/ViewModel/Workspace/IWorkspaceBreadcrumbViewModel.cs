// ------------------------------------------------------------------------------------------------
// <copyright file="IWorkspaceBreadcrumbViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Workspace
{
    using System.ComponentModel;

    using ErrorOr;

    using Organization = Mycelium.SDK.POCO.Organization;

    /// <summary>
    /// Defines the breadcrumb of the workspace header: the project opened in the workspace and the organization it was opened
    /// from.
    /// </summary>
    public interface IWorkspaceBreadcrumbViewModel : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets a value indicating whether a connection is open, without which no project can be opened.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the errors of the last opening: a project the server does not know, or a server that cannot be reached.
        /// </summary>
        IReadOnlyList<Error> Errors { get; }

        /// <summary>
        /// Gets the opened project, or <see langword="null" /> before it is read.
        /// </summary>
        Project Project { get; }

        /// <summary>
        /// Gets the organization the project was opened from, or <see langword="null" /> when it was opened from the Home page.
        /// </summary>
        Organization Organization { get; }

        /// <summary>
        /// Reads the project and, when it was opened from an organization, that organization.
        /// </summary>
        /// <param name="projectId">The identifier of the project on the server.</param>
        /// <param name="organizationSlug">The slug of the organization, or <see langword="null" /> from the Home page.</param>
        /// <param name="cancellationToken">The token that cancels the reading when the breadcrumb is removed.</param>
        /// <returns>The completion of the reading.</returns>
        Task OpenAsync(Guid projectId, string organizationSlug, CancellationToken cancellationToken);
    }
}
