// ------------------------------------------------------------------------------------------------
// <copyright file="TestListLoadingViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Organization
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using DynamicData;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Mycelium.Bloom.ViewModel.Organization;

    internal sealed class TestListLoadingViewModel : ListLoadingViewModel<string>
    {
        private readonly Func<Guid, CancellationToken, Task<ErrorOr<IReadOnlyList<string>>>> load;

        public TestListLoadingViewModel(Guid organizationId, ILogger logger, Func<Guid, CancellationToken, Task<ErrorOr<IReadOnlyList<string>>>> load)
            : base(organizationId, "widgets", logger)
        {
            this.load = load;
            this.BeginLoad();
        }

        public ISourceList<string> Items => this.Source;

        protected override Task<ErrorOr<IReadOnlyList<string>>> LoadRowsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return this.load(organizationId, cancellationToken);
        }
    }
}
