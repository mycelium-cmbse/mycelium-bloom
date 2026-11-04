// ------------------------------------------------------------------------------------------------
// <copyright file="DisposableViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System;

    using Moq;

    using Mycelium.Bloom.ViewModel;

    [TestFixture]
    public sealed class DisposableViewModelTestFixture
    {
        [Test]
        public void VerifyDisposeReleasesEveryResourceOnlyOnce()
        {
            var first = new Mock<IDisposable>();
            var second = new Mock<IDisposable>();
            var viewModel = new TestDisposableViewModel(first.Object, second.Object);

            viewModel.Dispose();
            viewModel.Dispose();

            using (Assert.EnterMultipleScope())
            {
                first.Verify(disposable => disposable.Dispose(), Times.Once);
                second.Verify(disposable => disposable.Dispose(), Times.Once);
            }
        }

        private sealed class TestDisposableViewModel : DisposableViewModel
        {
            public TestDisposableViewModel(params IDisposable[] resources)
            {
                this.Disposables.AddRange(resources);
            }
        }
    }
}
