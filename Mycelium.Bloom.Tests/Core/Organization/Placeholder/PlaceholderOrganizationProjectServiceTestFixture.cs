// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationProjectServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Organization.Placeholder
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Time.Testing;

    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;

    [TestFixture]
    public sealed class PlaceholderOrganizationProjectServiceTestFixture
    {
        private Guid orbitalSystemsId;

        private PlaceholderOrganizationProjectService service;

        [SetUp]
        public void SetUp()
        {
            this.orbitalSystemsId = OrganizationPlaceholderData.Organizations["orbital-systems"].Id;
            this.service = new PlaceholderOrganizationProjectService(new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero)));
        }

        [Test]
        public async Task VerifyProjectsFollowTheSamples()
        {
            var projects = await this.service.GetProjectsAsync(this.orbitalSystemsId, CancellationToken.None);
            var unknown = await this.service.GetProjectsAsync(Guid.NewGuid(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projects.Value, Has.Count.EqualTo(12));
                Assert.That(projects.Value.Select(project => project.Involves.Count), Has.Member(12));
                Assert.That(unknown.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(unknown.FirstError.Code, Is.EqualTo(ErrorCodes.NotFound));
            }
        }

        [Test]
        public void VerifyACancelledRequestIsNotAnswered()
        {
            Assert.That(() => this.service.GetProjectsAsync(this.orbitalSystemsId, new CancellationToken(true)), Throws.InstanceOf<OperationCanceledException>());
        }
    }
}
