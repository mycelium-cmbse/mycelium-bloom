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

    using DynamicData;

    using Moq;

    using Mycelium.Bloom.Core.Session;
    using Mycelium.Bloom.ViewModel.Home;

    [TestFixture]
    public sealed class HomeViewModelTestFixture
    {
        private Mock<IServerSessionService> sessionService;

        private Project[] projects;

        [SetUp]
        public void SetUp()
        {
            this.projects = [new Project { Id = Guid.NewGuid(), Name = "Spacecraft Mission" }, new Project { Id = Guid.NewGuid(), Name = "GroundStation-v2" }];
            this.sessionService = new Mock<IServerSessionService>();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyThePageShowsTheProjectsReadWhenTheConnectionOpened(bool isConnected)
        {
            this.sessionService.SetupGet(service => service.ServerAddress).Returns(isConnected ? new Uri("https://fabric.example.org") : null);
            this.sessionService.SetupGet(service => service.Projects).Returns(isConnected ? this.projects : []);

            using var viewModel = new HomeViewModel(this.sessionService.Object);
            var created = new Project { Id = Guid.NewGuid(), Name = "Lunar Lander" };
            viewModel.Projects.Add(created);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.IsConnected, Is.EqualTo(isConnected));
                Assert.That(viewModel.Rows, Is.EqualTo(isConnected ? (Project[])[.. this.projects, created] : (Project[])[created]));
            }
        }
    }
}
