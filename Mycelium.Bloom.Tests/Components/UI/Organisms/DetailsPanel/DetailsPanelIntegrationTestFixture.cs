// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelIntegrationTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.DetailsPanel
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bunit;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using static Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory;
    using Panel = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;

    /// <summary>Integrates the real panel, cached ViewModel and shared selection with canonical SDK data.</summary>
    [TestFixture, FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DetailsPanelIntegrationTestFixture : BunitContext
    {
        private readonly ContextAwareService selection = new();

        public DetailsPanelIntegrationTestFixture()
        {
            BlueprintTestSetup.Configure(this);
            this.Services.AddSingleton<IElementSelectionService>(this.selection);
            this.Services.AddSingleton<Func<IDetailsPanelViewModel>>(() => new DetailsPanelViewModel(this.selection));
        }

        [TearDown]
        public void TearDown() => this.Dispose();

        [Test]
        public async Task VerifyExpressionNavigationPreservesIndependentBrowsersAndCanonicalSelection()
        {
            var root = LoadQuantities();
            var dimensions = Descendants(root).Single(element => element.name == "dimensions");
            var target = Descendants(dimensions).OfType<IFeatureReferenceExpression>().First(expression => expression.referent?.name == "mRef").referent;
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            using var first = new ProjectBrowserViewModel(loader.Object, this.selection);
            using var second = new ProjectBrowserViewModel(loader.Object, this.selection);
            Assert.That(await first.InitializeAsync(CancellationToken.None), Is.True);
            Assert.That(await second.InitializeAsync(CancellationToken.None), Is.True);
            first.FilterText = "dimensions";
            this.selection.SelectedElement = dimensions;
            var firstSelected = first.SelectedNode;
            var secondSelected = second.SelectedNode;
            var component = this.Render<Panel>();
            await component.FindAll("button[aria-label^='Inspect']").First(button => button.GetAttribute("aria-label").EndsWith(": mRef", StringComparison.Ordinal)).ClickAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.selection.SelectedElement, Is.SameAs(target));
                Assert.That(component.Instance.ViewModel.Presentation.Element, Is.SameAs(target));
                Assert.That(first.SelectedNode, Is.SameAs(firstSelected));
                Assert.That(second.SelectedNode, Is.SameAs(secondSelected));
                Assert.That(first.FilterText, Is.EqualTo("dimensions"));
                Assert.That(second.FilterText, Is.Empty);
            }
        }

        [Test]
        public async Task VerifyDimensionsIdentityWrappingAndValueExpressionNavigation()
        {
            var root = LoadQuantities();
            var element = Descendants(root).Single(candidate => candidate.name == "dimensions");
            var expression = element.OwnedRelationship.OfType<SysML2.NET.Core.POCO.Kernel.FeatureValues.IFeatureValue>().Single().value;
            this.selection.SelectedElement = element;
            var component = this.Render<Panel>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[data-property-name='name'] dd").TextContent, Is.EqualTo("dimensions"));
                Assert.That(component.Find("[data-property-name='declaredName'] dd").TextContent, Is.EqualTo("Not declared"));
                var qualified = component.Find("[data-property-name='qualifiedName'] dd");
                Assert.That(qualified.TextContent, Is.EqualTo(element.qualifiedName));
                Assert.That(qualified.QuerySelectorAll(".mb-qualified-name-segment").Select(segment => segment.TextContent),
                    Is.EqualTo(new[] { "Quantities::", "TensorQuantityValue::", "dimensions" }));
                Assert.That(component.FindAll("button[aria-label='Inspect Value expression: mRef.dimensions']"), Has.Count.EqualTo(1));
            }
            await component.Find("button[aria-label='Inspect Value expression: mRef.dimensions']").ClickAsync();
            Assert.That(this.selection.SelectedElement, Is.SameAs(expression));
        }

        [Test]
        public void VerifyRealBaseQuantitiesDoesNotInventALocalOrInheritedValue()
        {
            var root = LoadQuantities();
            var attribute = Descendants(root).Single(element => element.DeclaredName == "baseQuantities");
            this.selection.SelectedElement = attribute;
            var component = this.Render<Panel>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(attribute.OwnedRelationship.OfType<SysML2.NET.Core.POCO.Kernel.FeatureValues.IFeatureValue>(), Is.Empty);
                Assert.That(component.Find("section[aria-label='Value']").TextContent,
                    Does.Contain("No local value; inherited/default values not resolved"));
                Assert.That(component.Instance.ViewModel.Presentation.HasValueSummary, Is.False);
                Assert.That(component.Find("section[aria-label='Classification']").TextContent, Does.Contain("[*]"));
                Assert.That(component.Find("section[aria-label='Multiplicity']").TextContent,
                    Does.Contain("*").And.Not.Contain("not supported"));
            }
        }

        [Test]
        public async Task VerifyCacheRefreshDisposalAndRemount()
        {
            var selected = new PartUsage { DeclaredName = "Initial" };
            this.selection.SelectedElement = selected;
            var component = this.Render<Panel>();
            var owned = component.Instance.ViewModel;
            var snapshot = owned.Presentation;
            selected.DeclaredName = "Updated";
            component.Render(parameters => parameters.Add(panel => panel.Class, "rerender"));
            Assert.That(owned.Presentation, Is.SameAs(snapshot));
            Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Initial"));
            await component.InvokeAsync(owned.Refresh);
            component.WaitForAssertion(() => Assert.That(component.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Updated")));
            var retained = owned.Presentation;
            await this.DisposeComponentsAsync();
            await Task.Run(() => this.selection.SelectedElement = new Namespace { DeclaredName = "Reopened" });
            Assert.That(owned.Presentation, Is.SameAs(retained));
            var reopened = this.Render<Panel>();
            Assert.That(reopened.Instance.ViewModel, Is.Not.SameAs(owned));
            Assert.That(reopened.Instance.ViewModel.Presentation.Element, Is.SameAs(this.selection.SelectedElement));
            await Task.Run(() => this.selection.SelectedElement = new Namespace { DeclaredName = "Background" });
            await reopened.WaitForAssertionAsync(() => Assert.That(reopened.Find(".mb-details-panel__element-name").TextContent, Is.EqualTo("Background")));
        }
    }
}
