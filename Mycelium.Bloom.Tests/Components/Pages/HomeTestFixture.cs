// ------------------------------------------------------------------------------------------------
// <copyright file="HomeTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Tests the <see cref="Home" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class HomeTestFixture : BunitContext
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
        /// Verifies that the home page displays the expected workspace content.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysHomeContent()
        {
            var viewModel = this.RegisterProjectBrowserViewModel();

            var component = this.Render<Home>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Project Browser"));
                Assert.That(component.Markup, Does.Contain("Quantities model"));
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Selected element"));
                Assert.That(component.Find(".mb-project-browser"), Is.Not.Null);
            }

            viewModel.Verify(x => x.InitializeAsync(), Times.Never);
        }

        private Mock<IProjectBrowserViewModel> RegisterProjectBrowserViewModel()
        {
            var viewModel = new Mock<IProjectBrowserViewModel>();
            viewModel.SetupGet(x => x.IsLoading).Returns(true);

            this.Services.AddSingleton(viewModel.Object);

            return viewModel;
        }
    }
}
