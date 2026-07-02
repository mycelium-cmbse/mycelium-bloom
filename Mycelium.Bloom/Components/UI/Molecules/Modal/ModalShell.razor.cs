// ------------------------------------------------------------------------------------------------
// <copyright file="ModalShell.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Molecules.Modal
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Reusable modal shell for Bloom dialogs and confirmation flows.
    /// </summary>
    public partial class ModalShell : ComponentBase
    {
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
        /// Gets or sets the callback invoked when the modal is closed.
        /// </summary>
        [Parameter]
        public EventCallback OnClose { get; set; }

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
        /// Gets or sets whether the close button should be shown.
        /// </summary>
        [Parameter]
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>
        /// Gets or sets whether clicking the backdrop closes the modal.
        /// </summary>
        [Parameter]
        public bool CloseOnBackdropClick { get; set; } = true;

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional custom header content.
        /// </summary>
        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        /// <summary>
        /// Gets or sets modal body content.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets optional footer content.
        /// </summary>
        [Parameter]
        public RenderFragment FooterContent { get; set; }

        /// <summary>
        /// Gets or sets unmatched attributes passed to the dialog panel.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        private string GetModalId()
        {
            var modalId = !string.IsNullOrWhiteSpace(this.Id)
                ? this.Id
                : this.generatedId;

            return modalId;
        }

        private string GetTitleId()
        {
            var titleId = $"{this.GetModalId()}-title";

            return titleId;
        }

        private bool HasHeader()
        {
            var hasHeader = this.HeaderContent != null ||
                            !string.IsNullOrWhiteSpace(this.Title) ||
                            !string.IsNullOrWhiteSpace(this.Description) ||
                            this.ShowCloseButton;

            return hasHeader;
        }

        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-modal__panel",
                this.GetSizeClass(),
                this.Class);

            return cssClass;
        }

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

        private async Task HandleBackdropClickAsync()
        {
            if (this.CloseOnBackdropClick)
            {
                await this.CloseAsync();
            }
        }

        private async Task CloseAsync()
        {
            await this.IsOpenChanged.InvokeAsync(false);
            await this.OnClose.InvokeAsync();
        }
    }
}
