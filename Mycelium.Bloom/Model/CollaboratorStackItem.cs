// ------------------------------------------------------------------------------------------------
// <copyright file="CollaboratorStackItem.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Represents a collaborator shown in a compact avatar stack.
    /// </summary>
    public class CollaboratorStackItem
    {
        /// <summary>
        /// Gets or sets the collaborator identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collaborator display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the initials shown in the avatar.
        /// </summary>
        public string Initials { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the avatar background color.
        /// </summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collaborator role.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the collaborator is online.
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this collaborator is the current user.
        /// </summary>
        public bool IsCurrentUser { get; set; }
    }
}
