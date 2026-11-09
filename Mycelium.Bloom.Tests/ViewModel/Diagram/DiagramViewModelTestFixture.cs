// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using ReactiveUI;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.SDK;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Dal;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DiagramViewModelTestFixture
    {
        private static readonly string[] ExpectedAttributes = ["mass : 18.5 kg", "power : 25 W"];

        private ServiceProvider provider;
        private IServiceScope scope;
        private INamespace root;
        private IContextAwareService context;

        [SetUp]
        public void SetUp()
        {
            var environment = new Mock<IHostEnvironment>(MockBehavior.Strict);
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(environment.Object);
            services.AddApplicationServices();
            this.provider = services.BuildServiceProvider();
            this.scope = this.provider.CreateScope();
            this.root = this.scope.ServiceProvider.GetRequiredService<InterconnectionPrototype>().Model;
            this.context = this.scope.ServiceProvider.GetRequiredService<IContextAwareService>();
        }

        [TearDown]
        public void TearDown()
        {
            this.scope.Dispose();
            this.provider.Dispose();
        }

        [Test]
        public void VerifyProjectionMapsOnlyCanonicalBinaryConnections()
        {
            using var diagram = this.CreateDiagram();
            var projection = diagram.Projection;
            var assembler = this.scope.ServiceProvider.GetRequiredService<IAssembler>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Parts, Has.Count.EqualTo(7));
                Assert.That(projection.Connections, Has.Count.EqualTo(5));
                Assert.That(projection.Diagnostics, Has.Length.EqualTo(1));
                Assert.That(projection.Diagnostics[0], Does.Contain("requires two resolved references"));
                Assert.That(
                    projection.Parts.Values.Single(part => part.Name.StartsWith("battery", StringComparison.Ordinal))
                        .ParentElementId,
                    Is.EqualTo(projection.Parts.Values
                        .Single(part => part.Name.StartsWith("power", StringComparison.Ordinal)).ElementId));
                Assert.That(projection.Parts.Values.SelectMany(part => part.Ports), Has.Exactly(10).Items);
                Assert.That(
                    projection.Parts.Values.Single(part => part.Name.StartsWith("aocs", StringComparison.Ordinal))
                        .Attributes,
                    Is.EquivalentTo(ExpectedAttributes));
                Assert.That(
                    projection.Connections.Values.All(connection =>
                        connection.SourceMultiplicity == "[1]" && connection.TargetMultiplicity == "[1]"), Is.True);
                Assert.That(diagram.State.Occurrences.All(node => node.Id.ToString() != node.ElementId), Is.True);
            }

            foreach (var element in InterconnectionProjection.Traverse(this.root))
            {
                Assert.That(assembler.Cache[element.Id].Value, Is.SameAs(element));
            }

            foreach (var connection in projection.Connections.Values)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(projection.Parts[connection.SourcePartId].Ports.Select(port => port.ElementId),
                        Does.Contain(connection.SourcePortId));
                    Assert.That(projection.Parts[connection.TargetPartId].Ports.Select(port => port.ElementId),
                        Does.Contain(connection.TargetPortId));
                }
            }
        }

        [TestCase(FeatureDirectionKind.In)]
        [TestCase(FeatureDirectionKind.Out)]
        [TestCase(FeatureDirectionKind.Inout)]
        [TestCase(null)]
        public void VerifyProjectionPreservesExplicitPortDirection(FeatureDirectionKind? direction)
        {
            var port = InterconnectionProjection.Traverse(this.root).OfType<IPortUsage>().First();
            port.Direction = direction;
            var projection = InterconnectionProjection.Create(this.root);

            Assert.That(
                projection.Parts.Values.SelectMany(part => part.Ports).Single(item => item.ElementId == port.ElementId)
                    .Direction,
                Is.EqualTo(direction));
            Assert.That(port.Direction, Is.EqualTo(direction));
        }

        [Test]
        public void VerifyCanvasChangesPreserveCanonicalIdentityAndIndependentTabState()
        {
            using var first = this.CreateDiagram();
            using var second = this.CreateDiagram();
            using var session = new DiagramCanvasSession(first);
            var initial = session.Capture();
            var node = initial.State.Occurrences.First(item => item.ParentId is null);
            var canonical = InterconnectionProjection.Traverse(this.root).ToDictionary(item => item.ElementId);
            var semanticProjection = JsonSerializer.Serialize(first.Projection);
            var secondJson = DiagramPresentationSerializer.Export(second.State);
            var change = new DiagramCanvasChange(initial.SessionId, initial.Revision, 1,
                [new(node.Id, node.Geometry with { X = node.Geometry.X + 20 })], [], null);
            var result = session.Apply(change);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Accepted, Is.True);
                Assert.That(result.Snapshot.State.Occurrences.Single(item => item.Id == node.Id).ElementId,
                    Is.EqualTo(node.ElementId));
                Assert.That(result.Snapshot.State.Occurrences.Single(item => item.Id == node.Id).Geometry.X,
                    Is.EqualTo(node.Geometry.X + 20));
                Assert.That(DiagramPresentationSerializer.Export(second.State), Is.EqualTo(secondJson));
                Assert.That(
                    InterconnectionProjection.Traverse(this.root)
                        .All(item => ReferenceEquals(item, canonical[item.ElementId])), Is.True);
                Assert.That(JsonSerializer.Serialize(InterconnectionProjection.Create(this.root)),
                    Is.EqualTo(semanticProjection));
            }
        }

        [Test]
        public void VerifyCanvasRejectsStaleReplayedAndUnknownIdentityCommands()
        {
            using var diagram = this.CreateDiagram();
            using var session = new DiagramCanvasSession(diagram);
            var initial = session.Capture();
            var node = initial.State.Occurrences[0];
            var change = new DiagramCanvasChange(initial.SessionId, initial.Revision, 1, [], [], new(20, 30, 0.8));
            Assert.That(session.Apply(change with { SessionId = Guid.NewGuid() }).Accepted, Is.False);
            Assert.That(session.Apply(change with { Revision = initial.Revision + 1 }).Accepted, Is.False);
            Assert.That(session.Apply(change with { Nodes = [new(Guid.NewGuid(), node.Geometry)] }).Accepted, Is.False);
            var next = session.Capture();
            Assert.That(session.Apply(change with { Sequence = next.Sequence + 1 }).Accepted, Is.True);
            Assert.That(session.Apply(change).Accepted, Is.False);
            var saved = diagram.State;
            diagram.AddManualBend();
            Assert.That(
                session.Apply(new(initial.SessionId, next.Revision, next.Sequence + 2, [], [], new(0, 0, 1))).Accepted,
                Is.False);
            Assert.That(diagram.State, Is.Not.SameAs(saved));
        }

        [Test]
        public async Task VerifyReadOnlyCanvasAllowsNavigationAndSelectionButRejectsGeometryAndRoutes()
        {
            using var diagram = this.CreateDiagram();
            using var session = new DiagramCanvasSession(diagram);
            diagram.IsReadOnly = true;
            var snapshot = session.Capture();
            var node = snapshot.State.Occurrences[0];
            var change = new DiagramCanvasChange(snapshot.SessionId, snapshot.Revision, 1, [], [], new(10, 20, 0.5));
            Assert.That(session.Apply(change).Accepted, Is.True);
            snapshot = session.Capture();
            change = change with
            {
                Revision = snapshot.Revision,
                Sequence = 2,
                Nodes = [new(node.Id, node.Geometry with { X = 100 })]
            };
            Assert.That(session.Apply(change).Accepted, Is.False);
            Assert.That(
                session.Apply(change with { Sequence = 3, Nodes = [], Routes = [new(diagram.State.Routes[0].Id, [])] })
                    .Accepted, Is.False);
            await session.SelectAsync(snapshot.SessionId, [node.Id], node.Id);
            Assert.That(this.context.SelectedElement.ElementId, Is.EqualTo(node.ElementId));
        }

        [Test]
        public void VerifyInvalidCanvasPresentationIsRejectedAtomically()
        {
            using var diagram = this.CreateDiagram();
            using var session = new DiagramCanvasSession(diagram);
            var snapshot = session.Capture();
            var node = snapshot.State.Occurrences[0];
            var invalid = new DiagramCanvasChange(snapshot.SessionId, snapshot.Revision, 1,
                [new(node.Id, node.Geometry with { X = double.NaN })], [], null);
            Assert.That(session.Apply(invalid).Accepted, Is.False);
            Assert.That(diagram.State, Is.SameAs(snapshot.State));
            Assert.That(
                session.Apply(invalid with
                {
                    Sequence = 2,
                    Nodes = [new(node.Id, node.Geometry), new(node.Id, node.Geometry)]
                }).Accepted, Is.False);
            Assert.That(session.Apply(invalid with { Sequence = 3, Nodes = default }).Accepted, Is.False);
        }

        [Test]
        public async Task VerifyCanvasDisposalRejectsLateCallbacksWithoutDisposingTab()
        {
            using var diagram = this.CreateDiagram();
            var session = new DiagramCanvasSession(diagram);
            var snapshot = session.Capture();
            var node = snapshot.State.Occurrences[0];
            session.Dispose();
            session.Dispose();
            Assert.That(session.Capture(), Is.Null);
            Assert.That(session.Apply(new(snapshot.SessionId, snapshot.Revision, 1, [], [], new(1, 2, 1))).Accepted,
                Is.False);
            await session.SelectAsync(snapshot.SessionId, [node.Id], node.Id);
            Assert.That(this.context.SelectedElement, Is.Null);
            Assert.That(diagram.AddExisting(node.ElementId), Is.True);
            using var remounted = new DiagramCanvasSession(diagram);
            Assert.That(remounted.Capture().SessionId, Is.Not.EqualTo(snapshot.SessionId));
            Assert.That(remounted.Capture().State, Is.SameAs(diagram.State));
        }

        [Test]
        public async Task VerifyCanvasRejectsInvalidSelectionWithoutSharedFeedback()
        {
            using var diagram = this.CreateDiagram();
            using var session = new DiagramCanvasSession(diagram);
            var snapshot = session.Capture();
            var node = snapshot.State.Occurrences[0];
            await session.SelectAsync(Guid.NewGuid(), [node.Id], node.Id);
            await session.SelectAsync(snapshot.SessionId, [Guid.NewGuid()], node.Id);
            await session.SelectAsync(snapshot.SessionId, [node.Id, node.Id], node.Id);
            await session.SelectAsync(snapshot.SessionId, [], node.Id);
            Assert.That(this.context.SelectedElement, Is.Null);
            Assert.That(diagram.SelectedOccurrences, Is.Empty);
            await session.SelectAsync(snapshot.SessionId, [node.Id], node.Id);
            Assert.That(this.context.SelectedElement.ElementId, Is.EqualTo(node.ElementId));
        }

        [Test]
        public async Task VerifySelectionUsesCanonicalObjectsWithoutChangingBrowserFocus()
        {
            using var diagram = this.CreateDiagram();
            using var browser = this.scope.ServiceProvider.GetRequiredService<Func<IProjectBrowserViewModel>>()();
            await browser.InitializeAsync(this.root, CancellationToken.None);
            browser.SelectNode(browser.RootNodes[0]);
            var browserSelection = browser.SelectedNode;
            var selected = diagram.State.Occurrences[0];
            var expected = InterconnectionProjection.Traverse(this.root)
                .Single(element => element.ElementId == selected.ElementId);
            var notifications = 0;
            using var subscription = this.context.WhenAnyValue(value => value.SelectedElement).Skip(1)
                .Subscribe(_ => notifications++);

            await diagram.SelectAsync([selected.Id], selected.Id);
            await diagram.SelectAsync([selected.Id], selected.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.context.SelectedElement, Is.SameAs(expected));
                Assert.That(browser.SelectedNode, Is.SameAs(browserSelection));
                Assert.That(diagram.SelectedOccurrences, Is.EquivalentTo(new[] { selected.Id }));
                Assert.That(notifications, Is.EqualTo(1));
            }
        }

        [Test]
        public void VerifyPresentationChangesLeaveSemanticDataAndOtherTabsUnchanged()
        {
            using var first = this.CreateDiagram();
            using var second = this.CreateDiagram();
            var secondState = second.State;
            var semanticBefore = JsonSerializer.Serialize(InterconnectionProjection.Create(this.root));
            var count = this.scope.ServiceProvider.GetRequiredService<IAssembler>().Cache.Count;
            var elementId = this.root.ownedElement.Single(element => element.DeclaredName == "aocs").ElementId;
            var node = first.State.Occurrences.Single(item => item.ElementId == elementId);
            var moved = node with
            {
                Geometry = node.Geometry with { X = node.Geometry.X + 45, Y = node.Geometry.Y + 20 }
            };
            Assert.That(first.ApplyPresentation(first.State with
            {
                Occurrences = first.State.Occurrences.Replace(node, moved),
                Viewport = new DiagramViewport(20, 40, 0.8)
            }), Is.True);
            Assert.That(first.AddExisting(node.ElementId), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.State.Occurrences, Has.Length.EqualTo(8));
                Assert.That(first.State.Occurrences.Count(item => item.ElementId == node.ElementId), Is.EqualTo(2));
                Assert.That(second.State, Is.SameAs(secondState));
                Assert.That(this.scope.ServiceProvider.GetRequiredService<IAssembler>().Cache,
                    Has.Count.EqualTo(count));
                Assert.That(JsonSerializer.Serialize(InterconnectionProjection.Create(this.root)),
                    Is.EqualTo(semanticBefore));
            }
        }

        [Test]
        public void VerifyPresentationRoundTripPreservesGeometryRoutesViewportAndIdentities()
        {
            using var diagram = this.CreateDiagram();
            var state = diagram.State with { Viewport = new DiagramViewport(-300, 80, 1.4) };
            Assert.That(diagram.ApplyPresentation(state), Is.True);
            var saved = DiagramPresentationSerializer.Export(diagram.State);
            diagram.AddManualBend();
            diagram.AddExisting(diagram.State.Occurrences[0].ElementId);
            Assert.That(diagram.Import(saved, out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DiagramPresentationSerializer.Export(diagram.State), Is.EqualTo(saved));
                Assert.That(
                    diagram.State.Routes
                        .Single(route => diagram.Projection.Connections[route.ElementId].Name == "CmdBus").Bends,
                    Has.Length.EqualTo(2));
            }
        }

        [TestCase("version")]
        [TestCase("scope")]
        [TestCase("duplicate")]
        [TestCase("unknown")]
        [TestCase("nesting")]
        [TestCase("endpoint")]
        [TestCase("geometry")]
        [TestCase("route")]
        public void VerifyImportRejectsInvalidPresentationAtomically(string failure)
        {
            using var diagram = this.CreateDiagram();
            var original = diagram.State;
            var first = original.Occurrences[0];
            var route = original.Routes[0];
            var invalid = failure switch
            {
                "version" => original with { Version = 2 },
                "scope" => original with { ModelScope = "different-model" },
                "duplicate" => original with { Occurrences = original.Occurrences.Add(first) },
                "unknown" => original with
                {
                    Occurrences = original.Occurrences.SetItem(0, first with { ElementId = "missing" })
                },
                "nesting" => original with
                {
                    Occurrences = original.Occurrences.SetItem(0, first with { ParentId = first.Id })
                },
                "endpoint" => original with
                {
                    Routes = original.Routes.SetItem(0, route with { SourcePortId = "missing" })
                },
                "geometry" => original with
                {
                    Occurrences = original.Occurrences.SetItem(0,
                        first with { Geometry = first.Geometry with { Width = -1 } })
                },
                _ => original with
                {
                    Routes = original.Routes.SetItem(0,
                        route with { Bends = [.. Enumerable.Repeat(new DiagramPoint(1, 1), 33)] })
                }
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.Import(DiagramPresentationSerializer.Export(invalid), out var error), Is.False);
                Assert.That(error, Is.Not.Empty);
                Assert.That(diagram.State, Is.SameAs(original));
            }
        }

        [TestCase("{")]
        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{\"version\":1,\"unexpected\":true}")]
        public void VerifyImportRejectsMalformedJson(string json)
        {
            using var diagram = this.CreateDiagram();
            Assert.That(diagram.Import(json, out _), Is.False);
        }

        [TestCase("add")]
        [TestCase("stress")]
        [TestCase("bend")]
        [TestCase("routes")]
        public void VerifyPresentationActionsPreserveValidatedLimits(string action)
        {
            using var diagram = this.CreateDiagram();
            var state = diagram.State;
            var route = state.Routes[0];
            var candidate = action switch
            {
                "add" => state with { Viewport = new DiagramViewport(100_000, 100_000, 0.1) },
                "stress" => state with
                {
                    Occurrences =
                    [
                        .. state.Occurrences.Select(node => node with
                        {
                            Geometry = node.Geometry with { X = node.Geometry.X + 99_000 }
                        })
                    ],
                    Routes =
                    [
                        .. state.Routes.Select(item => item with
                        {
                            Bends = [.. item.Bends.Select(point => point with { X = point.X + 99_000 })]
                        })
                    ]
                },
                "routes" => state with
                {
                    Routes =
                    [
                        .. Enumerable.Range(0, 100).SelectMany(_ =>
                            state.Routes.Select(item => item with { Id = Guid.NewGuid() }))
                    ]
                },
                _ => state with
                {
                    Occurrences =
                    [
                        .. state.Occurrences.Select(node => node.Id == route.SourceId || node.Id == route.TargetId
                            ? node with { Geometry = node.Geometry with { Y = 99_840 } }
                            : node)
                    ]
                }
            };
            Assert.That(diagram.Import(DiagramPresentationSerializer.Export(candidate), out var error), Is.True, error);
            var before = diagram.State;

            switch (action)
            {
                case "add":
                    this.context.SelectedElement = InterconnectionProjection.Traverse(this.root)
                        .Single(element => element.ElementId == state.Occurrences[0].ElementId);
                    Assert.That(diagram.AddSharedSelection(), Is.False);
                    break;
                case "stress":
                case "routes":
                    diagram.DisplayStressScene();
                    break;
                default:
                    diagram.AddManualBend();
                    break;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.State, Is.SameAs(before));
                Assert.That(diagram.Notice, Is.Not.Empty);
                Assert.That(DiagramPresentationSerializer.Validate(diagram.State, diagram.Projection, out error),
                    Is.True, error);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyAddSharedSelectionReportsUnsupportedSelection(bool selectRoot)
        {
            using var diagram = this.CreateDiagram();
            var before = diagram.State;
            if (selectRoot)
            {
                this.context.SelectedElement = this.root;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.AddSharedSelection(), Is.False);
                Assert.That(diagram.State, Is.SameAs(before));
                Assert.That(diagram.Notice, Does.Contain("Select a part"));
            }
        }

        [Test]
        public void VerifyStressSceneUsesOccurrencesOfTheSameSemanticObjects()
        {
            using var diagram = this.CreateDiagram();
            var canonicalCount = this.scope.ServiceProvider.GetRequiredService<IAssembler>().Cache.Count;
            diagram.DisplayStressScene();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.State.Occurrences, Has.Length.EqualTo(126));
                Assert.That(diagram.State.Routes, Has.Length.EqualTo(90));
                Assert.That(diagram.State.Occurrences.Select(node => node.ElementId).Distinct(), Has.Exactly(7).Items);
                Assert.That(DiagramPresentationSerializer.Validate(diagram.State, diagram.Projection, out var error),
                    Is.True, error);
                Assert.That(this.scope.ServiceProvider.GetRequiredService<IAssembler>().Cache,
                    Has.Count.EqualTo(canonicalCount));
            }
        }

        [TestCase(ProjectLifecycleKind.Review)]
        [TestCase(ProjectLifecycleKind.Archived)]
        public async Task VerifyReadOnlyAllowsNavigationAndSelectionButRejectsPresentationChanges(
            ProjectLifecycleKind lifecycle)
        {
            using var diagram = this.CreateDiagram();
            this.context.LifecycleState = lifecycle;
            var state = diagram.State;
            var selected = state.Occurrences[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.CanEditPresentation, Is.False);
                Assert.That(diagram.AddExisting(selected.ElementId), Is.False);
                Assert.That(diagram.Import(DiagramPresentationSerializer.Export(state), out _), Is.False);
                Assert.That(diagram.ApplyPresentation(state with { Occurrences = state.Occurrences.RemoveAt(0) }),
                    Is.False);
                Assert.That(diagram.ApplyPresentation(state with { Viewport = new DiagramViewport(10, 20, 0.7) }),
                    Is.True);
            }

            await diagram.SelectAsync([selected.Id], selected.Id);
            Assert.That(this.context.SelectedElement.ElementId, Is.EqualTo(selected.ElementId));
            diagram.IsReadOnly = false;
            Assert.That(diagram.CanEditPresentation, Is.False);
        }

        [Test]
        public void VerifyLocalReadOnlySettingCannotBeBypassedByLayoutTools()
        {
            using var diagram = this.CreateDiagram();
            var state = diagram.State;
            diagram.IsReadOnly = true;
            diagram.AddManualBend();
            diagram.DisplayStressScene();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagram.State, Is.SameAs(state));
                Assert.That(diagram.AddExisting(state.Occurrences[0].ElementId), Is.False);
            }
        }

        [Test]
        public async Task VerifyDisposalRejectsLateCanonicalSelectionAndFurtherMutations()
        {
            var completion = new TaskCompletionSource<IElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver.Setup(value => value.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<IElement>(completion.Task));
            var diagram = new DiagramViewModel(this.context, resolver.Object);
            diagram.Initialize(this.root);
            var selected = diagram.State.Occurrences[0];
            var pending = diagram.SelectAsync([selected.Id], selected.Id);
            diagram.Dispose();
            diagram.Dispose();
            completion.SetResult(InterconnectionProjection.Traverse(this.root)
                .Single(element => element.ElementId == selected.ElementId));
            await pending;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.context.SelectedElement, Is.Null);
                Assert.That(diagram.AddExisting(selected.ElementId), Is.False);
                Assert.That(diagram.ApplyPresentation(diagram.State), Is.False);
            }
        }

        [Test]
        public async Task VerifyUnmountCancelsLateCanonicalSelectionWhileTabSurvives()
        {
            var completion = new TaskCompletionSource<IElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            var resolver = new Mock<IElementIdResolver>(MockBehavior.Strict);
            resolver.Setup(value => value.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<IElement>(completion.Task));
            using var diagram = new DiagramViewModel(this.context, resolver.Object);
            diagram.Initialize(this.root);
            using var session = new DiagramCanvasSession(diagram);
            var snapshot = session.Capture();
            var selected = diagram.State.Occurrences[0];
            var pending = session.SelectAsync(snapshot.SessionId, [selected.Id], selected.Id);
            session.Dispose();
            completion.SetResult(InterconnectionProjection.Traverse(this.root)
                .Single(element => element.ElementId == selected.ElementId));
            await pending;
            Assert.That(this.context.SelectedElement, Is.Null);
            Assert.That(diagram.AddExisting(selected.ElementId), Is.True);
        }

        [Test]
        public void VerifyLargeBenchmarkLimitsPreserveNormalValidationAndCanonicalScope()
        {
            using var normal = this.CreateDiagram();
            using var benchmark = this.scope.ServiceProvider.GetRequiredService<Func<DiagramViewModel>>()();
            var options = new Mycelium.Bloom.Core.Configuration.DiagramBenchmarkOptions
            {
                Enabled = true,
                LargeSceneEnabled = true,
                AccessKey = new string('x', 32)
            };
            benchmark.ConfigureBenchmark(options);
            benchmark.Initialize(this.root);
            var source = benchmark.State.Occurrences.First(node => node.ParentId is null);
            var candidate = benchmark.State with
            {
                Occurrences = [.. Enumerable.Range(0, 10_000).Select(_ => source with { Id = Guid.NewGuid() })],
                Routes = [],
                Viewport = new DiagramViewport(0, 0, 0.01)
            };
            var json = DiagramPresentationSerializer.Export(candidate);
            Assert.That(normal.Limits, Is.SameAs(DiagramPresentationLimits.Default));
            Assert.That(normal.Import(json, out _), Is.False);
            Assert.That(benchmark.Import(json, out _), Is.True);
            Assert.That(benchmark.State.Occurrences, Has.Length.EqualTo(10_000));
            Assert.That(benchmark.Projection.Parts, Has.Count.EqualTo(7));
            Assert.That(benchmark.ApplyPresentation(benchmark.State with
            {
                Occurrences = benchmark.State.Occurrences.Add(source with { Id = Guid.NewGuid() })
            }), Is.False);
            Assert.That(benchmark.ApplyPresentation(benchmark.State with
            {
                Occurrences = benchmark.State.Occurrences.SetItem(0, source with { ElementId = "unknown" })
            }), Is.False);
            benchmark.IsReadOnly = true;
            Assert.That(benchmark.Import(json, out _), Is.False);
            Assert.That(() => benchmark.ConfigureBenchmark(options), Throws.TypeOf<InvalidOperationException>());
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void VerifyLargeBenchmarkCapacityRequiresBothFlags(bool enabled, bool large)
        {
            using var diagram = this.scope.ServiceProvider.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.ConfigureBenchmark(new Mycelium.Bloom.Core.Configuration.DiagramBenchmarkOptions
            {
                Enabled = enabled,
                LargeSceneEnabled = large,
                AccessKey = new string('x', 32)
            });
            Assert.That(diagram.Limits, Is.SameAs(DiagramPresentationLimits.Default));
        }

        [Test]
        public async Task VerifyOnDemandFixtureReadsOnlyRequestedBundlesAndPreservesCanonicalIdentity()
        {
            using var template = this.CreateDiagram();
            var source = this.scope.ServiceProvider.GetRequiredService<InterconnectionPrototype>()
                .CreateOnDemandSource(template.Projection, template.State, 100);
            var assembler = this.scope.ServiceProvider.GetRequiredService<IAssembler>();
            var initialObjects = assembler.Cache.Count;
            Assert.That(source.Metadata.Parts, Has.Count.EqualTo(700));
            Assert.That(source.Metadata.Parts.Values.All(part => part.Name.Length == 0 && part.Attributes.IsEmpty),
                Is.True);
            Assert.That(source.Reads, Is.Zero);
            using var session = ActivatorUtilities.CreateInstance<DiagramModelSession>(this.scope.ServiceProvider,
                source, new Mycelium.Bloom.Core.Configuration.DiagramDataOptions());
            using var lease = session.Acquire();
            var first = source.Presentation.Occurrences[0].ElementId;
            var data = await session.LoadAsync([first], true, CancellationToken.None);
            Assert.That(source.Reads, Is.EqualTo(1));
            Assert.That(data.Single().Parts, Has.Count.EqualTo(7));
            Assert.That(data.Single().Connections, Has.Count.EqualTo(5));
            Assert.That(assembler.Cache.Count - initialObjects, Is.LessThan(400));
            var canonical = await session.ResolveAsync(first, CancellationToken.None);
            var owner = (SysML2.NET.Core.POCO.Systems.Parts.IPartUsage)canonical;
            var references = owner.OwnedRelationship.ToArray();
            session.Cache.Invalidate();
            Assert.That(await session.ResolveAsync(first, CancellationToken.None), Is.SameAs(canonical));
            Assert.That(owner.OwnedRelationship, Is.EqualTo(references));
            Assert.That(source.Reads, Is.EqualTo(2));
        }

        [Test]
        public async Task VerifyOnDemandExportFetchesUnloadedDetailsWithoutFillingReadCache()
        {
            using var diagram = this.CreateDiagram();
            var source = this.scope.ServiceProvider.GetRequiredService<InterconnectionPrototype>()
                .CreateOnDemandSource(diagram.Projection, diagram.State, 3);
            using var session = ActivatorUtilities.CreateInstance<DiagramModelSession>(this.scope.ServiceProvider,
                source, new Mycelium.Bloom.Core.Configuration.DiagramDataOptions());
            diagram.InitializeOnDemand(session);
            var saved = DiagramPresentationSerializer.Export(diagram.State);
            var exported = await diagram.LoadExportProjectionAsync(CancellationToken.None);
            Assert.That(exported.Parts.Values.All(part => part.Name.Length > 0), Is.True);
            Assert.That(exported.Parts, Has.Count.EqualTo(21));
            Assert.That(exported.Connections, Has.Count.EqualTo(15));
            Assert.That(source.Reads, Is.EqualTo(3));
            Assert.That(session.Cache.Statistics.CurrentEntryCount, Is.Zero);
            Assert.That(DiagramPresentationSerializer.Export(diagram.State), Is.EqualTo(saved));
            Assert.That(diagram.Projection.Parts.Values.All(part => part.Name.Length == 0), Is.True);
        }

        [Test]
        public async Task VerifyClosingOneConsumerKeepsAnotherConsumerReadCacheAlive()
        {
            using var template = this.CreateDiagram();
            var source = this.scope.ServiceProvider.GetRequiredService<InterconnectionPrototype>()
                .CreateOnDemandSource(template.Projection, template.State, 3);
            using var session = ActivatorUtilities.CreateInstance<DiagramModelSession>(this.scope.ServiceProvider,
                source, new Mycelium.Bloom.Core.Configuration.DiagramDataOptions());
            var first = session.Acquire();
            var second = session.Acquire();
            var id = source.Presentation.Occurrences[0].ElementId;
            await session.LoadAsync([id], true, CancellationToken.None);
            first.Dispose();
            await session.LoadAsync([id], true, CancellationToken.None);
            Assert.That(source.Reads, Is.EqualTo(1));
            second.Dispose();
            using var reopened = session.Acquire();
            await session.LoadAsync([id], true, CancellationToken.None);
            Assert.That(source.Reads, Is.EqualTo(2));
        }

        [Test]
        public void VerifySelectRegionPreservesCrossingEndpointsAndAllContainmentAncestors()
        {
            using var diagram = this.CreateDiagram();
            var root = new DiagramOccurrence(Guid.NewGuid(), "root", new DiagramGeometry(-900, -900, 50, 50), null);
            var parent = new DiagramOccurrence(Guid.NewGuid(), "parent", new DiagramGeometry(-800, -800, 50, 50),
                root.Id);
            var child = new DiagramOccurrence(Guid.NewGuid(), "child", new DiagramGeometry(20, 20, 20, 20), parent.Id);
            var source = new DiagramOccurrence(Guid.NewGuid(), "source", new DiagramGeometry(-300, 50, 20, 20), null);
            var target = new DiagramOccurrence(Guid.NewGuid(), "target", new DiagramGeometry(300, 50, 20, 20), null);
            var unrelated =
                new DiagramOccurrence(Guid.NewGuid(), "unrelated", new DiagramGeometry(900, 900, 20, 20), null);
            var route = new DiagramRoute(Guid.NewGuid(), "connection", source.Id, "source-port", target.Id,
                "target-port", []);
            var state = diagram.State with
            {
                Occurrences = [root, parent, child, source, target, unrelated],
                Routes = [route],
                Viewport = new DiagramViewport(0, 0, 1)
            };
            var region = DiagramViewModel.SelectRegion(state, 100, 100, 0);
            Assert.That(region.Occurrences.Select(node => node.Id),
                Is.EquivalentTo(new[] { root.Id, parent.Id, child.Id, source.Id, target.Id }));
            Assert.That(region.Routes, Is.EqualTo(state.Routes));
            Assert.That(state.Occurrences, Has.Length.EqualTo(6));
        }

        private DiagramViewModel CreateDiagram()
        {
            var diagram = this.scope.ServiceProvider.GetRequiredService<Func<DiagramViewModel>>()();
            diagram.Initialize(this.root);
            return diagram;
        }

        [Test]
        public async Task VerifyReadSessionDisposalRetainsCircuitCanonicalIdentity()
        {
            using var template = this.CreateDiagram();
            var source = this.scope.ServiceProvider.GetRequiredService<InterconnectionPrototype>()
                .CreateOnDemandSource(template.Projection, template.State, 2);
            using var session = ActivatorUtilities.CreateInstance<DiagramModelSession>(this.scope.ServiceProvider,
                source, new Mycelium.Bloom.Core.Configuration.DiagramDataOptions());
            using var lease = session.Acquire();
            var canonical =
                await session.ResolveAsync(source.Presentation.Occurrences[0].ElementId, CancellationToken.None);
            var assembler = this.scope.ServiceProvider.GetRequiredService<IAssembler>();
            var count = assembler.Cache.Count;
            session.Dispose();
            Assert.That(assembler.Cache, Has.Count.EqualTo(count));
            Assert.That(assembler.Cache[canonical.Id].Value, Is.SameAs(canonical));
            Assert.That(session.AssembledBundles, Is.Zero);
            Assert.That(() => session.Acquire(), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(async () => await session.ResolveAsync(canonical.ElementId, CancellationToken.None),
                Throws.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void VerifyCanonicalObjectsCanBeCollectedAfterCircuitScopeRelease()
        {
            var reference = this.LoadAndReleaseCircuit();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.That(reference.IsAlive, Is.False);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WeakReference LoadAndReleaseCircuit()
        {
            using var circuit = this.provider.CreateScope();
            var model = circuit.ServiceProvider.GetRequiredService<InterconnectionPrototype>().Model;
            return new WeakReference(model);
        }
    }
}