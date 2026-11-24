// ------------------------------------------------------------------------------------------------
// <copyright file="IElementDragRegistry.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Presentation
{
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Connects mounted browser sources to their borrowed canonical elements within a workspace.</summary>
    public interface IElementDragRegistry
    {
        /// <summary>Registers a canonical source for one mounted row.</summary>
        /// <param name="token">The opaque mounted source identity.</param>
        /// <param name="element">The canonical browser element.</param>
        void Register(Guid token, IElement element);

        /// <summary>Releases a mounted source.</summary>
        /// <param name="token">The source identity to release.</param>
        void Remove(Guid token);

        /// <summary>Resolves a live source without loading or creating semantic elements.</summary>
        /// <param name="token">The opaque browser payload.</param>
        /// <returns>The canonical element, or null for an unavailable source.</returns>
        IElement Resolve(string token);
    }
}
