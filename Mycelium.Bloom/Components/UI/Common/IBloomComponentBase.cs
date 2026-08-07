// ------------------------------------------------------------------------------------------------
// <copyright file="IBloomComponentBase.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Common
{
    /// <summary>
    /// Defines the parameters shared by reusable Bloom UI components.
    /// </summary>
    public interface IBloomComponentBase
    {
        /// <summary>
        /// Gets or sets additional CSS classes applied to the component root element.
        /// </summary>
        string Class { get; set; }

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the component root element.
        /// </summary>
        IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }
    }
}
