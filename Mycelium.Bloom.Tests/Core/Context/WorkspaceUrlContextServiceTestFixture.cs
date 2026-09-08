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
