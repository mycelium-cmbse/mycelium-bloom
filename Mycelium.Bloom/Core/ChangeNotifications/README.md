# Change notifications

`IChangeNotificationService` is a scoped application service shared within one
Blazor circuit. Mutation-response and push adapters publish immutable events;
ViewModels own subscriptions obtained through `Listen(ChangeTarget target = null)`.
Delivery runs on the bus scheduler. A rendering consumer must cross the actual
Blazor renderer boundary before changing component state.

## Events and targets

`ChangeEvent` requires a valid change kind and source, non-empty element and commit
IDs, a generated SysML POCO metaclass class or interface, a complete containment
snapshot, and property names. Collections are copied into immutable collections.
An empty property set means the changed properties are unspecified, so consumers
cannot assume a partial update. `ChangeSource` identifies the mutation's origin,
not whether HTTP or SignalR delivered it; a user's own push echo is still local.

`Listen()` and `ChangeTarget.Global` receive all events. Other targets are:

| Factory | Matching |
| --- | --- |
| `ChangeTarget.Element(id)` | Exactly the changed element |
| `ChangeTarget.ForType(type)` | The metaclass and its derived metaclasses |
| `ChangeTarget.Subtree(id)` | The namespace itself and every containment descendant |

Targets are immutable value keys that distinguish element identity, namespace
scope and metaclass scope. Dispatch constructs matching keys and performs direct
dictionary lookups instead of scanning all subscribers.

## Containment snapshots

`NamespacePath` holds the complete namespace ancestry, nearest namespace first,
excluding the changed element. An empty path explicitly means no containing
namespace; it does not mean unknown ancestry. `ParentNamespaceId` is derived from
the path and is null for an uncontained element.

`NamespacePath.Capture(IElement)` follows SDK `owner` relationships, collecting
namespace owners even across intermediate non-namespace elements. This is the
upward counterpart of SDK `ownedElement` containment; aliases and imports are
not ownership. The producer must capture a coherent SDK graph under its model
owner's concurrency rules, or supply the equivalent complete backend snapshot.
The bus neither traverses live objects during delivery nor owns the model.

Created and updated events carry current containment. Deleted events carry the
last containment before deletion. Moved events require both current containment
and `PreviousContainment`, including an explicitly empty path when moving to or
from a root. Dispatch matches the union of both paths and delivers once per
target. A producer cannot substitute only the immediate parent when higher
ancestors exist; event validation cannot reconstruct omitted backend ancestry.

## Metaclass hierarchy

Generated SDK POCO classes use interfaces to represent metaclass ancestry:
`Package` implements `IPackage`, which extends `INamespace` and `IElement`.
`SysmlMetaclass` matches the SDK's class/interface `ClassAttribute.XmiId` metadata
and normalizes targets to metaclass interfaces. Thus `Namespace` and `INamespace`
share a target even though CLR `Package` does not derive from CLR `Namespace`.
Resolution inspects only the supplied type and its implemented interfaces. In the
pinned SysML2.NET 0.23.0 SDK, each generated POCO class directly implements the
metaclass interface carrying the same XMI identifier. Interface inputs already
identify their canonical metaclass. One per-type cache holds the canonical
interface and its ancestry; there is no assembly scan or global mapping table.
DTO types, unrelated CLR types and custom wrapper types are not accepted.

## Internal responsibilities

| Component | Owned responsibility |
| --- | --- |
| `ChangeNotificationService` | Public API, Rx window scheduling and serialized delivery/completion |
| `ChangeAccumulator` | Synchronous duplicate validation, pending batches, freeze, delivered retention and circuit breaker |
| `ChangeObservableRegistry` | Target routing, synchronous subscription registration, subject lifetime and callback isolation |
| `ModelRefreshCoordinator` | Current reload registration, request ordering and cancellation |
| `SysmlMetaclass` | Validated SDK type normalization and cached interface ancestry |

These are concrete internal collaborators, not independently registered services.
`PendingChange`, `CommitElement`, `ChangeObservable`, `ModelRefreshRegistration`
and `TargetScope` have dedicated definitions. The notification service owns the
accumulator and registry; DI continues to own the scoped refresh coordinator.

## Batching and duplicate identity

The first admitted change reserves a window in the accumulator. After admission
returns, the service enqueues that window request through a synchronized Rx
observer. Rx starts a single 75 ms timer when it processes the request; later
changes do not extend that window. When the timer fires, the accumulator freezes
the pending batch and permits a new window reservation atomically. Empty batches
are filtered before dispatch. An idle circuit schedules no timer and performs
no dispatch or duplicate cleanup.

Queued window requests and batch delivery use the injected `IScheduler`, whose
`Now` also controls duplicate expiry. Terminating the request stream cancels
outstanding timers and queues completion behind any in-flight delivery.
Production defaults to `TaskPoolScheduler.Default`; deterministic checks can use
a virtual-time scheduler. The scheduler must queue work rather than execute
timed work inline, and it remains owned by its supplier. The built-in immediate
and current-thread schedulers are rejected. The supplier must keep its scheduler
available through scheduled stream completion.

The duplicate identity is `(CommitId, ElementId)`. Pending and frozen aggregates
retain their keys until dispatch finishes, regardless of scheduler delay or
burst size. Only then does their two-second retention window begin, without
extending expiry for later echoes. Admission and nonempty delivery evict expired
remembered keys. The 4,096-key cap applies only to delivered changes; overload
evicts the oldest delivered key early, after which an echo can be admitted again.
Pending memory follows the queued workload and is never evicted to satisfy that
cap. At most 4,096 delivered entries remain retained while idle; the next active
operation removes expired entries. There is no separate cleanup loop.

Pending compatible echoes of every change kind combine property names and
preserve local origin if either copy is local, independently of arrival order.
An unspecified-property echo keeps the combined property set unspecified.
Compatibility requires the same commit, element, kind, normalized metaclass,
current containment and previous containment. Merging preserves the structural
kind and both paths and uses a canonical metaclass interface. Incompatible pending
duplicates are rejected instead of choosing a structural interpretation.

Deduplication admits one pending aggregate per key; it does not discard compatible
metadata before dispatch. The scheduler freezes every aggregate in a batch under
the state gate before any subscriber runs. After that boundary, duplicates are
ignored until expiry. A later local echo cannot retroactively change an immutable
notification already delivered as remote. Adapters must identify their own
commits as local at ingestion; merging covers echoes that arrive before the batch
is frozen, not future origin information unavailable within the delivery window.

Producers must emit one consolidated `ChangeEvent` per `(CommitId, ElementId)`.
Compatible HTTP/SignalR representations of that same logical change may merge;
distinct incompatible change kinds for the same element and commit indicate an
invalid producer contract. Producers must agree on structural kind and containment.
Created, deleted and moved changes are never folded into updates. Distinct commits
remain distinct even for the same element, preserving commit IDs for downstream
conflict handling.

## Concurrency and lifetime

There is no service-wide lock. Mutable batching and duplicate state have one
owner: `ChangeAccumulator`. Its narrow lock is required by the synchronous
`Publish()` contract: an incompatible pending duplicate throws before the call
returns. Deferring that check to the scheduler would change error delivery or
require blocking on scheduler work, including virtual time. The accumulator does
not call Rx, registry methods, handlers or subscribers while holding its lock.

`Publish()` validates the event, admits/merges it under the accumulator lock, then
enqueues a window request only after releasing that lock. The synchronized Rx
observer safely orders concurrent requests and termination; it contains no
mutable event state. `IsEnabled` and admission are atomic under the accumulator
lock, so disablement cannot admit an event into a discarded generation.

Subscription registration is synchronous and independently protected by the
registry lock. No accumulator and registry locks are nested. Delivery proceeds:

1. Freeze the pending batch under the accumulator lock, then release it.
2. On the delivery drain, check each event's generation through the accumulator.
3. For each target, acquire its subject lease under the registry lock, then
   release that lock before calling `OnNext`.
4. Release the lease after delivery and remember the delivered event through the
   accumulator only after all its targets finish.

A single downstream `ObserveOn` drain serializes all batch delivery and stream
completion on the injected Rx scheduler. Concurrent producers therefore cannot
overlap callbacks for a subscription; callbacks across targets are also
serialized. Scheduler execution does not imply a fixed OS thread, a UI context
or Blazor renderer affinity. Each individual subscription isolates and logs
callback failures through `ILogger<ChangeNotificationService>`, including
completion failures, before they can interrupt peers, other targets or the
scheduler. Normal Rx auto-detachment still applies to a throwing subscription;
healthy subscriptions continue receiving future changes. Consumers must not
synchronously wait for later notifications on the same drain. They can call bus
APIs without inheriting either component's lock.

`Listen` alone creates no subject. Each subscription acquires the canonical entry
in the registry's `Dictionary<ChangeTarget, ChangeObservable>`. Creation occurs
only at subscription time under the registry lock, so racing subscriptions
cannot create competing subjects and need neither `ConcurrentDictionary` nor
`Lazy`. Its stream uses
`Observable.Create(...).Publish().RefCount()`. Final disconnection removes that
specific subject and retires its lifetime. An Rx `RefCountDisposable` delays
physical disposal only while delivery or completion holds a lease, protecting the
gap between lookup and callback without holding a lock over subscriber code.
Attachment and disconnection share the registry
lock so a subscription cannot race onto a removed subject. A saved `Listen`
observable can subscribe again and acquire the current canonical subject.
`ActiveObservableCount` counts materialized targets, not subscribers or calls to
`Listen`.

Disabling notifications discards pending batches through generation invalidation
and clears duplicate memory. Existing subscriptions remain attached; reenabling
accepts fresh events without replaying discarded work. A callback already being
selected for delivery may finish. Disposal immediately rejects new work, empties
the canonical dictionary and cancels bus-initiated refresh requests. Completion
is queued through the same Rx drain after the in-flight callback returns, so
`OnCompleted` also runs outside both locks and cannot overlap `OnNext`. Terminal
delivery releases the retained subjects and batching resources; a virtual-time
scheduler must be advanced to execute it. Disposal is idempotent and does not
wait on subscriber code. Subsequent
publication, subscription, refresh or enablement fails as disposed usage.
Subscribing a saved observable after service disposal reports the disposal error
through Rx on the subscribing thread; the scheduler contract covers accepted
notification delivery and normal stream completion.

## Manual full-model refresh

`RequestRefresh(CancellationToken)` delegates to the scoped
`IModelRefreshCoordinator`, independently of `IsEnabled`. The connected
current-model/backend adapter registers an `IModelRefreshHandler` and owns the
returned disposable registration. Only one owner may be registered at a time.
Its `ReloadAsync` operation retrieves the complete backend model and coherently
replaces the current state, including publishing the owner's required state
notifications. It must honor cancellation before replacing state.

The coordinator serializes reload requests and returns their actual completion,
cancellation or failure. Detaching the owner or disposing the coordinator cancels
its accepted requests. A narrow registration lock reserves each reload's position
in a task chain; handlers and cancellation callbacks run outside it. Each next
position waits for both its predecessor and the current request to finish. A
cancelled queued request can return immediately without letting a later handler
overtake an active reload. An active handler retains its position until it returns,
even if it delays honoring cancellation. Failures propagate to the requester but
do not poison the completion chain. There is no semaphore, active-request counter
or synchronization-resource disposal bookkeeping. Without a registered owner,
refresh fails with `InvalidOperationException`.

The notification API retains `RequestRefresh()` as its full-model reload entry
point. Its only work is checking service lifetime, linking caller/service
cancellation and awaiting the coordinator; it neither interprets events as
refresh requests nor owns reload state.

Bloom's existing `ModelLoaderService` loads local files and caches the Quantities
model; it has no connected-backend/current-model retrieval context. The future
backend/current-model adapter fulfills `IModelRefreshHandler`. This boundary is
operational when an owner registers, but backend retrieval is not connected by
this infrastructure alone. The notification service owns no project, model,
session, connection or polling policy. `RequestRefresh()` implements the refresh
coordination boundary and awaits a registered full-model reload handler. The
current repository does not contain the active connected-model/backend owner
required to satisfy SSS-CC-BACK-M1V end to end. That system-level requirement is
not fully implemented; its full-backend-reload acceptance criterion can only be
exercised end to end after that owner implements and registers the handler.

## References

- [CDP4 message-bus subscription patterns](https://github.com/STARIONGROUP/COMET-SDK-Community-Edition/blob/development/CDP4Dal/CDPMessageBus.cs)
- [SDK Package declaration](https://github.com/STARIONGROUP/SysML2.NET/blob/905c249e885decd006eba7035d7fc605c7db035e/SysML2.NET/Core/AutoGenPoco/Package.cs)
- [SDK element ownership derivation](https://github.com/STARIONGROUP/SysML2.NET/blob/905c249e885decd006eba7035d7fc605c7db035e/SysML2.NET/Extend/ElementExtensions.cs)
