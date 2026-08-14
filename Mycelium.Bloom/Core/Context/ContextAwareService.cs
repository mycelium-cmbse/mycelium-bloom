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
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides reactive, circuit-scoped application context state.
    /// </summary>
    public sealed class ContextAwareService : ReactiveObject, IContextAwareService
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        public ProjectLifecycleState LifecycleState
        {
            get;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                this.RaiseAndSetIfChanged(ref field, value);
            }
        } = ProjectLifecycleState.Preparation;
    }
}
