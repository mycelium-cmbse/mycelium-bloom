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

    using DynamicData;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.ViewModel.Home;
    using Mycelium.SDK.REST;

    using SysML2.NET.PSM;

    [TestFixture]
    public sealed class HomeViewModelTestFixture
    {
        private Mock<ISessionService> sessionService;

        private Mock<ILogger<HomeViewModel>> logger;

        private Project[] projects;

        private SynchronizationContext testContext;

        [SetUp]
        public void SetUp()
        {
            this.testContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            this.projects = [new Project { Id = Guid.NewGuid(), Name = "Spacecraft Mission" }, new Project { Id = Guid.NewGuid(), Name = "GroundStation-v2" }];
            this.sessionService = new Mock<ISessionService>();
            this.sessionService.Setup(service => service.GetProjectsAsync(It.IsAny<QueryParameters>(), It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IReadOnlyList<Project>>(this.projects));
            this.logger = new Mock<ILogger<HomeViewModel>>();
        }

        [TearDown]
        public void TearDown()
        {
            SynchronizationContext.SetSynchronizationContext(this.testContext);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyThePageReadsTheProjectsOfTheServerOnlyWhenAConnectionIsOpen(bool isConnected)
        {
            this.sessionService.SetupGet(service => service.ServerAddress).Returns(isConnected ? new Uri("https://fabric.example.org") : null);

            using var viewModel = new HomeViewModel(this.sessionService.Object, this.logger.Object);
            var created = new Project { Id = Guid.NewGuid(), Name = "Lunar Lander" };
            viewModel.Projects.Add(created);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsConnected, Is.EqualTo(isConnected));
                Assert.That(viewModel.IsBusy, Is.EqualTo(!isConnected));
                Assert.That(viewModel.Errors, Is.Empty);
                Assert.That(viewModel.Rows, Is.EqualTo(isConnected ? (Project[])[.. this.projects, created] : (Project[])[created]));
                this.sessionService.Verify(service => service.GetProjectsAsync(It.IsAny<QueryParameters>(), It.IsAny<CancellationToken>()), isConnected ? Times.Once() : Times.Never());
            }
        }
    }
}
