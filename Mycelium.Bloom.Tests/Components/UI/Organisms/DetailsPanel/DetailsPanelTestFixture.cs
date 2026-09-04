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
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Tests.Common;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    using DetailsPanelComponent = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;

    /// <summary>
    /// Tests the <see cref="DetailsPanelComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DetailsPanelTestFixture : BunitContext
    {
        /// <summary>
        /// The expected labels in their required display order.
        /// </summary>
        private static readonly string[] ExpectedPropertyLabels =
        [
            "ID",
            "Declared name",
            "Declared short name",
            "Qualified name"
        ];

        /// <summary>
        /// The expected values for a fully populated element.
        /// </summary>
        private static readonly string[] ExpectedPropertyValues =
        [
            "element-42",
            "Declared element",
            "declared-short",
            "Package::Declared element"
        ];

        /// <summary>
        /// The expected fallback values for unavailable element properties.
        /// </summary>
        private static readonly string[] ExpectedMissingPropertyValues = ["—", "—", "—", "—"];

        /// <summary>
        /// The reactive selection source injected into the component under test.
        /// </summary>
        private readonly Mock<IElementSelectionService> selectionService;

        /// <summary>
        /// The selected element returned by the mocked selection source.
        /// </summary>
        private IElement selectedElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsPanelTestFixture" /> class.
        /// </summary>
        public DetailsPanelTestFixture()
        {
            BlueprintTestSetup.Configure(this);

            this.selectionService = new Mock<IElementSelectionService>();
            this.selectionService.SetupGet(service => service.SelectedElement)
                .Returns(() => this.selectedElement);
            this.Services.AddSingleton(this.selectionService.Object);
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
        /// Verifies a null selection displays the exact empty state without element properties.
        /// </summary>
        [Test]
        public void VerifyNullSelectionDisplaysEmptyState()
        {
            var component = this.Render<DetailsPanelComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance.ViewModel, Is.SameAs(this.selectionService.Object));
                Assert.That(component.Find(".mb-details-panel__title").TextContent.Trim(), Is.EqualTo("Details"));
                Assert.That(component.Find(".mb-details-panel__empty").TextContent.Trim(),
                    Is.EqualTo("Select an element to display its details."));
                Assert.That(component.FindAll(".mb-details-panel__element-name"), Is.Empty);
                Assert.That(component.FindAll("dl"), Is.Empty);
                Assert.That(component.FindAll("button[aria-label='Close details panel']"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies all required labels and values render in the configured order.
        /// </summary>
        [Test]
        public void VerifyPropertiesRenderInRequiredOrder()
        {
            var element = CreateElement(
                "element-42",
                "Declared element",
                "declared-short",
                "Effective element",
                "Package::Declared element");
            this.SetSelectedElement(element);
            var component = this.Render<DetailsPanelComponent>();
            var labels = component.FindAll("dl dt").Select(term => term.TextContent.Trim()).ToArray();
            var values = component.FindAll("dl dd").Select(description => description.TextContent.Trim()).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(labels, Is.EqualTo(ExpectedPropertyLabels));
                Assert.That(values, Is.EqualTo(ExpectedPropertyValues));
            }
        }

        /// <summary>
        /// Verifies the optional close control exposes an accessible action and emits close intent.
        /// </summary>
        [Test]
        public async Task VerifyCloseActionEmitsIntent()
        {
            var closeCount = 0;
            var component = this.Render<DetailsPanelComponent>(parameters => parameters
                .Add(panel => panel.CloseRequested,
                    EventCallback.Factory.Create(this, () => closeCount++)));
            var closeButton = component.Find("button[aria-label='Close details panel']");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closeButton.GetAttribute("type"), Is.EqualTo("button"));
                Assert.That(closeButton.GetAttribute("title"), Is.EqualTo("Close details panel"));
                Assert.That(closeButton.QuerySelector("svg").GetAttribute("aria-hidden"), Is.EqualTo("true"));
            }

            await closeButton.ClickAsync();

            Assert.That(closeCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies unavailable and whitespace-only property values use an em dash.
        /// </summary>
        [Test]
        public void VerifyMissingPropertyValuesUseFallback()
        {
            var element = CreateElement(null, string.Empty, " ", "Display name", "\t");
            this.SetSelectedElement(element);
            var component = this.Render<DetailsPanelComponent>();
            var values = component.FindAll("dl dd").Select(description => description.TextContent.Trim()).ToArray();

            Assert.That(values, Is.EqualTo(ExpectedMissingPropertyValues));
        }

        /// <summary>
        /// Verifies the selected-element display-name precedence.
        /// </summary>
        /// <param name="declaredName">The declared name.</param>
        /// <param name="name">The effective name.</param>
        /// <param name="qualifiedName">The qualified name.</param>
        /// <param name="expectedName">The expected displayed name.</param>
        [TestCase("Declared", " ", " ", "Declared", TestName = "VerifyHeadingDisplaysDeclaredName")]
        [TestCase(" ", "Name", "Qualified", "Name", TestName = "VerifyHeadingUsesNameFallback")]
        [TestCase(" ", " ", "Qualified", "Qualified", TestName = "VerifyHeadingUsesQualifiedNameFallback")]
        [TestCase("Declared", "Name", "Qualified", "Declared", TestName = "VerifyHeadingPreservesNamePrecedence")]
        public void VerifyHeadingDisplayName(
            string declaredName,
            string name,
            string qualifiedName,
            string expectedName)
        {
            var element = CreateElement("element", declaredName, "short", name, qualifiedName);
            this.SetSelectedElement(element);
            var component = this.Render<DetailsPanelComponent>();

            Assert.That(component.Find(".mb-details-panel__element-name").TextContent.Trim(),
                Is.EqualTo(expectedName));
        }

        /// <summary>
        /// Verifies the runtime type supplies the heading when an element has no available name.
        /// </summary>
        [Test]
        public void VerifyHeadingUsesRuntimeTypeFallback()
        {
            this.SetSelectedElement(new Namespace { DeclaredName = " " });
            var component = this.Render<DetailsPanelComponent>();

            Assert.That(component.Find(".mb-details-panel__element-name").TextContent.Trim(),
                Is.EqualTo(nameof(Namespace)));
        }

        /// <summary>
        /// Verifies reactive selection transitions replace the rendered element without retaining stale state.
        /// </summary>
        [Test]
        public void VerifySelectionServiceTransitions()
        {
            var firstElement = CreateElement("first-id", "First", "first", " ", " ");
            var secondElement = CreateElement("second-id", "Second", "second", " ", " ");
            var component = this.Render<DetailsPanelComponent>();

            Assert.That(component.Find(".mb-details-panel__empty").TextContent.Trim(),
                Is.EqualTo("Select an element to display its details."));

            this.SetSelectedElement(firstElement);

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(".mb-details-panel__element-name").TextContent.Trim(),
                        Is.EqualTo("First"));
                    Assert.That(component.Find("dl dd").TextContent.Trim(), Is.EqualTo("first-id"));
                }
            });

            this.SetSelectedElement(secondElement);

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(".mb-details-panel__element-name").TextContent.Trim(),
                        Is.EqualTo("Second"));
                    Assert.That(component.Find("dl dd").TextContent.Trim(), Is.EqualTo("second-id"));
                }
            });

            this.SetSelectedElement(null);

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(".mb-details-panel__empty").TextContent.Trim(),
                        Is.EqualTo("Select an element to display its details."));
                    Assert.That(component.FindAll("dl"), Is.Empty);
                }
            });
        }

        /// <summary>
        /// Verifies disposal removes reactive selection observation and prevents later renders.
        /// </summary>
        [Test]
        public async Task VerifyDisposedPanelIgnoresSelectionChanges()
        {
            var component = this.Render<DetailsPanelComponent>();

            await this.DisposeComponentsAsync();
            var renderCountAfterDisposal = component.RenderCount;

            await this.Renderer.Dispatcher.InvokeAsync(() =>
            {
                this.SetSelectedElement(new Namespace());
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.selectedElement, Is.TypeOf<Namespace>());
                Assert.That(component.RenderCount, Is.EqualTo(renderCountAfterDisposal));
            }
        }

        /// <summary>
        /// Verifies semantic structure, accessible labelling, custom classes, and unmatched attributes.
        /// </summary>
        [Test]
        public void VerifySemanticStructureAndRootAttributes()
        {
            var element = CreateElement("element", "Element", "short", "Name", "Package::Element");
            this.SetSelectedElement(element);
            var component = this.Render<DetailsPanelComponent>(parameters => parameters
                .Add(panel => panel.Class, "custom-details")
                .AddUnmatched("data-testid", "details-panel"));
            var section = component.Find("section");
            var headingId = section.GetAttribute("aria-labelledby");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(section.ClassList, Does.Contain("mb-details-panel"));
                Assert.That(section.ClassList, Does.Contain("custom-details"));
                Assert.That(section.GetAttribute("data-testid"), Is.EqualTo("details-panel"));
                Assert.That(headingId, Is.Not.Empty);
                Assert.That(component.Find($"#{headingId}").TextContent.Trim(), Is.EqualTo("Details"));
                Assert.That(section.QuerySelectorAll("dl"), Has.Length.EqualTo(1));
                Assert.That(section.QuerySelectorAll("dl > div"), Has.Length.EqualTo(4));
            }
        }

        /// <summary>
        /// Updates the mocked selection and publishes its reactive notification.
        /// </summary>
        /// <param name="element">The newly selected element.</param>
        private void SetSelectedElement(IElement element)
        {
            this.selectedElement = element;
            this.selectionService.Raise(
                service => service.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IElementSelectionService.SelectedElement)));
        }

        /// <summary>
        /// Creates an element with controlled identifying and display-name values.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="declaredName">The declared name.</param>
        /// <param name="declaredShortName">The declared short name.</param>
        /// <param name="name">The effective name.</param>
        /// <param name="qualifiedName">The qualified name.</param>
        /// <returns>The configured element.</returns>
        private static IElement CreateElement(
            string elementId,
            string declaredName,
            string declaredShortName,
            string name,
            string qualifiedName)
        {
            var element = new Mock<IElement>();
            element.SetupGet(x => x.ElementId).Returns(elementId);
            element.SetupGet(x => x.DeclaredName).Returns(declaredName);
            element.SetupGet(x => x.DeclaredShortName).Returns(declaredShortName);
            element.SetupGet(x => x.name).Returns(name);
            element.SetupGet(x => x.qualifiedName).Returns(qualifiedName);

            return element.Object;
        }
    }
}
