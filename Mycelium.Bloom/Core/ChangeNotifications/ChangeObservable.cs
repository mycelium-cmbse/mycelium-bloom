// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeObservable.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Reactive.Disposables;
    using System.Reactive.Subjects;

    /// <summary>Groups the shared stream and subject lifetime owned by its registry.</summary>
    /// <param name="Subject">The target's notification subject.</param>
    /// <param name="Observable">The shared reference-counted stream.</param>
    /// <param name="Lifetime">The lifetime protecting active delivery from concurrent removal.</param>
    internal sealed record ChangeObservable(Subject<ChangeEvent> Subject, IObservable<ChangeEvent> Observable,
        RefCountDisposable Lifetime);
}
