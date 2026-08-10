// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanel.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DetailsPanel
{
    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Core.Selection;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Displays the identifying properties of a selected SysML element.
    /// </summary>
    public sealed partial class DetailsPanel : BloomReactiveInjectableComponentBase<IElementSelectionService>
    {
        /// <summary>
        /// The stable identifier of the panel heading.
        /// </summary>
        private readonly string headingId = $"mb-details-panel-title-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets the injected selection service required by this component.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the selection service has not been provided through dependency injection.
        /// </exception>
        private IElementSelectionService RequiredViewModel =>
            this.ViewModel
            ?? throw new InvalidOperationException(
                $"{nameof(DetailsPanel)} requires an {nameof(IElementSelectionService)}.");

        /// <summary>
        /// Gets the currently selected SysML element.
        /// </summary>
        private IElement SelectedElement => this.RequiredViewModel.SelectedElement;

        /// <summary>
        /// Gets the final CSS class list applied to the details panel.
        /// </summary>
        /// <returns>The details-panel CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass("mb-details-panel");
        }

        /// <summary>
        /// Gets the best available display name for an element.
        /// </summary>
        /// <param name="element">The element to describe.</param>
        /// <returns>The display name for the element.</returns>
        private static string GetDisplayName(IElement element)
        {
            var displayName = element.DeclaredName.ToDisplayString();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = element.name.ToDisplayString();
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = element.qualifiedName.ToDisplayString();
            }

            return string.IsNullOrWhiteSpace(displayName) ? element.GetType().Name : displayName;
        }

        /// <summary>
        /// Gets an invariant property value for display.
        /// </summary>
        /// <param name="value">The value to display.</param>
        /// <returns>The converted value, or an em dash when the value is unavailable.</returns>
        private static string GetDisplayValue(object value)
        {
            var displayValue = value.ToDisplayString();

            return string.IsNullOrWhiteSpace(displayValue) ? "—" : displayValue;
        }
    }
}
