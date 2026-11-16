// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNodeComponentRegistration.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    /// <summary>Associates one presentation type with its Razor renderer.</summary>
    /// <param name="PresentationType">The supported presentation type.</param>
    /// <param name="ComponentType">The component accepting its Presentation parameter.</param>
    public sealed record DiagramNodeComponentRegistration(Type PresentationType, Type ComponentType);
}
