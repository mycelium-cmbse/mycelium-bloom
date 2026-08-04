// ------------------------------------------------------------------------------------------------
// <copyright file="ElementSelectionService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Selection
{
    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides reactive, circuit-scoped SysML element selection state.
    /// </summary>
    public sealed class ElementSelectionService : ReactiveObject, IElementSelectionService
    {
        /// <summary>
        /// The currently selected SysML element.
        /// </summary>
        private IElement selectedElement;

        /// <inheritdoc />
        public IElement SelectedElement => this.selectedElement;

        /// <inheritdoc />
        public void SelectElement(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            this.SetSelectedElement(element);
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            this.SetSelectedElement(null);
        }

        /// <summary>
        /// Updates selection using object identity instead of value equality.
        /// </summary>
        /// <param name="element">The new selection, or <see langword="null" /> to clear it.</param>
        private void SetSelectedElement(IElement element)
        {
            if (ReferenceEquals(this.selectedElement, element))
            {
                return;
            }

            this.RaisePropertyChanging(nameof(this.SelectedElement));
            this.selectedElement = element;
            this.RaisePropertyChanged(nameof(this.SelectedElement));
        }
    }
}
