// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceUrlContextRestoration.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Represents selected-element context derived from one authoritative browser location.
    /// </summary>
    public sealed class WorkspaceUrlContextRestoration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceUrlContextRestoration" /> class.
        /// </summary>
        /// <param name="selectedElement">The resolved canonical element, or <see langword="null" />.</param>
        /// <param name="canonicalUri">The replacement URI required for canonical input, or <see langword="null" />.</param>
        /// <param name="shouldFocusSelectedElement">Whether this browser transition requests local presentation focus.</param>
        public WorkspaceUrlContextRestoration(
            IElement selectedElement,
            string canonicalUri,
            bool shouldFocusSelectedElement)
        {
            this.SelectedElement = selectedElement;
            this.CanonicalUri = canonicalUri;
            this.ShouldFocusSelectedElement = shouldFocusSelectedElement;
        }

        /// <summary>
        /// Gets the resolved canonical element, or <see langword="null" /> when no selection can be restored.
        /// </summary>
        public IElement SelectedElement { get; }

        /// <summary>
        /// Gets the URI that removes ambiguity or invalid input, or <see langword="null" /> when already canonical.
        /// </summary>
        public string CanonicalUri { get; }

        /// <summary>
        /// Gets a value indicating whether the restored selection should receive local presentation focus.
        /// </summary>
        public bool ShouldFocusSelectedElement { get; }
    }
}
