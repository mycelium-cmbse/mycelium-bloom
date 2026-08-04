// ------------------------------------------------------------------------------------------------
// <copyright file="NavMenu.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Layout
{
    using Mycelium.Bloom.Components.Common;

    /// <summary>
    /// Presents Bloom's primary links as a desktop sidebar and a narrow-screen disclosure.
    /// </summary>
    public partial class NavMenu
    {
        /// <summary>
        /// Gets a value indicating whether the narrow-screen navigation disclosure is open.
        /// </summary>
        private bool IsExpanded { get; set; }

        /// <summary>
        /// Toggles the narrow-screen navigation disclosure.
        /// </summary>
        private void ToggleNavigation()
        {
            this.IsExpanded = !this.IsExpanded;
        }

        /// <summary>
        /// Closes the narrow-screen navigation after link activation.
        /// </summary>
        private void CloseNavigation()
        {
            this.IsExpanded = false;
        }

        /// <summary>
        /// Gets the navigation-link container classes.
        /// </summary>
        /// <returns>The final class list.</returns>
        private string GetLinksCssClass()
        {
            return CssClassBuilder.Build(
                "mb-nav-menu__links",
                CssClassBuilder.When("mb-nav-menu__links--expanded", this.IsExpanded));
        }
    }
}
