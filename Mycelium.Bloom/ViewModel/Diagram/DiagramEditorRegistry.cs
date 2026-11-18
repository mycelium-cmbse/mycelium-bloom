// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEditorRegistry.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Microsoft.Extensions.Hosting;

    using Mycelium.Bloom.Model.Diagram;

    /// <summary>Exposes registered editor kinds allowed in the current hosting environment.</summary>
    public sealed class DiagramEditorRegistry
    {
        /// <summary>Gets the immutable editor inventory in registration order.</summary>
        public ImmutableArray<DiagramEditorDescriptor> Editors { get; }

        /// <summary>Builds the available editor inventory without constructing tab-owned state.</summary>
        /// <param name="descriptors">The registered editor kinds.</param>
        /// <param name="environment">The optional host environment; absent environments expose production editors only.</param>
        /// <exception cref="ArgumentException">Editor action or view-type identities are duplicated.</exception>
        public DiagramEditorRegistry(IEnumerable<DiagramEditorDescriptor> descriptors, IHostEnvironment environment = null)
        {
            ArgumentNullException.ThrowIfNull(descriptors);
            this.Editors = descriptors.Where(descriptor => !descriptor.IsDevelopmentOnly || environment?.IsDevelopment() == true).ToImmutableArray();

            if (this.Editors.Select(editor => editor.ActionId).Distinct(StringComparer.Ordinal).Count() != this.Editors.Length
                || this.Editors.Select(editor => editor.ViewTypeKey).Distinct(StringComparer.Ordinal).Count() != this.Editors.Length)
            {
                throw new ArgumentException("Diagram editor action and view-type identities must be unique.", nameof(descriptors));
            }
        }

        /// <summary>Finds the registered editor accepting an add-tab action.</summary>
        /// <param name="actionId">The action identity.</param>
        /// <returns>The matching descriptor, or null when unavailable.</returns>
        public DiagramEditorDescriptor FindAction(string actionId) => this.Editors.FirstOrDefault(editor => editor.ActionId == actionId);

        /// <summary>Finds the registered editor for an existing tab kind.</summary>
        /// <param name="viewTypeKey">The durable tab-kind identity.</param>
        /// <returns>The matching descriptor, or null when unavailable.</returns>
        public DiagramEditorDescriptor FindView(string viewTypeKey) => this.Editors.FirstOrDefault(editor => editor.ViewTypeKey == viewTypeKey);
    }
}
