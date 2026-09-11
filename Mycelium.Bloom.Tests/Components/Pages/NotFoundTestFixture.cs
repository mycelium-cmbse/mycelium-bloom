// ------------------------------------------------------------------------------------------------
// <copyright file="NotFoundTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System;
    using System.Threading.Tasks;

    using Bunit;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using ErrorStateComponent = Mycelium.Bloom.Components.UI.Molecules.ApplicationErrorState.ApplicationErrorState;

    /// <summary>
    /// Tests the <see cref="NotFound" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class NotFoundTestFixture : BunitContext
    {
        /// <summary>
        /// Configures the page's Blueprint controls.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that the not found page displays the expected title and message.
        /// </summary>
        [Test]
        public void VerifyNotFoundPresentation()
        {
            var component = this.Render<NotFound>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("h1").TextContent, Is.EqualTo("This branch leads nowhere."));
                Assert.That(component.Find("p").TextContent, Is.EqualTo("The page may have moved or the route no longer exists."));
                Assert.That(component.Find("a").GetAttribute("href"), Is.EqualTo("/workspace/modeling"));
                Assert.That(component.Find("a").TextContent.Trim(), Is.EqualTo("Back to Modelling"));
                Assert.That(component.Find("button").TextContent.Trim(), Is.EqualTo("Go back"));
                Assert.That(component.Find("button").GetAttribute("type"), Is.EqualTo("button"));
                Assert.That(component.FindAll("dl"), Is.Empty);
                Assert.That(component.Markup, Does.Not.Contain("Not Found").And.Not.Contain("Sorry, the content"));
            }
        }

        /// <summary>
        /// Verifies that going back uses browser history without substituting a fixed route.
        /// </summary>
        [Test]
        public void VerifyGoBack()
        {
            var component = this.Render<NotFound>();

            Assert.That(component.Find("button").GetAttribute("data-error-action"), Is.EqualTo("back"));
        }

        /// <summary>
        /// Verifies the missing-route page configures and releases its own presentation state.
        /// </summary>
        [Test]
        public async Task VerifyPageOwnsNotFoundState()
        {
            var component = this.Render<NotFound>();
            var viewModel = component.FindComponent<ErrorStateComponent>().Instance.ViewModel;

            Assert.That(viewModel.Kind, Is.EqualTo(ApplicationErrorKind.NotFound));

            await this.DisposeComponentsAsync();

            Assert.That(() => viewModel.ReferenceId = "late-reference", Throws.TypeOf<ObjectDisposedException>());
        }
    }
}
