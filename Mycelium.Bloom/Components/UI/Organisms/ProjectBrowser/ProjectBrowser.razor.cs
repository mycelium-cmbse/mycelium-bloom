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

    using ReactiveUI.Blazor;
    using ReactiveUI.Primitives.Signals;

    /// <summary>
    /// Renders a reusable tree browser for a loaded SysML project model.
    /// </summary>
    public partial class ProjectBrowser : ReactiveInjectableComponentBase<IProjectBrowserViewModel>
    {
        /// <summary>
        /// A value indicating whether the component has been disposed.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Gets or sets additional CSS classes applied to the component root element.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional unmatched attributes applied to the component root element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } =
            new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the callback invoked when the selected node changes.
        /// </summary>
        [Parameter]
        public EventCallback<ProjectBrowserNodeViewModel> SelectedNodeChanged { get; set; }

        /// <summary>
        /// Initializes the project browser view model owned by this component.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task OnInitializedAsync()
        {
            if (this.ViewModel.IsLoaded
                || this.ViewModel.IsLoading
                || !string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage))
            {
                return;
            }

            await this.ViewModel.InitializeCommand.Execute();

            if (!this.isDisposed && this.ViewModel.IsLoaded && this.ViewModel.SelectedNode != null)
            {
                await this.SelectedNodeChanged.InvokeAsync(this.ViewModel.SelectedNode);
            }
        }

        /// <summary>
        /// Marks the component as disposed before ReactiveUI deactivates its view model.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release managed resources; otherwise, <see langword="false" />.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.isDisposed = true;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the final CSS class list applied to the project browser.
        /// </summary>
        /// <returns>The project browser CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-browser",
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Checks whether the loading state should be rendered.
        /// </summary>
        /// <returns>A value indicating whether the loading state should be shown.</returns>
        private bool ShouldShowLoadingState()
        {
            return this.ViewModel.IsLoading
                   || (!this.ViewModel.IsLoaded && string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage));
        }

        /// <summary>
        /// Checks whether the error state should be rendered.
        /// </summary>
        /// <returns>A value indicating whether the error state should be shown.</returns>
        private bool ShouldShowErrorState()
        {
            return !string.IsNullOrWhiteSpace(this.ViewModel.ErrorMessage);
        }

        /// <summary>
        /// Handles node selection and forwards the selected node to the parent component.
        /// </summary>
        /// <param name="node">The selected project browser node.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleNodeSelectedAsync(ProjectBrowserNodeViewModel node)
        {
            if (node == null)
            {
                return;
            }

            if (node.HasChildren)
            {
                await this.ViewModel.ToggleNodeCommand.Execute(node);
            }

            await this.ViewModel.SelectNodeCommand.Execute(node);

            if (!this.isDisposed)
            {
                await this.SelectedNodeChanged.InvokeAsync(node);
            }
        }
    }
}
