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

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Tests the behaviour of the placeholder organization context service that the screens rely on.
    /// </summary>
    [TestFixture]
    public sealed class PlaceholderOrganizationContextServiceTestFixture
    {
        /// <summary>
        /// Verifies that the sample account is returned.
        /// </summary>
        /// <returns>A task that completes when the verification has run.</returns>
        [Test]
        public async Task VerifyAccountIsTheSampleAccount()
        {
            var result = await CreateService(OrganizationRole.OrganizationAdministrator).GetAccountAsync(CancellationToken.None);

            Assert.That(result.Value, Is.EqualTo(OrganizationPlaceholderData.Account));
        }

        /// <summary>
        /// Verifies that a sample organization is returned with the account, and that only the administrator
        /// placeholder role may edit the settings.
        /// </summary>
        /// <param name="role">The configured placeholder role.</param>
        /// <param name="canEditSettings">The expected capability.</param>
        /// <returns>A task that completes when the verification has run.</returns>
        [TestCase(OrganizationRole.OrganizationAdministrator, true)]
        [TestCase(OrganizationRole.OrganizationMember, false)]
        public async Task VerifyOrganizationContextFollowsThePlaceholderRole(OrganizationRole role, bool canEditSettings)
        {
            var expected = OrganizationPlaceholderData.Organizations[0];

            var result = await CreateService(role).GetOrganizationContextAsync(expected.Slug, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Value.Account, Is.EqualTo(OrganizationPlaceholderData.Account));
                Assert.That(result.Value.Organization, Is.EqualTo(expected));
                Assert.That(result.Value.Capabilities.CanEditSettings, Is.EqualTo(canEditSettings));
            }
        }

        /// <summary>
        /// Verifies that an unknown slug, including a slug that is not in lower case, is refused as not found.
        /// </summary>
        /// <param name="slug">The slug to look up.</param>
        /// <returns>A task that completes when the verification has run.</returns>
        [TestCase("does-not-exist")]
        [TestCase("ORBITAL-Systems")]
        [TestCase("")]
        public async Task VerifyUnknownSlugIsRefusedAsNotFound(string slug)
        {
            var result = await CreateService(OrganizationRole.OrganizationAdministrator).GetOrganizationContextAsync(slug, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Error.Code, Is.EqualTo(OrganizationErrorCode.NotFound));
            }
        }

        /// <summary>
        /// Creates the service under test over a placeholder role.
        /// </summary>
        /// <param name="role">The placeholder role of the sample account.</param>
        /// <returns>The service.</returns>
        private static PlaceholderOrganizationContextService CreateService(OrganizationRole role)
        {
            return new PlaceholderOrganizationContextService(Options.Create(new OrganizationOptions { PlaceholderRole = role }));
        }
    }
}
