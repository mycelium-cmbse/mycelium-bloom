// ------------------------------------------------------------------------------------------------
// <copyright file="BloomBaseViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using Mycelium.Bloom.ViewModel;

    /// <summary>
    /// Tests the <see cref="BloomBaseViewModel" />.
    /// </summary>
    [TestFixture]
    public sealed class BloomBaseViewModelTestFixture
    {
        /// <summary>
        /// Verifies that loading state can be started and stopped.
        /// </summary>
        [Test]
        public void VerifyLoadingState()
        {
            var viewModel = new BloomBaseViewModelStub();

            viewModel.StartLoadingState();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsLoading, Is.True);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
            }

            viewModel.StopLoadingState();

            Assert.That(viewModel.IsLoading, Is.False);
        }

        /// <summary>
        /// Verifies that loaded state clears previous errors.
        /// </summary>
        [Test]
        public void VerifyLoadedState()
        {
            var viewModel = new BloomBaseViewModelStub();

            viewModel.SetErrorState("Model load failed");
            viewModel.SetLoadedState();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsLoaded, Is.True);
                Assert.That(viewModel.ErrorMessage, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that error state records the error and marks the view model as not loaded.
        /// </summary>
        [Test]
        public void VerifyErrorState()
        {
            var viewModel = new BloomBaseViewModelStub();

            viewModel.SetLoadedState();
            viewModel.SetErrorState("Model load failed");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsLoaded, Is.False);
                Assert.That(viewModel.ErrorMessage, Is.EqualTo("Model load failed"));
            }
        }

        private sealed class BloomBaseViewModelStub : BloomBaseViewModel
        {
            /// <summary>
            /// Exposes the protected loading start behavior for testing.
            /// </summary>
            public void StartLoadingState()
            {
                this.StartLoading();
            }

            /// <summary>
            /// Exposes the protected loading stop behavior for testing.
            /// </summary>
            public void StopLoadingState()
            {
                this.StopLoading();
            }

            /// <summary>
            /// Exposes the protected loaded state behavior for testing.
            /// </summary>
            public void SetLoadedState()
            {
                this.SetLoaded();
            }

            /// <summary>
            /// Exposes the protected error state behavior for testing.
            /// </summary>
            /// <param name="errorMessage">The error message to apply.</param>
            public void SetErrorState(string errorMessage)
            {
                this.SetError(errorMessage);
            }
        }
    }
}
