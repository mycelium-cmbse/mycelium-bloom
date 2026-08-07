// ------------------------------------------------------------------------------------------------
// <copyright file="BloomReactiveComponentBase.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Common
{
    using System.ComponentModel;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;

    using ReactiveUI.Blazor;

    /// <summary>
    /// Provides common Bloom parameters for reactive components whose ViewModel is supplied as a parameter.
    /// </summary>
    /// <typeparam name="TViewModel">The observable ViewModel type.</typeparam>
    public abstract class BloomReactiveComponentBase<TViewModel> : ReactiveComponentBase<TViewModel>, IBloomComponentBase
        where TViewModel : class, INotifyPropertyChanged
    {
        /// <inheritdoc />
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <inheritdoc />
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Builds the CSS class list for a component root and appends the configured custom class.
        /// </summary>
        /// <param name="cssClasses">The component-owned CSS classes.</param>
        /// <returns>The root CSS classes separated by spaces.</returns>
        protected string BuildRootCssClass(params string[] cssClasses)
        {
            var rootCssClass = CssClassBuilder.Build([.. cssClasses, this.Class]);

            return rootCssClass;
        }
    }
}
