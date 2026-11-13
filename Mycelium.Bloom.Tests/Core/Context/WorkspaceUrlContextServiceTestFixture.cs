// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceUrlContextServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Context
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.JSInterop;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests reactive projection between browser locations and shared selected-element identity.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class WorkspaceUrlContextServiceTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit context after each test.
        /// </summary>
        [TearDown]
        public Task TearDown()
        {
            return this.DisposeAsync().AsTask();
        }

        /// <summary>
        /// Verifies direct encoded URL context resolves to the exact canonical model element.
        /// </summary>
        [Test]
        public async Task VerifyRestorationsResolveEncodedElementIdentity()
        {
            var element = new Namespace { ElementId = "part/alpha value" };
            var resolver = CreateResolver("part/alpha value", element);
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo(
                "/workspace/modeling?mode=review&selectedElement=part%2Falpha%20value#target");
            using var service = new WorkspaceUrlContextService(
                navigation,
                resolver.Object,
                new ContextAwareService(),
                NullLogger<WorkspaceUrlContextService>.Instance);

            var restoration = await service.Restorations.FirstAsync().ToTask();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(restoration.SelectedElement, Is.SameAs(element));
                Assert.That(restoration.CanonicalUri, Is.Null);
                Assert.That(restoration.ShouldFocusSelectedElement, Is.True);
                resolver.Verify(
                    candidate => candidate.ResolveAsync(
                        "part/alpha value",
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        /// <summary>
        /// Verifies empty, unknown, and conflicting parameters remove only selected-element context.
        /// </summary>
        /// <param name="route">The runtime location to reconcile.</param>
        [TestCase("/workspace/modeling?other=one&selectedElement=#target")]
        [TestCase("/workspace/modeling?other=one&selectedElement=unknown#target")]
        [TestCase("/workspace/modeling?other=one&selectedElement=%ZZ#target")]
        [TestCase("/workspace/modeling?selectedElement=first&other=one&selectedElement=second#target")]
        public async Task VerifyInvalidRuntimeInputCanonicalizesWithoutLosingUnrelatedUriParts(string route)
        {
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver
                .Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<IElement>(null));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo(route);
            using var service = new WorkspaceUrlContextService(
                navigation,
                resolver.Object,
                new ContextAwareService(),
                NullLogger<WorkspaceUrlContextService>.Instance);

            var canonicalUri = await service.NavigationRequests.FirstAsync().ToTask();
            var parsedUri = navigation.ToAbsoluteUri(canonicalUri);
            var query = QueryHelpers.ParseQuery(parsedUri.Query);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsedUri.AbsolutePath, Is.EqualTo("/workspace/modeling"));
                Assert.That(parsedUri.Fragment, Is.EqualTo("#target"));
                Assert.That(query["other"].Single(), Is.EqualTo("one"));
                Assert.That(query.ContainsKey(WorkspaceUrlContextService.SelectedElementParameterName), Is.False);
            }
        }

        /// <summary>
        /// Verifies identical duplicate values resolve once and canonicalize to one selected-element parameter.
        /// </summary>
        [Test]
        public async Task VerifyIdenticalDuplicatesResolveAndCanonicalizeOnce()
        {
            var element = new Namespace { ElementId = "shared" };
            var resolver = CreateResolver("shared", element);
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo(
                "/workspace/dashboard?selectedElement=shared&other=one&selectedElement=shared#target");
            using var service = new WorkspaceUrlContextService(
                navigation,
                resolver.Object,
                new ContextAwareService(),
                NullLogger<WorkspaceUrlContextService>.Instance);

            var restoration = await service.Restorations.FirstAsync().ToTask();
            var canonicalUri = await service.NavigationRequests.FirstAsync().ToTask();
            var parsedUri = navigation.ToAbsoluteUri(canonicalUri);
            var query = QueryHelpers.ParseQuery(parsedUri.Query);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(restoration.SelectedElement, Is.SameAs(element));
                Assert.That(parsedUri.AbsolutePath, Is.EqualTo("/workspace/dashboard"));
                Assert.That(parsedUri.Fragment, Is.EqualTo("#target"));
                Assert.That(query[WorkspaceUrlContextService.SelectedElementParameterName], Has.Count.EqualTo(1));
                Assert.That(
                    query[WorkspaceUrlContextService.SelectedElementParameterName][0],
                    Is.EqualTo("shared"));
                Assert.That(query["other"][0], Is.EqualTo("one"));
            }
        }

        /// <summary>
        /// Verifies selection changes add, replace, and remove only selected-element URL context.
        /// </summary>
        [Test]
        public async Task VerifyNavigationRequestsFollowDistinctSharedSelectionIdentity()
        {
            var context = new ContextAwareService();
            var first = new Namespace { ElementId = "first/value" };
            var equivalentFirst = new Namespace { ElementId = "first/value" };
            var second = new Namespace { ElementId = "second value" };
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver
                .Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string elementId, CancellationToken _) => ValueTask.FromResult<IElement>(
                    elementId switch
                    {
                        "first/value" => first,
                        "second value" => second,
                        _ => null
                    }));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/?other=one#target");
            using var service = new WorkspaceUrlContextService(
                navigation,
                resolver.Object,
                context,
                NullLogger<WorkspaceUrlContextService>.Instance);
            _ = await service.Restorations.FirstAsync().ToTask();
            var requests = new List<string>();
            using var subscription = service.NavigationRequests.Subscribe(requests.Add);
            var applicationLocationRestoration = service.Restorations
                .Skip(1)
                .FirstAsync()
                .ToTask();

            context.SelectedElement = first;
            navigation.NavigateTo(requests[0]);
            var restoredApplicationLocation = await applicationLocationRestoration;
            context.SelectedElement = equivalentFirst;
            context.SelectedElement = second;
            navigation.NavigateTo(requests[1]);
            context.SelectedElement = null;

            var addedUri = navigation.ToAbsoluteUri(requests[0]);
            var replacedUri = navigation.ToAbsoluteUri(requests[1]);
            var clearedUri = navigation.ToAbsoluteUri(requests[2]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requests, Has.Count.EqualTo(3));
                Assert.That(addedUri.AbsolutePath, Is.EqualTo("/"));
                Assert.That(addedUri.Query, Does.Contain("other=one"));
                Assert.That(addedUri.Query, Does.Contain("selectedElement=first%2Fvalue"));
                Assert.That(addedUri.Fragment, Is.EqualTo("#target"));
                Assert.That(replacedUri.Query, Does.Contain("selectedElement=second%20value"));
                Assert.That(clearedUri.Query, Is.EqualTo("?other=one"));
                Assert.That(clearedUri.Fragment, Is.EqualTo("#target"));
                Assert.That(restoredApplicationLocation.ShouldFocusSelectedElement, Is.False);
            }
        }

        /// <summary>
        /// Verifies a route transition requests local focus even when shared selection already has the URL identity.
        /// </summary>
        [Test]
        public async Task VerifyRouteTransitionRequestsFocusForExistingSharedSelection()
        {
            var element = new Namespace { ElementId = "shared" };
            var context = new ContextAwareService { SelectedElement = element };
            var resolver = CreateResolver("shared", element);
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/dashboard?selectedElement=shared");
            using var service = new WorkspaceUrlContextService(
                navigation,
                resolver.Object,
                context,
                NullLogger<WorkspaceUrlContextService>.Instance);
            var initialRestoration = await service.Restorations.FirstAsync().ToTask();
            var routeRestoration = service.Restorations.Skip(1).FirstAsync().ToTask();

            navigation.NavigateTo("/workspace/modeling?selectedElement=shared");
            var restoredRoute = await routeRestoration;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(initialRestoration.ShouldFocusSelectedElement, Is.True);
                Assert.That(restoredRoute.SelectedElement, Is.SameAs(element));
                Assert.That(restoredRoute.ShouldFocusSelectedElement, Is.True);
            }
        }

        /// <summary>
        /// Verifies stale asynchronous element resolution cannot overwrite the latest browser location.
        /// </summary>
        [Test]
        public async Task VerifyRestorationsSwitchAwayStaleResolution()
        {
            var first = new Namespace { ElementId = "first" };
            var second = new Namespace { ElementId = "second" };
            var firstResolution = new TaskCompletionSource<IElement>();
            var firstResolutionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver
                .Setup(candidate => candidate.ResolveAsync("first", It.IsAny<CancellationToken>()))
                .Callback(() => firstResolutionStarted.SetResult())
                .Returns(new ValueTask<IElement>(firstResolution.Task));
            resolver
                .Setup(candidate => candidate.ResolveAsync("second", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<IElement>(second));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            using var service = new WorkspaceUrlContextService(
                navigation,
                resolver.Object,
                new ContextAwareService(),
                NullLogger<WorkspaceUrlContextService>.Instance);
            _ = await service.Restorations.FirstAsync().ToTask();
            var restored = new List<IElement>();
            using var subscription = service.Restorations
                .Skip(1)
                .Select(restoration => restoration.SelectedElement)
                .Subscribe(restored.Add);

            navigation.NavigateTo("/workspace/modeling?selectedElement=first");
            await firstResolutionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            navigation.NavigateTo("/workspace/modeling?selectedElement=second");
            await service.Restorations
                .Where(restoration => ReferenceEquals(restoration.SelectedElement, second))
                .FirstAsync()
                .ToTask();
            firstResolution.SetResult(first);
            await Task.Yield();

            Assert.That(restored, Is.EqualTo(new[] { second }));
        }

        /// <summary>
        /// Verifies browser-history location changes restore each URL's selected-element context.
        /// </summary>
        [Test]
        public async Task VerifyRestorationsFollowBackAndForwardLocationChanges()
        {
            var first = new Namespace { ElementId = "first" };
            var second = new Namespace { ElementId = "second" };
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver
                .Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string elementId, CancellationToken _) => ValueTask.FromResult<IElement>(
                    string.Equals(elementId, "first", StringComparison.Ordinal) ? first : second));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?selectedElement=first");
            using var service = new WorkspaceUrlContextService(
                navigation,
                resolver.Object,
                new ContextAwareService(),
                NullLogger<WorkspaceUrlContextService>.Instance);
            var restored = new List<IElement>();
            var threeRestorations = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var subscription = service.Restorations.Subscribe(restoration =>
            {
                restored.Add(restoration.SelectedElement);

                if (restored.Count == 3)
                {
                    threeRestorations.SetResult();
                }
            });

            navigation.NavigateTo("/workspace/dashboard?selectedElement=second");
            navigation.NavigateTo("/workspace/modeling?selectedElement=first");
            await threeRestorations.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.That(restored, Is.EqualTo(new[] { first, second, first }));
        }

        /// <summary>
        /// Verifies NavigationRail destinations carry only current selected-element context.
        /// </summary>
        [Test]
        public void VerifyGetDestinationUriUsesCanonicalRouteAndCurrentSelection()
        {
            var context = new ContextAwareService
            {
                SelectedElement = new Namespace { ElementId = "part/alpha value" }
            };
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?unrelated=discard#old");
            using var service = new WorkspaceUrlContextService(
                navigation,
                new Mock<IElementIdResolver>(MockBehavior.Strict).Object,
                context,
                NullLogger<WorkspaceUrlContextService>.Instance);

            var destination = service.GetDestinationUri("/workspace/dashboard");

            Assert.That(
                destination,
                Is.EqualTo("/workspace/dashboard?selectedElement=part%2Falpha%20value"));
        }

        [Test]
        public async Task VerifyStaleInspectionCannotOverwriteNewerReferenceAndDisposalStopsNavigation()
        {
            var selected = new Namespace { ElementId = "A" };
            var second = new Namespace { ElementId = "B" };
            var third = new Namespace { ElementId = "C" };
            var delayed = new TaskCompletionSource<IElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver.Setup(candidate => candidate.ResolveAsync("A", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<IElement>(selected));
            resolver.Setup(candidate => candidate.ResolveAsync("B", It.IsAny<CancellationToken>()))
                .Callback(() => started.TrySetResult())
                .Returns(() => new ValueTask<IElement>(delayed.Task));
            resolver.Setup(candidate => candidate.ResolveAsync("C", It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult<IElement>(third));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A");
            var context = new ContextAwareService { SelectedElement = selected };
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context,
                NullLogger<WorkspaceUrlContextService>.Instance);
            var inspections = new List<IElement>();
            using var subscription = service.Restorations.Subscribe(restoration => inspections.Add(restoration.InspectedElement));
            service.Inspect(second);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            service.Inspect(third);
            await service.Restorations.Where(restoration => ReferenceEquals(restoration.InspectedElement, third)).FirstAsync().ToTask();
            delayed.SetResult(second);
            await Task.Yield();
            Assert.That(inspections, Is.EqualTo(new IElement[] { null, third }));
            Assert.That(context.SelectedElement, Is.SameAs(selected));
            service.Dispose();
            var uri = navigation.Uri;
            service.Inspect(second);
            context.InspectedElement = third;
            await service.GoBackAsync();
            Assert.That(navigation.Uri, Is.EqualTo(uri));
        }

        [Test]
        public async Task VerifyPanelBackTraversesHistoryAndPreservesNativeForward()
        {
            var b = new Namespace { ElementId = "B" };
            var c = new Namespace { ElementId = "C" };
            var d = new Namespace { ElementId = "D" };
            var a = new Namespace { ElementId = "A", DeclaredName = "Alpha" };
            Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.Own<OwningMembership>(a, b);
            Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.Own<OwningMembership>(a, c);
            Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.Own<OwningMembership>(a, d);
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(
                    id == "A" ? a : id == "B" ? b : id == "C" ? c : d));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&mode=review#target");
            var context = new ContextAwareService { SelectedElement = a };
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(a);
            using var browser = new Mycelium.Bloom.ViewModel.ProjectBrowser.ProjectBrowserViewModel(loader.Object, context) { IsInspectionActive = true };
            await browser.InitializeAsync(CancellationToken.None);
            browser.SelectNode(browser.RootNodes[0]);
            var js = new InspectionHistoryJsRuntime(() => navigation.Move(-1));
            var dependencies = new ServiceCollection();
            dependencies.AddSingleton<IJSRuntime>(js);
            using var provider = dependencies.BuildServiceProvider();
            // Match the production factory, including resolution of the optional JS runtime.
            using var service = ActivatorUtilities.CreateInstance<WorkspaceUrlContextService>(provider,
                navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance);
            using var restoration = service.Restorations.Subscribe(value =>
            {
                context.SelectedElement = value.SelectedElement;
                context.InspectedElement = value.InspectedElement;
                Assert.That(browser.InspectedNode?.SourceElement, Is.SameAs(value.InspectedElement));
                Assert.That(browser.SelectedNode.SourceElement, Is.SameAs(a));
            });
            service.Inspect(b);
            service.Inspect(c);
            Assert.That(context.InspectedElement, Is.SameAs(c));
            var entries = navigation.EntryCount;
            await service.GoBackAsync();
            Assert.That(context.InspectedElement, Is.SameAs(b));
            navigation.Move(1);
            Assert.That(context.InspectedElement, Is.SameAs(c));
            await service.GoBackAsync();
            await service.GoBackAsync();
            Assert.That(context.InspectedElement, Is.Null);
            Assert.That(context.SelectedElement, Is.SameAs(a));
            Assert.That(navigation.Uri, Does.EndWith("selectedElement=A&mode=review#target"));
            Assert.That(navigation.EntryCount, Is.EqualTo(entries));
            navigation.Move(1);
            Assert.That(context.InspectedElement, Is.SameAs(b));
            service.Inspect(d);
            navigation.Move(1);
            Assert.That(context.InspectedElement, Is.SameAs(d));
            await service.GoBackAsync();
            Assert.That(context.InspectedElement, Is.SameAs(b));
            Assert.That(context.SelectedElement, Is.SameAs(a));
            Assert.That(js.BackCalls, Is.EqualTo(4));
        }

        [Test]
        public async Task VerifyDirectLinkBackNeverTraversesOutsideWorkspace()
        {
            var a = new Namespace { ElementId = "A" };
            var b = new Namespace { ElementId = "B" };
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(id == "A" ? a : b));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=B#target");
            var context = new ContextAwareService();
            var js = new InspectionHistoryJsRuntime(() => navigation.Move(-1));
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context,
                NullLogger<WorkspaceUrlContextService>.Instance, js);
            using var restoration = service.Restorations.Subscribe(value =>
            {
                context.SelectedElement = value.SelectedElement;
                context.InspectedElement = value.InspectedElement;
            });
            await service.GoBackAsync();
            Assert.That(js.BackCalls, Is.Zero);
            Assert.That(navigation.Uri, Does.EndWith("/workspace/modeling?selectedElement=A#target"));
            Assert.That(context.SelectedElement, Is.SameAs(a));
            Assert.That(context.InspectedElement, Is.Null);
            Assert.That(navigation.EntryCount, Is.EqualTo(2));
        }

        [Test]
        public async Task VerifyRapidBackClicksDoNotTraverseTwiceBeforeLocationChanges()
        {
            var a = new Namespace { ElementId = "A" };
            var b = new Namespace { ElementId = "B" };
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(id == "A" ? a : b));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A");
            var context = new ContextAwareService { SelectedElement = a };
            var js = new InspectionHistoryJsRuntime(() => { });
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context,
                NullLogger<WorkspaceUrlContextService>.Instance, js);
            using var restoration = service.Restorations.Subscribe(value => context.InspectedElement = value.InspectedElement);
            service.Inspect(b);
            await service.GoBackAsync();
            await service.GoBackAsync();
            Assert.That(js.BackCalls, Is.EqualTo(1));
            navigation.Move(-1);
            service.Inspect(b);
            await service.GoBackAsync();
            Assert.That(js.BackCalls, Is.EqualTo(2));
        }

        [Test]
        public async Task VerifyTrailFollowsNativeHistoryBranchingAndExplicitStepNavigation()
        {
            var elements = new[] { "A", "B", "C", "D", "E" }.ToDictionary(id => id, id => new Namespace { ElementId = id, DeclaredName = id });
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(elements.GetValueOrDefault(id)));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&mode=review#target");
            var context = new ContextAwareService { SelectedElement = elements["A"] };
            var js = new InspectionHistoryJsRuntime(() => navigation.Move(-1));
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance, js);
            Mycelium.Bloom.Model.WorkspaceUrlContextRestoration snapshot = null;
            using var subscription = service.Restorations.Subscribe(value =>
            {
                context.SelectedElement = value.SelectedElement;
                context.InspectedElement = value.InspectedElement;
                snapshot = value;
            });
            AssertTrail("A");
            service.Inspect(elements["B"]);
            AssertTrail("A", "B");
            service.Inspect(elements["C"]);
            AssertTrail("A", "B", "C");
            service.Inspect(elements["D"]);
            AssertTrail("A", "B", "C", "D");
            await service.GoBackAsync();
            AssertTrail("A", "B", "C");
            navigation.Move(1);
            AssertTrail("A", "B", "C", "D");
            await service.GoBackAsync();
            await service.GoBackAsync();
            AssertTrail("A", "B");
            service.Inspect(elements["E"]);
            AssertTrail("A", "B", "E");
            navigation.Move(1);
            AssertTrail("A", "B", "E");
            var count = navigation.EntryCount;
            await service.InspectTrailStepAsync(1, "B");
            AssertTrail("A", "B");
            Assert.That(navigation.EntryCount, Is.EqualTo(count + 1));
            navigation.Move(-1);
            AssertTrail("A", "B", "E");
            navigation.Move(1);
            AssertTrail("A", "B");
            count = navigation.EntryCount;
            service.Inspect(elements["B"]);
            await service.InspectTrailStepAsync(1, "B");
            await service.InspectTrailStepAsync(0, "stale");
            Assert.That(navigation.EntryCount, Is.EqualTo(count));
            await service.InspectTrailStepAsync(0, "A");
            AssertTrail("A");
            Assert.That(navigation.Uri, Does.Not.Contain("inspectedElement").And.Contain("mode=review").And.EndsWith("#target"));
            navigation.Move(-1);
            AssertTrail("A", "B");
            service.Dispose();
            var retained = navigation.Uri;
            await service.InspectTrailStepAsync(0, "A");
            Assert.That(navigation.Uri, Is.EqualTo(retained));

            void AssertTrail(params string[] expected)
            {
                Assert.That(snapshot.InspectionTrail.Select(step => step.ElementId), Is.EqualTo(expected));
                Assert.That(context.SelectedElement, Is.SameAs(elements["A"]));
                Assert.That(context.InspectedElement?.ElementId, Is.EqualTo(expected.Length > 1 ? expected[^1] : null));
            }
        }

        [Test]
        public async Task VerifyTrailIsBoundedAndUnresolvedIntermediateStepsCannotNavigate()
        {
            var elements = Enumerable.Range(0, 41).ToDictionary(index => index.ToString(), index => new Namespace { ElementId = index.ToString() });
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(elements.GetValueOrDefault(id)));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=0");
            var context = new ContextAwareService { SelectedElement = elements["0"] };
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance,
                new InspectionHistoryJsRuntime(() => navigation.Move(-1)));
            Mycelium.Bloom.Model.WorkspaceUrlContextRestoration snapshot = null;
            using var subscription = service.Restorations.Subscribe(value =>
            {
                context.SelectedElement = value.SelectedElement;
                context.InspectedElement = value.InspectedElement;
                snapshot = value;
            });
            for (var index = 1; index <= 40; index++)
            {
                service.Inspect(elements[index.ToString()]);
            }
            Assert.That(snapshot.InspectionTrail, Has.Count.EqualTo(32));
            Assert.That(snapshot.InspectionTrail[0].ElementId, Is.EqualTo("0"));
            Assert.That(snapshot.InspectionTrail[^1].ElementId, Is.EqualTo("40"));
            Assert.That(navigation.HistoryEntryState, Does.Not.Contain("DisplayName").And.Not.Contain("DeclaredName"));
            var unavailable = snapshot.InspectionTrail[1].ElementId;
            elements.Remove(unavailable);
            navigation.Move(-1);
            var step = snapshot.InspectionTrail.Single(item => item.ElementId == unavailable);
            Assert.That(step.IsAvailable, Is.False);
            var uri = navigation.Uri;
            await service.InspectTrailStepAsync(snapshot.InspectionTrail.ToList().IndexOf(step), unavailable);
            Assert.That(navigation.Uri, Is.EqualTo(uri));
            navigation.NavigateTo("/workspace/modeling?selectedElement=0&inspectedElement=40");
            Assert.That(snapshot.InspectionTrail.Select(item => item.ElementId), Is.EqualTo(new[] { "0", "40" }));
            navigation.NavigateTo("/workspace/modeling?selectedElement=0&inspectedElement=40", new NavigationOptions
            {
                HistoryEntryState = "{not valid json}"
            });
            Assert.That(snapshot.InspectionTrail.Select(item => item.ElementId), Is.EqualTo(new[] { "0", "40" }));
        }

        [Test]
        public void VerifySameUriHistoryEntriesRestoreTheirOwnTrailAndRejectContradictoryMetadata()
        {
            var elements = new[] { "A", "B", "D" }.ToDictionary(id => id, id => new Namespace { ElementId = id });
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(elements.GetValueOrDefault(id)));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=D");
            var context = new ContextAwareService();
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance);
            Mycelium.Bloom.Model.WorkspaceUrlContextRestoration snapshot = null;
            using var subscription = service.Restorations.Subscribe(value => snapshot = value);
            var uri = navigation.Uri;
            Assert.That(snapshot.InspectionTrail.Select(step => step.ElementId), Is.EqualTo(new[] { "A", "D" }));
            navigation.NavigateTo(uri, new NavigationOptions
            {
                HistoryEntryState = System.Text.Json.JsonSerializer.Serialize(new { CurrentUri = uri, PreviousUri = (string)null, Trail = new[] { "A", "B", "D" } })
            });
            Assert.That(snapshot.InspectionTrail.Select(step => step.ElementId), Is.EqualTo(new[] { "A", "B", "D" }));
            navigation.NavigateTo(uri, new NavigationOptions
            {
                HistoryEntryState = System.Text.Json.JsonSerializer.Serialize(new { CurrentUri = uri, PreviousUri = (string)null, Trail = new[] { "B", "A", "D" } })
            });
            Assert.That(snapshot.InspectionTrail.Select(step => step.ElementId), Is.EqualTo(new[] { "A", "D" }));
            navigation.Move(-1);
            Assert.That(snapshot.InspectionTrail.Select(step => step.ElementId), Is.EqualTo(new[] { "A", "B", "D" }));
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=A");
            Assert.That(snapshot.InspectionTrail.Select(step => step.ElementId), Is.EqualTo(new[] { "A" }));
            Assert.That(snapshot.InspectedElement, Is.Null);
            Assert.That(snapshot.CanonicalUri, Does.Not.Contain("inspectedElement"));
        }

        [Test]
        public async Task VerifyCopiedElementUrlsAreCanonicalReopenableAndContainNoTrailMetadata()
        {
            var a = new Namespace { ElementId = "A/root" };
            var b = new Namespace { ElementId = "B value" };
            var context = new ContextAwareService { SelectedElement = a, InspectedElement = b };
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(id == a.ElementId ? a : id == b.ElementId ? b : null));
            var navigation = this.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/workspace/modeling?mode=review&selectedElement=A%2Froot&inspectedElement=B%20value#target", new NavigationOptions
            {
                HistoryEntryState = "private inspection trail metadata"
            });
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance);
            var original = navigation.Uri;
            var selectedLink = service.GetElementUri(new Namespace { ElementId = a.ElementId });
            var inspectedLink = service.GetElementUri(b);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedLink, Does.StartWith("http://localhost/").And.Contain("selectedElement=A%2Froot").And.Not.Contain("inspectedElement"));
                Assert.That(inspectedLink, Does.Contain("selectedElement=A%2Froot").And.Contain("inspectedElement=B%20value").And.Contain("mode=review").And.EndsWith("#target"));
                Assert.That(inspectedLink, Does.Not.Contain("private").And.Not.Contain("Trail"));
                Assert.That(navigation.Uri, Is.EqualTo(original));
                Assert.That(context.SelectedElement, Is.SameAs(a));
                Assert.That(context.InspectedElement, Is.SameAs(b));
                Assert.That(service.GetElementUri(new Namespace { ElementId = "" }), Is.Null);
            }
            navigation.NavigateTo(inspectedLink);
            using var reopened = new WorkspaceUrlContextService(navigation, resolver.Object, new ContextAwareService(), NullLogger<WorkspaceUrlContextService>.Instance);
            var restoration = await reopened.Restorations.FirstAsync().ToTask();
            Assert.That(restoration.SelectedElement, Is.SameAs(a));
            Assert.That(restoration.InspectedElement, Is.SameAs(b));
            Assert.That(restoration.InspectionTrail.Select(step => step.ElementId), Is.EqualTo(new[] { a.ElementId, b.ElementId }));
        }

        [Test]
        public void VerifyImmediateReferenceNavigationUsesLatestSelectionBeforeUriReplacement()
        {
            var a = new Namespace { ElementId = "A" };
            var d = new Namespace { ElementId = "D" };
            var b = new Namespace { ElementId = "B" };
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(id == "A" ? a : id == "D" ? d : b));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&mode=review#target");
            var context = new ContextAwareService { SelectedElement = a };
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance);
            var pendingReplacements = new List<string>();
            using var requests = service.NavigationRequests.Subscribe(pendingReplacements.Add);

            context.SelectedElement = d;
            service.Inspect(b);

            var query = QueryHelpers.ParseQuery(new Uri(navigation.Uri).Query);
            Assert.That(pendingReplacements, Has.Count.EqualTo(1));
            Assert.That(query["selectedElement"].Single(), Is.EqualTo("D"));
            Assert.That(query["inspectedElement"].Single(), Is.EqualTo("B"));
            Assert.That(query["mode"].Single(), Is.EqualTo("review"));
            Assert.That(new Uri(navigation.Uri).Fragment, Is.EqualTo("#target"));
            Assert.That(context.SelectedElement, Is.SameAs(d));
            navigation.Move(-1);
            Assert.That(navigation.Uri, Does.Contain("selectedElement=D").And.Not.Contain("inspectedElement"));
        }

        [Test]
        public void VerifyPendingDuplicateCanonicalizationPreservesTheInspectedPredecessor()
        {
            var elements = new[] { "A", "B", "C" }.ToDictionary(id => id, id => new Namespace { ElementId = id });
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(elements.GetValueOrDefault(id)));
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&selectedElement=A&inspectedElement=B");
            var context = new ContextAwareService { SelectedElement = elements["A"], InspectedElement = elements["B"] };
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance);

            service.Inspect(elements["C"]);
            navigation.Move(-1);

            var query = QueryHelpers.ParseQuery(new Uri(navigation.Uri).Query);
            Assert.That(query["selectedElement"].ToArray(), Is.EqualTo(new[] { "A" }));
            Assert.That(query["inspectedElement"].Single(), Is.EqualTo("B"));
        }

        [Test]
        public async Task VerifyReselectingOriginalBrowserElementClearsInspectionAndReplacesUri()
        {
            var b = new Namespace { ElementId = "B" };
            var a = new Namespace { ElementId = "A", DeclaredName = "Alpha" };
            Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory.Own<OwningMembership>(a, b);
            var context = new ContextAwareService { SelectedElement = a, InspectedElement = b };
            var loader = new Mock<IModelLoaderService>();
            loader.Setup(service => service.LoadQuantitiesModel()).Returns(a);
            using var browser = new Mycelium.Bloom.ViewModel.ProjectBrowser.ProjectBrowserViewModel(loader.Object, context) { IsInspectionActive = true };
            await browser.InitializeAsync(CancellationToken.None);
            var navigation = new InspectionHistoryNavigationManager();
            navigation.NavigateTo("/workspace/modeling?selectedElement=A&inspectedElement=B&mode=review#target");
            var resolver = new Mock<IElementIdResolver>();
            resolver.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string id, CancellationToken _) => ValueTask.FromResult<IElement>(id == "A" ? a : b));
            using var service = new WorkspaceUrlContextService(navigation, resolver.Object, context, NullLogger<WorkspaceUrlContextService>.Instance);
            var entries = navigation.EntryCount;

            browser.SelectNode(browser.RootNodes[0]);
            service.ClearInspection();

            Assert.That(context.SelectedElement, Is.SameAs(a));
            Assert.That(context.InspectedElement, Is.Null);
            Assert.That(browser.InspectedNode, Is.Null);
            Assert.That(navigation.Uri, Does.Contain("selectedElement=A").And.Contain("mode=review").And.EndsWith("#target").And.Not.Contain("inspectedElement"));
            Assert.That(navigation.EntryCount, Is.EqualTo(entries));
        }

        private sealed class InspectionHistoryJsRuntime(Action back) : IJSRuntime
        {
            public int BackCalls { get; private set; }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
            {
                return this.InvokeAsync<TValue>(identifier, CancellationToken.None, args);
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
            {
                Assert.That(identifier, Is.EqualTo("history.back"));
                this.BackCalls++;
                back();
                return ValueTask.FromResult(default(TValue));
            }
        }

        // Unlike bUnit NavigateTo replays, this double retains the browser's cursor and forward entries.
        private sealed class InspectionHistoryNavigationManager : NavigationManager
        {
            private readonly List<(string Location, string State)> entries = new();
            private int index;
            public int EntryCount => this.entries.Count;

            public InspectionHistoryNavigationManager()
            {
                this.Initialize("http://localhost/", "http://localhost/outside-workspace");
                this.entries.Add((this.Uri, null));
            }

            public void Move(int delta)
            {
                var next = this.index + delta;
                if (next < 0 || next >= this.entries.Count)
                {
                    return;
                }

                this.index = next;
                this.Uri = this.entries[next].Location;
                this.HistoryEntryState = this.entries[next].State;
                this.NotifyLocationChanged(false);
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                this.NavigateToCore(uri, new NavigationOptions { ForceLoad = forceLoad });
            }

            protected override void NavigateToCore(string uri, NavigationOptions options)
            {
                Assert.That(options.ForceLoad, Is.False);
                var entry = (this.ToAbsoluteUri(uri).AbsoluteUri, options.HistoryEntryState);
                if (options.ReplaceHistoryEntry)
                {
                    this.entries[this.index] = entry;
                }
                else
                {
                    this.entries.RemoveRange(this.index + 1, this.entries.Count - this.index - 1);
                    this.entries.Add(entry);
                    this.index++;
                }

                this.Uri = entry.Item1;
                this.HistoryEntryState = entry.HistoryEntryState;
                this.NotifyLocationChanged(false);
            }
        }

        private static Mock<IElementIdResolver> CreateResolver(string elementId, IElement element)
        {
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver
                .Setup(candidate => candidate.ResolveAsync(elementId, It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult(element));

            return resolver;
        }
    }
}
