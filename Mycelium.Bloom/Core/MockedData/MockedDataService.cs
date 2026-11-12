// ------------------------------------------------------------------------------------------------
// <copyright file="MockedDataService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.MockedData
{
    using System.Security.Cryptography;
    using System.Text;

    using Mycelium.SDK;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    /// <summary>
    /// Builds a fictional functional-data graph that exercises every SDK class and enumeration literal, with every reference
    /// set in both directions. Identifiers derive from names, so that two instances agree; dates derive from the current
    /// time, so that the data always looks recent.
    /// </summary>
    /// <remarks>
    /// Mocked data, to be removed when the Fabric client is bound.
    /// </remarks>
    public sealed class MockedDataService : IMockedDataService
    {
        /// <summary>
        /// The engineering domains that a project can define, from which each project copies its ownerships.
        /// </summary>
        private static readonly IReadOnlyList<Ownership> OwnershipTemplates =
        [
            new() { Name = "System", ShortName = "SYS", Description = "System architecture, requirements and budgets" },
            new() { Name = "Power", ShortName = "PWR", Description = "Power generation, storage and distribution" },
            new() { Name = "Thermal", ShortName = "THM", Description = "Thermal control and analysis" },
            new() { Name = "Attitude and Orbit Control", ShortName = "AOCS", Description = "Attitude determination, control and orbit manoeuvres" },
            new() { Name = "Payload", ShortName = "PLD", Description = "Mission instruments and payload data handling" },
            new() { Name = "Structure", ShortName = "STR", Description = "Primary and secondary structure and mechanisms" },
            new() { Name = "Communications", ShortName = "COM", Description = "Telemetry, telecommand and data links" },
            new() { Name = "Propulsion", ShortName = "PRP", Description = "Propulsion subsystem and propellant budget" }
        ];

        /// <summary>
        /// The titles given to reviews, in turn.
        /// </summary>
        private static readonly IReadOnlyList<string> ReviewTitles =
        [
            "Update the power budget",
            "Refine the thermal interfaces",
            "Add the star tracker to the AOCS architecture",
            "Rework the payload data chain",
            "Align the structure mass margins",
            "Introduce the X-band downlink",
            "Split the propulsion requirements",
            "Baseline the mission phases"
        ];

        /// <summary>
        /// The statuses given to the reviews of projects that are not archived, in turn.
        /// </summary>
        private static readonly IReadOnlyList<ReviewStatus> OngoingReviewStatuses =
            [ReviewStatus.Draft, ReviewStatus.Ready, ReviewStatus.ChangesRequested, ReviewStatus.Approved];

        /// <summary>
        /// The contents given to comments, in turn.
        /// </summary>
        private static readonly IReadOnlyList<string> CommentContents =
        [
            "The margin on this value looks too tight for the current maturity.",
            "Could this reuse the definition from the shared library instead?",
            "Please trace this change to the requirement it satisfies.",
            "Agreed, but the value should be expressed in SI units."
        ];

        /// <summary>
        /// The roles the current account takes in projects, in turn.
        /// </summary>
        private static readonly IReadOnlyList<ProjectMemberRole> CurrentUserProjectRoles =
            [ProjectMemberRole.Administrator, ProjectMemberRole.Participant, ProjectMemberRole.Viewer];

        /// <summary>
        /// The time from which every date of the graph is computed.
        /// </summary>
        private readonly DateTime now;

        /// <summary>
        /// The mocked users.
        /// </summary>
        private readonly List<User> users = [];

        /// <summary>
        /// The mocked organizations by slug.
        /// </summary>
        private readonly Dictionary<string, Organization> organizations = new(StringComparer.Ordinal);

        /// <summary>
        /// The mocked projects.
        /// </summary>
        private readonly List<FunctionalProject> projects = [];

        /// <summary>
        /// The number of reviews created so far, which picks the title and status of the next one.
        /// </summary>
        private int reviewSequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockedDataService" /> class.
        /// </summary>
        /// <param name="timeProvider">The provider of the current time.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeProvider" /> is <see langword="null" />.</exception>
        public MockedDataService(TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.now = timeProvider.GetUtcNow().UtcDateTime;

            this.CurrentUser = this.AddUser("Demo Account", "demo.account@example.org");
            this.CurrentUser.UserPreferences = new Dictionary<string, string> { ["theme"] = "dark", ["locale"] = "en-GB", ["projectBrowser.showLibraries"] = "false" };

            var elena = this.AddUser("Elena Marsh", "elena.marsh@orbital-systems.example");
            elena.UserPreferences = new Dictionary<string, string> { ["locale"] = "en-IE" };
            var tomas = this.AddUser("Tomas Lindqvist", "tomas.lindqvist@orbital-systems.example");
            var priya = this.AddUser("Priya Raman", "priya.raman@orbital-systems.example");
            var ruth = this.AddUser("Ruth Abebe", "ruth.abebe@orbital-systems.example");
            var oskar = this.AddUser("Oskar Nilsson", "oskar.nilsson@orbital-systems.example");
            var greta = this.AddUser("Greta Holm", "greta.holm@orbital-systems.example", ActivationStatus.Suspended);
            var victor = this.AddUser("Victor Lambert", "victor.lambert@orbital-systems.example", ActivationStatus.Archived);
            var clara = this.AddUser("Clara Schmidt", "clara.schmidt@orbital-systems.example", ActivationStatus.Deleted);

            List<User> orbitalStaff =
            [
                priya,
                this.AddUser("Hugo Delcourt", "hugo.delcourt@orbital-systems.example"),
                this.AddUser("Amara Okafor", "amara.okafor@orbital-systems.example"),
                this.AddUser("Jonas Weber", "jonas.weber@orbital-systems.example"),
                this.AddUser("Sofia Ferreira", "sofia.ferreira@orbital-systems.example"),
                this.AddUser("Kenji Watanabe", "kenji.watanabe@orbital-systems.example"),
                this.AddUser("Lena Novak", "lena.novak@orbital-systems.example"),
                this.AddUser("Mateo Rossi", "mateo.rossi@orbital-systems.example"),
                this.AddUser("Chloé Martin", "chloe.martin@orbital-systems.example"),
                ruth,
                oskar,
                this.AddUser("Yara Haddad", "yara.haddad@orbital-systems.example"),
                this.AddUser("Daniel Kowalski", "daniel.kowalski@orbital-systems.example"),
                this.AddUser("Ines Moreau", "ines.moreau@orbital-systems.example"),
                this.AddUser("Felix Braun", "felix.braun@orbital-systems.example"),
                this.AddUser("Nadia Petrova", "nadia.petrova@orbital-systems.example"),
                this.AddUser("Samuel Osei", "samuel.osei@orbital-systems.example"),
                this.AddUser("Aiko Tanaka", "aiko.tanaka@orbital-systems.example"),
                this.AddUser("Marco Bianchi", "marco.bianchi@orbital-systems.example", ActivationStatus.Pending),
                greta,
                victor,
                clara
            ];

            var rafael = this.AddUser("Rafael Costa", "rafael.costa@payload-lab.example");
            var mei = this.AddUser("Mei Chen", "mei.chen@payload-lab.example");
            var lucas = this.AddUser("Lucas Dubois", "lucas.dubois@payload-lab.example");
            var hanna = this.AddUser("Hanna Virtanen", "hanna.virtanen@payload-lab.example");
            var omar = this.AddUser("Omar Farouk", "omar.farouk@payload-lab.example", ActivationStatus.Pending);

            var orbitalSystems = this.AddOrganization("orbital-systems", "Orbital Systems", "Spacecraft prime for Earth observation and science missions",
                ActivationStatus.Active, elena,
                new OrganizationPolicy { AllowProjectCreation = true, DefaultProjectLifecycleOnCreate = ProjectLifecycleKind.Preparation, GrantReadOnlyViewForAudit = true });

            AddOrganizationMembers(orbitalSystems, OrganizationMembershipRole.Owner, elena);
            AddOrganizationMembers(orbitalSystems, OrganizationMembershipRole.Administrator, this.CurrentUser, tomas);
            AddOrganizationMembers(orbitalSystems, OrganizationMembershipRole.Member, orbitalStaff);

            var payloadLab = this.AddOrganization("payload-lab", "Payload Lab", "Instrument development laboratory",
                ActivationStatus.Active, rafael,
                new OrganizationPolicy { AllowProjectCreation = false, DefaultProjectLifecycleOnCreate = ProjectLifecycleKind.Open, GrantReadOnlyViewForAudit = false });

            AddOrganizationMembers(payloadLab, OrganizationMembershipRole.Owner, rafael);
            AddOrganizationMembers(payloadLab, OrganizationMembershipRole.Administrator, mei);
            AddOrganizationMembers(payloadLab, OrganizationMembershipRole.Member, this.CurrentUser, priya, lucas, hanna, omar);

            var heritageAvionics = this.AddOrganization("heritage-avionics", "Heritage Avionics", "Avionics supplier, archived after its merger",
                ActivationStatus.Archived, victor,
                new OrganizationPolicy { AllowProjectCreation = false, DefaultProjectLifecycleOnCreate = ProjectLifecycleKind.Archived, GrantReadOnlyViewForAudit = true });

            AddOrganizationMembers(heritageAvionics, OrganizationMembershipRole.Owner, victor);
            AddOrganizationMembers(heritageAvionics, OrganizationMembershipRole.Member, this.CurrentUser, greta, clara, ruth);

            var launchPartners = this.AddOrganization("launch-partners", "Launch Partners", "Launch services consortium, suspended pending renewal",
                ActivationStatus.Suspended, rafael,
                new OrganizationPolicy { AllowProjectCreation = true, DefaultProjectLifecycleOnCreate = ProjectLifecycleKind.Open, GrantReadOnlyViewForAudit = false });

            AddOrganizationMembers(launchPartners, OrganizationMembershipRole.Owner, rafael);
            AddOrganizationMembers(launchPartners, OrganizationMembershipRole.Administrator, mei);
            AddOrganizationMembers(launchPartners, OrganizationMembershipRole.Member, this.CurrentUser, oskar);

            this.AddProject(orbitalSystems, "Aurora Orbiter", "LEO imaging satellite", ProjectVisibility.Organization, ProjectMode.Regular, ProjectLifecycleKind.Open, TimeSpan.FromHours(2), 12);
            this.AddProject(orbitalSystems, "Lunar Relay Study", "Phase A concept", ProjectVisibility.Private, ProjectMode.Concurrent, ProjectLifecycleKind.Review, TimeSpan.FromDays(1), 8);
            this.AddProject(orbitalSystems, "Deimos Lander Concept", "Descent and landing study", ProjectVisibility.Private, ProjectMode.Concurrent, ProjectLifecycleKind.Preparation, TimeSpan.FromMinutes(30), 26);
            this.AddProject(orbitalSystems, "Propulsion Research", "Electric propulsion R&D", ProjectVisibility.Private, ProjectMode.Regular, ProjectLifecycleKind.Open, TimeSpan.FromHours(3), 15);
            this.AddProject(orbitalSystems, "Mars Sample Relay", "Phase 0 feasibility", ProjectVisibility.Private, ProjectMode.Concurrent, ProjectLifecycleKind.Open, TimeSpan.FromHours(4), 21);
            this.AddProject(orbitalSystems, "Comms Constellation", "Inter-satellite link study", ProjectVisibility.Organization, ProjectMode.Regular, ProjectLifecycleKind.Open, TimeSpan.FromDays(1.5), 11);
            this.AddProject(orbitalSystems, "Ground Station v2", "Ground segment data model", ProjectVisibility.Organization, ProjectMode.Regular, ProjectLifecycleKind.Open, TimeSpan.FromDays(5), 9);
            this.AddProject(orbitalSystems, "Solar Array Trade-off", "Power sizing study", ProjectVisibility.Organization, ProjectMode.Regular, ProjectLifecycleKind.Open, TimeSpan.FromDays(6), 6);
            this.AddProject(orbitalSystems, "Attitude Benchmark", "Attitude control", ProjectVisibility.Organization, ProjectMode.Regular, ProjectLifecycleKind.Archived, TimeSpan.FromDays(7), 5);
            this.AddProject(orbitalSystems, "Thermal Reference Models", "Shared thermal library", ProjectVisibility.Public, ProjectMode.Regular, ProjectLifecycleKind.Open, TimeSpan.FromDays(14), 4);
            this.AddProject(orbitalSystems, "Ocean Survey", "Payload subsystem model", ProjectVisibility.Public, ProjectMode.Regular, ProjectLifecycleKind.Open, TimeSpan.FromDays(21), 7);
            this.AddProject(orbitalSystems, "Legacy Migration", "Archive of imported models", ProjectVisibility.Private, ProjectMode.Regular, ProjectLifecycleKind.Archived, TimeSpan.FromDays(35), 3);
            this.AddProject(heritageAvionics, "Avionics Bus Heritage", "Legacy data handling architecture", ProjectVisibility.Private, ProjectMode.Regular, ProjectLifecycleKind.Archived, TimeSpan.FromDays(400), 4);
            this.AddProject(heritageAvionics, "Flight Software Baseline", "On-board software decomposition", ProjectVisibility.Organization, ProjectMode.Regular, ProjectLifecycleKind.Archived, TimeSpan.FromDays(380), 3);
            this.AddProject(launchPartners, "Rideshare Interface Control", "Launcher interface definition", ProjectVisibility.Public, ProjectMode.Concurrent, ProjectLifecycleKind.Open, TimeSpan.FromDays(10), 5);
        }

        /// <summary>
        /// Gets the account that is using Bloom, which belongs to every mocked organization.
        /// </summary>
        public User CurrentUser { get; }

        /// <summary>
        /// Gets every mocked user, in each <see cref="ActivationStatus" />, including the current one.
        /// </summary>
        public IReadOnlyList<User> Users => this.users;

        /// <summary>
        /// Gets the mocked organizations by canonical, lower-case slug.
        /// </summary>
        public IReadOnlyDictionary<string, Organization> Organizations => this.organizations;

        /// <summary>
        /// Gets every mocked project, across all organizations.
        /// </summary>
        public IReadOnlyList<FunctionalProject> Projects => this.projects;

        /// <summary>
        /// Derives a stable identifier from a key.
        /// </summary>
        /// <param name="key">The key that is unique to the identified thing.</param>
        /// <returns>The identifier.</returns>
        private static Guid CreateId(string key)
        {
            return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
        }

        /// <summary>
        /// Sets who created and last updated a thing, and when.
        /// </summary>
        /// <param name="thing">The thing to stamp.</param>
        /// <param name="createdBy">The creator.</param>
        /// <param name="createdOn">The creation time.</param>
        /// <param name="updatedBy">The last editor.</param>
        /// <param name="updatedOn">The last update time.</param>
        private static void Stamp(IAuditableThing thing, IUser createdBy, DateTime createdOn, IUser updatedBy, DateTime updatedOn)
        {
            thing.CreatedBy = createdBy;
            thing.CreatedOn = createdOn;
            thing.UpdatedBy = updatedBy;
            thing.UpdatedOn = updatedOn;
        }

        /// <summary>
        /// Makes users members of an organization, from both sides of the membership.
        /// </summary>
        /// <param name="organization">The organization.</param>
        /// <param name="role">The role of the users in the organization.</param>
        /// <param name="members">The users.</param>
        private static void AddOrganizationMembers(Organization organization, OrganizationMembershipRole role, params IEnumerable<User> members)
        {
            foreach (var user in members)
            {
                var joinedOn = user.CreatedOn > organization.CreatedOn ? user.CreatedOn : organization.CreatedOn;

                var membership = new OrganizationMember
                {
                    Id = CreateId($"organization-member:{organization.Id}:{user.Email}"),
                    Organization = organization,
                    Role = role,
                    User = user
                };

                Stamp(membership, organization.CreatedBy, joinedOn, organization.CreatedBy, joinedOn);
                organization.InvolvedUser.Add(membership);
                user.IsPartOfOrganizations.Add(membership);
            }
        }

        /// <summary>
        /// Selects the role of a project member from its position in the project roster.
        /// </summary>
        /// <param name="position">The position of the member, the current account being first.</param>
        /// <param name="memberCount">The number of members of the project.</param>
        /// <param name="projectIndex">The index of the project, which varies the role of the current account.</param>
        /// <returns>The role.</returns>
        private static ProjectMemberRole SelectProjectRole(int position, int memberCount, int projectIndex)
        {
            if (position == 0)
            {
                return CurrentUserProjectRoles[projectIndex % CurrentUserProjectRoles.Count];
            }

            if (position == 1)
            {
                return ProjectMemberRole.Administrator;
            }

            return position >= memberCount - memberCount / 4 ? ProjectMemberRole.Viewer : ProjectMemberRole.Participant;
        }

        /// <summary>
        /// Selects the ownerships of a project member: none for a viewer, the system domain in addition for an administrator.
        /// </summary>
        /// <param name="project">The project, whose ownerships are already defined.</param>
        /// <param name="role">The role of the member.</param>
        /// <param name="position">The position of the member in the project roster.</param>
        /// <returns>The ownerships.</returns>
        private static List<IOwnership> SelectOwnerships(FunctionalProject project, ProjectMemberRole role, int position)
        {
            if (role == ProjectMemberRole.Viewer)
            {
                return [];
            }

            var domain = project.Defines[position % project.Defines.Count];

            return role == ProjectMemberRole.Administrator && domain != project.Defines[0] ? [project.Defines[0], domain] : [domain];
        }

        /// <summary>
        /// Rotates a list so that it starts at an offset, which spreads the same people over different projects.
        /// </summary>
        /// <param name="items">The list.</param>
        /// <param name="offset">The offset, taken modulo the list length.</param>
        /// <returns>The rotated items.</returns>
        private static IEnumerable<User> Rotate(List<User> items, int offset)
        {
            if (items.Count == 0)
            {
                return items;
            }

            var start = offset % items.Count;

            return items.Skip(start).Concat(items.Take(start));
        }

        /// <summary>
        /// Creates a user that registered itself; a pending user has not signed in yet and so has no external identifier.
        /// </summary>
        /// <param name="name">The full name.</param>
        /// <param name="email">The e-mail address, which is unique.</param>
        /// <param name="status">The activation status.</param>
        /// <returns>The user.</returns>
        private User AddUser(string name, string email, ActivationStatus status = ActivationStatus.Active)
        {
            var createdOn = this.now.AddDays(-1500 + 10 * this.users.Count);

            var user = new User
            {
                Id = CreateId($"user:{email}"),
                Name = name,
                Email = email,
                Status = status,
                ExternalIdentifier = status == ActivationStatus.Pending ? null : $"idp|{email[..email.IndexOf('@')]}"
            };

            Stamp(user, user, createdOn, user, createdOn.AddDays(7));
            this.users.Add(user);

            return user;
        }

        /// <summary>
        /// Creates an organization with its policy, both set up by the owner.
        /// </summary>
        /// <param name="slug">The canonical slug.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="status">The activation status.</param>
        /// <param name="owner">The user that created the organization.</param>
        /// <param name="policy">The policy, whose identifier and audit data are set here.</param>
        /// <returns>The organization.</returns>
        private Organization AddOrganization(string slug, string name, string description, ActivationStatus status, User owner, OrganizationPolicy policy)
        {
            var createdOn = this.now.AddDays(-1100 + 120 * this.organizations.Count);

            var organization = new Organization
            {
                Id = CreateId($"organization:{slug}"),
                Name = name,
                Description = description,
                Status = status,
                Policy = policy
            };

            policy.Id = CreateId($"organization-policy:{slug}");
            Stamp(organization, owner, createdOn, owner, createdOn.AddDays(30));
            Stamp(policy, owner, createdOn, owner, createdOn);
            this.organizations.Add(slug, organization);

            return organization;
        }

        /// <summary>
        /// Creates a project with its policy, ownerships, members, branch rules and reviews.
        /// </summary>
        /// <param name="organization">The owning organization, whose members are already set.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The short description.</param>
        /// <param name="visibility">Who can see the project.</param>
        /// <param name="mode">How the members work on the project.</param>
        /// <param name="lifecycle">The lifecycle state.</param>
        /// <param name="age">The time since the last update.</param>
        /// <param name="memberCount">The number of members, the current account included.</param>
        private void AddProject(Organization organization, string name, string description, ProjectVisibility visibility, ProjectMode mode,
            ProjectLifecycleKind lifecycle, TimeSpan age, int memberCount)
        {
            var updatedOn = this.now - age;
            var createdOn = updatedOn.AddDays(-45 - 15 * organization.Projects.Count);
            var owner = organization.InvolvedUser.First(member => member.Role == OrganizationMembershipRole.Owner).User;

            var project = new FunctionalProject
            {
                Id = CreateId($"project:{name}"),
                Name = name,
                Description = description,
                Visibility = visibility,
                CurrentMode = mode,
                Lifecycle = lifecycle,
                BelongsTo = organization,
                EngineeringProjectId = CreateId($"engineering-project:{name}"),
                SharedPreferences = visibility == ProjectVisibility.Private ? [] : new Dictionary<string, string> { ["units"] = "SI", ["diagramNotation"] = "SysML v2" },
                Policy = new FunctionalProjectPolicy
                {
                    Id = CreateId($"project-policy:{name}"),
                    AllowAutoNamespaceImport = visibility != ProjectVisibility.Private,
                    AllowAutoPublishMode = mode == ProjectMode.Concurrent,
                    AllowVersionBranching = lifecycle is ProjectLifecycleKind.Open or ProjectLifecycleKind.Review
                }
            };

            Stamp(project.Policy, owner, createdOn, owner, createdOn);

            foreach (var template in OwnershipTemplates.Take(Math.Clamp(2 + memberCount / 4, 2, OwnershipTemplates.Count)))
            {
                var ownership = new Ownership
                {
                    Id = CreateId($"ownership:{name}:{template.ShortName}"),
                    Name = template.Name,
                    ShortName = template.ShortName,
                    Description = template.Description,
                    EngineeringMetadataId = CreateId($"engineering-metadata:{name}:{template.ShortName}")
                };

                Stamp(ownership, owner, createdOn, owner, createdOn);
                project.Defines.Add(ownership);
            }

            this.AddProjectMembers(project, memberCount, owner, createdOn);
            Stamp(project, owner, createdOn, project.Involves[1].User, updatedOn);
            AddBranchRules(project);
            this.AddReviews(project);

            organization.Projects.Add(project);
            this.projects.Add(project);
        }

        /// <summary>
        /// Makes the current account, members of the owning organization and, in concurrent or public projects, two users
        /// from outside that organization members of a project.
        /// </summary>
        /// <param name="project">The project, whose ownerships are already defined.</param>
        /// <param name="memberCount">The number of members.</param>
        /// <param name="owner">The owner of the organization, who adds the members.</param>
        /// <param name="createdOn">The creation time of the project.</param>
        private void AddProjectMembers(FunctionalProject project, int memberCount, IUser owner, DateTime createdOn)
        {
            var organization = project.BelongsTo;
            var projectIndex = this.projects.Count;

            var insiders = organization.InvolvedUser.Select(membership => membership.User).OfType<User>().Where(user => user != this.CurrentUser).ToList();
            var outsiders = this.users.Where(user => user.IsPartOfOrganizations.All(membership => membership.Organization != organization)).ToList();

            var outsiderCount = project.CurrentMode == ProjectMode.Concurrent || project.Visibility == ProjectVisibility.Public ? Math.Min(2, outsiders.Count) : 0;

            List<User> memberUsers =
            [
                this.CurrentUser,
                .. Rotate(insiders, projectIndex).Take(memberCount - 1 - outsiderCount),
                .. Rotate(outsiders, projectIndex).Take(outsiderCount)
            ];

            for (var position = 0; position < memberUsers.Count; position++)
            {
                var user = memberUsers[position];
                var role = SelectProjectRole(position, memberUsers.Count, projectIndex);
                var joinedOn = createdOn.AddDays(position);

                var member = new ProjectMember
                {
                    Id = CreateId($"project-member:{project.Name}:{user.Email}"),
                    User = user,
                    IsPartOf = project,
                    Role = role,
                    Owns = SelectOwnerships(project, role, position)
                };

                member.ActiveOwnership = member.Owns.LastOrDefault();
                Stamp(member, owner, joinedOn, owner, joinedOn);
                project.Involves.Add(member);
                user.IsPartOfProjects.Add(member);
            }
        }

        /// <summary>
        /// Protects the main branch of a project once it leaves preparation, and its release and exploration branches when the
        /// project works concurrently or allows version branching.
        /// </summary>
        /// <param name="project">The project, whose members are already set.</param>
        private static void AddBranchRules(FunctionalProject project)
        {
            if (project.Lifecycle == ProjectLifecycleKind.Preparation)
            {
                return;
            }

            var administrators = project.Involves.Where(member => member.Role == ProjectMemberRole.Administrator).Select(member => member.User).ToList();
            var isConcurrent = project.CurrentMode == ProjectMode.Concurrent;

            project.BranchRules.Add(CreateBranchRule(project, "main", "Main baseline", true, isConcurrent ? 2 : 1, [ProjectMemberRole.Administrator], administrators));

            if (isConcurrent)
            {
                project.BranchRules.Add(CreateBranchRule(project, "release", "Release candidate", true, 1, [ProjectMemberRole.Administrator, ProjectMemberRole.Participant], []));
            }

            if (project.Policy.AllowVersionBranching)
            {
                project.BranchRules.Add(CreateBranchRule(project, "exploration", "Exploration", false, 0, [ProjectMemberRole.Administrator, ProjectMemberRole.Participant], []));
            }
        }

        /// <summary>
        /// Creates a protection rule on a branch of a project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="branch">The name of the protected branch.</param>
        /// <param name="name">The name of the rule.</param>
        /// <param name="reviewRequired">Whether merging into the branch requires a review.</param>
        /// <param name="minimumRequiredApprovals">The number of approvals required to merge.</param>
        /// <param name="mergeAllowedFor">The roles allowed to merge.</param>
        /// <param name="defaultReviewers">The reviewers proposed by default.</param>
        /// <returns>The rule.</returns>
        private static BranchProtectionRule CreateBranchRule(FunctionalProject project, string branch, string name, bool reviewRequired, int minimumRequiredApprovals,
            List<ProjectMemberRole> mergeAllowedFor, List<IUser> defaultReviewers)
        {
            var rule = new BranchProtectionRule
            {
                Id = CreateId($"branch-rule:{project.Name}:{branch}"),
                Name = name,
                EngineeringBranchId = CreateId($"branch:{project.Name}:{branch}"),
                ReviewRequired = reviewRequired,
                MinimumRequiredApprovals = minimumRequiredApprovals,
                MergeAllowedFor = mergeAllowedFor,
                DefaultReviewers = defaultReviewers
            };

            Stamp(rule, project.CreatedBy, project.CreatedOn, project.CreatedBy, project.CreatedOn);

            return rule;
        }

        /// <summary>
        /// Adds reviews to a project: none in preparation, one closed review when archived, and reviews in turn of every
        /// other status otherwise.
        /// </summary>
        /// <param name="project">The project, whose members are already set.</param>
        private void AddReviews(FunctionalProject project)
        {
            var reviewCount = project.Lifecycle switch
            {
                ProjectLifecycleKind.Preparation => 0,
                ProjectLifecycleKind.Archived => 1,
                ProjectLifecycleKind.Review => 3,
                _ => 2
            };

            for (var index = 0; index < reviewCount; index++)
            {
                var status = project.Lifecycle == ProjectLifecycleKind.Archived
                    ? ReviewStatus.Closed
                    : OngoingReviewStatuses[this.reviewSequence % OngoingReviewStatuses.Count];

                project.Reviews.Add(this.CreateReview(project, index, status));
                this.reviewSequence++;
            }
        }

        /// <summary>
        /// Creates a review of a project, opened by a contributor and assigned to up to two others; a review past its draft
        /// carries comments.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="index">The index of the review in the project.</param>
        /// <param name="status">The status.</param>
        /// <returns>The review.</returns>
        private Review CreateReview(FunctionalProject project, int index, ReviewStatus status)
        {
            var title = ReviewTitles[this.reviewSequence % ReviewTitles.Count];
            var contributors = project.Involves.Where(member => member.Role != ProjectMemberRole.Viewer).Select(member => member.User).ToList();
            var author = contributors[(index + 1) % contributors.Count];
            var createdOn = project.UpdatedOn.AddDays(-2 * (index + 1));

            var review = new Review
            {
                Id = CreateId($"review:{project.Name}:{title}"),
                Title = title,
                Description = $"{title} of {project.Name}.",
                Status = status,
                SourceBranchId = CreateId($"branch:{project.Name}:{title}"),
                TargetBranchId = CreateId($"branch:{project.Name}:main"),
                Reviewers = [.. contributors.Where(user => user != author).Take(2)]
            };

            Stamp(review, author, createdOn, author, createdOn.AddHours(6));

            if (status != ReviewStatus.Draft)
            {
                this.AddComments(review, author);
            }

            return review;
        }

        /// <summary>
        /// Adds a comment thread to a review: a first comment answered by the author and followed up by a reply quoting the
        /// answer, and a second comment quoting the first when the review has a second reviewer. Comments are resolved once
        /// the review is approved or closed.
        /// </summary>
        /// <param name="review">The review.</param>
        /// <param name="author">The author of the review.</param>
        private void AddComments(Review review, IUser author)
        {
            var status = review.Status is ReviewStatus.Approved or ReviewStatus.Closed ? CommentStatus.Resolved : CommentStatus.Open;
            var raisedOn = review.CreatedOn.AddHours(1);
            var reviewer = review.Reviewers.FirstOrDefault() ?? author;

            var comment = this.CreateComment(review, 0, reviewer, raisedOn, status);
            var answer = CreateReply(comment, 0, "Updated, thanks for spotting it.", author, raisedOn.AddHours(1), null);
            var followUp = CreateReply(comment, 1, "Confirmed, the change now reads correctly.", reviewer, raisedOn.AddHours(2), answer);
            comment.Replies.AddRange([answer, followUp]);
            review.Comments.Add(comment);

            if (review.Reviewers.Count > 1)
            {
                var secondComment = this.CreateComment(review, 1, review.Reviewers[1], raisedOn.AddHours(3), status);
                secondComment.Quotes = comment;
                review.Comments.Add(secondComment);
            }
        }

        /// <summary>
        /// Creates a comment on a model element under review.
        /// </summary>
        /// <param name="review">The review.</param>
        /// <param name="index">The index of the comment in the review.</param>
        /// <param name="author">The author.</param>
        /// <param name="postedOn">The posting time.</param>
        /// <param name="status">The status.</param>
        /// <returns>The comment.</returns>
        private Comment CreateComment(Review review, int index, IUser author, DateTime postedOn, CommentStatus status)
        {
            var comment = new Comment
            {
                Id = CreateId($"comment:{review.Id}:{index}"),
                Content = CommentContents[(this.reviewSequence + index) % CommentContents.Count],
                Status = status,
                TargetElementId = CreateId($"element:{review.Id}:{index}")
            };

            Stamp(comment, author, postedOn, author, postedOn);

            return comment;
        }

        /// <summary>
        /// Creates a reply to a comment.
        /// </summary>
        /// <param name="comment">The comment answered.</param>
        /// <param name="index">The index of the reply in the thread.</param>
        /// <param name="content">The content.</param>
        /// <param name="author">The author.</param>
        /// <param name="postedOn">The posting time.</param>
        /// <param name="quotes">The message quoted, if any.</param>
        /// <returns>The reply.</returns>
        private static Reply CreateReply(Comment comment, int index, string content, IUser author, DateTime postedOn, IMessage quotes)
        {
            var reply = new Reply
            {
                Id = CreateId($"reply:{comment.Id}:{index}"),
                Content = content,
                Quotes = quotes
            };

            Stamp(reply, author, postedOn, author, postedOn);

            return reply;
        }
    }
}
