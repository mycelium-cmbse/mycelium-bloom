// ------------------------------------------------------------------------------------------------
// <copyright file="MembersView.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.MembersView
{
    using System.Globalization;

    /// <summary>
    /// Shows the people of an organization with a search and a filter by kind. It flips on the busy state, the errors and the
    /// rows of its ViewModel, and does the searching, the filtering, the counting and the paging itself, because the
    /// ViewModel holds the whole list for any client.
    /// </summary>
    public sealed partial class MembersView
    {
        /// <summary>
        /// The number of rows from which the table shows its pager.
        /// </summary>
        private const int PageSize = 25;

        /// <summary>
        /// The role label of a person who belongs to no organization role.
        /// </summary>
        private const string OutsideCollaboratorLabel = "Outside collaborator";

        /// <summary>
        /// The words of the chip that shows every person.
        /// </summary>
        private const string AllLabel = "All";

        /// <summary>
        /// The words of the chip that shows the Outside Collaborators, which only an account that may see them is offered.
        /// </summary>
        private const string OutsideCollaboratorsLabel = "Outside collaborators";

        /// <summary>
        /// The chips of the filter, in order, each with the words it shows and the test that a row passes to be counted and shown
        /// by it. The owner of the organization administers it, so it counts with the administrators. An Outside Collaborator has
        /// no organization role, and only a member can be deactivated, which a suspended or an archived account is.
        /// </summary>
        private static readonly (string Label, Func<MemberRowViewModel, bool> Matches)[] Filters =
        [
            (AllLabel, _ => true),
            ("Administrators", row => row.Role is OrganizationMembershipRole.Administrator or OrganizationMembershipRole.Owner),
            ("Members", row => row.Role == OrganizationMembershipRole.Member),
            (OutsideCollaboratorsLabel, row => row.Role is null),
            ("Deactivated", row => row.Role is not null && row.User.Status is ActivationStatus.Suspended or ActivationStatus.Archived)
        ];

        /// <summary>
        /// Gets or sets the text that the name or the email of the people must contain.
        /// </summary>
        private string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the words of the chip that the list is narrowed to.
        /// </summary>
        private string FilterLabel { get; set; } = AllLabel;

        /// <summary>
        /// Gets the people that match the search and the chip.
        /// </summary>
        private IReadOnlyList<MemberRowViewModel> FilteredRows => [.. this.ViewModel.Rows.Where(this.Matches)];

        /// <summary>
        /// Gets the chips that the filter offers: the one of the Outside Collaborators only for an account that may see them.
        /// </summary>
        private IEnumerable<(string Label, Func<MemberRowViewModel, bool> Matches)> Chips =>
            Filters.Where(filter => filter.Label != OutsideCollaboratorsLabel || this.ViewModel.CanViewOutsideCollaborators);

        /// <summary>
        /// Gets a value indicating whether the last load failed.
        /// </summary>
        private bool HasFailed => this.ViewModel.Errors.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the search and the chips are shown: when there are people to narrow, and when a
        /// load fails, so that the user can change the criteria and see that nothing else is wrong.
        /// </summary>
        private bool ShowsToolbar => this.HasFailed || this.ViewModel.Rows.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the loading placeholder replaces the table.
        /// </summary>
        private bool ShowsSkeleton => this.ViewModel.IsBusy && this.ViewModel.Rows.Count == 0;

        /// <summary>
        /// Gets a value indicating whether the organization has no people at all.
        /// </summary>
        private bool ShowsEmptyOrganization => !this.ViewModel.IsBusy && this.ViewModel.Rows.Count == 0;

        /// <summary>
        /// Gets the words under the table: how many people it shows out of the people of the organization.
        /// </summary>
        private string ShowingText => $"Showing {this.FilteredRows.Count} of {"person".ToQuantity(this.ViewModel.Rows.Count)}";

        /// <summary>
        /// Gets the title of the empty table, which names the search when there is one.
        /// </summary>
        private string NoResultsTitle => string.IsNullOrWhiteSpace(this.SearchText)
            ? "No member matches this filter"
            : $"No member matches \"{this.SearchText.Trim()}\"";

        /// <summary>
        /// Builds the style that gives the avatar of a person the collaborator colour of their email address.
        /// </summary>
        /// <param name="row">The row of the person.</param>
        /// <returns>The style.</returns>
        private static string GetAvatarStyle(MemberRowViewModel row)
        {
            return string.Create(CultureInfo.InvariantCulture, $"background: var(--collaborator-c{row.User.GetAvatarColor():D2})");
        }

        /// <summary>
        /// Gets the CSS class that tints the role badge of an Organization Administrator and of the owner, who administers the
        /// organization too.
        /// </summary>
        /// <param name="row">The row of the person.</param>
        /// <returns>The CSS class, or an empty text for any other role.</returns>
        private static string GetRoleCssClass(MemberRowViewModel row)
        {
            return row.Role is OrganizationMembershipRole.Administrator or OrganizationMembershipRole.Owner ? "mb-members-view__role--administrator" : string.Empty;
        }

        /// <summary>
        /// Tells whether a row passes the chosen chip and contains the search in its name or its email.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns><see langword="true" /> when the row matches both.</returns>
        private bool Matches(MemberRowViewModel row)
        {
            var text = this.SearchText.Trim();

            return Filters.Single(filter => filter.Label == this.FilterLabel).Matches(row)
                   && (row.User.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || row.User.Email.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Empties the search and shows every kind of person again.
        /// </summary>
        private void HandleClearClicked()
        {
            this.SearchText = string.Empty;
            this.FilterLabel = AllLabel;
        }
    }
}
