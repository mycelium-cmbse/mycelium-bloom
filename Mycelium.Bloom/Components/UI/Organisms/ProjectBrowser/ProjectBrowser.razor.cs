// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowser.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Renders a reusable tree browser for a loaded SysML project model.
    /// </summary>
    public partial class ProjectBrowser : ComponentBase
    {
        /// <summary>
        /// Gets or sets the project browser view model.
        /// </summary>
        [Parameter]
        public IProjectBrowserViewModel ViewModel { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the selected node changes.
        /// </summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> SelectedNodeChanged { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the tree container.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Starts loading the project browser after the component has rendered once.
        /// </summary>
        /// <param name="firstRender">A value indicating whether this is the first render.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender
                || this.ViewModel == null
                || this.ViewModel.IsLoaded
                || this.ViewModel.IsLoading
                || !string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage))
            {
                return;
            }

            await this.ViewModel.InitializeAsync();

            if (this.ViewModel.IsLoaded && this.ViewModel.SelectedNode != null)
            {
                await this.SelectedNodeChanged.InvokeAsync(this.ViewModel.SelectedNode);
            }

            await this.InvokeAsync(this.StateHasChanged);
        }

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-browser",
                this.Class);

            return cssClass;
        }

        private bool ShouldShowLoadingState()
        {
            if (this.ViewModel == null)
            {
                return true;
            }

            return this.ViewModel.IsLoading
                || (!this.ViewModel.IsLoaded && string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage));
        }

        private bool ShouldShowErrorState()
        {
            return this.ViewModel != null && !string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage);
        }

        private Task HandleStateChangedAsync()
        {
            return this.InvokeAsync(this.StateHasChanged);
        }

        private async Task HandleNodeSelectedAsync(ProjectBrowserNodeViewModel node)
        {
            await this.SelectedNodeChanged.InvokeAsync(node);
            await this.HandleStateChangedAsync();
        }
    }
}
