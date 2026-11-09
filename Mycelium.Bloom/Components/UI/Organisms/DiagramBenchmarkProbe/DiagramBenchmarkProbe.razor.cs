// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramBenchmarkProbe.razor.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DiagramBenchmarkProbe
{
    using System.Diagnostics;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Reflection.PortableExecutable;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.Options;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram;

    using ReactiveUI;

    /// <summary>Owns an opt-in, read-only measurement boundary shared by both renderers.</summary>
    public sealed partial class DiagramBenchmarkProbe : ComponentBase, IAsyncDisposable
    {
        /// <summary>The measurement mount identity.</summary>
        private readonly Guid sessionId = Guid.NewGuid();

        /// <summary>Cancels measurement subscriptions when their renderer unmounts.</summary>
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>The owned hidden browser registration.</summary>
        private ElementReference root;

        /// <summary>The owned browser module.</summary>
        private IJSObjectReference module;

        /// <summary>The owned callback reference.</summary>
        private DotNetObjectReference<DiagramBenchmarkProbe> reference;

        /// <summary>The initialization joined by disposal.</summary>
        private Task initialization;

        /// <summary>The last observed authoritative state reference.</summary>
        private DiagramPresentationState observed;

        /// <summary>The mount's observed presentation revision.</summary>
        private long revision;

        /// <summary>Whether this measurement mount has ended.</summary>
        private bool disposed;

        /// <summary>The listener owned by an explicitly started diagnostic window.</summary>
        private ActivityListener traceListener;

        /// <summary>Aggregates observations delivered from arbitrary activity-completion threads.</summary>
        private ConcurrentDictionary<string, (int Count, double Milliseconds, long Allocated)> traceTotals = new();

        /// <summary>Retains a bounded timeline of individually expensive operations.</summary>
        private ConcurrentQueue<object> traceEvents = new();

        /// <summary>Starts an isolated observation window without changing diagram commands.</summary>
        /// <returns>The renderer-dispatched registration.</returns>
        [JSInvokable]
        public Task StartBenchmarkTrace() => this.InvokeAsync(() =>
        {
            if (this.disposed || !this.Options.Value.Enabled || !this.Options.Value.LargeSceneEnabled)
            {
                return;
            }

            this.traceListener?.Dispose();
            var totals = this.traceTotals = new();
            var events = this.traceEvents = new();
            this.traceListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == Mycelium.Bloom.Core.DiagramDiagnostics.Source.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => activity.SetCustomProperty("allocation", (Environment.CurrentManagedThreadId, GC.GetAllocatedBytesForCurrentThread())),
                ActivityStopped = activity =>
                {
                    var start = ((int Thread, long Bytes))activity.GetCustomProperty("allocation");
                    var allocated = start.Thread == Environment.CurrentManagedThreadId ? GC.GetAllocatedBytesForCurrentThread() - start.Bytes : 0;
                    totals.AddOrUpdate(activity.OperationName, (1, activity.Duration.TotalMilliseconds, allocated),
                        (_, value) => (value.Count + 1, value.Milliseconds + activity.Duration.TotalMilliseconds, value.Allocated + allocated));
                    if (activity.Duration.TotalMilliseconds >= 0.5 && events.Count < 4096)
                    {
                        events.Enqueue(new
                        {
                            Name = activity.OperationName,
                            StartUnixMs = (activity.StartTimeUtc.Ticks - DateTime.UnixEpoch.Ticks) / (double)TimeSpan.TicksPerMillisecond,
                            Milliseconds = activity.Duration.TotalMilliseconds,
                            AllocatedBytes = allocated
                        });
                    }
                }
            };
            ActivitySource.AddActivityListener(this.traceListener);
        });

        /// <summary>Ends collection and returns bounded diagnostic evidence.</summary>
        /// <returns>Aggregate inclusive timings and retained spans.</returns>
        [JSInvokable]
        public async Task<object> StopBenchmarkTrace()
        {
            object result = null;
            await this.InvokeAsync(() =>
            {
                this.traceListener?.Dispose();
                this.traceListener = null;
                if (!this.disposed && this.Options.Value.Enabled && this.Options.Value.LargeSceneEnabled)
                {
                    result = new
                    {
                        Totals = this.traceTotals.Select(pair => new { Name = pair.Key, pair.Value.Count, pair.Value.Milliseconds, AllocatedBytes = pair.Value.Allocated }).ToArray(),
                        Events = this.traceEvents.ToArray()
                    };
                }
            });
            return result;
        }

        /// <summary>Gets or sets the caller-owned presentation authority.</summary>
        [Parameter, EditorRequired]
        public DiagramViewModel ViewModel { get; set; }

        /// <summary>Gets or sets startup-validated measurement configuration.</summary>
        [Inject]
        private IOptions<DiagramBenchmarkOptions> Options { get; set; }

        /// <summary>Gets or sets the browser interop boundary.</summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        /// <summary>Registers the probe only in explicitly isolated diagnostic execution.</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && this.Options.Value.Enabled && !this.disposed)
            {
                this.initialization = this.InitializeAsync();
                await this.initialization;
            }
        }

        /// <summary>Creates mount-owned resources without outliving disposal.</summary>
        private async Task InitializeAsync()
        {
            var imported = await this.JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js");
            if (this.disposed)
            {
                await imported.DisposeAsync();
                return;
            }

            this.module = imported;
            this.reference = DotNetObjectReference.Create(this);
            await imported.InvokeVoidAsync("mount", this.root, this.reference);
        }

        /// <summary>Reads accepted geometry through the same SignalR transport used by diagram commands.</summary>
        /// <param name="occurrenceId">The graphical occurrence being measured.</param>
        /// <returns>The coherent readback or null after disposal.</returns>
        [JSInvokable]
        public async Task<DiagramBenchmarkSnapshot> ReadBenchmarkSnapshot(Guid occurrenceId)
        {
            DiagramBenchmarkSnapshot result = null;
            if (!this.disposed && this.Options.Value.Enabled)
            {
                await this.InvokeAsync(() =>
                {
                    if (this.disposed)
                    {
                        return;
                    }

                    var state = this.ViewModel.State;
                    if (!ReferenceEquals(this.observed, state))
                    {
                        this.observed = state;
                        this.revision++;
                    }

                    result = new(this.sessionId, state.DiagramId, this.revision, Stopwatch.GetTimestamp(), Stopwatch.Frequency,
                        state.Occurrences.FirstOrDefault(node => node.Id == occurrenceId)?.Geometry, state.Viewport,
                        state.Occurrences.Length, state.Routes.Length);
                });
            }

            return result;
        }

        /// <summary>Echoes a bounded monotonic timestamp for effective transport RTT measurements.</summary>
        /// <returns>The server timestamp, or zero outside the diagnostic lifetime.</returns>
        [JSInvokable]
        public long BenchmarkPing() => !this.disposed && this.Options.Value.Enabled ? Stopwatch.GetTimestamp() : 0;

        /// <summary>Acknowledges an accepted geometry publication without changing either engine's command path.</summary>
        /// <param name="occurrenceId">The graphical occurrence being measured.</param>
        /// <param name="x">The expected accepted horizontal coordinate.</param>
        /// <param name="y">The expected accepted vertical coordinate.</param>
        /// <returns>The matching accepted-state readback or null when the watch ends.</returns>
        [JSInvokable]
        public async Task<DiagramBenchmarkSnapshot> WatchBenchmarkGeometry(Guid occurrenceId, double x, double y)
        {
            if (this.disposed || !this.Options.Value.Enabled || !double.IsFinite(x) || !double.IsFinite(y))
            {
                return null;
            }

            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(this.lifetime.Token);
            cancellation.CancelAfter(TimeSpan.FromSeconds(15));
            try
            {
                await this.ViewModel.WhenAnyValue(value => value.State)
                    .Where(state => state.Occurrences.Any(node => node.Id == occurrenceId
                        && Math.Abs(node.Geometry.X - x) < 0.001 && Math.Abs(node.Geometry.Y - y) < 0.001))
                    .Take(1).ToTask(cancellation.Token);
                return await this.ReadBenchmarkSnapshot(occurrenceId);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                return null;
            }
        }

        /// <summary>Reads process counters and supported runtime settings without exposing environment secrets.</summary>
        /// <returns>The diagnostic process observations.</returns>
        [JSInvokable]
        public object ReadBenchmarkResources()
        {
            if (this.disposed || !this.Options.Value.Enabled)
            {
                return null;
            }

            using var process = Process.GetCurrentProcess();
            return new
            {
                Timestamp = Stopwatch.GetTimestamp(),
                Frequency = Stopwatch.Frequency,
                CpuMilliseconds = process.TotalProcessorTime.TotalMilliseconds,
                WorkingSetBytes = process.WorkingSet64,
                ManagedBytes = GC.GetTotalMemory(false),
                AllocatedBytes = GC.GetTotalAllocatedBytes(),
                HeapBytes = GC.GetGCMemoryInfo().HeapSizeBytes,
                AvailableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
                Collections = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) },
                JitMethods = JitInfo.GetCompiledMethodCount(),
                JitIlBytes = JitInfo.GetCompiledILBytes(),
                JitMilliseconds = JitInfo.GetCompilationTime().TotalMilliseconds,
                Runtime = RuntimeInformation.FrameworkDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                ServerGc = GCSettings.IsServerGC,
                OptimizerDisabled = Assembly.GetExecutingAssembly().GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled,
                AppReadyToRun = HasNativeImage(Assembly.GetExecutingAssembly()),
                FrameworkReadyToRun = HasNativeImage(typeof(object).Assembly),
                CompilationOverrides = new Dictionary<string, string>
                {
                    ["DOTNET_TieredCompilation"] = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
                    ["DOTNET_TieredPGO"] = Environment.GetEnvironmentVariable("DOTNET_TieredPGO"),
                    ["DOTNET_ReadyToRun"] = Environment.GetEnvironmentVariable("DOTNET_ReadyToRun")
                }
            };
        }

        /// <summary>Checks available precompiled image metadata without changing runtime optimization.</summary>
        /// <param name="assembly">The image being inspected.</param>
        /// <returns>Whether the image contains a managed native header.</returns>
        private static bool HasNativeImage(Assembly assembly)
        {
            using var stream = File.OpenRead(assembly.Location);
            using var reader = new PEReader(stream);
            return reader.PEHeaders.CorHeader?.ManagedNativeHeaderDirectory.Size > 0;
        }

        /// <summary>Releases measurement resources without disposing caller-owned state.</summary>
        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.traceListener?.Dispose();
            this.traceListener = null;
            this.lifetime.Cancel();
            this.lifetime.Dispose();
            try
            {
                try
                {
                    if (this.initialization is not null)
                    {
                        await this.initialization;
                    }
                }
                finally
                {
                    if (this.module is not null)
                    {
                        try
                        {
                            await this.module.InvokeVoidAsync("unmount", this.root);
                        }
                        finally
                        {
                            await this.module.DisposeAsync();
                        }
                    }
                }
            }
            catch (JSDisconnectedException)
            {
            }
            finally
            {
                this.reference?.Dispose();
            }
        }
    }
}
