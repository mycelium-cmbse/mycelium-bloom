// ------------------------------------------------------------------------------------------------
// <copyright file="Error.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ApplicationErrorState;

    /// <summary>
    /// Represents the default error page displayed when an unhandled exception occurs.
    /// </summary>
    public partial class Error : IDisposable
    {
        /// <summary>
        /// Gets the presentation state owned by this error page.
        /// </summary>
        private ApplicationErrorStateViewModel ViewModel { get; } = new(ApplicationErrorKind.ServerError);

        /// <summary>
        /// Gets or sets the current HTTP context provided as a cascading parameter.
        /// </summary>
        [CascadingParameter]
        private HttpContext HttpContext { get; set; } = default;

        /// <summary>
        /// Gets or sets the request identifier preserved from the initial error response.
        /// </summary>
        [PersistentState]
        public string RequestId
        {
            get => this.ViewModel.ReferenceId;
            set => this.ViewModel.ReferenceId = value;
        }

        /// <summary>
        /// Initializes the component and resolves the request identifier for the current error request.
        /// </summary>
        protected override void OnInitialized()
        {
            this.RequestId ??= Activity.Current?.Id ?? this.HttpContext?.TraceIdentifier;
        }

        /// <summary>
        /// Releases the presentation state owned by this page.
        /// </summary>
        public void Dispose()
        {
            this.ViewModel.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
