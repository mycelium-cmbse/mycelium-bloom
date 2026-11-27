// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaContext.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Hypha
{
    using AGUI.Abstractions;

    /// <summary>
    /// What Bloom tells Hypha about the user's situation with each question.
    /// </summary>
    /// <param name="ProjectId">The identifier of the open project.</param>
    /// <param name="UserId">The identifier of the current user.</param>
    /// <param name="UserName">The display name of the current user.</param>
    /// <param name="SelectedElement">
    /// The selected element as "name (metaclass, id)", or <see langword="null" /> when nothing is selected.
    /// </param>
    public sealed record HyphaContext(string ProjectId, string UserId, string UserName, string SelectedElement)
    {
        /// <summary>
        /// Gets the conversation identifier: Fabric keeps one conversation per user and project.
        /// </summary>
        public string ThreadId => $"{this.UserId}:{this.ProjectId}";

        /// <summary>
        /// Converts this context to the AG-UI context entries sent with a question.
        /// </summary>
        /// <returns>The context entries, in a stable order.</returns>
        public IList<AGUIContext> ToAGUIContext()
        {
            return
            [
                new AGUIContext { Description = "Project", Value = this.ProjectId },
                new AGUIContext { Description = "User", Value = $"{this.UserName} (id {this.UserId})" },
                new AGUIContext { Description = "Selected element", Value = this.SelectedElement ?? "none" }
            ];
        }
    }
}
