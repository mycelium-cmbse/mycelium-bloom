// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceRoutes.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Session
{
    /// <summary>
    /// Builds the address of the modelling workspace for one project, so that a reload or a shared link opens the same project.
    /// </summary>
    public static class WorkspaceRoutes
    {
        /// <summary>
        /// The name of the query parameter that holds the identifier of the opened project.
        /// </summary>
        public const string ProjectParameter = "project";

        /// <summary>
        /// The name of the query parameter that holds the slug of the organization the project was opened from.
        /// </summary>
        public const string OrganizationParameter = "organization";

        /// <summary>
        /// The name of the query parameter of the start page that holds the project to open once the connection is open.
        /// </summary>
        public const string ReturnProjectParameter = "returnProject";

        /// <summary>
        /// The route of the modelling workspace.
        /// </summary>
        private const string ModelingPath = "/workspace/modeling";

        /// <summary>
        /// The route of the start page, which opens a connection.
        /// </summary>
        private const string StartPath = "/";

        /// <summary>
        /// Gets the address of the start page that comes back to the workspace of one project once the connection is open, used when
        /// a workspace address is opened without a connection.
        /// </summary>
        /// <param name="projectId">The identifier of the project on the server.</param>
        /// <returns>The relative address of the start page.</returns>
        public static string GetConnectionPath(Guid projectId)
        {
            return $"{StartPath}?{ReturnProjectParameter}={projectId}";
        }

        /// <summary>
        /// Gets the address of the modelling workspace for one project, opened from the Home page or from an organization.
        /// </summary>
        /// <param name="projectId">The identifier of the project on the server.</param>
        /// <param name="organizationSlug">The slug of the organization the project was opened from, or <see langword="null" /> from the Home page.</param>
        /// <returns>The relative address of the workspace.</returns>
        public static string GetProjectPath(Guid projectId, string organizationSlug = null)
        {
            var path = $"{ModelingPath}?{ProjectParameter}={projectId}";

            return string.IsNullOrEmpty(organizationSlug) ? path : $"{path}&{OrganizationParameter}={Uri.EscapeDataString(organizationSlug)}";
        }
    }
}
