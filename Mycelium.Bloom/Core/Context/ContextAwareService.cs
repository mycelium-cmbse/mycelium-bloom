// ------------------------------------------------------------------------------------------------
// <copyright file="ContextAwareService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Context
{
    using System.ComponentModel;
    using System.Reactive.Linq;

    using Mycelium.SDK;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides reactive, circuit-scoped application context state.
    /// </summary>
    public sealed class ContextAwareService : ReactiveObject, IContextAwareService, IDisposable
    {
        /// <summary>Owns inspection clearing for this circuit's selection changes.</summary>
        private readonly IDisposable selectionChanges;

        /// <summary>Gets or sets the canonical main selection using reference identity.</summary>
        public IElement SelectedElement
        {
            get;
            set
            {
                if (ReferenceEquals(field, value))
                {
                    return;
                }

                this.RaisePropertyChanging(nameof(this.SelectedElement));
                field = value;
                this.RaisePropertyChanged(nameof(this.SelectedElement));
            }
        }

        /// <summary>Gets or sets the canonical reference inspected independently of main selection.</summary>
        public IElement InspectedElement
        {
            get;
            set => this.RaiseAndSetIfChanged(ref field, value);
        }

        /// <summary>Gets or sets the validated project lifecycle state for this circuit.</summary>
        public ProjectLifecycleKind LifecycleState
        {
            get;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ProjectLifecycleKind));
                }

                this.RaiseAndSetIfChanged(ref field, value);
            }
        } = ProjectLifecycleKind.Preparation;

        /// <summary>Observes canonical selection changes for the lifetime of this context.</summary>
        public ContextAwareService()
        {
            this.selectionChanges = this.Changing
                .Where(change => change.PropertyName == nameof(this.SelectedElement))
                .Subscribe(_ => this.InspectedElement = null);
        }

        /// <summary>Detaches inspection clearing when the owning circuit ends.</summary>
        public void Dispose()
        {
            this.selectionChanges.Dispose();
        }
    }
}
