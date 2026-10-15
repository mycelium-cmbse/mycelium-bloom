// ------------------------------------------------------------------------------------------------
// <copyright file="ExpressionPanelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.DetailsPanel
{
    using System.Linq;
    using System.Threading.Tasks;
    using Bunit;
    using Microsoft.Extensions.DependencyInjection;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Moq;
    using Mycelium.Bloom.Core.ModelLoading;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using static Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory;
    using Panel = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ExpressionPanelTestFixture : BunitContext
    {
        [Test]
        public async Task VerifyInspectionOmitsEmptyWrappersAndAlreadyDisplayedObjects()
        {
            this.selection.SelectedElement = Descendants(LoadQuantities()).Single(element => element.name == "dimensions");
            var component = this.Render<Panel>();
            await component.Find("button[aria-label='Advanced metadata for Value: Feature chain']").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("button[aria-label='Inspect Result: Feature']"), Is.Empty);
                Assert.That(component.FindAll("button[aria-label='Inspect membership of Result']"), Is.Empty);
                Assert.That(component.FindAll("button[aria-label='Inspect Owned element: Feature']"), Is.Empty);
                Assert.That(component.FindAll(".value-group__identity"), Has.Count.EqualTo(6));
            }
            await component.Find("button[aria-label='Inspect Operand 1: Feature']").ClickAsync();
            await component.Find("button[aria-label='Advanced metadata for Input: Feature']").ClickAsync();
            Assert.That(component.FindAll("button[aria-label='Inspect Input: Feature']"), Is.Empty);
            await component.Find("button[aria-label='Inspect membership of Input']").ClickAsync();
            await component.Find("button[aria-label='Advanced metadata for Input: Feature']").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("button[aria-label='Inspect membership of Input']"), Is.Empty);
                Assert.That(component.FindAll("button[aria-label='Inspect Input: Feature']"), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task VerifyBackFromMembershipRestoresCanonicalSelectionAndClearResetsTrail()
        {
            var dimensions = Descendants(LoadQuantities()).Single(element => element.name == "dimensions");
            this.selection.SelectedElement = dimensions;
            var component = this.Render<Panel>();
            Assert.That(component.Find("button[aria-label='Back to previous inspected element']").HasAttribute("disabled"), Is.True);
            await component.Find("button[aria-label='Advanced metadata for Value: Feature chain']").ClickAsync();
            var membership = component.Instance.ViewModel.Presentation.Expressions[0].Membership;
            await component.Find("button[aria-label='Inspect membership of Value']").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.selection.SelectedElement, Is.SameAs(membership));
                Assert.That(component.Find("button[aria-label='Back to previous inspected element']").HasAttribute("disabled"), Is.False);
                Assert.That(component.Find(".mb-details-panel__notation-text").TextContent, Does.Contain("mRef.dimensions"));
            }
            await component.Find("button[aria-label='Back to previous inspected element']").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.selection.SelectedElement, Is.SameAs(dimensions));
                Assert.That(component.Find("button[aria-label='Back to previous inspected element']").HasAttribute("disabled"), Is.True);
            }
            await component.InvokeAsync(() => component.Instance.ViewModel.Inspect(membership));
            await component.Find("button[aria-label='Clear selection']").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.ViewModel.CanGoBack, Is.False);
                Assert.That(component.Find(".mb-details-panel__empty"), Is.Not.Null);
            }
        }

        [Test]
        public async Task VerifyLeafResultsHaveValueStatesWithoutEmptyDisclosureAndMetadataIsOptIn()
        {
            this.selection.SelectedElement = Descendants(LoadQuantities()).Single(element => element.name == "dimensions");
            var component = this.Render<Panel>();
            await ExpandAll(component);
            var results = component.FindAll(".value-group[data-expression-role='Result']");
            Assert.That(results, Is.Not.Empty);
            foreach (var result in results)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(result.QuerySelector(".value-group__trigger"), Is.Null);
                    Assert.That(result.TextContent, Does.Contain("No explicit value"));
                }
            }
            Assert.That(component.FindAll("dl dd").Select(value => value.TextContent), Does.Not.Contain(this.selection.SelectedElement.ElementId));
            await component.Find("button[aria-label='Advanced metadata for Value: Feature chain']").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("dl dd").Select(value => value.TextContent), Does.Contain(this.selection.SelectedElement.ElementId));
                Assert.That(component.FindAll("button[aria-label^='Advanced metadata']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".value-group__identity dt").Select(label => label.TextContent), Does.Not.Contain("SDK ID"));
                Assert.That(component.FindAll(".value-group__identity dt").Select(label => label.TextContent), Does.Not.Contain("Membership SDK ID"));
            }
        }

        [Test]
        public void VerifyRerendersReuseSdkNotationUntilExplicitInvalidation()
        {
            this.selection.SelectedElement = Descendants(LoadQuantities()).Single(element => element.name == "orderSum");
            var component = this.Render<Panel>();
            var snapshot = component.Instance.ViewModel.Presentation;
            var equality = (IOperatorExpression)snapshot.Expressions[0].Children[0].Element;
            equality.Operator = "!=";
            component.Render(parameters => parameters.Add(panel => panel.Class, "rerendered"));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.ViewModel.Presentation, Is.SameAs(snapshot));
                Assert.That(component.Find(".mb-details-panel__notation-text").TextContent, Does.Contain(" == "));
            }
            component.Instance.ViewModel.Refresh();
            component.WaitForAssertion(() =>
                Assert.That(component.Find(".mb-details-panel__notation-text").TextContent, Does.Contain(" != ")));
        }

        [Test]
        public async Task VerifyPanelDisposalRemountAndBackgroundSelection()
        {
            this.selection.SelectedElement = new PartUsage { DeclaredName = "Initial" };
            var component = this.Render<Panel>();
            var first = component.Instance.ViewModel;
            var initial = first.Presentation;
            await this.DisposeComponentsAsync();
            await Task.Run(() => this.selection.SelectedElement = new PartUsage { DeclaredName = "Reopened" });
            Assert.That(first.Presentation, Is.SameAs(initial));
            var reopened = this.Render<Panel>();
            Assert.That(reopened.Instance.ViewModel, Is.Not.SameAs(first));
            Assert.That(reopened.Instance.ViewModel.Presentation.Element, Is.SameAs(this.selection.SelectedElement));
            await Task.Run(() => this.selection.SelectedElement = new PartUsage { DeclaredName = "Background" });
            await reopened.WaitForAssertionAsync(() =>
                Assert.That(reopened.Find(".mb-details-panel__element-name").TextContent.Trim(), Is.EqualTo("Background")));
        }

        [Test]
        public async Task VerifyReplacingViewModelReleasesOwnedStateWithoutDisposingCallerState()
        {
            this.selection.SelectedElement = new PartUsage { DeclaredName = "Initial" };
            var component = this.Render<Panel>();
            var owned = component.Instance.ViewModel;
            var initial = owned.Presentation;
            using var replacement = new DetailsPanelViewModel(this.selection);
            component.Render(parameters => parameters.Add(panel => panel.ViewModel, replacement));
            this.selection.SelectedElement = new PartUsage { DeclaredName = "Replacement" };
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find(".mb-details-panel__element-name").TextContent.Trim(), Is.EqualTo("Replacement")));
            Assert.That(owned.Presentation, Is.SameAs(initial));
            await this.DisposeComponentsAsync();
            this.selection.SelectedElement = new PartUsage { DeclaredName = "Caller survives" };
            Assert.That(replacement.Presentation.Element, Is.SameAs(this.selection.SelectedElement));
        }

        private readonly ContextAwareService selection = new();

        public ExpressionPanelTestFixture()
        {
            BlueprintTestSetup.Configure(this);
            this.Services.AddSingleton<IElementSelectionService>(this.selection);
            this.Services.AddScoped<System.Func<IDetailsPanelViewModel>>(provider =>
                () => ActivatorUtilities.CreateInstance<DetailsPanelViewModel>(provider));
        }

        [TearDown]
        public void TearDown() => this.Dispose();

        [TestCase("dimensions", ".", "mRef")]
        [TestCase("orderSum", "==", "contravariantOrder")]
        [TestCase("boundMatch", "or", "isBound")]
        public async Task VerifyExpressionSectionRendersOwnedStructureAndInspectsCanonicalTargets(
            string name, string operation, string targetName)
        {
            var root = LoadQuantities();
            var elements = Descendants(root).ToArray();
            this.selection.SelectedElement = elements.Single(element => element.name == name);
            var expected = Descendants(this.selection.SelectedElement).OfType<IFeatureReferenceExpression>()
                .First(expression => expression.referent?.name == targetName).referent;
            var component = this.Render<Panel>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("section[aria-label='Expression']").TextContent, Does.Contain(operation));
                Assert.That(component.Find(".mb-details-panel__notation-text").TextContent, Does.Contain(operation));
            }
            await ExpandAll(component);
            Assert.That(component.FindAll("button.value-group__details-trigger"), Has.Count.EqualTo(1));
            var targetButton = component.FindAll("button").First(button =>
                button.GetAttribute("aria-label") == $"Inspect reference: {targetName}");
            await targetButton.ClickAsync();
            Assert.That(this.selection.SelectedElement, Is.SameAs(expected));
            await component.WaitForAssertionAsync(() => Assert.That(component.Find(".mb-details-panel__element-name").TextContent.Trim(),
                Is.EqualTo(targetName)));
        }

        [Test]
        public async Task VerifyInputAndMembershipInspectionPreserveSelectionAndIndependentBrowsers()
        {
            var root = LoadQuantities();
            var dimensions = Descendants(root).Single(element => element.name == "dimensions");
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var first = new ProjectBrowserViewModel(loader.Object, this.selection);
            using var second = new ProjectBrowserViewModel(loader.Object, this.selection);
            Assert.That(await first.InitializeAsync(System.Threading.CancellationToken.None), Is.True);
            Assert.That(await second.InitializeAsync(System.Threading.CancellationToken.None), Is.True);
            this.selection.SelectedElement = dimensions;
            var component = this.Render<Panel>();
            var input = dimensions.ownedElement[0].ownedElement[0];
            await component.Find("button[aria-label='Expand or collapse Operand 1: Feature']").ClickAsync();
            await component.Find("button[aria-label='Advanced metadata for Value: Feature chain']").ClickAsync();
            await component.Find("button[aria-label='Inspect Operand 1: Feature']").ClickAsync();
            Assert.That(this.selection.SelectedElement, Is.SameAs(input));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("button[aria-label='Advanced metadata for Input: Feature']"), Is.Not.Null));
            await component.Find("button[aria-label='Advanced metadata for Input: Feature']").ClickAsync();
            var identity = component.Find(".value-group__identity");
            Assert.That(identity.TextContent, Does.Contain("private"));
            Assert.That(identity.TextContent, Does.Contain(input.ElementId));
            Assert.That(identity.TextContent, Does.Contain(input.owningMembership.ElementId));
            await component.Find("button[aria-label='Inspect membership of Input']").ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.selection.SelectedElement, Is.SameAs(input.owningMembership));
                Assert.That(first.SelectedNode, Is.SameAs(first.RootNodes[0]));
                Assert.That(second.SelectedNode, Is.SameAs(second.RootNodes[0]));
                Assert.That(second.FilterText, Is.Empty);
            }
        }

        [Test]
        public async Task VerifyDisclosureResetsForNewSelectionAndPartsKeepExistingProperties()
        {
            var elements = Descendants(LoadQuantities()).ToArray();
            this.selection.SelectedElement = elements.Single(element => element.name == "dimensions");
            var component = this.Render<Panel>();
            var trigger = component.Find("button[aria-label='Expand or collapse Value: Feature chain']");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(trigger.ClassList, Does.Contain("value-group__trigger"));
                Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("true"));
            }
            await trigger.ClickAsync();
            Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            await this.Renderer.Dispatcher.InvokeAsync(() =>
                this.selection.SelectedElement = elements.Single(element => element.name == "orderSum"));
            await component.WaitForAssertionAsync(() =>
                Assert.That(component.Find("button[aria-label='Expand or collapse Expression: orderSum']")
                    .GetAttribute("aria-expanded"), Is.EqualTo("true")));
            var part = new PartUsage { DeclaredName = "Harness", IsComposite = true };
            await this.Renderer.Dispatcher.InvokeAsync(() => this.selection.SelectedElement = part);
            await component.WaitForAssertionAsync(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll("section[aria-label='Expression']"), Is.Empty);
                    Assert.That(component.Find("section[aria-label='Classification']").TextContent, Does.Contain("composite"));
                }
            });
        }

        [TestCase("dimensions")]
        [TestCase("orderSum")]
        [TestCase("boundMatch")]
        public async Task VerifyNotationLeadsSectionAndRemainsVisibleWhenStructureIsCollapsed(string name)
        {
            this.selection.SelectedElement = Descendants(LoadQuantities()).Single(element => element.name == name);
            var component = this.Render<Panel>();
            var section = component.Find("section[aria-label='Expression']");
            var notation = section.QuerySelector(".mb-details-panel__notation");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-details-panel__section")[0].GetAttribute("aria-label"), Is.EqualTo("Expression"));
                Assert.That(section.Children[1].ClassList, Does.Contain("mb-details-panel__notation"));
                var qualifiedName = component.FindAll(".mb-details-panel__property")
                    .Single(property => property.QuerySelector("dt").TextContent == "Qualified name").QuerySelector("dd");
                Assert.That(qualifiedName.TextContent, Is.EqualTo(this.selection.SelectedElement.qualifiedName));
                Assert.That(qualifiedName.QuerySelectorAll("wbr"), Has.Length.EqualTo(2));
                Assert.That(component.FindAll(".value-group__identity"), Is.Empty);
                Assert.That(component.FindAll("button[aria-label^='Inspect Operand']"), Is.Empty);
                Assert.That(component.FindAll(".value-group > div > .value-group__heading button[aria-expanded='true']"), Has.Count.EqualTo(1));
                Assert.That(component.FindAll(".value-group > div > .value-group__heading button[aria-expanded='false']"), Is.Not.Empty);
            }
            var text = notation.TextContent;
            await component.Find(".value-group__trigger").ClickAsync();
            Assert.That(component.Find(".mb-details-panel__notation").TextContent, Is.EqualTo(text));
        }

        private static async Task ExpandAll(IRenderedComponent<Panel> component)
        {
            for (var index = 0; index < 100; index++)
            {
                var button = component.FindAll("button[aria-expanded='false']")
                    .FirstOrDefault(element => element.GetAttribute("aria-label")?.StartsWith("Expand or collapse") == true);
                if (button is null)
                {
                    return;
                }
                await button.ClickAsync();
            }
            Assert.Fail("Expression disclosure did not converge.");
        }
    }
}
