// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationHeader.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.OrganizationHeader
{
    using System.Reactive.Linq;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents the header of the account and organization pages: the logo, the organization and its pages when
    /// an organization is displayed, and the account menu.
    /// </summary>
    public sealed partial class OrganizationHeader
    {
        /// <summary>
        /// Gets or sets the navigation service that opens the page selected in a menu.
        /// </summary>
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        /// <summary>
        /// Opens the page that the ViewModel associates with a selected menu entry.
        /// </summary>
        /// <param name="item">The selected menu entry.</param>
        /// <returns>A task that completes when the navigation has been requested.</returns>
        private async Task HandleItemSelectedAsync(ActionMenuItem item)
        {
            var target = await this.ViewModel.SelectItemCommand.Execute(item);

            this.NavigationManager.NavigateTo(target);
        }
    }
}
