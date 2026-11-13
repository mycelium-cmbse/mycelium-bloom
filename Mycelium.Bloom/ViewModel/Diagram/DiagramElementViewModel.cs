// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramElementViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Projects selection for one graphical occurrence without observing or owning the canonical selection service.</summary>
    public abstract class DiagramElementViewModel : BloomBaseViewModel
    {
        /// <summary>The derived selection decoration for this occurrence.</summary>
        private bool isSelected;

        /// <summary>Gets the exact canonical object represented by this occurrence.</summary>
        public IElement Element { get; }

        /// <summary>Gets whether this occurrence represents the circuit's selected canonical object.</summary>
        public bool IsSelected
        {
            get => this.isSelected;
            private set => this.RaiseAndSetIfChanged(ref this.isSelected, value);
        }

        /// <summary>Gets the tab-owned state and action authority borrowed by this presentation.</summary>
        protected IGeneralDiagramViewModel Owner { get; }

        /// <summary>Creates an occurrence-scoped presentation over borrowed canonical data.</summary>
        /// <param name="element">The canonical object, independent of its graphical occurrence identity.</param>
        /// <param name="owner">The tab-owned action authority.</param>
        protected DiagramElementViewModel(IElement element, IGeneralDiagramViewModel owner)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(owner);
            this.Element = element;
            this.Owner = owner;
        }

        /// <summary>Delegates selection to the tab and its canonical selection contract.</summary>
        public void Select() => this.Owner.Select(this.Element);

        /// <summary>Applies a selection decoration calculated by the tab's single selection subscription.</summary>
        /// <param name="selected">Whether the canonical object is selected.</param>
        internal void SetSelected(bool selected) => this.IsSelected = selected;
    }
}
