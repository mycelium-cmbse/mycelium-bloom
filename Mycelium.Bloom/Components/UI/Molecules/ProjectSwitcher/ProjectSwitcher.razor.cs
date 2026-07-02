// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectSwitcher.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.ProjectSwitcher
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom project switcher for compact application header project selection.
    /// </summary>
    public partial class ProjectSwitcher : ComponentBase
    {
        /// <summary>
        /// Gets or sets the available project options.
        /// </summary>
        [Parameter]
        public IReadOnlyList<ProjectSwitcherItem> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the selected project identifier.
        /// </summary>
        [Parameter]
        public string SelectedProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the callback invoked when the selected project identifier changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> SelectedProjectIdChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text shown when no project is selected.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = "Select project";

        /// <summary>
        /// Gets or sets a value indicating whether the project switcher is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the project switcher wrapper.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether the dropdown is currently open.
        /// </summary>
        private bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets whether focus is currently inside the project switcher.
        /// </summary>
        private bool HasFocusWithin { get; set; }

        /// <summary>
        /// Closes the dropdown when the project switcher becomes disabled.
        /// </summary>
        protected override void OnParametersSet()
        {
            if (this.Disabled)
            {
                this.CloseDropdown();
            }
        }

        /// <summary>
        /// Gets the final CSS class list applied to the project switcher wrapper.
        /// </summary>
        /// <returns>The project switcher CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-switcher",
                CssClassBuilder.When("mb-project-switcher--disabled", this.Disabled),
                CssClassBuilder.When("mb-project-switcher--open", this.IsOpen),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class list applied to a project option.
        /// </summary>
        /// <param name="item">The project option.</param>
        /// <returns>The project option CSS class list.</returns>
        private string GetItemClass(ProjectSwitcherItem item)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-project-switcher__item",
                CssClassBuilder.When("mb-project-switcher__item--selected", this.IsSelected(item)),
                CssClassBuilder.When("mb-project-switcher__item--disabled", item.Disabled));

            return cssClass;
        }

        /// <summary>
        /// Gets the currently selected project when it exists in the available options.
        /// </summary>
        /// <returns>The selected project option, or null when no matching option exists.</returns>
        private ProjectSwitcherItem GetSelectedProject()
        {
            var selectedProject = this.Items.FirstOrDefault(item => item.Id == this.SelectedProjectId);

            return selectedProject;
        }

        /// <summary>
        /// Gets a value indicating whether the provided project option is selected.
        /// </summary>
        /// <param name="item">The project option.</param>
        /// <returns>True when the option is selected; otherwise, false.</returns>
        private bool IsSelected(ProjectSwitcherItem item)
        {
            var isSelected = item.Id == this.SelectedProjectId;

            return isSelected;
        }

        /// <summary>
        /// Toggles whether the dropdown is open.
        /// </summary>
        private void ToggleDropdown()
        {
            if (this.Disabled)
            {
                return;
            }

            if (this.IsOpen)
            {
                this.CloseDropdown();
            }
            else
            {
                this.OpenDropdown();
            }
        }

        /// <summary>
        /// Selects the provided project option when it is enabled.
        /// </summary>
        /// <param name="item">The selected project option.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectItemAsync(ProjectSwitcherItem item)
        {
            if (item.Disabled)
            {
                return;
            }

            this.SelectedProjectId = item.Id;
            this.CloseDropdown();

            await this.SelectedProjectIdChanged.InvokeAsync(item.Id);
        }

        /// <summary>
        /// Tracks focus entering the project switcher.
        /// </summary>
        private void HandleFocusIn()
        {
            this.HasFocusWithin = true;
        }

        /// <summary>
        /// Closes the dropdown when focus leaves the project switcher.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleFocusOutAsync()
        {
            this.HasFocusWithin = false;

            await Task.Delay(100);

            if (!this.HasFocusWithin)
            {
                this.CloseDropdown();
            }
        }

        /// <summary>
        /// Opens the dropdown.
        /// </summary>
        private void OpenDropdown()
        {
            this.IsOpen = true;
        }

        /// <summary>
        /// Closes the dropdown.
        /// </summary>
        private void CloseDropdown()
        {
            this.IsOpen = false;
        }
    }
}
