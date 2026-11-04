// ------------------------------------------------------------------------------------------------
// <copyright file="PlaceholderOrganizationContextServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Organization.Placeholder
{
    using System.Threading;
    using System.Threading.Tasks;

    using ErrorOr;

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.Bloom.Model.Enum;

    [TestFixture]
    public sealed class PlaceholderOrganizationContextServiceTestFixture
    {
        private OrganizationOptions options;

        private PlaceholderOrganizationContextService service;

        [SetUp]
        public void SetUp()
        {
            this.options = new OrganizationOptions();
            this.service = new PlaceholderOrganizationContextService(Options.Create(this.options));
        }

        [Test]
        public async Task VerifyAccountIsTheSampleAccount()
        {
            var result = await this.service.GetAccountAsync(CancellationToken.None);

            Assert.That(result.Value, Is.EqualTo(OrganizationPlaceholderData.Account));
        }

        [TestCase(OrganizationRole.OrganizationAdministrator, true)]
        [TestCase(OrganizationRole.OrganizationMember, false)]
        public async Task VerifyOrganizationContextFollowsThePlaceholderRole(OrganizationRole role, bool canEditSettings)
        {
            var expected = OrganizationPlaceholderData.Organizations[0];
            this.options.PlaceholderRole = role;

            var result = await this.service.GetOrganizationContextAsync(expected.Slug, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Value.Account, Is.EqualTo(OrganizationPlaceholderData.Account));
                Assert.That(result.Value.Organization, Is.EqualTo(expected));
                Assert.That(result.Value.Capabilities.CanEditSettings, Is.EqualTo(canEditSettings));
                Assert.That(result.Value.Capabilities.CanCreateProject, Is.EqualTo(canEditSettings));
            }
        }

        [TestCase("does-not-exist")]
        [TestCase("ORBITAL-Systems")]
        [TestCase("")]
        public async Task VerifyUnknownSlugIsANotFoundError(string slug)
        {
            var result = await this.service.GetOrganizationContextAsync(slug, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Code, Is.EqualTo(OrganizationErrorCodes.NotFound));
            }
        }
    }
}
