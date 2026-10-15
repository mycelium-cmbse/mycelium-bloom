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
    using System.IO;
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
        /// The visible node titles expected for a deep filtered match.
        /// </summary>
        private static readonly string[] ExpectedVisibleFilterPathTitles = ["Root", "Branch", "Needle"];

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies default package rows use the exported folder icon without displaying CD notation.
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
                Assert.That(component.FindAll(".mb-project-browser-node__stereotype"), Is.Empty);
                Assert.That(node.Presentation.Stereotype, Is.EqualTo("«package»"));
                Assert.That(component.Find(".mb-project-browser-node__title").TextContent.Trim(), Is.EqualTo("Quantities"));
                Assert.That(component.Find(".mb-project-browser-node__element-icon").GetAttribute("style"),
                    Does.Contain("icons/project-browser/folder.svg"));
                Assert.That(component.Find("button").GetAttribute("title"), Is.EqualTo("Quantities - Package"));
                Assert.That(component.Markup, Does.Not.Contain("LibraryPackage"));
            }
        }

        /// <summary>
        /// Verifies kinds without an established tree icon use the same neutral outline fallback.
        /// </summary>
        /// <typeparam name="TElement">The represented SysML element category.</typeparam>
        [TestCase(TypeArgs = [typeof(IDocumentation)])]
        [TestCase(TypeArgs = [typeof(IComment)])]
        [TestCase(TypeArgs = [typeof(IAnnotation)])]
        [TestCase(TypeArgs = [typeof(IAnnotatingElement)])]
        [TestCase(TypeArgs = [typeof(IImport)])]
        [TestCase(TypeArgs = [typeof(IMembership)])]
        [TestCase(TypeArgs = [typeof(IRelationship)])]
        [TestCase(TypeArgs = [typeof(IDefinition)])]
        [TestCase(TypeArgs = [typeof(IUsage)])]
        [TestCase(TypeArgs = [typeof(IFeature)])]
        [TestCase(TypeArgs = [typeof(IType)])]
        [TestCase(TypeArgs = [typeof(INamespace)])]
        [TestCase(TypeArgs = [typeof(IElement)])]
        public void VerifyRenderUsesNeutralFallbackForUnmappedIcons<TElement>()
            where TElement : class, IElement
        {
            var element = new Mock<TElement>(MockBehavior.Strict);

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
        /// Verifies node rows retain intrinsic content width for Project Browser horizontal scrolling.
        /// </summary>
        [Test]
        public void VerifyNodeStylePreservesIntrinsicWidthWithoutTitleEllipsis()
        {
            var repositoryRoot = TestRepository.GetRootPath();
            var componentDirectory = Path.Combine(
                repositoryRoot,
                "Mycelium.Bloom",
                "Components",
                "UI",
                "Organisms",
                "ProjectBrowser");
            var style = File.ReadAllText(Path.Combine(componentDirectory, "ProjectBrowserNode.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-project-browser-node\s*\{[^}]*box-sizing:\s*border-box;[^}]*width:\s*max-content;[^}]*min-width:\s*100%;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-project-browser-node__line\s*\{[^}]*width:\s*max-content;[^}]*min-width:\s*100%;"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-project-browser-node__title\s*\{[^}]*flex:\s*0\s+0\s+auto;[^}]*min-width:\s*max-content;[^}]*white-space:\s*nowrap;"));
                Assert.That(
                    style,
                    Does.Not.Match(
                        @"(?s)\.mb-project-browser-node__title\s*\{[^}]*text-overflow:"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-project-browser-node__children\s*\{[^}]*width:\s*max-content;[^}]*min-width:\s*100%;"));
            }
        }

        /// <summary>
        /// Verifies that selecting a parent node raises the selected node callback.
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

            var selectedNode = default(ProjectBrowserNodeViewModel);

            var component = this.Render<ProjectBrowserNodeComponent>(parameters => parameters
                .Add(component => component.ViewModel, node)
                .Add(component => component.OnNodeSelected, selected => selectedNode = selected));

            component.Find("button").Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.IsExpanded, Is.False);
                Assert.That(selectedNode, Is.SameAs(node));
                Assert.That(component.Find("[role='treeitem']").GetAttribute("aria-selected"), Is.EqualTo("false"));
                Assert.That(component.Markup, Does.Not.Contain("Length"));
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
                    component.Find("button").ClassList.Contains("mb-project-browser-node__row--selected"),
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
                        component.Find("button").ClassList.Contains("mb-project-browser-node__row--selected"),
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
