// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEndLabel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Combines optional role, multiplicity and adornment notation at a relationship end.</summary>
    /// <param name="Role">The optional end role name.</param>
    /// <param name="Multiplicity">The optional SDK multiplicity notation.</param>
    /// <param name="Adornments">The optional end adornments.</param>
    public sealed record DiagramEndLabel(string Role = null, string Multiplicity = null, string Adornments = null)
    {

        /// <summary>Formats the supplied end notation without inventing absent labels.</summary>
        /// <returns>The nonempty end labels separated by spaces.</returns>
        public override string ToString() => string.Join(" ", new[] { this.Role, this.Multiplicity, this.Adornments }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
