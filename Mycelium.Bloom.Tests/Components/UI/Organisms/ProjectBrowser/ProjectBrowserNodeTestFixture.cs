// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserNodeTestFixture.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.ProjectBrowser
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Bunit;
    using Moq;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using ProjectBrowserNodeComponent = Mycelium.Bloom.Components.UI.Organisms.ProjectBrowser.ProjectBrowserNode;

    /// <summary>
    /// Tests the <see cref="ProjectBrowserNodeComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ProjectBrowserNodeTestFixture : BunitContext
    {
        /// <summary>
        /// The visible node titles expected for a deep filtered match.
        /// </summary>
        private static readonly string[] ExpectedVisibleFilterPathTitles = ["Root", "Branch", "Needle"];

        [Test]
        public void VerifyRecordedVerdictRendersAsAnAccessibleBadge()
        {
            var model = BrowserAcceptanceModelTestFactory.CreateVerification("pass");
            var node = new ProjectBrowserNodeViewModel("verification", model.Verification.DeclaredName,
                new ProjectBrowserNodeMetadata("verification", null, model.Verification), []);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(value => value.ViewModel, node));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[aria-label='Verdict: pass']").TextContent, Is.EqualTo("Verdict: pass"));
                Assert.That(component.FindAll("[role='treeitem']"), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void VerifyInitialMarkerRendersOnTheCanonicalState()
        {
            var model = BrowserAcceptanceModelTestFactory.CreateStates();
            var node = new ProjectBrowserNodeViewModel("operating", model.Initial.DeclaredName,
                new ProjectBrowserNodeMetadata("operating", null, model.Initial), []);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(value => value.ViewModel, node));
            Assert.That(component.Find("[title='Initial']").TextContent.Trim(), Is.EqualTo("Initial"));
        }

        [Test]
        public void VerifyUnitAndExpressionMultiplicityRenderInlineOnOwner()
        {
            var model = BrowserAcceptanceModelTestFactory.CreateQuantity();
            var root = new SysML2.NET.Core.POCO.Kernel.Packages.Package { DeclaredName = "Assembly" };
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, model.Library);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, model.Attribute);
            var count = new Feature { DeclaredName = "count" };
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, count);
            var range = new MultiplicityRange();
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(model.Attribute, range);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(range, new LiteralInteger { Value = 0 });
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(range,
                BrowserAcceptanceModelTestFactory.Binary("+", BrowserAcceptanceModelTestFactory.Reference(count),
                    new LiteralInteger { Value = 1 }));
            var node = new ProjectBrowserNodeViewModel("mass", model.Attribute.DeclaredName,
                new ProjectBrowserNodeMetadata("mass", null, model.Attribute), []);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(value => value.ViewModel, node));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-browser-node__value").TextContent,
                    Is.EqualTo("= 3[SI::kg]"));
                Assert.That(component.Find(".mb-project-browser-node__multiplicity").TextContent,
                    Is.EqualTo("[0..Assembly::count + 1]"));
                Assert.That(component.FindAll("[role='treeitem']"), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void VerifyGroupHeaderTogglesWithoutPublishingModelSelection()
        {
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode("member", "Member");
            var group = new ProjectBrowserNodeViewModel("group", "Connections", [child]);
            ProjectBrowserNodeViewModel selected = null;
            ProjectBrowserNodeViewModel toggled = null;
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(value => value.ViewModel, group)
                .Add(value => value.OnNodeSelected, node => selected = node)
                .Add(value => value.OnNodeExpansionToggled, node => toggled = node));

            var treeItem = component.Find("[role='treeitem']");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(treeItem.GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(treeItem.HasAttribute("aria-selected"), Is.False);
                Assert.That(treeItem.HasAttribute("data-element-id"), Is.False);
                Assert.That(component.FindAll("[role='group']"), Is.Empty);
            }
            component.Find("[data-testid='project-browser-node-selection']").Click();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(toggled, Is.SameAs(group));
                Assert.That(selected, Is.Null);
            }
            group.IsExpanded = true;
            component.WaitForAssertion(() => Assert.That(component.FindAll("[role='group']"), Has.Count.EqualTo(1)));
        }

        [Test]
        public void VerifyRowShowsAvailableTypeValueAndDocumentationWithoutInventingOwnership()
        {
            var attribute = new Mock<SysML2.NET.Core.POCO.Systems.Attributes.IAttributeUsage>();
            attribute.SetupGet(element => element.type).Returns([new PartDefinition { DeclaredName = "Scalar" }]);
            attribute.SetupGet(element => element.ownedElement).Returns([new Documentation { Body = "Description" }]);
            var value = new Mock<SysML2.NET.Core.POCO.Kernel.FeatureValues.IFeatureValue>();
            value.SetupGet(element => element.value).Returns(new LiteralInteger { Value = 42 });
            attribute.SetupGet(element => element.OwnedRelationship).Returns([value.Object]);
            var node = new ProjectBrowserNodeViewModel("attribute", "mass",
                new ProjectBrowserNodeMetadata("attribute", null, attribute.Object), []);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(element => element.ViewModel, node));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-browser-node__stereotype").TextContent, Is.EqualTo("«attribute»"));
                Assert.That(component.Find(".mb-project-browser-node__type").TextContent, Is.EqualTo(": Scalar"));
                Assert.That(component.Find(".mb-project-browser-node__value").TextContent, Is.EqualTo("= 42"));
                Assert.That(component.Find("[aria-label='Documentation']"), Is.Not.Null);
                Assert.That(component.Markup, Does.Not.Contain("SYS"));
                Assert.That(component.FindAll(".mb-project-browser-node__visibility"), Is.Empty);
                var facets = component.Find(".mb-project-browser-node__label").Children.Select(child => child.ClassName).ToArray();
                Assert.That(Array.IndexOf(facets, "mb-project-browser-node__title"),
                    Is.LessThan(Array.IndexOf(facets, "mb-project-browser-node__type")));
                Assert.That(Array.IndexOf(facets, "mb-project-browser-node__value"),
                    Is.LessThan(Array.IndexOf(facets, "mb-project-browser-node__stereotype")));
            }
        }

        [Test]
        public void VerifyResolvedMultiplicityRendersInline()
        {
            var multiplicity = ProjectBrowserNodeTestFactory.CreateElement<MultiplicityRange>(
                "range", null, new LiteralInteger { Value = 2 }, new LiteralInfinity());
            var part = ProjectBrowserNodeTestFactory.CreateElement<PartUsage>("part", "Part", multiplicity);
            var node = new ProjectBrowserNodeViewModel(
                "part", "Part", new ProjectBrowserNodeMetadata("part", null, part), []);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(value => value.ViewModel, node));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-browser-node__multiplicity").TextContent, Is.EqualTo("[2..*]"));
                Assert.That(component.FindAll("[role='treeitem']"), Has.Count.EqualTo(1));
            }
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
        /// Verifies library rows display their kind, name and available library indicators.
        /// </summary>
        [Test]
        public void VerifyRenderUsesSemanticPackagePresentation()
        {
            var node = new ProjectBrowserNodeViewModel(
                "quantities",
                "Quantities",
                new ProjectBrowserNodeMetadata(
                    "quantities",
                    "Quantities",
                    new LibraryPackage()),
                []);

            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-project-browser-node__stereotype").TextContent, Is.EqualTo("«library package»"));
                Assert.That(node.Presentation.Stereotype, Is.EqualTo("«library package»"));
                Assert.That(component.Find(".mb-project-browser-node__indicator [aria-label='Library']"), Is.Not.Null);
                Assert.That(component.Find(".mb-project-browser-node__title").TextContent.Trim(),
                    Is.EqualTo("Quantities"));
                Assert.That(component.Find(".mb-project-browser-node__element-icon").GetAttribute("style"),
                    Does.Contain("icons/project-browser/folder.svg"));
                Assert.That(component.Find("button.mb-project-browser-node__row").GetAttribute("title"),
                    Does.Contain("Quantities - Library package"));
                Assert.That(component.FindAll("[data-testid='project-browser-node-disclosure']"), Is.Empty);
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-expanded"), Is.Null);
                Assert.That(component.Markup, Does.Not.Contain("LibraryPackage"));
            }
        }

        /// <summary>Verifies common semantic kinds render distinct library symbols with their inline kind.</summary>
        /// <param name="elementType">The canonical SDK kind.</param>
        /// <param name="symbol">The expected established icon.</param>
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Attributes.AttributeUsage), "hash")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.States.StateUsage), "circle-dot")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.Connections.ConnectionUsage), "link")]
        [TestCase(typeof(SysML2.NET.Core.POCO.Systems.VerificationCases.VerificationCaseUsage), "clipboard-check")]
        public void VerifySemanticKindSymbols(System.Type elementType, string symbol)
        {
            var element = (IElement)Activator.CreateInstance(elementType);
            var membership = new Membership
            {
                MemberElement = element,
                Visibility = SysML2.NET.Core.Root.Namespaces.VisibilityKind.Private
            };
            var node = new ProjectBrowserNodeViewModel("element", "Readable name",
                new ProjectBrowserNodeMetadata("element", null, element), [], membership);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(value => value.ViewModel, node));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindComponent<BlazorBlueprint.Icons.Lucide.Components.LucideIcon>().Instance.Name, Is.EqualTo(symbol));
                Assert.That(component.Find(".mb-project-browser-node__fallback-icon svg").Children, Has.Length.GreaterThan(0));
                Assert.That(component.Find(".mb-project-browser-node__stereotype").TextContent, Is.EqualTo(node.Presentation.Stereotype));
                Assert.That(component.Find("[title='Visibility: private'] .sr-only").TextContent, Is.EqualTo("private"));
                Assert.That(component.Find("[title='Visibility: private'] [aria-hidden='true']").TextContent, Is.EqualTo("−"));
            }
        }

        /// <summary>
        /// Verifies structural kinds without an exported icon use the Figma kind marker.
        /// </summary>
        /// <typeparam name="TElement">The represented SysML element category.</typeparam>
        [TestCase(TypeArgs = [typeof(IDefinition)])]
        [TestCase(TypeArgs = [typeof(IUsage)])]
        [TestCase(TypeArgs = [typeof(IFeature)])]
        [TestCase(TypeArgs = [typeof(IType)])]
        [TestCase(TypeArgs = [typeof(INamespace)])]
        [TestCase(TypeArgs = [typeof(IElement)])]
        public void VerifyRenderUsesNeutralFallbackForUnmappedIcons<TElement>()
            where TElement : class, IElement
        {
            var element = new Mock<TElement>();

            if (typeof(IType).IsAssignableFrom(typeof(TElement)))
            {
                element.As<IType>().SetupGet(value => value.multiplicity).Returns((IMultiplicity)null);
            }

            var node = new ProjectBrowserNodeViewModel(
                "element",
                "Element",
                new ProjectBrowserNodeMetadata("element", "Element", element.Object),
                []);

            using var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(projectBrowserNode => projectBrowserNode.ViewModel, node));

            Assert.That(component.FindAll(".mb-project-browser-node__fallback-icon svg"), Has.Count.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".mb-project-browser-node__element-icon"), Is.Empty);
                Assert.That(component.Find("button.mb-project-browser-node__row").HasAttribute("style"), Is.False);
                Assert.That(component.Find(".mb-project-browser-node__ownership").TextContent, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies a project browser node renders parameters inherited from the Bloom reactive base.
        /// </summary>
        [Test]
        public void VerifyRenderUsesInheritedBloomParameters()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            using var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(projectBrowserNode => projectBrowserNode.ViewModel, node)
                .Add(projectBrowserNode => projectBrowserNode.Class, "custom-project-browser-node")
                .AddUnmatched("data-testid", "project-browser-node")
                .AddUnmatched("role", "presentation")
                .AddUnmatched("aria-selected", "true"));

            var root = component.Find("[role='treeitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ClassList.Contains("mb-project-browser-node"), Is.True);
                Assert.That(root.ClassList.Contains("custom-project-browser-node"), Is.True);
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("project-browser-node"));
                Assert.That(root.GetAttribute("role"), Is.EqualTo("treeitem"));
                Assert.That(root.GetAttribute("aria-selected"), Is.EqualTo("false"));
            }
        }

        /// <summary>
        /// Verifies the constrained visual row retains full semantic information for hover and assistive technology.
        /// </summary>
        [Test]
        public void VerifyTruncatedRowRetainsFullAccessibleNotation()
        {
            var model = BrowserAcceptanceModelTestFactory.CreateQuantity();
            var node = new ProjectBrowserNodeViewModel("mass", model.Attribute.DeclaredName,
                new ProjectBrowserNodeMetadata("mass", null, model.Attribute), []);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(value => value.ViewModel, node));
            var accessibleLabel = component.Find("[role='treeitem']").GetAttribute("aria-label");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(accessibleLabel, Does.Contain(model.Attribute.DeclaredName).And.Contain("3[SI::kg]").And.Contain("«attribute»"));
                Assert.That(component.Find(".mb-project-browser-node__row").GetAttribute("title"), Does.StartWith(accessibleLabel));
                Assert.That(component.Find(".mb-project-browser-node__label").TextContent, Does.Contain(model.Attribute.DeclaredName));
                Assert.That(component.Find(".mb-project-browser-node__row").GetAttribute("tabindex"), Is.EqualTo("-1"));
            }
        }

        /// <summary>
        /// Verifies selecting a parent node preserves expansion and raises the selected node callback.
        /// </summary>
        [Test]
        public void VerifyRenderRaisesSelectedNode()
        {
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "quantities/length",
                "Length",
                "length",
                "Quantities::Length");

            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "quantities",
                "Quantities",
                child);
            node.IsExpanded = true;

            var selectedNode = default(ProjectBrowserNodeViewModel);

            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node)
                .Add(component => component.OnNodeSelected, selected => selectedNode = selected));

            component.Find("[data-testid='project-browser-node-selection']").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.IsExpanded, Is.True);
                Assert.That(selectedNode, Is.SameAs(node));
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(component.Markup, Does.Contain("Length"));
            }
        }

        /// <summary>Verifies disclosure is a separate accessible control that never selects its node.</summary>
        [Test]
        public void VerifyDisclosureControlTogglesWithoutSelectingNode()
        {
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "quantities/length",
                "Length",
                "length",
                "Quantities::Length");
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities", child);
            var selectedNode = default(ProjectBrowserNodeViewModel);

            using var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node)
                .Add(component => component.OnNodeSelected, selected => selectedNode = selected)
                .Add(component => component.OnNodeExpansionToggled, toggledNode =>
                {
                    toggledNode.IsExpanded = !toggledNode.IsExpanded;
                    return Task.CompletedTask;
                }));

            var disclosure = component.Find("[data-testid='project-browser-node-disclosure']");
            disclosure.Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedNode, Is.Null);
                Assert.That(node.IsExpanded, Is.True);
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(
                    component.Find("[data-testid='project-browser-node-disclosure']").GetAttribute("aria-expanded"),
                    Is.EqualTo("true"));
                Assert.That(
                    component.Find("[data-testid='project-browser-node-disclosure']").GetAttribute("aria-label"),
                    Is.EqualTo("Collapse Quantities"));
                Assert.That(component.Find("[data-testid='project-browser-node-disclosure']").GetAttribute("type"),
                    Is.EqualTo("button"));
                Assert.That(
                    component.FindAll(".mb-project-browser-node__row [data-testid='project-browser-node-disclosure']"),
                    Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that owner-published selection state rerenders the node.
        /// </summary>
        [Test]
        public void VerifySelectedNodeParameterChangeRerendersNode()
        {
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities");
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(
                    component.Find("button.mb-project-browser-node__row").ClassList
                        .Contains("mb-project-browser-node__row--selected"),
                    Is.False);
            }

            component.Render(parameters => parameters
                .Add(projectBrowserNode => projectBrowserNode.ViewModel, node)
                .Add(projectBrowserNode => projectBrowserNode.SelectedNode, node));

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("true"));
                    Assert.That(
                        component.Find("button.mb-project-browser-node__row").ClassList
                            .Contains("mb-project-browser-node__row--selected"),
                        Is.True);
                }
            });
        }

        /// <summary>
        /// Verifies that changing the reactive expansion state rerenders the node and its children.
        /// </summary>
        [Test]
        public void VerifyIsExpandedChangeRerendersNode()
        {
            var child = ProjectBrowserNodeTestFactory.CreateNamespaceNode(
                "quantities/length",
                "Length",
                "length",
                "Quantities::Length");
            var node = ProjectBrowserNodeTestFactory.CreateNamespaceNode("quantities", "Quantities", child);
            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-expanded"), Is.EqualTo("false"));
                Assert.That(component.Markup, Does.Not.Contain("Length"));
            }

            node.IsExpanded = true;

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-expanded"), Is.EqualTo("true"));
                    Assert.That(component.Markup, Does.Contain("Length"));
                }
            });
        }

        /// <summary>
        /// Verifies an active presentation renders only the matching ancestor path as effectively expanded.
        /// </summary>
        [Test]
        public async Task VerifyActiveFilterRendersOnlyVisibleAncestorPathAsExpanded()
        {
            using var presentationOwner = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            var root = presentationOwner.RootNodes[0];
            var branch = root.Children[0];
            branch.IsExpanded = false;
            presentationOwner.FilterText = "needle";

            using var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(projectBrowserNode => projectBrowserNode.ViewModel, root)
                .Add(projectBrowserNode => projectBrowserNode.FilterPresentation,
                    presentationOwner.FilterPresentation));
            var titles = component.FindAll(".mb-project-browser-node__title")
                .Select(title => title.TextContent)
                .ToArray();
            var treeItems = component.FindAll("[role='treeitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(titles, Is.EqualTo(ExpectedVisibleFilterPathTitles));
                Assert.That(component.Markup, Does.Not.Contain("Sibling"));
                Assert.That(component.Markup, Does.Not.Contain("Hidden descendant"));
                Assert.That(component.FindAll("[role='group']"), Has.Count.EqualTo(2));
                Assert.That(treeItems[0].GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(treeItems[1].GetAttribute("aria-expanded"), Is.EqualTo("true"));
                Assert.That(treeItems[2].GetAttribute("aria-expanded"), Is.Null);
                Assert.That(component.FindAll("[data-testid='project-browser-node-disclosure']"), Is.Empty);
                Assert.That(branch.IsExpanded, Is.False);
            }
        }

        /// <summary>
        /// Verifies a visible matching node whose real children are hidden is presented as a leaf.
        /// </summary>
        [Test]
        public async Task VerifyActiveFilterPresentsNodeWithOnlyHiddenChildrenAsLeaf()
        {
            using var presentationOwner = await ProjectBrowserNodeTestFactory.CreateFilterTreeViewModelAsync();
            var matchingNode = presentationOwner.RootNodes[0].Children[0].Children[0];
            matchingNode.IsExpanded = true;
            presentationOwner.FilterText = "needle";

            using var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(projectBrowserNode => projectBrowserNode.ViewModel, matchingNode)
                .Add(projectBrowserNode => projectBrowserNode.FilterPresentation,
                    presentationOwner.FilterPresentation));
            var treeItem = component.Find("[role='treeitem']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Needle"));
                Assert.That(component.Markup, Does.Not.Contain("Hidden descendant"));
                Assert.That(component.FindAll(".mb-project-browser-node__toggle"), Is.Empty);
                Assert.That(component.FindAll("[role='group']"), Is.Empty);
                Assert.That(treeItem.GetAttribute("aria-expanded"), Is.Null);
                Assert.That(matchingNode.IsExpanded, Is.True);
            }
        }
    }
}
