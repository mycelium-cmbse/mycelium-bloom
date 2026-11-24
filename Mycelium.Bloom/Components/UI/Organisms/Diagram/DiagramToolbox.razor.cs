// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramToolbox.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    using ReactiveUI;

    /// <summary>Renders the tab's applicable tool placements and delegates activation to their owner.</summary>
    public sealed partial class DiagramToolbox : BloomComponentBase, IDisposable
    {
        /// <summary>Owns observation of the currently borrowed tool state.</summary>
        private readonly SerialDisposable observation = new();

        /// <summary>The tool state currently observed by this component.</summary>
        private IDiagramToolState observed;

        /// <summary>Gets or sets the caller-owned diagram presentation.</summary>
        [Parameter, EditorRequired]
        public IDiagramViewModel ViewModel { get; set; }

        /// <summary>Observes the current toolbox, retained selection and editing policy.</summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            var tools = this.ViewModel.Tools;

            if (ReferenceEquals(this.observed, tools))
            {
                return;
            }

            this.observed = tools;
            this.observation.Disposable = tools.WhenAnyValue(state => state.Toolbox, state => state.Active, state => state.CanEdit)
                .Skip(1).Select(_ => Observable.FromAsync(() => this.InvokeAsync(() =>
                {
                    if (ReferenceEquals(this.observed, tools))
                    {
                        this.StateHasChanged();
                    }
                }))).Concat().Subscribe();
        }

        /// <summary>Detaches toolbox observation without disposing the caller-owned state.</summary>
        public void Dispose()
        {
            this.observed = null;
            this.observation.Dispose();
        }
    }
}
