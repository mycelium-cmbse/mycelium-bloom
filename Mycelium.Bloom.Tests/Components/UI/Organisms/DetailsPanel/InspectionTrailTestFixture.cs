// ------------------------------------------------------------------------------------------------
// <copyright file="InspectionTrailTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.DetailsPanel
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;

    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model;
    using Mycelium.Bloom.Tests.Common;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    using Trail = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.InspectionTrail;

    [TestFixture, FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class InspectionTrailTestFixture : BunitContext
    {
        private static readonly string[] HiddenStepNames = ["B", "C"];
        private static readonly double[] MeasuredInspectionWidths = [482d, 600d];
        private static readonly double[] MeasuredWidthsAfterReturn = [482d, 600d, 380d];

        private readonly BehaviorSubject<WorkspaceUrlContextRestoration> restorations = new(CreateRestoration("A"));
        private readonly Mock<IWorkspaceUrlContextService> urls = new();
        private readonly IRenderedComponent<BbPortalHost> portal;

        public InspectionTrailTestFixture()
        {
            this.portal = BlueprintTestSetup.ConfigureWithPortalHost(this);
            this.urls.SetupGet(service => service.Restorations).Returns(this.restorations);
            this.urls.Setup(service => service.InspectTrailStepAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.DisposeAsync();
            this.restorations.Dispose();
        }

        [Test]
        public async Task VerifyCompactTrailAndEllipsisNavigationUseTheRealSteps()
        {
            var component = this.Render<Trail>(parameters => parameters.AddCascadingValue(this.urls.Object)
                .Add(trail => trail.IsInspecting, false).Add(trail => trail.DisplayedElementId, "A"));
            Assert.That(component.FindAll("nav"), Is.Empty);
            foreach (var ids in new[] { new[] { "A", "B" }, new[] { "A", "B", "C" }, new[] { "A", "B", "C", "D" } })
            {
                await component.InvokeAsync(() => this.restorations.OnNext(CreateRestoration(ids)));
                component.Render(parameters => parameters.Add(trail => trail.IsInspecting, true).Add(trail => trail.DisplayedElementId, ids[^1]));
                await component.WaitForAssertionAsync(() =>
                {
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(component.Find("nav").GetAttribute("aria-label"), Is.EqualTo("Inspection Trail"));
                        Assert.That(component.Find("button[title='A']").TextContent.Trim(), Is.EqualTo("A"));
                        Assert.That(component.Find("[aria-current='step']").TextContent.Trim(), Is.EqualTo(ids[^1]));
                        Assert.That(component.Find("[aria-current='step']").QuerySelector("button"), Is.Null);
                        Assert.That(component.FindAll("[aria-selected]"), Is.Empty);
                        Assert.That(component.FindAll(".mb-inspection-trail__step"), Has.Count.EqualTo(2));
                        Assert.That(component.FindAll("button[aria-label='Show earlier inspected elements']"), Has.Count.EqualTo(ids.Length > 2 ? 1 : 0));
                    }
                });
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("button[title='B']"), Is.Empty);
                Assert.That(component.FindAll("button[title='C']"), Is.Empty);
            }
            await component.Find("button[aria-label='Show earlier inspected elements']").ClickAsync();
            await this.portal.WaitForAssertionAsync(() => Assert.That(this.portal.FindAll("[role='menuitem']"), Has.Count.EqualTo(2)));
            Assert.That(this.portal.FindAll("[role='menuitem']").Select(item => item.TextContent.Trim()), Is.EqualTo(HiddenStepNames));
            await this.portal.FindAll("[role='menuitem']")[0].ClickAsync();
            this.urls.Verify(service => service.InspectTrailStepAsync(1, "B"), Times.Once);
            await component.Find("button[title='A']").ClickAsync();
            this.urls.Verify(service => service.InspectTrailStepAsync(0, "A"), Times.Once);
            await component.InvokeAsync(() => this.restorations.OnNext(CreateRestoration("A", "B")));
            component.Render(parameters => parameters.Add(trail => trail.DisplayedElementId, "B"));
            Assert.That(component.FindAll("button[aria-label='Show earlier inspected elements']"), Is.Empty);
            await component.InvokeAsync(() => this.restorations.OnNext(CreateRestoration("A")));
            component.Render(parameters => parameters.Add(trail => trail.IsInspecting, false).Add(trail => trail.DisplayedElementId, "A"));
            Assert.That(component.FindAll("nav"), Is.Empty);
        }

        [Test]
        public async Task VerifyUnavailableStepsAndStaleEndpointsAreNotInteractiveAndSubscriptionIsDisposed()
        {
            var value = CreateRestoration("A", "missing", "C", "D");
            var steps = value.InspectionTrail.Select(step => step.ElementId == "missing" ? step with { IsAvailable = false } : step).ToArray();
            this.restorations.OnNext(new WorkspaceUrlContextRestoration(value.SelectedElement, null, false, value.InspectedElement) { InspectionTrail = steps });
            var component = this.Render<Trail>(parameters => parameters.AddCascadingValue(this.urls.Object)
                .Add(trail => trail.IsInspecting, true).Add(trail => trail.DisplayedElementId, "D"));
            await component.Find("button[aria-label='Show earlier inspected elements']").ClickAsync();
            await this.portal.WaitForAssertionAsync(() => Assert.That(this.portal.FindAll("[role='menuitem'][aria-disabled='true']"), Has.Count.EqualTo(1)));
            component.Render(parameters => parameters.Add(trail => trail.DisplayedElementId, "new-location"));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("nav"), Is.Empty);
                Assert.That(this.restorations.HasObservers, Is.True);
            }
            await this.DisposeComponentsAsync();
            Assert.That(this.restorations.HasObservers, Is.False);
            var css = File.ReadAllText(Path.Combine(TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                "Components", "UI", "Organisms", "DetailsPanel", "InspectionTrail.razor.css"));
            Assert.That(css, Does.Contain("min-width: 0").And.Contain("text-overflow: ellipsis").And.Contain("white-space: nowrap"));
        }

        [Test]
        public async Task VerifyCompactHeaderKeepsRootAndCurrentAndMovesPreviousIntoMenu()
        {
            this.restorations.OnNext(CreateRestoration("A", "B", "C", "D"));
            var component = this.Render<Trail>(parameters => parameters.AddCascadingValue(this.urls.Object)
                .Add(trail => trail.IsInspecting, true).Add(trail => trail.DisplayedElementId, "D"));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("button[title='C']"), Is.Empty);
                Assert.That(component.Find("button[title='A']"), Is.Not.Null);
                Assert.That(component.Find("[aria-current='step']").TextContent.Trim(), Is.EqualTo("D"));
            }
            await component.Find("button[aria-label='Show earlier inspected elements']").ClickAsync();
            await this.portal.WaitForAssertionAsync(() => Assert.That(this.portal.FindAll("[role='menuitem']"), Has.Count.EqualTo(2)));
            Assert.That(this.portal.FindAll("[role='menuitem']").Select(item => item.TextContent.Trim()), Is.EqualTo(HiddenStepNames));
            await this.portal.FindAll("[role='menuitem']")[1].ClickAsync();
            this.urls.Verify(service => service.InspectTrailStepAsync(2, "C"), Times.Once);
            Assert.That(component.FindAll("button[title='C']"), Is.Empty);
        }

        [Test]
        public async Task VerifyLongNamesRetainFullAccessibleTooltipsInCompactHeader()
        {
            var rootName = string.Concat(Enumerable.Repeat("Selected root with a long name ", 8));
            var currentName = string.Concat(Enumerable.Repeat("Inspected element with a long name ", 8));
            var snapshot = CreateRestoration("A", "B", "C", "D");
            this.restorations.OnNext(new WorkspaceUrlContextRestoration(snapshot.SelectedElement, null, false, snapshot.InspectedElement)
            {
                InspectionTrail = snapshot.InspectionTrail.Select((step, index) => index == 0 ? step with { DisplayName = rootName }
                    : index == 3 ? step with { DisplayName = currentName } : step).ToArray()
            });
            var component = this.Render<Trail>(parameters => parameters.AddCascadingValue(this.urls.Object)
                .Add(trail => trail.IsInspecting, true).Add(trail => trail.DisplayedElementId, "D"));
            var root = component.Find("nav li:first-child button");
            var current = component.Find("[aria-current='step'] span[title]");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetAttribute("title"), Is.EqualTo(rootName));
                Assert.That(current.GetAttribute("title"), Is.EqualTo(currentName));
                Assert.That(current.TextContent, Is.EqualTo(currentName));
            }
            await root.ClickAsync();
            this.urls.Verify(service => service.InspectTrailStepAsync(0, "A"), Times.Once);
        }

        [Test]
        public async Task VerifyMeasuredMinimumIsBoundedResetsOnReturnAndIgnoresLateCallbacks()
        {
            var widths = new System.Collections.Generic.List<double>();
            this.restorations.OnNext(CreateRestoration("A", "B"));
            var component = this.Render<Trail>(parameters => parameters.AddCascadingValue(this.urls.Object)
                .Add(trail => trail.IsInspecting, true).Add(trail => trail.DisplayedElementId, "B")
                .Add(trail => trail.MinimumWidthChanged, width => widths.Add(width)));
            await component.Instance.SetRequiredWidth(481.2);
            await component.Instance.SetRequiredWidth(481.2);
            await component.Instance.SetRequiredWidth(double.NaN);
            await component.Instance.SetRequiredWidth(1000);
            Assert.That(widths, Is.EqualTo(MeasuredInspectionWidths));
            component.Render(parameters => parameters.Add(trail => trail.IsInspecting, false));
            await component.WaitForAssertionAsync(() => Assert.That(widths, Is.EqualTo(MeasuredWidthsAfterReturn)));
            await component.Instance.DisposeAsync();
            await component.Instance.SetRequiredWidth(500);
            Assert.That(widths, Has.Count.EqualTo(3));
        }

        private static WorkspaceUrlContextRestoration CreateRestoration(params string[] ids)
        {
            var selected = new Namespace { ElementId = ids[0], DeclaredName = ids[0] };
            var inspected = ids.Length > 1 ? new Namespace { ElementId = ids[^1], DeclaredName = ids[^1] } : null;
            return new WorkspaceUrlContextRestoration(selected, null, false, inspected)
            {
                InspectionTrail = ids.Select(id => new InspectionTrailStep(id, id, true)).ToArray()
            };
        }
    }
}
