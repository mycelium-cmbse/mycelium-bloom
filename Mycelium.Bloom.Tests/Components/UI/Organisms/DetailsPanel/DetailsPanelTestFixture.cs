// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.DetailsPanel
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Bunit;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using Panel = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;

    /// <summary>Verifies panel rendering and event boundaries with a mocked ViewModel.</summary>
    [TestFixture, FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DetailsPanelTestFixture : BunitContext
    {
        private readonly Mock<IDetailsPanelViewModel> viewModel = new();

        public DetailsPanelTestFixture()
        {
            BlueprintTestSetup.Configure(this);
            this.Services.AddSingleton<Func<IDetailsPanelViewModel>>(() => this.viewModel.Object);
        }

        [TearDown]
        public void TearDown() => this.Dispose();

        [Test]
        public void VerifyEmptyStateAndAccessibleRoot()
        {
            var component = this.Render<Panel>(parameters => parameters.Add(panel => panel.Class, "custom-details")
                .AddUnmatched("data-testid", "details"));
            var root = component.Find("section");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-details-panel__empty").TextContent, Does.Contain("Select an element"));
                Assert.That(root.ClassList, Does.Contain("custom-details"));
                Assert.That(component.Find("#" + root.GetAttribute("aria-labelledby")).TextContent, Is.EqualTo("Properties"));
                Assert.That(component.FindAll("[role='tab']"), Is.Empty);
            }
        }

        [Test]
        public async Task VerifyShellTabsAndNoUnsupportedControls()
        {
            this.SetPresentation(new PartUsage { DeclaredName = "Harness" });
            var closed = false;
            var component = this.Render<Panel>(parameters => parameters.Add(panel => panel.CloseRequested, () => closed = true));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='tab']").Select(tab => tab.TextContent.Trim()),
                    Is.EqualTo(new[] { "Properties", "Relations", "Comments", "Attachments" }));
                Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
                Assert.That(component.Markup, Does.Not.Contain("Back to previous").And.Not.Contain("SDK textual notation").And.Not.Contain("Advanced metadata"));
                Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Properties"));
                Assert.That(component.Find("[role='tab'][data-state='active']").GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='tab'][data-state='inactive']").All(tab => tab.GetAttribute("aria-selected") == "false"), Is.True);
            }
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Attachments").ClickAsync();
            Assert.That(component.Find("[role='tabpanel']").TextContent, Does.Contain("Attachments are not available for this model."));
            Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Comments").ClickAsync();
            Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Relations").ClickAsync();
            Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
            await component.Find("[aria-label='Close Properties']").ClickAsync();
            Assert.That(closed, Is.True);
            this.viewModel.Verify(vm => vm.ClearSelection(), Times.Never);
        }

        [Test]
        public async Task VerifyCanonicalReferenceAndBrowserCallbacks()
        {
            var target = new PartDefinition { DeclaredName = "HarnessDef" };
            var part = new PartUsage { DeclaredName = "Harness" };
            BrowserAcceptanceModelTestFactory.AddRelationship(part, new SysML2.NET.Core.POCO.Core.Features.FeatureTyping { Type = target });
            this.SetPresentation(part);
            IElement revealed = null;
            var component = this.Render<Panel>(parameters => parameters.Add(panel => panel.ShowInBrowserRequested, element => revealed = element));
            await component.Find("button[aria-label='Inspect Typed by: HarnessDef']").ClickAsync();
            this.viewModel.Verify(vm => vm.Inspect(target), Times.Once);
            await component.FindAll("button").Single(button => button.TextContent.Trim() == "Show in Browser").ClickAsync();
            Assert.That(revealed, Is.SameAs(part));
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Relations").ClickAsync();
            Assert.That(component.Find("section[aria-label='Typing'] .mb-relation-identity").TextContent, Does.Contain("HarnessDef"));
            await component.Find("button[aria-label='Inspect Typed by: HarnessDef']").ClickAsync();
            this.viewModel.Verify(vm => vm.Inspect(target), Times.Exactly(2));
        }

        [Test]
        public async Task VerifyReactiveReplacementAndNullSelection()
        {
            this.SetPresentation(new Namespace { DeclaredName = "First" });
            var component = this.Render<Panel>();
            await component.InvokeAsync(() => this.SetPresentation(new Namespace { DeclaredName = "Second" }));
            component.WaitForAssertion(() => Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Second")));
            await component.InvokeAsync(() => this.SetPresentation(null));
            component.WaitForAssertion(() => Assert.That(component.FindAll("dl"), Is.Empty));
        }

        [Test]
        public async Task VerifyOwnedAndCallerSuppliedViewModelLifetimes()
        {
            var component = this.Render<Panel>();
            var replacement = new Mock<IDetailsPanelViewModel>();
            component.Render(parameters => parameters.Add(panel => panel.ViewModel, replacement.Object));
            this.viewModel.Verify(vm => vm.Dispose(), Times.Once);
            await this.DisposeComponentsAsync();
            replacement.Verify(vm => vm.Dispose(), Times.Never);
            this.viewModel.Verify(vm => vm.Dispose(), Times.Once);
        }

        [Test]
        public async Task VerifyUnmountDisposesFactoryStateExactlyOnce()
        {
            this.Render<Panel>();
            await this.DisposeComponentsAsync();
            this.viewModel.Verify(vm => vm.Dispose(), Times.Once);
        }

        private void SetPresentation(IElement element)
        {
            this.viewModel.SetupGet(vm => vm.Presentation).Returns(element == null ? null : new DetailsPanelPresentation(element));
            this.viewModel.Raise(vm => vm.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IDetailsPanelViewModel.Presentation)));
        }
    }
}
