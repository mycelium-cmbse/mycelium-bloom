// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContextViewTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.OrganizationContextView
{
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;

    using Moq;

    using Mycelium.Bloom.Components.UI.Molecules.OrganizationContextView;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Tests the <see cref="OrganizationContextView" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class OrganizationContextViewTestFixture : BunitContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationContextViewTestFixture" /> class.
        /// </summary>
        public OrganizationContextViewTestFixture()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        /// <returns>A task that completes when the context is disposed.</returns>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies that the page content is displayed once the organization context is ready, and that a status or
        /// the matching error content replaces it otherwise.
        /// </summary>
        /// <param name="state">The state of the organization context.</param>
        /// <param name="selector">The selector of the content that is displayed.</param>
        /// <param name="showsPageContent">A value indicating whether the page content is displayed.</param>
        [TestCase(OrganizationContextState.Ready, ".page-content", true)]
        [TestCase(OrganizationContextState.Loading, "[role='status']", false)]
        [TestCase(OrganizationContextState.NotFound, "[data-error-action='back']", false)]
        [TestCase(OrganizationContextState.Error, "[data-error-action='reload']", false)]
        public void VerifyStateSelectsTheDisplayedContent(OrganizationContextState state, string selector, bool showsPageContent)
        {
            var component = this.RenderFor(state);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(selector), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".page-content"), Has.Count.EqualTo(showsPageContent ? 1 : 0));
            }
        }

        /// <summary>
        /// Renders the component over a mocked ViewModel in the given state.
        /// </summary>
        /// <param name="state">The state of the mocked ViewModel.</param>
        /// <returns>The rendered component.</returns>
        private IRenderedComponent<OrganizationContextView> RenderFor(OrganizationContextState state)
        {
            var viewModel = new Mock<IOrganizationContextViewModel>();
            viewModel.SetupGet(model => model.State).Returns(state);
            RenderFragment content = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "page-content");
                builder.CloseElement();
            };

            return this.Render<OrganizationContextView>(parameters => parameters
                .Add(view => view.ViewModel, viewModel.Object)
                .Add(view => view.ChildContent, content));
        }
    }
}
