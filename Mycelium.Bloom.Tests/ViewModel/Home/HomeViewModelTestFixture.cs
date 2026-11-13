// ------------------------------------------------------------------------------------------------
// <copyright file="HomeViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Home
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Session;
    using Mycelium.Bloom.ViewModel.Home;

    [TestFixture]
    public sealed class HomeViewModelTestFixture
    {
        private Mock<IServerSessionService> sessionService;

        private Mock<ILogger<HomeViewModel>> logger;

        private Project[] projects;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.projects = [new Project { Id = Guid.NewGuid(), Name = "Spacecraft Mission" }, new Project { Id = Guid.NewGuid(), Name = "GroundStation-v2" }];
            this.logger = new Mock<ILogger<HomeViewModel>>();
            this.sessionService = new Mock<IServerSessionService>();
            this.sessionService.SetupGet(service => service.ServerAddress).Returns(new Uri("https://fabric.example.org"));
            this.sessionService.Setup(service => service.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.projects);
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [Test]
        public void VerifyTheProjectsOfTheServerAreLoadedInTheirOrder()
        {
            using var viewModel = new HomeViewModel(this.sessionService.Object, this.logger.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsConnected, Is.True);
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Is.EqualTo(this.projects));
                Assert.That(viewModel.Projects.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void VerifyAServerThatFailsLeavesNoRowsAndKeepsItsError()
        {
            var unavailable = Error.Failure(ErrorCodes.Unavailable, "Cannot connect to this server");
            this.sessionService.Setup(service => service.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(unavailable);

            using var viewModel = new HomeViewModel(this.sessionService.Object, this.logger.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Rows, Is.Empty);
                Assert.That(viewModel.Errors, Has.Count.EqualTo(1));
                Assert.That(viewModel.Errors[0], Is.EqualTo(unavailable));
            }
        }

        [Test]
        public void VerifyAnUnexpectedFailureKeepsAnUnavailableError()
        {
            this.sessionService.Setup(service => service.GetProjectsAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            using var viewModel = new HomeViewModel(this.sessionService.Object, this.logger.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsBusy, Is.False);
                Assert.That(viewModel.Errors, Has.Count.EqualTo(1));
                Assert.That(viewModel.Errors[0].Code, Is.EqualTo(ErrorCodes.Unavailable));
            }
        }

        [Test]
        public void VerifyNothingIsLoadedWithoutAConnection()
        {
            this.sessionService.SetupGet(service => service.ServerAddress).Returns((Uri)null);

            using var viewModel = new HomeViewModel(this.sessionService.Object, this.logger.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsConnected, Is.False);
                Assert.That(viewModel.IsBusy, Is.False);
                this.sessionService.Verify(service => service.GetProjectsAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Test]
        public async Task VerifyClosingThePageCancelsALoadThatIsStillRunning()
        {
            var answer = new TaskCompletionSource<ErrorOr<IReadOnlyList<Project>>>();
            var cancellation = CancellationToken.None;
            this.sessionService.Setup(service => service.GetProjectsAsync(It.IsAny<CancellationToken>())).Callback<CancellationToken>(token => cancellation = token).Returns(answer.Task);
            var viewModel = new HomeViewModel(this.sessionService.Object, this.logger.Object);
            var isBusyMeanwhile = viewModel.IsBusy;

            viewModel.Dispose();
            answer.SetResult(Error.Failure(ErrorCodes.Unavailable, "Cannot connect to this server"));
            await answer.Task;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isBusyMeanwhile, Is.True);
                Assert.That(cancellation.IsCancellationRequested, Is.True);
                Assert.That(viewModel.Errors, Is.Empty);
            }
        }
    }
}
