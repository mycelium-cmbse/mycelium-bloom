// ------------------------------------------------------------------------------------------------
// <copyright file="PackageDiagramNodeFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Projects canonical packages into their dedicated notation.</summary>
    public sealed class PackageDiagramNodeFactory : IDiagramNodeFactory
    {
        /// <summary>Gets the supported SDK interface.</summary>
        public Type ElementType => typeof(IPackage);

        /// <summary>Creates the typed notation for the supplied occurrence.</summary>
        /// <param name="occurrence">The borrowed canonical element and graphical geometry.</param>
        /// <param name="source">The optional canonical reference site supplying view-local names.</param>
        /// <returns>The dedicated node presentation.</returns>
        public DiagramNodePresentation Create(DiagramOccurrence occurrence, IElement source = null)
            => new DiagramPackagePresentation(occurrence, DiagramNotation.Name(occurrence.Element, source));
    }
}
