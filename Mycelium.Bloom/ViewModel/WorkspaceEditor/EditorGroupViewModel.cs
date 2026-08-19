// ------------------------------------------------------------------------------------------------
// <copyright file="EditorGroupViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.WorkspaceEditor
{
    using System.Collections.ObjectModel;

    using Mycelium.Bloom.Model;

    using ReactiveUI;

    /// <summary>
    /// Owns the ordered tabs and active-tab state for one editor group.
    /// </summary>
    public sealed class EditorGroupViewModel : ReactiveObject
    {
        /// <summary>
        /// The mutable tab collection owned exclusively by this editor group.
        /// </summary>
        private readonly ObservableCollection<EditorTabItem> tabs = [];

        /// <summary>
        /// The stable read-only projection of <see cref="tabs" />.
        /// </summary>
        private readonly ReadOnlyObservableCollection<EditorTabItem> readOnlyTabs;

        /// <summary>
        /// The active tab, or <see langword="null" /> when this group is empty.
        /// </summary>
        private EditorTabItem activeTab;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorGroupViewModel" /> class.
        /// </summary>
        internal EditorGroupViewModel()
        {
            this.Id = Guid.NewGuid();
            this.readOnlyTabs = new ReadOnlyObservableCollection<EditorTabItem>(this.tabs);
        }

        /// <summary>
        /// Gets the stable identity of this editor group.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the ordered, read-only tab collection.
        /// </summary>
        public ReadOnlyObservableCollection<EditorTabItem> Tabs => this.readOnlyTabs;

        /// <summary>
        /// Gets the active tab, or <see langword="null" /> when this group is empty.
        /// </summary>
        public EditorTabItem ActiveTab
        {
            get => this.activeTab;
            private set => this.RaiseAndSetIfChanged(ref this.activeTab, value);
        }

        /// <summary>
        /// Appends a tab and makes it active in this group.
        /// </summary>
        /// <param name="tab">The tab instance transferred to this group.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="tab" /> is <see langword="null" />.
        /// </exception>
        internal void AddTab(EditorTabItem tab)
        {
            ArgumentNullException.ThrowIfNull(tab);

            this.tabs.Add(tab);
            this.ActiveTab = tab;
        }

        /// <summary>
        /// Attempts to make an owned tab active.
        /// </summary>
        /// <param name="tabId">The identity of the tab to activate.</param>
        /// <returns>
        /// <see langword="true" /> when the tab belongs to this group; otherwise, <see langword="false" />.
        /// </returns>
        internal bool TryActivateTab(Guid tabId)
        {
            if (!this.TryGetTab(tabId, out var tab))
            {
                return false;
            }

            this.ActiveTab = tab;

            return true;
        }

        /// <summary>
        /// Attempts to retrieve an owned tab by identity.
        /// </summary>
        /// <param name="tabId">The identity of the tab to retrieve.</param>
        /// <param name="tab">
        /// The matching tab when found; otherwise, <see langword="null" />.
        /// </param>
        /// <returns>
        /// <see langword="true" /> when the tab belongs to this group; otherwise, <see langword="false" />.
        /// </returns>
        internal bool TryGetTab(Guid tabId, out EditorTabItem tab)
        {
            var tabIndex = this.FindTabIndex(tabId);

            if (tabIndex < 0)
            {
                tab = null;

                return false;
            }

            tab = this.tabs[tabIndex];

            return true;
        }

        /// <summary>
        /// Attempts to remove an owned tab and reconciles active-tab state when necessary.
        /// </summary>
        /// <param name="tabId">The identity of the tab to remove.</param>
        /// <param name="removedTab">
        /// The removed tab when found; otherwise, <see langword="null" />.
        /// </param>
        /// <returns>
        /// <see langword="true" /> when the tab was removed; otherwise, <see langword="false" />.
        /// </returns>
        internal bool TryRemoveTab(Guid tabId, out EditorTabItem removedTab)
        {
            var tabIndex = this.FindTabIndex(tabId);

            if (tabIndex < 0)
            {
                removedTab = null;

                return false;
            }

            removedTab = this.tabs[tabIndex];
            var wasActive = ReferenceEquals(this.ActiveTab, removedTab);

            this.tabs.RemoveAt(tabIndex);

            if (wasActive)
            {
                this.ActiveTab = this.tabs.Count == 0
                    ? null
                    : this.tabs[Math.Min(tabIndex, this.tabs.Count - 1)];
            }

            return true;
        }

        /// <summary>
        /// Finds an owned tab's position without exposing the mutable collection.
        /// </summary>
        /// <param name="tabId">The identity of the tab to locate.</param>
        /// <returns>The zero-based tab index when found; otherwise, <c>-1</c>.</returns>
        private int FindTabIndex(Guid tabId)
        {
            if (tabId == Guid.Empty)
            {
                return -1;
            }

            for (var index = 0; index < this.tabs.Count; index++)
            {
                if (this.tabs[index].Id == tabId)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
