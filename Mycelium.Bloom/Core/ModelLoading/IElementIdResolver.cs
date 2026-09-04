// ------------------------------------------------------------------------------------------------
// <copyright file="IElementIdResolver.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Resolves stable SysML element identifiers against the currently loaded model.
    /// </summary>
    public interface IElementIdResolver
    {
        /// <summary>
        /// Resolves an exact element identifier to its canonical loaded model object.
        /// </summary>
        /// <param name="elementId">The stable SysML element identifier.</param>
        /// <param name="cancellationToken">Cancels waiting for model resolution.</param>
        /// <returns>The canonical element, or <see langword="null" /> when the identifier is unresolved.</returns>
        ValueTask<IElement> ResolveAsync(string elementId, CancellationToken cancellationToken);
    }
}
