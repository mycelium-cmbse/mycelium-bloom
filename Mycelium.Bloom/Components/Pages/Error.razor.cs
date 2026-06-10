namespace Mycelium.Bloom.Components.Pages
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Represents the default error page displayed when an unhandled exception occurs.
    /// </summary>
    public partial class Error
    {
        /// <summary>
        /// Gets or sets the current HTTP context provided as a cascading parameter.
        /// </summary>
        [CascadingParameter]
        private HttpContext HttpContext { get; set; } = default!;

        /// <summary>
        /// Gets the request identifier associated with the current error.
        /// </summary>
        private string RequestId { get; set; }

        /// <summary>
        /// Gets a value indicating whether the request identifier should be displayed.
        /// </summary>
        private bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

        /// <summary>
        /// Initializes the component and resolves the request identifier for the current error request.
        /// </summary>
        protected override void OnInitialized()
        {
            this.RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
        }
    }
}