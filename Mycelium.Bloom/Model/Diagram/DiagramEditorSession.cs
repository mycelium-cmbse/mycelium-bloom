// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEditorSession.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Retains a tab's registered editor descriptor and borrowed component parameters.</summary>
    public sealed class DiagramEditorSession
    {
        /// <summary>Gets the registered editor identity and component.</summary>
        public DiagramEditorDescriptor Descriptor { get; }

        /// <summary>Gets the presentation state disposed by the owning editor tab.</summary>
        public IDiagramViewModel ViewModel { get; }

        /// <summary>Gets stable framework parameters for the current tab lifetime.</summary>
        public IDictionary<string, object> Parameters { get; }

        /// <summary>Captures the state and descriptor owned by one tab.</summary>
        /// <param name="descriptor">The registered diagram editor.</param>
        /// <param name="viewModel">The tab-owned reactive state.</param>
        public DiagramEditorSession(DiagramEditorDescriptor descriptor, IDiagramViewModel viewModel)
        {
            this.Descriptor = descriptor;
            this.ViewModel = viewModel;
            this.Parameters = new Dictionary<string, object> { [nameof(this.ViewModel)] = viewModel };
        }
    }
}
