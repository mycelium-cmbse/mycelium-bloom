// ------------------------------------------------------------------------------------------------
// <copyright file="MemberTestData.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System;

    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    internal static class MemberTestData
    {
        public static User CreateMember(Organization organization, string name, OrganizationMembershipRole role, ActivationStatus status, DateTime joinedOn, DateTime? lastActiveOn)
        {
            var user = CreateUser(name, status, joinedOn, lastActiveOn);
            user.IsPartOfOrganizations = [new OrganizationMember { Id = Guid.NewGuid(), Organization = organization, User = user, Role = role, CreatedOn = joinedOn }];

            return user;
        }

        public static User CreateOutsideCollaborator(Organization partner, string name, DateTime joinedOn, DateTime? lastActiveOn, ActivationStatus status = ActivationStatus.Active)
        {
            var user = CreateUser(name, status, joinedOn, lastActiveOn);
            user.IsPartOfOrganizations = [new OrganizationMember { Id = Guid.NewGuid(), Organization = partner, User = user, Role = OrganizationMembershipRole.Member, CreatedOn = joinedOn }];
            user.IsPartOfProjects = [new ProjectMember { Id = Guid.NewGuid(), User = user, CreatedOn = joinedOn }];

            return user;
        }

        private static User CreateUser(string name, ActivationStatus status, DateTime createdOn, DateTime? lastActiveOn)
        {
            return new User { Id = Guid.NewGuid(), Name = name, Email = $"{name.Replace(' ', '.')}@example.org", Status = status, CreatedOn = createdOn, UpdatedOn = lastActiveOn ?? createdOn };
        }
    }
}
