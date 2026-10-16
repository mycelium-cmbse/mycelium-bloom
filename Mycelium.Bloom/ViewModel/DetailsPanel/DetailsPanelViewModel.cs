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
    using System.Reactive.Subjects;
    using DynamicData;
    using Mycelium.Bloom.Core.Selection;
    using ReactiveUI;
    using ReactiveUI.Primitives.Concurrency;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Projects shared selection into cached panel state without owning model objects.</summary>
    public sealed class DetailsPanelViewModel : BloomBaseViewModel, IDetailsPanelViewModel
    {
        /// <summary>The circuit-owned selection authority.</summary>
        private readonly IElementSelectionService selection;

        /// <summary>Requests explicit refreshes without changing selection identity.</summary>
        private readonly Subject<IElement> invalidations = new();

        /// <summary>Owns the projection subscription for this panel lifetime.</summary>
        private readonly CompositeDisposable subscriptions = new();

        /// <summary>Publishes one coherent presentation for each relevant change.</summary>
        private readonly ObservableAsPropertyHelper<DetailsPanelPresentation> presentation;

        /// <summary>Retains at most thirty-two canonical predecessors for this panel.</summary>
        private readonly SourceList<IElement> history = new();

        /// <summary>Publishes Back availability from the panel-owned trail.</summary>
        private readonly ObservableAsPropertyHelper<bool> canGoBack;

        /// <summary>Identifies the synchronous selection notification requested by this panel.</summary>
        private IElement inspectionTarget;

        /// <summary>Observes the existing shared selection and initializes its presentation.</summary>
        /// <param name="selection">The circuit-owned selection authority.</param>
        public DetailsPanelViewModel(IElementSelectionService selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            this.selection = selection;
            this.canGoBack = this.history.CountChanged.Select(count => count > 0)
                .ToProperty(this, viewModel => viewModel.CanGoBack, scheduler: Sequencer.Immediate);
            this.subscriptions.Add(this.canGoBack);
            this.subscriptions.Add(this.history);
            this.presentation = selection.WhenAnyValue(context => context.SelectedElement)
                .DistinctUntilChanged<IElement>(ReferenceEqualityComparer.Instance)
                .Do(this.ObserveSelection)
                .Merge(this.invalidations)
                .Select(element => element is null ? null : new DetailsPanelPresentation(element))
                .ToProperty(this, viewModel => viewModel.Presentation, scheduler: Sequencer.Immediate);
            this.subscriptions.Add(this.presentation);
            this.subscriptions.Add(this.invalidations);
        }

        /// <summary>Gets the cached presentation of the selected canonical element.</summary>
        public DetailsPanelPresentation Presentation => this.presentation.Value;

        /// <summary>Gets whether this panel can return to an earlier inspected element.</summary>
        public bool CanGoBack => this.canGoBack.Value;

        /// <summary>Updates shared selection without changing any browser's local state.</summary>
        /// <param name="element">The canonical target to inspect.</param>
        public void Inspect(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            if (this.subscriptions.IsDisposed || ReferenceEquals(element, this.selection.SelectedElement)) return;
            var previous = this.selection.SelectedElement;
            this.history.Edit(items =>
            {
                var index = Enumerable.Range(0, items.Count).FirstOrDefault(i => ReferenceEquals(items[i], element), -1);
                if (index >= 0) items.RemoveRange(index, items.Count - index);
                else if (previous is not null)
                {
                    if (items.Count == 32) items.RemoveAt(0);
                    items.Add(previous);
                }
            });
            this.SelectInspectedElement(element);
        }

        /// <summary>Restores the previous canonical selection without adding a forward entry.</summary>
        public void GoBack()
        {
            if (this.subscriptions.IsDisposed || this.history.Count == 0) return;
            var previous = this.history.Items[this.history.Count - 1];
            this.history.Edit(items => items.RemoveAt(items.Count - 1));
            this.SelectInspectedElement(previous);
        }

        /// <summary>Starts a new external inspection context without changing canonical selection.</summary>
        public void ResetInspectionHistory()
        {
            if (!this.subscriptions.IsDisposed) this.history.Clear();
        }

        /// <summary>Distinguishes this panel's one-shot Inspect intention from external selection.</summary>
        private void SelectInspectedElement(IElement element)
        {
            this.inspectionTarget = element;
            try { this.selection.SelectedElement = element; }
            finally { this.inspectionTarget = null; }
        }

        /// <summary>Clears unrelated inspection trails before publishing an external selection.</summary>
        private void ObserveSelection(IElement element)
        {
            if (element is null || !ReferenceEquals(element, this.inspectionTarget)) this.history.Clear();
            this.inspectionTarget = null;
        }

        /// <summary>Clears the canonical selection without changing workspace region visibility.</summary>
        public void ClearSelection()
        {
            if (this.subscriptions.IsDisposed) return;
            this.history.Clear();
            this.selection.SelectedElement = null;
        }

        /// <summary>Refreshes the selected element without requiring a selection change.</summary>
        public void Refresh()
        {
            if (this.subscriptions.IsDisposed) return;
            this.invalidations.OnNext(this.selection.SelectedElement);
        }

        /// <summary>Detaches the panel from shared state without disposing the shared authority.</summary>
        public void Dispose()
        {
            this.subscriptions.Dispose();
        }
    }
}
