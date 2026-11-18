// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEditorDescriptor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Registers one diagram editor's action, identity, caller-owned factory and component.</summary>
    /// <param name="ActionId">The add-tab action identity.</param>
    /// <param name="Title">The editor title and action label.</param>
    /// <param name="ViewTypeKey">The durable editor-kind identity.</param>
    /// <param name="CreateViewModel">Creates tab-owned state over an optional canonical root.</param>
    /// <param name="ComponentType">The Razor component accepting the diagram ViewModel.</param>
    /// <param name="Symbol">The existing editor-tab symbol.</param>
    public sealed record DiagramEditorDescriptor(string ActionId, string Title, string ViewTypeKey,
        Func<IElement, IDiagramViewModel> CreateViewModel, Type ComponentType, SymbolIconName Symbol = SymbolIconName.Document)
    {
        /// <summary>Gets whether the descriptor is limited to development and explicit test composition.</summary>
        public bool IsDevelopmentOnly { get; init; }
    }
}
