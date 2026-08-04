// ------------------------------------------------------------------------------------------------
// <copyright file="HomeViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel
{
    using System.Globalization;

    using Mycelium.Bloom.Core.Selection;

    using ReactiveUI;
    using ReactiveUI.Primitives;
    using ReactiveUI.Primitives.Disposables;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Provides reactive selection projections for the Bloom home workspace.
    /// </summary>
    public sealed class HomeViewModel : ReactiveObject, IActivatableViewModel
    {
        /// <summary>
        /// The shared element selection service.
        /// </summary>
        private readonly IElementSelectionService elementSelectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel" /> class.
        /// </summary>
        /// <param name="elementSelectionService">The shared element selection service.</param>
        public HomeViewModel(IElementSelectionService elementSelectionService)
        {
            ArgumentNullException.ThrowIfNull(elementSelectionService);

            this.elementSelectionService = elementSelectionService;
            this.Activator = new ViewModelActivator();

            this.WhenActivated((MultipleDisposable disposables) =>
            {
                System.ObservableExtensions
                    .Subscribe(
                        this.elementSelectionService.WhenAnyValue(service => service.SelectedElement),
                        _ => this.NotifySelectionChanged())
                    .DisposeWith(disposables);
            });
        }

        /// <inheritdoc />
        public ViewModelActivator Activator { get; }

        /// <summary>
        /// Gets the selected element directly from the shared source of truth.
        /// </summary>
        public IElement SelectedElement => this.elementSelectionService.SelectedElement;

        /// <summary>
        /// Gets the best available display name for the selected element.
        /// </summary>
        public string SelectedElementName
        {
            get
            {
                var element = this.SelectedElement;

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
        }

        /// <summary>
        /// Raises changes for the Home projections of the shared selection.
        /// </summary>
        private void NotifySelectionChanged()
        {
            this.RaisePropertyChanged(nameof(this.SelectedElement));
            this.RaisePropertyChanged(nameof(this.SelectedElementName));
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
