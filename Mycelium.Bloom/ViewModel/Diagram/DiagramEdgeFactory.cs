// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramEdgeFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Registers one SDK type with its graphical-notation policy.</summary>
    /// <typeparam name="TElement">The SDK interface or implementation accepted by the policy.</typeparam>
    public sealed class DiagramEdgeFactory<TElement> : IDiagramEdgeFactory where TElement : IElement
    {
        /// <summary>The immutable notation policy for the registered SDK type.</summary>
        private readonly Func<TElement, DiagramEdgeNotation> create;

        /// <summary>Gets the accepted canonical SDK type.</summary>
        public Type ElementType => typeof(TElement);

        /// <summary>Creates one registered notation policy.</summary>
        /// <param name="create">The descriptor policy, which may explicitly omit specialized notation.</param>
        public DiagramEdgeFactory(Func<TElement, DiagramEdgeNotation> create)
        {
            ArgumentNullException.ThrowIfNull(create);
            this.create = create;
        }

        /// <summary>Formats notation for a canonical object accepted by this registration.</summary>
        /// <param name="element">The canonical SDK element.</param>
        /// <returns>The supported descriptor, or null when notation is intentionally omitted.</returns>
        public DiagramEdgeNotation Create(IElement element) => this.create((TElement)element);
    }
}
