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

    using Moq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    [TestFixture]
    public sealed class PlaceholderOrganizationProjectServiceTestFixture
    {
        private ProjectFilter noFilter;

        private Guid orbitalSystemsId;

        private PlaceholderOrganizationProjectService service;

        [SetUp]
        public void SetUp()
        {
            this.noFilter = new ProjectFilter(string.Empty, null, null);
            this.orbitalSystemsId = OrganizationPlaceholderData.Organizations[0].Id;
            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
            this.service = new PlaceholderOrganizationProjectService(timeProvider.Object);
        }

        [Test]
        public async Task VerifyProjectsAndCountsFollowTheSamples()
        {
            var lastPage = await this.service.GetProjectsAsync(this.orbitalSystemsId, this.noFilter, new PagedRequest(2, 5), CancellationToken.None);
            var counts = await this.service.GetProjectCountsAsync(this.orbitalSystemsId, CancellationToken.None);
            var unknown = await this.service.GetProjectsAsync(Guid.NewGuid(), this.noFilter, new PagedRequest(0, 5), CancellationToken.None);
            var unknownCounts = await this.service.GetProjectCountsAsync(Guid.NewGuid(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lastPage.Value.TotalCount, Is.EqualTo(12));
                Assert.That(lastPage.Value.Items, Has.Count.EqualTo(2));
                Assert.That(lastPage.Value.Items.Select(project => project.UpdatedAt), Is.Ordered.Descending);
                Assert.That(counts.Value, Is.EqualTo(new ProjectCounts(12, 1, 8, 1, 2)));
                Assert.That(unknown.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(unknown.FirstError.Code, Is.EqualTo(OrganizationErrorCodes.NotFound));
                Assert.That(unknownCounts.FirstError.Code, Is.EqualTo(OrganizationErrorCodes.NotFound));
            }
        }

        [Test]
        public void VerifyACancelledRequestIsNotAnswered()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => this.service.GetProjectsAsync(this.orbitalSystemsId, this.noFilter, new PagedRequest(0, 5), new CancellationToken(true)), Throws.InstanceOf<OperationCanceledException>());
                Assert.That(() => this.service.GetProjectCountsAsync(this.orbitalSystemsId, new CancellationToken(true)), Throws.InstanceOf<OperationCanceledException>());
            }
        }

        [TestCase("AURORA", null, null, 1)]
        [TestCase("phase", null, null, 2)]
        [TestCase("no such project", null, null, 0)]
        [TestCase("", ProjectLifecycleState.Archived, null, 2)]
        [TestCase("", null, ProjectMode.ConcurrentDesign, 3)]
        [TestCase("phase", ProjectLifecycleState.Review, ProjectMode.ConcurrentDesign, 1)]
        public async Task VerifyFilterNarrowsTheProjects(string searchText, ProjectLifecycleState? state, ProjectMode? mode, int expectedCount)
        {
            var filter = new ProjectFilter(searchText, state, mode);

            var projects = await this.service.GetProjectsAsync(this.orbitalSystemsId, filter, new PagedRequest(0, 50), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projects.Value.TotalCount, Is.EqualTo(expectedCount));
                Assert.That(projects.Value.Items, Has.Count.EqualTo(expectedCount));
            }
        }
    }
}
