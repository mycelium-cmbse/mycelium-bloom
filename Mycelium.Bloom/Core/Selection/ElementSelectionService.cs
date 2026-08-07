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
    using System.Diagnostics.CodeAnalysis;

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
        [AllowNull]
        [MaybeNull]
        private IElement selectedElement;

        /// <inheritdoc />
        [AllowNull]
        [MaybeNull]
        public IElement SelectedElement
        {
            get => this.selectedElement;
            set
            {
                if (ReferenceEquals(this.selectedElement, value))
                {
                    return;
                }

                this.RaisePropertyChanging(nameof(this.SelectedElement));
                this.selectedElement = value;
                this.RaisePropertyChanged(nameof(this.SelectedElement));
            }
        }
    }
}
