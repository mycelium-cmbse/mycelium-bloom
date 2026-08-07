// ------------------------------------------------------------------------------------------------
// <copyright file="IElementSelectionService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Selection
{
    using System.Diagnostics.CodeAnalysis;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides the circuit-scoped source of truth for the selected SysML element.
    /// </summary>
    public interface IElementSelectionService : IReactiveObject
    {
        /// <summary>
        /// Gets or sets the currently selected SysML element, or <see langword="null" /> when no element is selected.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        IElement SelectedElement { get; set; }
    }
}
