// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationLayoutTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Layout
{
    using System;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Layout;
    using Mycelium.Bloom.Components.UI.Molecules.OrganizationContextView;
    using Mycelium.Bloom.Components.UI.Organisms.OrganizationHeader;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Tests the <see cref="OrganizationLayout" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class OrganizationLayoutTestFixture : BunitContext
    {
        /// <summary>
        /// The mocked organization context owned by the layout.
        /// </summary>
        private readonly Mock<IOrganizationContextViewModel> viewModel = new();

        /// <summary>
        /// The mocked header state owned by the layout.
        /// </summary>
        private readonly Mock<IOrganizationHeaderViewModel> headerViewModel = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationLayoutTestFixture" /> class.
        /// </summary>
        public OrganizationLayoutTestFixture()
        {
            BlueprintTestSetup.Configure(this);
            this.viewModel.SetupGet(model => model.State).Returns(OrganizationContextState.Ready);
            this.Services.AddSingleton<Func<IOrganizationContextViewModel>>(() => this.viewModel.Object);
            this.headerViewModel.SetupGet(model => model.Presentation).Returns(OrganizationHeaderPresentation.Empty);
            this.Services.AddSingleton<Func<IOrganizationContextViewModel, IOrganizationHeaderViewModel>>(
                _ => this.headerViewModel.Object);
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
        /// Verifies that the layout hands the header state to the header, the organization context to the content area
        /// and the body of the page to the content area, and releases both ViewModels it created when it is disposed.
        /// </summary>
        [Test]
        public void VerifyLayoutWiresItsPartsAndReleasesItsViewModels()
        {
            RenderFragment body = builder => builder.AddContent(0, "Page content");

            var component = this.Render<OrganizationLayout>(parameters => parameters
                .Add(layout => layout.Body, body));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponent<OrganizationHeader>().Instance.ViewModel, Is.SameAs(this.headerViewModel.Object));
                Assert.That(component.FindComponent<OrganizationContextView>().Instance.ViewModel, Is.SameAs(this.viewModel.Object));
                Assert.That(component.Find("main").TextContent, Does.Contain("Page content"));
            }

            component.Instance.Dispose();

            this.viewModel.Verify(model => model.Dispose(), Times.Once);
            this.headerViewModel.Verify(model => model.Dispose(), Times.Once);
        }
    }
}
