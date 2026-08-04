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
    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Represents a controlled compact project-selection composition backed by a styled Blueprint menu.
    /// </summary>
    public partial class ProjectSwitcher : BloomComponentBase
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
        /// Gets or sets the callback invoked when a project is selected.
        /// </summary>
        [Parameter]
        public EventCallback<string> SelectedProjectIdChanged { get; set; }

        /// <summary>
        /// Gets or sets the placeholder displayed when no project is selected.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = "Select project";

        /// <summary>
        /// Gets or sets a value indicating whether project selection is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the accessible trigger label prefix.
        /// </summary>
        [Parameter]
        public string AriaLabel { get; set; } = "Select project";

        /// <summary>
        /// Gets the action-menu projection of the available projects.
        /// </summary>
        private IReadOnlyList<ActionMenuItem> ActionItems { get; set; } = [];

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            this.ActionItems = this.Items
                .Select(item => new ActionMenuItem
                {
                    Id = item.Id,
                    Label = item.Name,
                    Description = item.Description,
                    Icon = GetInitial(item),
                    Disabled = item.Disabled,
                    IsSelected = string.Equals(item.Id, this.SelectedProjectId, StringComparison.Ordinal)
                })
                .ToArray();
        }

        /// <summary>
        /// Gets the final CSS class list applied to the project-switcher root.
        /// </summary>
        /// <returns>The project-switcher CSS class list.</returns>
        private string GetCssClass()
        {
            return this.BuildRootCssClass(
                "mb-project-switcher",
                CssClassBuilder.When("mb-project-switcher--disabled", this.Disabled));
        }

        /// <summary>
        /// Gets the selected project from the controlled parameter value.
        /// </summary>
        /// <returns>The selected project, or null when no item matches.</returns>
        private ProjectSwitcherItem GetSelectedProject()
        {
            return this.Items.FirstOrDefault(item =>
                string.Equals(item.Id, this.SelectedProjectId, StringComparison.Ordinal));
        }

        /// <summary>
        /// Gets the initial displayed by the current-project trigger.
        /// </summary>
        /// <returns>The selected project initial or a neutral fallback.</returns>
        private string GetCurrentInitial()
        {
            var selectedProject = this.GetSelectedProject();

            return selectedProject is null ? "P" : GetInitial(selectedProject);
        }

        /// <summary>
        /// Gets a compact initial for a project.
        /// </summary>
        /// <param name="item">The project option.</param>
        /// <returns>The configured initial, the first name character, or a neutral fallback.</returns>
        private static string GetInitial(ProjectSwitcherItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Initial))
            {
                return item.Initial;
            }

            return string.IsNullOrWhiteSpace(item.Name)
                ? "P"
                : char.ToUpperInvariant(item.Name[0]).ToString();
        }

        /// <summary>
        /// Gets an accessible label that announces the current project.
        /// </summary>
        /// <returns>The project-switcher trigger label.</returns>
        private string GetTriggerAriaLabel()
        {
            var selectedProject = this.GetSelectedProject();

            return selectedProject is null
                ? this.AriaLabel
                : $"{this.AriaLabel}. Current project: {selectedProject.Name}";
        }

        /// <summary>
        /// Maps the selected action back to the controlled project identifier callback.
        /// </summary>
        /// <param name="action">The selected projected action.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleActionSelectedAsync(ActionMenuItem action)
        {
            var project = this.Items.FirstOrDefault(item =>
                string.Equals(item.Id, action.Id, StringComparison.Ordinal));

            if (this.Disabled || project is null || project.Disabled)
            {
                return;
            }

            await this.SelectedProjectIdChanged.InvokeAsync(project.Id);
        }
    }
}
