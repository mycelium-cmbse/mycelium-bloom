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
    using System.ComponentModel;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides the circuit-scoped source of truth for the selected SysML element.
    /// </summary>
    public interface IElementSelectionService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the currently selected SysML element, or <see langword="null" /> when no element is selected.
        /// </summary>
        IElement SelectedElement { get; }

        /// <summary>
        /// Selects a SysML element.
        /// </summary>
        /// <param name="element">The element to select.</param>
        void SelectElement(IElement element);

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        void ClearSelection();
    }
}
