// ------------------------------------------------------------------------------------------------
// <copyright file="TestListLoadingViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using DynamicData;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Mycelium.Bloom.ViewModel;

    internal sealed class TestListLoadingViewModel : ListLoadingViewModel<string>
    {
        private readonly Func<CancellationToken, Task<ErrorOr<IReadOnlyList<string>>>> load;

        public TestListLoadingViewModel(ILogger logger, Func<CancellationToken, Task<ErrorOr<IReadOnlyList<string>>>> load)
            : base("widgets", logger)
        {
            this.load = load;
            this.BeginLoad();
        }

        public ISourceList<string> Items => this.Source;

        protected override Task<ErrorOr<IReadOnlyList<string>>> LoadRowsAsync(CancellationToken cancellationToken)
        {
            return this.load(cancellationToken);
        }
    }
}
