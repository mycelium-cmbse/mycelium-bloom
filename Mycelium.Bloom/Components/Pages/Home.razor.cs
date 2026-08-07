// ------------------------------------------------------------------------------------------------
// <copyright file="Home.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using System.ComponentModel;
    using System.Globalization;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Core.Selection;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Renders the Bloom home workspace.
    /// </summary>
    public sealed partial class Home : ComponentBase, IDisposable
    {
        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Gets or sets the shared element selection service.
        /// </summary>
        [Inject]
        public IElementSelectionService ElementSelectionService { get; set; }

        /// <summary>
        /// Subscribes to the shared selected element.
        /// </summary>
        protected override void OnInitialized()
        {
            this.ElementSelectionService.PropertyChanged += this.HandleSelectionChanged;

            base.OnInitialized();
        }

        /// <summary>
        /// Removes the shared selection subscription.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.ElementSelectionService.PropertyChanged -= this.HandleSelectionChanged;
        }

        /// <summary>
        /// Queues a renderer-safe refresh when the selected element changes.
        /// </summary>
        /// <param name="sender">The notification source.</param>
        /// <param name="eventArgs">The changed property.</param>
        private void HandleSelectionChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.PropertyName)
                || eventArgs.PropertyName == nameof(IElementSelectionService.SelectedElement))
            {
                this.QueueRender();
            }
        }

        /// <summary>
        /// Dispatches a render only while this component remains alive.
        /// </summary>
        private void QueueRender()
        {
            if (this.isDisposed)
            {
                return;
            }

            _ = this.InvokeAsync(() =>
            {
                if (!this.isDisposed)
                {
                    this.StateHasChanged();
                }
            });
        }

        /// <summary>
        /// Gets the best available display name for the selected element.
        /// </summary>
        /// <param name="element">The selected element, or <see langword="null" />.</param>
        /// <returns>The selected element display name.</returns>
        private static string GetSelectedElementName(IElement element)
        {
            if (element == null)
            {
                return "None";
            }

            var displayName = ToDisplayString(element.DeclaredName);

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = ToDisplayString(element.name);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = ToDisplayString(element.qualifiedName);
            }

            return string.IsNullOrWhiteSpace(displayName) ? element.GetType().Name : displayName;
        }

        /// <summary>
        /// Converts a SysML SDK value into an invariant display string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value, or an empty string when no value is available.</returns>
        private static string ToDisplayString(object value)
        {
            var displayString = Convert.ToString(value, CultureInfo.InvariantCulture);

            return displayString ?? string.Empty;
        }
    }
}
