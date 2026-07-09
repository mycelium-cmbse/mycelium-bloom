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
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Represents the Bloom home page with the issue #8 Project Browser feature.
    /// </summary>
    public partial class Home : ComponentBase
    {
        /// <summary>
        /// Gets the selected model element name.
        /// </summary>
        private string SelectedModelElementName
        {
            get
            {
                var displayName = this.SelectedProjectBrowserNode?.DisplayName;

                return string.IsNullOrWhiteSpace(displayName) ? "None" : displayName;
            }
        }

        /// <summary>
        /// Gets or sets the selected project browser node.
        /// </summary>
        private ProjectBrowserNodeViewModel SelectedProjectBrowserNode { get; set; }

        /// <summary>
        /// Handles project browser node selection changes.
        /// </summary>
        /// <param name="node">The selected project browser node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task HandleProjectBrowserNodeSelectedAsync(ProjectBrowserNodeViewModel node)
        {
            this.SelectedProjectBrowserNode = node;

            return this.InvokeAsync(this.StateHasChanged);
        }
    }
}
