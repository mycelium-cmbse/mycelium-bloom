// ------------------------------------------------------------------------------------------------
// <copyright file="NavigationRailContext.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using System.Diagnostics.CodeAnalysis;

    using Mycelium.Bloom.Model.Enum;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Captures project and model-selection context used to derive available rail destinations.
    /// </summary>
    public sealed class NavigationRailContext
    {
        /// <summary>
        /// Gets the current project lifecycle state.
        /// </summary>
        public ProjectLifecycleState LifecycleState { get; init; }

        /// <summary>
        /// Gets the currently selected model element, or <see langword="null" />.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        public IElement SelectedElement { get; init; }
    }
}
