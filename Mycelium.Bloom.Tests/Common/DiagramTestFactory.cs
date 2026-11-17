// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramTestFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System.Collections.Generic;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Root.Elements;

    internal static class DiagramTestFactory
    {
        public static GeneralViewProjector CreateProjector() => new([new PackageDiagramNodeFactory(), new CommentDiagramNodeFactory(), new ShellDiagramNodeFactory()]);

        public static DiagramProjection Create(IEnumerable<IElement> elements, string name = null, bool omitted = false) => CreateProjector().Create(elements, name, omitted);

        public static DiagramProjection CreateForRoot(IElement root) => CreateProjector().CreateForRoot(root);

        public static DiagramShellPresentation Node(IElement element, int index = 0) => (DiagramShellPresentation)CreateProjector().Node(element, index);
    }
}
