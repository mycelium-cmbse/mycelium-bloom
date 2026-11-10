// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.DetailsPanel
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Mycelium.Bloom.Core.Selection;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Projects shared selection into cached panel state without owning model objects.</summary>
    public sealed class DetailsPanelViewModel : BloomBaseViewModel, IDetailsPanelViewModel
    {
        /// <summary>The circuit-owned selection authority.</summary>
        private readonly IElementSelectionService selection;

        /// <summary>Owns the existing selection observation for this panel lifetime.</summary>
        private readonly CompositeDisposable subscriptions = new();

        /// <summary>The current coherent presentation snapshot.</summary>
        private DetailsPanelPresentation presentation;

        /// <summary>Gets the cached presentation of the selected canonical element.</summary>
        public DetailsPanelPresentation Presentation
        {
            get => this.presentation;
            private set => this.RaiseAndSetIfChanged(ref this.presentation, value);
        }

        /// <summary>Observes shared selection and initializes the presentation.</summary>
        /// <param name="selection">The circuit-owned selection authority.</param>
        public DetailsPanelViewModel(IElementSelectionService selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            this.selection = selection;
            this.subscriptions.Add(selection.WhenAnyValue(context => context.SelectedElement)
                .DistinctUntilChanged<IElement>(ReferenceEqualityComparer.Instance)
                .Subscribe(_ => this.Refresh()));
        }

        /// <summary>
        /// Selects the supplied canonical element while this panel's selection subscription is active.
        /// </summary>
        /// <param name="element">The canonical SDK element to inspect.</param>
        /// <exception cref="ArgumentNullException">The supplied element is null.</exception>
        public void Inspect(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            if (!this.subscriptions.IsDisposed)
            {
                this.selection.SelectedElement = element;
            }
        }

        /// <summary>
        /// Clears the shared selection while this panel's selection subscription is active.
        /// </summary>
        public void ClearSelection()
        {
            if (!this.subscriptions.IsDisposed)
            {
                this.selection.SelectedElement = null;
            }
        }

        /// <summary>
        /// Rebuilds the cached presentation from the shared selection, or clears it when nothing is selected.
        /// </summary>
        public void Refresh()
        {
            if (!this.subscriptions.IsDisposed)
            {
                var element = this.selection.SelectedElement;
                this.Presentation = element is null ? null : new DetailsPanelPresentation(element);
            }
        }

        /// <summary>
        /// Releases selection observation without disposing the shared service or canonical model objects.
        /// </summary>
        public void Dispose()
        {
            this.subscriptions.Dispose();
        }
    }
}
