// ------------------------------------------------------------------------------------------------
// <copyright file="ListObservableExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;

    using Mycelium.Bloom.Extensions;

    [TestFixture]
    public sealed class ListObservableExtensionsTestFixture
    {
        private static readonly string[] LoadedQueries = ["a", "b"];

        private Subject<string> texts;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.texts = new Subject<string>();
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
            this.texts.Dispose();
        }

        [Test]
        public void VerifyOnlyTheLatestDistinctQueryIsLoaded()
        {
            var tokens = new Dictionary<string, CancellationToken>();
            var loaded = new List<string>();
            using var subscription = this.texts.LoadLatest((query, cancellationToken) =>
            {
                tokens[query] = cancellationToken;
                loaded.Add(query);
                return Task.Delay(Timeout.Infinite, cancellationToken);
            }).Subscribe();

            this.texts.OnNext("a");
            this.texts.OnNext("a");
            this.texts.OnNext("b");
            var firstCancelled = tokens["a"].IsCancellationRequested;
            var secondCancelledBeforeDisposal = tokens["b"].IsCancellationRequested;
            subscription.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loaded, Is.EqualTo(LoadedQueries));
                Assert.That(firstCancelled, Is.True);
                Assert.That(secondCancelledBeforeDisposal, Is.False);
                Assert.That(tokens["b"].IsCancellationRequested, Is.True);
            }
        }
    }
}
