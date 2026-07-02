// ------------------------------------------------------------------------------------------------
// <copyright file="DesignSystemTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages.DesignSystem
{
    using System.Collections.Generic;
    using System.Linq;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using DesignSystemComponent = Mycelium.Bloom.Components.Pages.DesignSystem.DesignSystem;

    /// <summary>
    /// Tests the <see cref="DesignSystemComponent" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DesignSystemTestFixture : BunitContext
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
        /// Verifies that the design-system page renders component samples and handles sample dialogs.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysDesignSystemSamples()
        {
            var projectBrowserViewModelService = this.RegisterProjectBrowserViewModelService();
            SetupKeyboardNavigationModule(this);

            var component = this.Render<DesignSystemComponent>();

            FindButton(component, "Open modal").Click();
            FindButton(component, "Default").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Bloom Design System"));
                Assert.That(component.Markup, Does.Contain("Foundations"));
                Assert.That(component.Markup, Does.Contain("Atoms"));
                Assert.That(component.Markup, Does.Contain("Molecules"));
                Assert.That(component.Markup, Does.Contain("Organisms"));
                Assert.That(component.Markup, Does.Contain("Feedback"));
                Assert.That(component.Markup, Does.Contain("Forms"));
                Assert.That(component.Markup, Does.Contain("Collaboration"));
                Assert.That(component.Markup, Does.Contain("Review pending model changes"));
                Assert.That(component.Markup, Does.Contain("Commit workspace changes"));
                Assert.That(component.FindAll(".mb-panel"), Is.Not.Empty);
                Assert.That(component.FindAll(".mb-button"), Is.Not.Empty);
                Assert.That(component.FindAll(".mb-model-tree"), Is.Not.Empty);
                projectBrowserViewModelService.Verify(x => x.CreateQuantitiesProjectBrowserViewModel(), Times.Once);
            }

            component.Find(".mb-modal__close-button").Click();
            FindButton(component, "Default").Click();
            component.FindAll(".mb-confirm-dialog__footer .mb-button")[1].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Not.Contain("Review pending model changes"));
                Assert.That(component.Markup, Does.Not.Contain("Commit workspace changes"));
            }
        }

        /// <summary>
        /// Finds a button by its rendered text content.
        /// </summary>
        /// <param name="component">The rendered design-system page.</param>
        /// <param name="text">The text to find.</param>
        /// <returns>The matching button element.</returns>
        private static AngleSharp.Dom.IElement FindButton(IRenderedComponent<DesignSystemComponent> component, string text)
        {
            return component.FindAll("button")
                .First(button => button.TextContent.Contains(text));
        }

        /// <summary>
        /// Sets up the keyboard navigation JavaScript module.
        /// </summary>
        /// <param name="context">The bUnit test context.</param>
        private static void SetupKeyboardNavigationModule(BunitContext context)
        {
            var module = context.JSInterop.SetupModule("/js/keyboardNavigation.js");

            module.SetupVoid("registerNavigationKeyPrevention", _ => true).SetVoidResult();
            module.SetupVoid("disposeNavigationKeyPrevention", _ => true).SetVoidResult();
        }

        /// <summary>
        /// Registers the mocked project browser ViewModel service required by the design-system page.
        /// </summary>
        /// <returns>The registered project browser ViewModel service mock.</returns>
        private Mock<IProjectBrowserViewModelService> RegisterProjectBrowserViewModelService()
        {
            IReadOnlyList<ProjectBrowserNodeViewModel> rootNodes = [];

            var viewModel = new Mock<IProjectBrowserViewModel>(MockBehavior.Strict);

            viewModel.SetupGet(x => x.RootNodes).Returns(rootNodes);
            viewModel.SetupGet(x => x.IsLoading).Returns(false);
            viewModel.SetupGet(x => x.IsLoaded).Returns(true);
            viewModel.SetupGet(x => x.ErrorMessage).Returns(string.Empty);

            var projectBrowserViewModelService = new Mock<IProjectBrowserViewModelService>(MockBehavior.Strict);

            projectBrowserViewModelService
                .Setup(x => x.CreateQuantitiesProjectBrowserViewModel())
                .Returns(viewModel.Object);

            this.Services.AddSingleton(projectBrowserViewModelService.Object);

            return projectBrowserViewModelService;
        }
    }
}
