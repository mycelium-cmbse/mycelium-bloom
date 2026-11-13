// ------------------------------------------------------------------------------------------------
// <copyright file="ConnectViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Session
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Moq;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Session;
    using Mycelium.Bloom.ViewModel.Session;

    [TestFixture]
    public sealed class ConnectViewModelTestFixture
    {
        private Mock<IServerSessionService> sessionService;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.sessionService = new Mock<IServerSessionService>();
            this.sessionService.Setup(service => service.OpenAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [Test]
        public async Task VerifyAServerThatAnswersOpensTheConnectionAndKeepsTheViewModelBusyMeanwhile()
        {
            var answer = new TaskCompletionSource<ErrorOr<Success>>();
            this.sessionService.Setup(service => service.OpenAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(answer.Task);
            using var viewModel = new ConnectViewModel(this.sessionService.Object);
            viewModel.ServerAddress = " https://fabric.example.org ";

            var connection = viewModel.Connect.Execute().ToTask();
            var isBusyMeanwhile = viewModel.IsBusy;
            var canConnectMeanwhile = await viewModel.Connect.CanExecute.FirstAsync().ToTask();
            answer.SetResult(Result.Success);
            await connection;

            using (Assert.EnterMultipleScope())
            {
                this.sessionService.Verify(service => service.OpenAsync(new Uri("https://fabric.example.org"), It.IsAny<CancellationToken>()), Times.Once);
                Assert.That(isBusyMeanwhile, Is.True);
                Assert.That(canConnectMeanwhile, Is.False);
                Assert.That(viewModel.IsConnected, Is.True);
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
            }
        }

        [TestCase("fabric.example.org")]
        [TestCase("ftp://fabric.example.org")]
        [TestCase("https://user:secret@fabric.example.org")]
        public async Task VerifyAnAddressThatIsNotAPlainHttpAddressIsRefusedUntilItChanges(string address)
        {
            using var viewModel = new ConnectViewModel(this.sessionService.Object);
            viewModel.ServerAddress = address;

            await viewModel.Connect.Execute().ToTask();
            var refusal = viewModel.Errors;
            viewModel.ServerAddress = "https://fabric.example.org";

            using (Assert.EnterMultipleScope())
            {
                this.sessionService.Verify(service => service.OpenAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Never);
                Assert.That(refusal, Has.Count.EqualTo(1));
                Assert.That(refusal[0].Type, Is.EqualTo(ErrorType.Validation));
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.IsConnected, Is.False);
            }
        }

        [Test]
        public async Task VerifyAServerThatCannotBeReachedAfterAnotherOneDisconnectsTheViewModel()
        {
            var unavailable = Error.Failure(ErrorCodes.Unavailable, "Cannot connect to this server");
            this.sessionService.SetupSequence(service => service.OpenAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success).ReturnsAsync(unavailable);
            using var viewModel = new ConnectViewModel(this.sessionService.Object);
            viewModel.ServerAddress = "https://fabric.example.org";
            await viewModel.Connect.Execute().ToTask();
            viewModel.ServerAddress = "http://localhost:5000";

            await viewModel.Connect.Execute().ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Errors, Has.Count.EqualTo(1));
                Assert.That(viewModel.Errors[0], Is.EqualTo(unavailable));
                Assert.That(viewModel.IsConnected, Is.False);
                Assert.That(viewModel.IsBusy, Is.False);
            }
        }

        [Test]
        public async Task VerifyAnUnexpectedFailureLeavesTheViewModelIdleAndDisconnected()
        {
            this.sessionService.Setup(service => service.OpenAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
            using var viewModel = new ConnectViewModel(this.sessionService.Object);
            viewModel.ServerAddress = "https://fabric.example.org";

            Assert.That(async () => await viewModel.Connect.Execute().ToTask(), Throws.InvalidOperationException);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.IsConnected, Is.False);
                Assert.That(await viewModel.Connect.CanExecute.FirstAsync().ToTask(), Is.True);
            }
        }

        [TestCase("https://fabric.example.org/", "", true)]
        [TestCase(null, "", false)]
        [TestCase(null, "   ", false)]
        [TestCase(null, "x", true)]
        public async Task VerifyTheViewModelStartsFromTheConnectionOfTheCircuit(string openAddress, string typedAddress, bool canConnect)
        {
            this.sessionService.SetupGet(service => service.ServerAddress).Returns(openAddress is null ? null : new Uri(openAddress));

            using var viewModel = new ConnectViewModel(this.sessionService.Object);
            var initialAddress = viewModel.ServerAddress;
            viewModel.ServerAddress = string.IsNullOrEmpty(typedAddress) ? viewModel.ServerAddress : typedAddress;
            var canExecute = await viewModel.Connect.CanExecute.FirstAsync().ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsConnected, Is.EqualTo(openAddress is not null));
                Assert.That(initialAddress, Is.EqualTo(openAddress ?? string.Empty));
                Assert.That(canExecute, Is.EqualTo(canConnect));
            }
        }
    }
}
