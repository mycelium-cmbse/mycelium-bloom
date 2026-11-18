// ------------------------------------------------------------------------------------------------
// <copyright file="IGeneralViewProjector.cs" company="Starion Group S.A.">
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

    /// <summary>Projects canonical model slices independently of workspace composition.</summary>
    public interface IGeneralViewProjector
    {
        /// <summary>Projects a canonical root and its direct members.</summary>
        /// <param name="root">The selected canonical root, or null.</param>
        /// <returns>The bounded read-only projection.</returns>
        DiagramProjection CreateForRoot(IElement root);

        /// <summary>Projects an explicit bounded canonical slice.</summary>
        /// <param name="elements">The canonical elements requested by the caller.</param>
        /// <param name="name">The optional frame name.</param>
        /// <param name="omitted">Whether content is already known to be omitted.</param>
        /// <returns>The immutable graphical projection.</returns>
        DiagramProjection Create(IEnumerable<IElement> elements, string name = null, bool omitted = false);

        /// <summary>Projects one canonical element using the most specific registered factory.</summary>
        /// <param name="element">The canonical element.</param>
        /// <param name="index">The initial grid position.</param>
        /// <param name="source">The optional canonical reference site.</param>
        /// <returns>The typed node presentation.</returns>
        DiagramNodePresentation Node(IElement element, int index = 0, IElement source = null);
    }
}
