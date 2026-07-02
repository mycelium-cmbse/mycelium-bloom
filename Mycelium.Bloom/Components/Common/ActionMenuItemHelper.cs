// ------------------------------------------------------------------------------------------------
// <copyright file="ActionMenuItemHelper.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Common
{
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Provides shared helpers for UI components that render <see cref="ActionMenuItem" /> entries.
    /// </summary>
    public static class ActionMenuItemHelper
    {
        /// <summary>
        /// Builds the CSS class list applied to an action menu item.
        /// </summary>
        /// <param name="item">The action menu item.</param>
        /// <param name="baseCssClass">The component-specific base CSS class for the item.</param>
        /// <returns>The action menu item CSS class list.</returns>
        public static string BuildItemCssClass(ActionMenuItem item, string baseCssClass)
        {
            ArgumentNullException.ThrowIfNull(item);

            var cssClass = CssClassBuilder.Build(
                baseCssClass,
                CssClassBuilder.When($"{baseCssClass}--danger", item.Variant == ActionMenuItemVariant.Danger),
                CssClassBuilder.When($"{baseCssClass}--disabled", item.Disabled),
                CssClassBuilder.When($"{baseCssClass}--separator", item.SeparatorBefore));

            return cssClass;
        }

        /// <summary>
        /// Checks whether the provided action menu item can be selected.
        /// </summary>
        /// <param name="item">The action menu item.</param>
        /// <returns>A value indicating whether the item is enabled.</returns>
        public static bool IsEnabled(ActionMenuItem item)
        {
            return !item.Disabled;
        }
    }
}
