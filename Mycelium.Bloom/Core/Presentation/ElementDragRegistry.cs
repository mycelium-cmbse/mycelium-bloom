// ------------------------------------------------------------------------------------------------
// <copyright file="ElementDragRegistry.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Presentation
{
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Resolves mounted Browser drag tokens to borrowed canonical objects within one circuit.</summary>
    public sealed class ElementDragRegistry : IElementDragRegistry
    {
        /// <summary>The mounted DOM sources, released by their owning components.</summary>
        private readonly Dictionary<Guid, IElement> sources = new();

        /// <summary>Registers one mounted Browser occurrence without using semantic IDs as drag identities.</summary>
        /// <param name="token">The unique DOM source token.</param>
        /// <param name="element">The exact canonical element borrowed from the Browser.</param>
        public void Register(Guid token, IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            this.sources.Add(token, element);
        }

        /// <summary>Releases a mounted source so stale browser payloads cannot resolve it.</summary>
        /// <param name="token">The source token being unmounted.</param>
        public void Remove(Guid token) => this.sources.Remove(token);

        /// <summary>Resolves an opaque source token without loading another model or changing selection.</summary>
        /// <param name="token">The browser payload token.</param>
        /// <returns>The mounted canonical object, or null for an unavailable source.</returns>
        public IElement Resolve(string token) => Guid.TryParse(token, out var id) && this.sources.TryGetValue(id, out var element) ? element : null;
    }
}
