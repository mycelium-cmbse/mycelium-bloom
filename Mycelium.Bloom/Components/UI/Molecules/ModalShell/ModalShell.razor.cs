// ------------------------------------------------------------------------------------------------
// <copyright file="ModalShell.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.ModalShell
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Represents a reusable modal overlay and dialog shell.
    /// </summary>
    public partial class ModalShell : BloomComponentBase
    {
        /// <summary>
        /// Indicates whether the close callbacks are currently running.
        /// </summary>
        private bool isClosing;

        /// <summary>
        /// Provides a stable generated identifier when no modal identifier is configured.
        /// </summary>
        private readonly string generatedId = $"mb-modal-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets or sets a value indicating whether the modal is open.
        /// </summary>
        [Parameter]
        public bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the open state changes.
        /// </summary>
        [Parameter]
        public EventCallback<bool> IsOpenChanged { get; set; }

        /// <summary>
        /// Gets or sets the modal identifier.
        /// </summary>
        [Parameter]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the modal title.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional modal description.
        /// </summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the modal size.
        /// </summary>
        [Parameter]
        public ModalSize Size { get; set; } = ModalSize.Medium;

        /// <summary>
        /// Gets or sets optional custom header content.
        /// </summary>
        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        /// <summary>
        /// Gets or sets the main modal body content.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets optional modal footer content.
        /// </summary>
        [Parameter]
        public RenderFragment FooterContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a backdrop click closes the modal.
        /// </summary>
        [Parameter]
        public bool CloseOnBackdropClick { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the close button is rendered.
        /// </summary>
        [Parameter]
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>
        /// Gets or sets the callback invoked after the modal is closed.
        /// </summary>
        [Parameter]
        public EventCallback OnClose { get; set; }

        /// <summary>
        /// Gets the stable identifier for the modal instance.
        /// </summary>
        /// <returns>The configured or generated modal identifier.</returns>
        private string GetModalId()
        {
            var modalId = !string.IsNullOrWhiteSpace(this.Id)
                ? this.Id
                : this.generatedId;

            return modalId;
        }

        /// <summary>
        /// Gets the identifier for the default title element.
        /// </summary>
        /// <returns>The modal title identifier.</returns>
        private string GetTitleId()
        {
            var titleId = $"{this.GetModalId()}-title";

            return titleId;
        }

        /// <summary>
        /// Gets the identifier for the default description element.
        /// </summary>
        /// <returns>The modal description identifier.</returns>
        private string GetDescriptionId()
        {
            var descriptionId = $"{this.GetModalId()}-description";

            return descriptionId;
        }

        /// <summary>
        /// Gets a value indicating whether the modal header should be rendered.
        /// </summary>
        /// <returns>A value indicating whether header content is available.</returns>
        private bool HasHeader()
        {
            var hasHeader = this.HeaderContent is not null ||
                            !string.IsNullOrWhiteSpace(this.Title) ||
                            !string.IsNullOrWhiteSpace(this.Description) ||
                            this.ShowCloseButton;

            return hasHeader;
        }

        /// <summary>
        /// Gets a value indicating whether the default title element is rendered.
        /// </summary>
        /// <returns>A value indicating whether the default title is available.</returns>
        private bool HasDefaultTitle()
        {
            var hasDefaultTitle = this.HeaderContent is null &&
                                  !string.IsNullOrWhiteSpace(this.Title);

            return hasDefaultTitle;
        }

        /// <summary>
        /// Gets a value indicating whether the default description element is rendered.
        /// </summary>
        /// <returns>A value indicating whether the default description is available.</returns>
        private bool HasDefaultDescription()
        {
            var hasDefaultDescription = this.HeaderContent is null &&
                                        !string.IsNullOrWhiteSpace(this.Description);

            return hasDefaultDescription;
        }

        /// <summary>
        /// Gets a fallback accessible label when the default title element is not rendered.
        /// </summary>
        /// <returns>The fallback dialog label, or <see langword="null" /> when the default title labels the dialog.</returns>
        private string GetDialogLabel()
        {
            if (this.HasDefaultTitle())
            {
                return null;
            }

            var dialogLabel = !string.IsNullOrWhiteSpace(this.Title)
                ? this.Title
                : "Dialog";

            return dialogLabel;
        }

        /// <summary>
        /// Gets the final CSS class list applied to the dialog panel.
        /// </summary>
        /// <returns>The dialog panel CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = this.BuildRootCssClass(
                "mb-modal__panel",
                this.GetSizeClass());

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class matching the selected modal size.
        /// </summary>
        /// <returns>The modal size CSS class.</returns>
        private string GetSizeClass()
        {
            var cssClass = this.Size switch
            {
                ModalSize.Small => "mb-modal__panel--small",
                ModalSize.Large => "mb-modal__panel--large",
                ModalSize.Wide => "mb-modal__panel--wide",
                _ => "mb-modal__panel--medium"
            };

            return cssClass;
        }

        /// <summary>
        /// Handles a backdrop click when backdrop closing is enabled.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleBackdropClickAsync()
        {
            if (this.CloseOnBackdropClick)
            {
                await this.CloseAsync();
            }
        }

        /// <summary>
        /// Closes the modal and invokes the close callbacks.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CloseAsync()
        {
            if (this.isClosing)
            {
                return;
            }

            this.isClosing = true;

            try
            {
                await this.IsOpenChanged.InvokeAsync(false);
                await this.OnClose.InvokeAsync();
            }
            finally
            {
                this.isClosing = false;
            }
        }
    }
}
