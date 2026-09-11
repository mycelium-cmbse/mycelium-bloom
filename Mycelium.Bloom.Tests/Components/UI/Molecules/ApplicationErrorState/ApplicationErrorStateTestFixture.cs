// ------------------------------------------------------------------------------------------------
// <copyright file="ApplicationErrorStateTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.ApplicationErrorState
{
    using System.Threading.Tasks;

    using Bunit;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ApplicationErrorState;

    using ErrorStateComponent = Mycelium.Bloom.Components.UI.Molecules.ApplicationErrorState.ApplicationErrorState;

    /// <summary>
    /// Verifies the shared error View consumes and observes caller-owned reactive presentation state.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ApplicationErrorStateTestFixture : BunitContext
    {
        /// <summary>
        /// Configures the existing Blueprint controls.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Releases the bUnit renderer after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies the View renders content and native recovery attributes from its ViewModel.
        /// </summary>
        /// <param name="kind">The supplied presentation kind.</param>
        [TestCase(ApplicationErrorKind.NotFound)]
        [TestCase(ApplicationErrorKind.ServerError)]
        public void VerifyViewModelContract(ApplicationErrorKind kind)
        {
            using var viewModel = new ApplicationErrorStateViewModel(kind);
            using var component = this.Render<ErrorStateComponent>(parameters => parameters.Add(view => view.ViewModel, viewModel));
            var actions = component.FindAll(".application-error-state__actions > a, .application-error-state__actions > button");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("h1").TextContent, Is.EqualTo(viewModel.Title));
                Assert.That(component.Find("p").TextContent, Is.EqualTo(viewModel.Description));
                Assert.That(component.FindAll(".application-error-state__status"), Is.Empty);
                Assert.That(component.Markup, Does.Not.Contain("404").And.Not.Contain("500").And.Not.Contain("Request error"));
                Assert.That(actions, Has.Count.EqualTo(2));
                Assert.That(actions[0].TextContent.Trim(), Is.EqualTo(viewModel.PrimaryAction.Label));
                Assert.That(actions[0].GetAttribute("href"), Is.EqualTo(viewModel.PrimaryAction.Href));
                Assert.That(actions[0].GetAttribute("data-error-action"), Is.EqualTo(viewModel.PrimaryAction.BrowserAction));
                Assert.That(actions[1].TextContent.Trim(), Is.EqualTo(viewModel.SecondaryAction.Label));
                Assert.That(actions[1].GetAttribute("href"), Is.EqualTo(viewModel.SecondaryAction.Href));
                Assert.That(actions[1].GetAttribute("data-error-action"), Is.EqualTo(viewModel.SecondaryAction.BrowserAction));
            }
        }

        /// <summary>
        /// Verifies reference updates rerender on the page renderer without replacing component parameters.
        /// </summary>
        [Test]
        public async Task VerifyReactiveReferenceRendering()
        {
            using var viewModel = new ApplicationErrorStateViewModel(ApplicationErrorKind.ServerError);
            using var component = this.Render<ErrorStateComponent>(parameters => parameters.Add(view => view.ViewModel, viewModel));
            Assert.That(component.FindAll("dl"), Is.Empty);

            await component.InvokeAsync(() => viewModel.ReferenceId = "updated-reference");

            await component.WaitForAssertionAsync(() => Assert.That(component.Find("code").TextContent, Is.EqualTo("updated-reference")));

            await component.InvokeAsync(() => viewModel.ReferenceId = " ");

            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("dl"), Is.Empty));
        }

        /// <summary>
        /// Verifies replacement switches reactive observation and leaves both ViewModels caller-owned.
        /// </summary>
        [Test]
        public async Task VerifyViewModelReplacementAndDisposal()
        {
            using var first = new ApplicationErrorStateViewModel(ApplicationErrorKind.NotFound);
            using var second = new ApplicationErrorStateViewModel(ApplicationErrorKind.ServerError);
            using var component = this.Render<ErrorStateComponent>(parameters => parameters.Add(view => view.ViewModel, first));

            component.Render(parameters => parameters.Add(view => view.ViewModel, second));
            var renderCount = component.RenderCount;
            await component.InvokeAsync(() => first.ReferenceId = "detached-reference");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.RenderCount, Is.EqualTo(renderCount));
                Assert.That(component.Find("h1").TextContent, Is.EqualTo(second.Title));
                Assert.That(component.FindAll("dl"), Is.Empty);
            }

            await component.InvokeAsync(() => second.ReferenceId = "active-reference");
            await component.WaitForAssertionAsync(() => Assert.That(component.Find("code").TextContent, Is.EqualTo("active-reference")));
            await this.DisposeComponentsAsync();
            renderCount = component.RenderCount;

            await component.InvokeAsync(() => second.ReferenceId = "caller-still-owns-reference");

            Assert.That(component.RenderCount, Is.EqualTo(renderCount));
        }

        /// <summary>
        /// Verifies an unassigned View does not invent presentation state.
        /// </summary>
        [Test]
        public void VerifyMissingViewModel()
        {
            using var component = this.Render<ErrorStateComponent>();

            Assert.That(component.Markup, Is.Empty);
        }
    }
}
