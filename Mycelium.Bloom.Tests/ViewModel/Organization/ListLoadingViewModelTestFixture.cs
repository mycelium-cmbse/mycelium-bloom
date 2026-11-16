// ------------------------------------------------------------------------------------------------
// <copyright file="ListLoadingViewModelTestFixture.cs" company="Starion Group S.A.">
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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DynamicData;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core;

    [TestFixture]
    public sealed class ListLoadingViewModelTestFixture
    {
        private static readonly string[] ExpectedRows = ["first", "second", "third"];

        private Guid organizationId;

        private Mock<ILogger> logger;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.organizationId = Guid.NewGuid();
            this.logger = new Mock<ILogger>();
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [Test]
        public void VerifyRowsAreLoadedOnceInTheOrderOfTheLoaderAndAddedRowsJoinThemWithoutAnotherLoad()
        {
            var loads = 0;

            using var viewModel = new TestListLoadingViewModel(this.organizationId, this.logger.Object, (id, _) =>
            {
                loads++;
                return Task.FromResult(ErrorOrFactory.From<IReadOnlyList<string>>(["first", "second"]));
            });
            viewModel.Items.Add("third");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Is.EqualTo(ExpectedRows));
                Assert.That(loads, Is.EqualTo(1));
            }
        }

        [TestCase(true, ErrorCodes.NotFound)]
        [TestCase(false, ErrorCodes.Unavailable)]
        public void VerifyErrorOrFailureIsKeptAsAnErrorAndLeavesNoRow(bool returnsError, string expectedCode)
        {
            using var viewModel = new TestListLoadingViewModel(this.organizationId, this.logger.Object, (id, _) => returnsError ? Task.FromResult<ErrorOr<IReadOnlyList<string>>>(Error.NotFound(ErrorCodes.NotFound, "Not found.")) : throw new InvalidOperationException());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors.Single().Code, Is.EqualTo(expectedCode));
                Assert.That(viewModel.Errors.Single().Description, Is.EqualTo(returnsError ? "Not found." : "The widgets could not be loaded."));
                Assert.That(viewModel.Rows, Is.Empty);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyDisposingCancelsTheLoadWhichShowsNothing(bool endsCancelled)
        {
            var cancellation = CancellationToken.None;
            var pending = new TaskCompletionSource<ErrorOr<IReadOnlyList<string>>>();

            var viewModel = new TestListLoadingViewModel(this.organizationId, this.logger.Object, (id, token) =>
            {
                cancellation = token;
                return pending.Task;
            });
            var busyWhileLoading = viewModel.IsBusy;
            viewModel.Dispose();

            if (endsCancelled)
            {
                pending.SetCanceled(cancellation);
            }
            else
            {
                pending.SetResult(ErrorOrFactory.From<IReadOnlyList<string>>(["late"]));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(busyWhileLoading, Is.True);
                Assert.That(cancellation.IsCancellationRequested, Is.True);
                Assert.That(viewModel.IsBusy, Is.True);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Is.Empty);
            }
        }

        [Test]
        public void VerifyTheLoadRunsOnTheContextTheViewModelWasCreatedOn()
        {
            var renderContext = new CountingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(renderContext);

            using var viewModel = new TestListLoadingViewModel(this.organizationId, this.logger.Object, (id, _) => Task.FromResult(ErrorOrFactory.From<IReadOnlyList<string>>(["first"])));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Rows, Has.Count.EqualTo(1));
                Assert.That(renderContext.Posts, Is.GreaterThan(0));
            }
        }
    }
}
