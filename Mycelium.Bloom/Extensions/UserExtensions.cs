// ------------------------------------------------------------------------------------------------
// <copyright file="UserExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using Mycelium.SDK.POCO;

    /// <summary>
    /// Gives what the lists show about a user: the initials and the colour of the avatar.
    /// </summary>
    public static class UserExtensions
    {
        /// <summary>
        /// The number of avatar colours that the design defines.
        /// </summary>
        private const int AvatarColorCount = 12;

        /// <summary>
        /// The most letters that the initials of a person hold.
        /// </summary>
        private const int MaximumInitials = 2;

        /// <summary>
        /// Defines what the lists show about a user, whatever type of the SDK describes them.
        /// </summary>
        /// <param name="user">The user whose initials and avatar colour are read.</param>
        extension(IUser user)
        {
            /// <summary>
            /// Gets the initials of a person from the first letters of the first two words of the name, or the first letter of
            /// the mail when the name is empty.
            /// </summary>
            /// <returns>The initials in upper case.</returns>
            /// <exception cref="ArgumentNullException">Thrown when the user is <see langword="null" />.</exception>
            public string GetInitials()
            {
                ArgumentNullException.ThrowIfNull(user);

                var words = user.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (words.Length == 0)
                {
                    return user.Email.Length == 0 ? string.Empty : char.ToUpperInvariant(user.Email[0]).ToString();
                }

                return string.Concat(words.Take(MaximumInitials).Select(word => char.ToUpperInvariant(word[0])));
            }

            /// <summary>
            /// Gets the number, from 1, of the avatar colour of a person. It comes from the mail, so that a person keeps the same
            /// colour on every screen.
            /// </summary>
            /// <returns>The number of the colour.</returns>
            /// <exception cref="ArgumentNullException">Thrown when the user is <see langword="null" />.</exception>
            public int GetAvatarColor()
            {
                ArgumentNullException.ThrowIfNull(user);

                return (int)(user.Email.Aggregate(0u, (sum, letter) => unchecked(sum + letter)) % AvatarColorCount) + 1;
            }
        }
    }
}
