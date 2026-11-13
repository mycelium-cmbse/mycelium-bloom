// ------------------------------------------------------------------------------------------------
// <copyright file="ServerSessionServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Session
{
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Session;

    using SysML2.NET.REST.Exceptions;

    using SySML2.NET.REST;

    [TestFixture]
    public sealed class ServerSessionServiceTestFixture
    {
        private static readonly Uri ServerAddress = new("https://fabric.example.org");

        private static readonly Exception[] ConnectionFailures = [new HttpRequestException("No such host"), new PsmApiException("Not found"), new JsonException("Not a page of projects"), new NotSupportedException("Not a JSON answer"), new TaskCanceledException("Timed out")];

        private Mock<ISession> session;

        private ServerSessionService service;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<ISession>();
            this.service = new ServerSessionService(this.session.Object, new Mock<ILogger<ServerSessionService>>().Object);
        }

        [Test]
        public async Task VerifyAServerThatAnswersOpensTheConnection()
        {
            var result = await this.service.OpenAsync(ServerAddress, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                this.session.Verify(session => session.Open(ServerAddress, It.IsAny<CancellationToken>()), Times.Once);
                Assert.That(result.IsError, Is.False);
                Assert.That(this.service.ServerAddress, Is.EqualTo(ServerAddress));
            }
        }

        [TestCaseSource(nameof(ConnectionFailures))]
        public async Task VerifyAServerThatFailsClosesTheConnection(Exception failure)
        {
            await this.service.OpenAsync(ServerAddress, CancellationToken.None);
            this.session.Setup(session => session.Open(ServerAddress, It.IsAny<CancellationToken>())).ThrowsAsync(failure);

            var result = await this.service.OpenAsync(ServerAddress, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Code, Is.EqualTo(ErrorCodes.Unavailable));
                Assert.That(this.service.ServerAddress, Is.Null);
            }
        }

        [Test]
        public void VerifyACancelledConnectionIsNotReportedAsAFailure()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            this.session.Setup(session => session.Open(ServerAddress, It.IsAny<CancellationToken>())).ThrowsAsync(new TaskCanceledException());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await this.service.OpenAsync(ServerAddress, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
                Assert.That(this.service.ServerAddress, Is.Null);
            }
        }
    }
}
