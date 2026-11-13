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
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;

    using Panel = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;

    [TestFixture, FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DetailsPanelTestFixture : BunitContext
    {
        private static readonly string[] ExpectedTabTitles = ["Properties", "Relations", "Comments", "Attachments"];

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
                Assert.That(component.FindAll("[role='tab']").Select(tab => tab.TextContent.Trim()), Is.EqualTo(ExpectedTabTitles));
                Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
                Assert.That(component.Markup, Does.Not.Contain("Back to previous").And.Not.Contain("SDK textual notation").And.Not.Contain("Advanced metadata"));
                Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Properties"));
                Assert.That(component.Find("[role='tab'][data-state='active']").GetAttribute("aria-selected"), Is.EqualTo("true"));
                Assert.That(component.FindAll("[role='tab'][data-state='inactive']").All(tab => tab.GetAttribute("aria-selected") == "false"), Is.True);
            }

            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Attachments").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='tabpanel']").TextContent, Does.Contain("Attachments are not available for this model."));
                Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
            }
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Comments").ClickAsync();
            Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Relations").ClickAsync();
            Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Harness"));
            await component.Find("[aria-label='Close Properties']").ClickAsync();
            Assert.That(closed, Is.True);
            this.viewModel.Verify(vm => vm.ClearSelection(), Times.Never);
        }

        [Test]
        public void VerifySelectedElementIdIsRenderedOnceInPropertiesIdentity()
        {
            var selected = new PartUsage { ElementId = "selected-part", Id = Guid.NewGuid(), DeclaredName = "Harness" };
            this.SetPresentation(selected);
            var component = this.Render<Panel>();
            var identity = component.Find("[role='tabpanel'] section[aria-label='Identity']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Properties"));
                Assert.That(identity.QuerySelector("[data-property-name='id'] dd")?.TextContent.Trim(), Is.EqualTo(selected.ElementId));
                Assert.That(component.FindAll("dd").Count(field => field.TextContent.Trim() == selected.ElementId), Is.EqualTo(1));
                Assert.That(component.Markup, Does.Not.Contain(selected.Id.ToString()).And.Not.Contain("Advanced metadata"));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        public void VerifyMissingElementIdDoesNotUseSdkIdAsFallback(string elementId)
        {
            var selected = new PartUsage { ElementId = elementId, Id = Guid.NewGuid(), DeclaredName = "Harness" };
            this.SetPresentation(selected);
            var component = this.Render<Panel>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[data-property-name='id']"), Is.Empty);
                Assert.That(component.Markup, Does.Not.Contain(selected.Id.ToString()));
            }
        }

        [Test]
        public async Task VerifyCanonicalReferenceAndBrowserCallbacks()
        {
            var target = new PartDefinition { ElementId = "harness-definition", DeclaredName = "HarnessDef" };
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
            await component.WaitForAssertionAsync(() => Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Second")));
            await component.InvokeAsync(() => this.SetPresentation(null));
            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("dl"), Is.Empty));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifySelfAndUnresolvedReferencesAreNotKeyboardControls(bool isSelfReference)
        {
            var selected = new PartUsage { DeclaredName = "Harness" };
            var link = new ElementLinkPresentation("Target", isSelfReference ? selected : null, "Harness");
            var component = this.Render<Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.PropertyReference>(parameters => parameters
                .Add(reference => reference.Link, link)
                .Add(reference => reference.SelectedElement, selected)
                .Add(reference => reference.InspectRequested, _ => Assert.Fail("A read-only reference cannot navigate.")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("span").TextContent.Trim(), Is.EqualTo("Harness"));
                Assert.That(component.FindAll("button, a, [tabindex], [role='button']"), Is.Empty);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("selected")]
        public void VerifyUnavailableAndEquivalentReferenceIdentitiesAreNotActionable(string elementId)
        {
            var selected = new PartUsage { ElementId = "selected", DeclaredName = "Selected" };
            var target = new PartUsage { ElementId = elementId, DeclaredName = "Reference" };
            var component = this.Render<Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.PropertyReference>(parameters => parameters
                .Add(reference => reference.Link, new ElementLinkPresentation("Target", target, "Reference"))
                .Add(reference => reference.SelectedElement, selected)
                .Add(reference => reference.InspectRequested, _ => Assert.Fail("This reference must be read-only.")));

            Assert.That(component.FindAll("button, a, [tabindex], [role='button']"), Is.Empty);
            Assert.That(component.Find("span").TextContent.Trim(), Is.EqualTo("Reference"));
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

        [TestCase("A")]
        [TestCase(null)]
        public async Task VerifyTabsResetOnlyWhenDisplayedElementChanges(string elementId)
        {
            var first = new Namespace { ElementId = elementId, DeclaredName = "First" };
            this.SetPresentation(first);
            var component = this.Render<Panel>();
            var instance = component.Instance;
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Comments").ClickAsync();
            await component.InvokeAsync(() => this.SetPresentation(first));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Comments")));
            component.Render(parameters => parameters.Add(panel => panel.Class, "unrelated-rerender"));
            Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Comments"));
            if (elementId is not null)
            {
                await component.InvokeAsync(() => this.SetPresentation(new Namespace { ElementId = elementId, DeclaredName = "Refreshed" }));
                await component.WaitForAssertionAsync(() =>
                    Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Comments")));
            }

            await component.InvokeAsync(() => this.SetPresentation(new Namespace { ElementId = "B", DeclaredName = "Second" }));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Properties")));
            await component.FindAll("[role='tab']").Single(tab => tab.TextContent == "Attachments").ClickAsync();
            await component.InvokeAsync(() => this.SetPresentation(null));
            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("[role='tab']"), Is.Empty));
            await component.InvokeAsync(() => this.SetPresentation(first));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("[role='tab'][data-state='active']").TextContent, Is.EqualTo("Properties")));
            Assert.That(component.Instance, Is.SameAs(instance));
        }

        [Test]
        public async Task VerifyLeadingControlSwapRetainsTitleAndDoesNotReplayOnRerender()
        {
            var urls = new Mock<IWorkspaceUrlContextService>();
            urls.Setup(context => context.GoBackAsync()).Returns(Task.CompletedTask);
            this.SetPresentation(new Namespace { ElementId = "A" });
            var component = this.Render<Panel>(parameters => parameters.AddCascadingValue(urls.Object));
            var title = component.Find("h2").Id;
            var leading = component.Find(".mb-details-panel__leading");
            Assert.That(leading.QuerySelector("button"), Is.Null);
            Assert.That(component.FindAll(".mb-details-panel__leading-control--changed"), Is.Empty);
            this.viewModel.SetupGet(vm => vm.IsInspecting).Returns(true);
            await component.InvokeAsync(() => this.SetPresentation(new Namespace { ElementId = "B" }));
            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("button[aria-label='Go Back']"), Has.Count.EqualTo(1)));
            Assert.That(component.Find(".mb-details-panel__leading button").GetAttribute("aria-label"), Is.EqualTo("Go Back"));
            Assert.That(component.FindAll(".mb-details-panel__icon"), Is.Empty);
            Assert.That(component.Find("h2").Id, Is.EqualTo(title));
            Assert.That(component.Find("h2").PreviousElementSibling.ClassList, Does.Contain("mb-details-panel__leading"));
            var backButton = component.FindComponent<BlazorBlueprint.Components.BbButton>().Instance;
            component.Render(parameters => parameters.Add(panel => panel.Class, "unrelated"));
            Assert.That(component.FindComponent<BlazorBlueprint.Components.BbButton>().Instance, Is.SameAs(backButton));
            await component.Find("button[aria-label='Go Back']").ClickAsync();
            urls.Verify(context => context.GoBackAsync(), Times.Once);
            this.viewModel.SetupGet(vm => vm.IsInspecting).Returns(false);
            await component.InvokeAsync(() => this.SetPresentation(new Namespace { ElementId = "A" }));
            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("button[aria-label='Go Back']"), Is.Empty));
            Assert.That(component.FindAll(".mb-details-panel__icon"), Has.Count.EqualTo(1));
            Assert.That(component.Find("h2").Id, Is.EqualTo(title));
            var css = File.ReadAllText(Path.Combine(TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                "Components", "UI", "Organisms", "DetailsPanel", "DetailsPanel.razor.css"));
            Assert.That(css, Does.Contain("flex: 0 0 var(--spacing-16)").And.Contain("170ms").And.Contain("prefers-reduced-motion: reduce").And.Contain("animation: none"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyCopyLinkReportsOnlySuccessfulClipboardWrites(bool success)
        {
            var element = new Namespace { ElementId = "A" };
            this.SetPresentation(element);
            var urls = new Mock<IWorkspaceUrlContextService>();
            const string link = "http://localhost/workspace/modeling?selectedElement=A";
            urls.Setup(context => context.GetElementUri(element)).Returns(link);
            var invocation = this.JSInterop.SetupVoid("navigator.clipboard.writeText", link);
            if (success)
            {
                invocation.SetVoidResult();
            }
            else
            {
                invocation.SetException(new Microsoft.JSInterop.JSException("Clipboard denied or unavailable"));
            }
            var component = this.Render<Panel>(parameters => parameters.AddCascadingValue(urls.Object).Add(panel => panel.CloseRequested, () => { }));
            await component.Find("button[aria-label='Copy link to element']").ClickAsync();
            Assert.That(component.Find("header [role='status']").TextContent, Is.EqualTo(success ? "Link copied" : "Unable to copy link"));
            var copy = component.Find("header .mb-properties-copy");
            Assert.That(copy.NextElementSibling.ClassList, Does.Contain("mb-properties-close"));
            Assert.That(component.Find("header").QuerySelector("h2").TextContent, Is.EqualTo("Properties"));
            urls.Verify(context => context.GetElementUri(element), Times.Once);
            component.Render(parameters => parameters.Add(panel => panel.Class, "unrelated"));
            Assert.That(component.FindAll(".mb-details-panel__header"), Has.Count.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task VerifyPendingCopyDoesNotUpdateDisposedPanel(bool success)
        {
            var element = new Namespace { ElementId = "A" };
            this.SetPresentation(element);
            var urls = new Mock<IWorkspaceUrlContextService>();
            const string link = "http://localhost/workspace/modeling?selectedElement=A";
            urls.Setup(context => context.GetElementUri(element)).Returns(link);
            var invocation = this.JSInterop.SetupVoid("navigator.clipboard.writeText", link);
            var component = this.Render<Panel>(parameters => parameters.AddCascadingValue(urls.Object));
            var click = component.Find("button[aria-label='Copy link to element']").ClickAsync();
            await component.WaitForAssertionAsync(() => Assert.That(component.Find(".mb-properties-copy").HasAttribute("disabled"), Is.True));
            await this.DisposeComponentsAsync();
            if (success)
            {
                invocation.SetVoidResult();
            }
            else
            {
                invocation.SetException(new Microsoft.JSInterop.JSException("Clipboard rejected"));
            }
            await click;
            this.viewModel.Verify(vm => vm.Dispose(), Times.Once);
        }

        [Test]
        public async Task VerifyClipboardFailureForPreviousElementDoesNotAppearOnNewSelection()
        {
            var element = new Namespace { ElementId = "A" };
            this.SetPresentation(element);
            var urls = new Mock<IWorkspaceUrlContextService>();
            const string link = "http://localhost/workspace/modeling?selectedElement=A";
            urls.Setup(context => context.GetElementUri(element)).Returns(link);
            var invocation = this.JSInterop.SetupVoid("navigator.clipboard.writeText", link);
            var component = this.Render<Panel>(parameters => parameters.AddCascadingValue(urls.Object));
            var click = component.Find("button[aria-label='Copy link to element']").ClickAsync();
            await component.WaitForAssertionAsync(() => Assert.That(component.Find(".mb-properties-copy").HasAttribute("disabled"), Is.True));
            await component.InvokeAsync(() => this.SetPresentation(new Namespace { ElementId = "B" }));
            invocation.SetException(new Microsoft.JSInterop.JSException("Clipboard rejected"));
            await click;
            Assert.That(component.Find("header [role='status']").TextContent, Is.Empty);
        }

        private void SetPresentation(IElement element)
        {
            this.viewModel.SetupGet(vm => vm.Presentation).Returns(element == null ? null : new DetailsPanelPresentation(element));
            this.viewModel.Raise(vm => vm.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IDetailsPanelViewModel.Presentation)));
        }
    }
}
