// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceModelSnapshot.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model
{
    using Mycelium.Bloom.Model.Enum;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>Identifies a local workspace load independently of any backend revision.</summary>
    /// <param name="Id">The local context identity used to reject stale completions.</param>
    /// <param name="Root">The canonical loaded root, when available.</param>
    /// <param name="Status">The current loading or availability state.</param>
    public sealed record WorkspaceModelSnapshot(Guid Id, INamespace Root, WorkspaceModelStatus Status);
}
