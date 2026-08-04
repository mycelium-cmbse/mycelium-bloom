// ------------------------------------------------------------------------------------------------
// <copyright file="Home.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using Mycelium.Bloom.ViewModel;

    using ReactiveUI.Blazor;

    /// <summary>
    /// Represents the Bloom home workspace.
    /// </summary>
    public partial class Home : ReactiveInjectableComponentBase<HomeViewModel>;
}
