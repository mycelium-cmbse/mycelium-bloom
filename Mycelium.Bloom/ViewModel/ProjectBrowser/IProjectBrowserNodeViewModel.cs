// ------------------------------------------------------------------------------------------------
// <copyright file="IProjectBrowserNodeViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    using System.ComponentModel;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Exposes canonical Browser row identity to inspection consumers.</summary>
    public interface IProjectBrowserNodeViewModel : INotifyPropertyChanged
    {
        /// <summary>Gets the identifier of this Browser occurrence.</summary>
        string Id { get; }

        /// <summary>Gets the canonical SDK element represented by this row.</summary>
        IElement SourceElement { get; }
    }
}
