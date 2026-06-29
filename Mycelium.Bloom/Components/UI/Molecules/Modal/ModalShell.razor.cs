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

    public partial class ModalShell : ComponentBase
    {
        private readonly string generatedId = $"mb-modal-{Guid.NewGuid():N}";

        [Parameter]
        public bool IsOpen { get; set; }

        [Parameter]
        public EventCallback<bool> IsOpenChanged { get; set; }

        [Parameter]
        public EventCallback OnClose { get; set; }

        [Parameter]
        public string Id { get; set; } = string.Empty;

        [Parameter]
        public string Title { get; set; } = string.Empty;

        [Parameter]
        public string Description { get; set; } = string.Empty;

        [Parameter]
        public ModalSize Size { get; set; } = ModalSize.Medium;

        [Parameter]
        public bool ShowCloseButton { get; set; } = true;

        [Parameter]
        public bool CloseOnBackdropClick { get; set; } = true;

        [Parameter]
        public string Class { get; set; } = string.Empty;

        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment FooterContent { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }

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
