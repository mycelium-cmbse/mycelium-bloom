// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationHeaderViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Organization
{
    using System.Reactive.Linq;

    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.SDK.POCO;

    using Organization = Mycelium.SDK.POCO.Organization;

    using ReactiveUI;
    using ReactiveUI.Primitives.Concurrency;

    /// <summary>
    /// Derives the header of the account and organization pages from the organization context.
    /// </summary>
    public sealed class OrganizationHeaderViewModel : DisposableViewModel, IOrganizationHeaderViewModel
    {
        /// <summary>
        /// The entries of the account menu, which do not depend on the organization.
        /// </summary>
        private static readonly ActionMenuItem[] AccountItems =
            [new() { Id = OrganizationRoutes.AccountPath, Label = "Your profile" }];

        /// <summary>
        /// The pages of an organization, in the order of the menu.
        /// </summary>
        private static readonly SectionDescriptor[] Sections =
        [
            new(OrganizationSection.Overview, "Overview", "layout-dashboard"),
            new(OrganizationSection.Projects, "Projects", "folder"),
            new(OrganizationSection.Members, "Members", "users"),
            new(OrganizationSection.Settings, "Settings", "settings")
        ];

        /// <summary>
        /// Observes the presentation derived from the organization context.
        /// </summary>
        private readonly ObservableAsPropertyHelper<OrganizationHeaderPresentation> presentation;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationHeaderViewModel" /> class.
        /// </summary>
        /// <param name="context">The organization context that the header follows.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context" /> is <see langword="null" />.</exception>
        public OrganizationHeaderViewModel(IOrganizationContextViewModel context)
        {
            ArgumentNullException.ThrowIfNull(context);

            this.SelectItemCommand = ReactiveCommand.Create<ActionMenuItem, string>(item => item.Id);

            this.presentation = context
                .WhenAnyValue(model => model.IsLoading, model => model.Location, model => model.Account, model => model.Organization,
                    model => model.Capabilities)
                .Select(_ => CreatePresentation(context))
                .ToProperty(this, viewModel => viewModel.Presentation, initialValue: OrganizationHeaderPresentation.Empty,
                    scheduler: ImmediateSequencer.Instance);

            this.Disposables.Add(this.presentation);
            this.Disposables.Add(this.SelectItemCommand);
        }

        /// <summary>
        /// Gets what the header displays. It follows the organization context.
        /// </summary>
        public OrganizationHeaderPresentation Presentation => this.presentation.Value;

        /// <summary>
        /// Gets the command that turns the selection of a menu entry into the route to navigate to.
        /// </summary>
        public ReactiveCommand<ActionMenuItem, string> SelectItemCommand { get; }

        /// <summary>
        /// Derives the presentation of the header from the properties of the organization context. Nothing is
        /// displayed while the context loads, once it failed, and while the properties of a new resolution are still
        /// being announced one by one, so a stale organization is never shown.
        /// </summary>
        /// <param name="context">The organization context.</param>
        /// <returns>The presentation of the header.</returns>
        private static OrganizationHeaderPresentation CreatePresentation(IOrganizationContextViewModel context)
        {
            var account = context.Account;

            if (context.IsLoading || account is null)
            {
                return OrganizationHeaderPresentation.Empty;
            }

            var location = context.Location;
            var organization = context.Organization;
            var capabilities = context.Capabilities;

            var accountPresentation = new OrganizationHeaderPresentation
            {
                Kind = location.Kind,
                AccountName = account.Name,
                AccountEmail = account.Mail,
                AccountItems = AccountItems
            };

            if (location.Kind != OrganizationLocationKind.Organization)
            {
                return accountPresentation;
            }

            if (organization is null || capabilities is null)
            {
                return OrganizationHeaderPresentation.Empty;
            }

            var current = Sections.First(descriptor => descriptor.Section == location.Section);

            return accountPresentation with
            {
                OrganizationName = organization.Name,
                OrganizationPath = OrganizationRoutes.GetOrganizationPath(location.Slug, OrganizationSection.Overview),
                SectionLabel = current.Label,
                SectionIcon = current.Icon,
                SectionItems = GetSectionItems(location.Slug, location.Section, capabilities)
            };
        }

        /// <summary>
        /// Builds the pages of the organization that the current account may open.
        /// </summary>
        /// <param name="slug">The slug of the designated organization, which the address carries.</param>
        /// <param name="section">The current page of the organization.</param>
        /// <param name="capabilities">What the current account may do in the organization.</param>
        /// <returns>The menu entries, in the order of the pages.</returns>
        private static ActionMenuItem[] GetSectionItems(string slug, OrganizationSection section, OrganizationCapabilities capabilities)
        {
            return Sections
                .Where(descriptor => descriptor.Section != OrganizationSection.Settings || capabilities.CanEditSettings)
                .Select(descriptor => new ActionMenuItem
                {
                    Id = OrganizationRoutes.GetOrganizationPath(slug, descriptor.Section),
                    Label = descriptor.Label,
                    IsSelected = descriptor.Section == section
                })
                .ToArray();
        }

        /// <summary>
        /// Describes how a page of an organization appears in the header.
        /// </summary>
        /// <param name="Section">The page of the organization.</param>
        /// <param name="Label">The label of the page.</param>
        /// <param name="Icon">The name of the icon of the page.</param>
        private sealed record SectionDescriptor(OrganizationSection Section, string Label, string Icon);
    }
}
