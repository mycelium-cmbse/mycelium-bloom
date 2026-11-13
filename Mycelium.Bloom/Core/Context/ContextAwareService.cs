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

    using Mycelium.SDK;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides reactive, circuit-scoped application context state.
    /// </summary>
    public sealed class ContextAwareService : ReactiveObject, IContextAwareService
    {
        /// <summary>Gets or sets the canonical main selection, clearing reference inspection when it changes.</summary>
        public IElement SelectedElement
        {
            get;
            set
            {
                if (ReferenceEquals(field, value))
                {
                    return;
                }

                this.InspectedElement = null;
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
    }
}
