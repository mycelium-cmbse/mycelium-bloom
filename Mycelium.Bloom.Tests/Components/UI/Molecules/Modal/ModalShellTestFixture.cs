// ------------------------------------------------------------------------------------------------
// <copyright file="ModalShellTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.Modal
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using ModalShellComponent = Mycelium.Bloom.Components.UI.Molecules.Modal.ModalShell;

    /// <summary>
    /// Tests the <see cref="ModalShellComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ModalShellTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that the modal renders configured header, body, footer, and attributes.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysConfiguredModal()
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Id, "edit-modal")
                .Add(component => component.Title, "Edit element")
                .Add(component => component.Description, "Update selected element properties.")
                .Add(component => component.Size, ModalSize.Wide)
                .Add(component => component.Class, "custom-modal")
                .Add(component => component.ChildContent, "<p>Body content</p>")
                .Add(component => component.FooterContent, "<button>Save</button>")
                .AddUnmatched("data-testid", "edit-modal"));

            var modal = component.Find("[role='dialog']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(modal.GetAttribute("id"), Is.EqualTo("edit-modal"));
                Assert.That(modal.GetAttribute("aria-modal"), Is.EqualTo("true"));
                Assert.That(modal.GetAttribute("aria-labelledby"), Is.EqualTo("edit-modal-title"));
                Assert.That(modal.GetAttribute("data-testid"), Is.EqualTo("edit-modal"));
                Assert.That(modal.GetAttribute("class"), Does.Contain("mb-modal__panel--wide"));
                Assert.That(modal.GetAttribute("class"), Does.Contain("custom-modal"));
                Assert.That(component.Find(".mb-modal__title").TextContent.Trim(), Is.EqualTo("Edit element"));
                Assert.That(component.Find(".mb-modal__description").TextContent.Trim(), Is.EqualTo("Update selected element properties."));
                Assert.That(component.Find(".mb-modal__body").TextContent.Trim(), Is.EqualTo("Body content"));
                Assert.That(component.Find(".mb-modal__footer").TextContent.Trim(), Is.EqualTo("Save"));
            }
        }

        /// <summary>
        /// Verifies that close button and backdrop invoke close callbacks.
        /// </summary>
        [Test]
        public void VerifyCloseActionsInvokeCallbacks()
        {
            var openChangedCount = 0;
            var closeCount = 0;

            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.IsOpenChanged, value =>
                {
                    if (!value)
                    {
                        openChangedCount++;
                    }
                })
                .Add(component => component.OnClose, () => closeCount++)
                .Add(component => component.Title, "Edit element")
                .Add(component => component.ChildContent, "Body"));

            component.Find(".mb-modal__close-button").Click();
            component.Find(".mb-modal__backdrop").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(openChangedCount, Is.EqualTo(2));
                Assert.That(closeCount, Is.EqualTo(2));
            }
        }

        /// <summary>
        /// Verifies that hidden, custom-header, and non-closing backdrop states render correctly.
        /// </summary>
        [Test]
        public void VerifyRenderHandlesOptionalStates()
        {
            var closeCount = 0;

            var closedComponent = this.Render<ModalShellComponent>();
            var customHeaderComponent = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.HeaderContent, "<h2>Custom header</h2>")
                .Add(component => component.ChildContent, "Body")
                .Add(component => component.ShowCloseButton, false)
                .Add(component => component.CloseOnBackdropClick, false)
                .Add(component => component.OnClose, () => closeCount++)
                .Add(component => component.Size, ModalSize.Small));

            customHeaderComponent.Find(".mb-modal__backdrop").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closedComponent.FindAll(".mb-modal"), Is.Empty);
                Assert.That(customHeaderComponent.Find(".mb-modal__header").TextContent.Trim(), Is.EqualTo("Custom header"));
                Assert.That(customHeaderComponent.Find(".mb-modal__panel").GetAttribute("class"), Does.Contain("mb-modal__panel--small"));
                Assert.That(customHeaderComponent.FindAll(".mb-modal__close-button"), Is.Empty);
                Assert.That(closeCount, Is.EqualTo(0));
            }
        }

        /// <summary>
        /// Verifies that large and medium modal sizes use the expected CSS classes.
        /// </summary>
        /// <param name="size">The modal size.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(ModalSize.Medium, "mb-modal__panel--medium")]
        [TestCase(ModalSize.Large, "mb-modal__panel--large")]
        public void VerifyRenderUsesExpectedSizeClass(ModalSize size, string expectedCssClass)
        {
            var component = this.Render<ModalShellComponent>(parameters => parameters
                .Add(component => component.IsOpen, true)
                .Add(component => component.Size, size)
                .Add(component => component.ChildContent, "Body"));

            Assert.That(component.Find(".mb-modal__panel").GetAttribute("class"), Does.Contain(expectedCssClass));
        }
    }
}
