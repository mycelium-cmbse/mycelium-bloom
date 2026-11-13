// ------------------------------------------------------------------------------------------------
// <copyright file="WorkspaceUrlContextService.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Context
{
    using System.Reactive.Linq;
    using System.Text.Json;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Routing;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.JSInterop;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Coordinates selected-element context around Blazor's authoritative navigation service.
    /// </summary>
    public sealed class WorkspaceUrlContextService : IWorkspaceUrlContextService
    {
        /// <summary>
        /// The query parameter used for stable selected-element identity.
        /// </summary>
        public const string SelectedElementParameterName = "selectedElement";

        /// <summary>The independent Properties reference identifier.</summary>
        public const string InspectedElementParameterName = "inspectedElement";

        /// <summary>
        /// Resolves stable identifiers against the loaded SysML model.
        /// </summary>
        private readonly IElementIdResolver elementIdResolver;

        /// <summary>
        /// Provides the shared selected-element authority.
        /// </summary>
        private readonly IElementSelectionService elementSelectionService;

        /// <summary>
        /// Reports model-resolution failures without terminating browser-location observation.
        /// </summary>
        private readonly ILogger<WorkspaceUrlContextService> logger;

        /// <summary>
        /// Observes and derives actual browser locations.
        /// </summary>
        private readonly NavigationManager navigationManager;

        /// <summary>Invokes native Back only for a known reference-navigation entry.</summary>
        private readonly IJSRuntime jsRuntime;

        /// <summary>Prevents repeated Back clicks while a native traversal is pending.</summary>
        private bool backPending;

        /// <summary>Bounds each entry's identifier-only trail, retaining the root and newest steps.</summary>
        private const int MaximumTrailLength = 32;

        /// <summary>Cancels explicit trail-step resolution when the layout service is disposed.</summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

        /// <summary>
        /// Owns the single connection to framework location events.
        /// </summary>
        private readonly IDisposable restorationConnection;

        /// <summary>
        /// A value indicating whether final service disposal has occurred.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceUrlContextService" /> class.
        /// </summary>
        /// <param name="navigationManager">Blazor's authoritative navigation service.</param>
        /// <param name="elementIdResolver">The model-level stable identifier resolver.</param>
        /// <param name="elementSelectionService">The shared selected-element authority.</param>
        /// <param name="logger">Reports model-resolution failures.</param>
        /// <param name="jsRuntime">Supports native traversal of reference history entries.</param>
        public WorkspaceUrlContextService(
            NavigationManager navigationManager,
            IElementIdResolver elementIdResolver,
            IElementSelectionService elementSelectionService,
            ILogger<WorkspaceUrlContextService> logger,
            IJSRuntime jsRuntime = null)
        {
            ArgumentNullException.ThrowIfNull(navigationManager);
            ArgumentNullException.ThrowIfNull(elementIdResolver);
            ArgumentNullException.ThrowIfNull(elementSelectionService);
            ArgumentNullException.ThrowIfNull(logger);

            this.navigationManager = navigationManager;
            this.elementIdResolver = elementIdResolver;
            this.elementSelectionService = elementSelectionService;
            this.logger = logger;
            this.jsRuntime = jsRuntime;

            var restorations = Observable
                .FromEventPattern<LocationChangedEventArgs>(
                    handler => this.navigationManager.LocationChanged += handler,
                    handler => this.navigationManager.LocationChanged -= handler)
                .Select(_ =>
                {
                    this.backPending = false;
                    return (Location: this.navigationManager.Uri, EntryState: this.navigationManager.HistoryEntryState);
                })
                .StartWith((Location: this.navigationManager.Uri, EntryState: this.navigationManager.HistoryEntryState))
                .DistinctUntilChanged()
                .Select(location => this.CreateLocationTransition(location.Location) with { EntryState = location.EntryState })
                .Scan((previous, current) => current with
                {
                    RouteChanged = !string.Equals(
                        previous.AbsolutePath,
                        current.AbsolutePath,
                        StringComparison.OrdinalIgnoreCase)
                })
                .Select(transition => this.ParseLocation(transition) with { EntryState = transition.EntryState })
                .Select(parsedLocation => Observable.FromAsync(cancellationToken =>
                    this.ResolveTrailRestorationAsync(parsedLocation, cancellationToken)))
                .Switch()
                .Replay(1);

            this.Restorations = restorations;

            var selectionNavigationRequests = this.elementSelectionService
                .WhenAnyValue(selection => selection.SelectedElement)
                .Select(GetStableElementId)
                .DistinctUntilChanged(StringComparer.Ordinal)
                .Skip(1)
                .SkipUntil(restorations.Take(1))
                .Where(elementId => !string.Equals(elementId,
                    QueryHelpers.ParseQuery(this.navigationManager.ToAbsoluteUri(this.navigationManager.Uri).Query)
                        .TryGetValue(SelectedElementParameterName, out var selected) ? selected.FirstOrDefault() : null,
                    StringComparison.Ordinal))
                .Select(elementId => this.GetUriWithInspection(this.GetUriWithSelectedElement(this.navigationManager.Uri, elementId), null))
                .Where(this.IsDifferentFromCurrentUri);

            var canonicalNavigationRequests = restorations
                .Select(restoration => restoration.CanonicalUri)
                .Where(uri => uri is not null)
                .Where(this.IsDifferentFromCurrentUri);

            this.NavigationRequests = canonicalNavigationRequests
                .Merge(selectionNavigationRequests);
            this.restorationConnection = restorations.Connect();
        }

        /// <summary>
        /// Gets replayed workspace restorations derived from authoritative browser locations.
        /// </summary>
        public IObservable<WorkspaceUrlContextRestoration> Restorations { get; }

        /// <summary>
        /// Gets replacement navigation requests derived from shared selection and URL canonicalization.
        /// </summary>
        public IObservable<string> NavigationRequests { get; }

        /// <summary>
        /// Adds the current stable element selection to a canonical workspace destination.
        /// </summary>
        /// <param name="canonicalHref">The canonical destination route.</param>
        /// <returns>The destination URI carrying the transferable selected-element context.</returns>
        public string GetDestinationUri(string canonicalHref)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(canonicalHref);

            var elementId = GetStableElementId(this.elementSelectionService.SelectedElement);

            return elementId is null
                ? canonicalHref
                : QueryHelpers.AddQueryString(canonicalHref, SelectedElementParameterName, elementId);
        }

        /// <summary>Creates an absolute canonical link for the displayed element without inspection-history metadata.</summary>
        /// <param name="element">The canonical element displayed in Properties.</param>
        /// <returns>The reopenable URI, or <see langword="null" /> when stable identity is unavailable or the service has ended.</returns>
        public string GetElementUri(IElement element)
        {
            var elementId = GetStableElementId(element);
            if (this.isDisposed || elementId is null)
            {
                return null;
            }

            var selectedId = GetStableElementId(this.elementSelectionService.SelectedElement) ?? elementId;
            var uri = this.GetUriWithSelectedElement(this.navigationManager.Uri, selectedId);
            return this.GetUriWithInspection(uri, elementId == selectedId ? null : elementId);
        }

        /// <summary>Follows a distinct stable reference while preserving the canonical main selection.</summary>
        /// <param name="element">The canonical SDK reference target.</param>
        public void Inspect(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            var id = GetStableElementId(element);
            if (this.isDisposed || this.backPending || id is null)
            {
                return;
            }

            var displayedId = GetStableElementId(this.elementSelectionService.InspectedElement)
                              ?? GetStableElementId(this.elementSelectionService.SelectedElement);
            if (string.Equals(id, displayedId, StringComparison.Ordinal))
            {
                return;
            }

            var selectedId = GetStableElementId(this.elementSelectionService.SelectedElement);
            var selectedUri = this.GetUriWithSelectedElement(this.navigationManager.Uri, selectedId);

            if (this.IsDifferentFromCurrentUri(selectedUri))
            {
                // Reconcile a pending Browser selection before pushing its first reference entry.
                var inspectedId = GetStableElementId(this.elementSelectionService.InspectedElement);
                this.navigationManager.NavigateTo(this.GetUriWithInspection(selectedUri, inspectedId), new NavigationOptions { ReplaceHistoryEntry = true });
            }

            var uri = this.GetUriWithInspection(this.navigationManager.Uri, id);
            if (!this.IsDifferentFromCurrentUri(uri))
            {
                return;
            }

            var trail = ReadTrailIds(this.navigationManager.Uri, this.navigationManager.HistoryEntryState,
                GetStableElementId(this.elementSelectionService.SelectedElement),
                GetStableElementId(this.elementSelectionService.InspectedElement));
            var nextTrail = string.Equals(id, selectedId, StringComparison.Ordinal)
                ? new[] { selectedId }
                : BoundTrail(trail.Append(id));
            this.PushInspection(uri, nextTrail);
        }

        /// <summary>Removes temporary inspection after the Browser has applied explicit selection.</summary>
        public void ClearInspection()
        {
            if (this.isDisposed)
            {
                return;
            }

            var selectedId = GetStableElementId(this.elementSelectionService.SelectedElement);
            var uri = this.GetUriWithInspection(this.GetUriWithSelectedElement(this.navigationManager.Uri, selectedId), null);

            if (this.IsDifferentFromCurrentUri(uri))
            {
                this.navigationManager.NavigateTo(uri, new NavigationOptions { ReplaceHistoryEntry = true });
            }
        }

        /// <summary>Traverses the known predecessor or returns safely to selection for a direct link.</summary>
        /// <returns>The asynchronous browser navigation operation.</returns>
        public async Task GoBackAsync()
        {
            if (this.isDisposed || this.backPending || this.elementSelectionService.InspectedElement is null)
            {
                return;
            }

            var previous = this.ReadPreviousInspectionUri();
            if (previous is not null && this.jsRuntime is not null)
            {
                this.backPending = true;
                try
                {
                    await this.jsRuntime.InvokeVoidAsync("history.back", this.lifetimeCancellation.Token);
                }
                catch (OperationCanceledException) when (this.lifetimeCancellation.IsCancellationRequested)
                {
                    this.backPending = false;
                }
                catch
                {
                    this.backPending = false;
                    throw;
                }

                return;
            }

            // A direct link has no known workspace predecessor; stay on this route.
            var destination = previous ?? this.GetUriWithInspection(this.navigationManager.Uri, null);
            if (this.IsDifferentFromCurrentUri(destination))
            {
                this.navigationManager.NavigateTo(destination, new NavigationOptions { ReplaceHistoryEntry = true });
            }
        }

        /// <summary>Recognizes the immediate predecessor recorded when this reference entry was pushed.</summary>
        /// <returns>The validated preceding workspace URI, or <see langword="null" /> when no predecessor is known.</returns>
        private string ReadPreviousInspectionUri()
        {
            try
            {
                var state = JsonSerializer.Deserialize<InspectionHistory>(this.navigationManager.HistoryEntryState ?? "null");
                if (state is null || state.CurrentUri != this.navigationManager.Uri || string.IsNullOrEmpty(state.PreviousUri))
                {
                    return null;
                }

                var current = this.navigationManager.ToAbsoluteUri(state.CurrentUri);
                var previous = this.navigationManager.ToAbsoluteUri(state.PreviousUri);
                var query = QueryHelpers.ParseQuery(previous.Query);
                var selectedId = query.TryGetValue(SelectedElementParameterName, out var selected) && selected.Count == 1
                    ? selected[0] : null;
                return previous.GetLeftPart(UriPartial.Path) == current.GetLeftPart(UriPartial.Path)
                       && string.Equals(selectedId, GetStableElementId(this.elementSelectionService.SelectedElement), StringComparison.Ordinal)
                    ? state.PreviousUri : null;
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or UriFormatException)
            {
                return null;
            }
        }

        /// <summary>Marks one reference entry and its immediate workspace predecessor without duplicating browser history.</summary>
        /// <param name="CurrentUri">The exact URI belonging to this entry.</param>
        /// <param name="PreviousUri">The immediate workspace predecessor.</param>
        /// <param name="Trail">The optional bounded stable identifier snapshot.</param>
        private sealed record InspectionHistory(string CurrentUri, string PreviousUri, string[] Trail = null);

        /// <summary>Pushes inspection navigation with only the new entry's bounded stable identifiers.</summary>
        /// <param name="uri">The canonical inspection destination.</param>
        /// <param name="trail">The bounded stable identifier snapshot for the destination entry.</param>
        private void PushInspection(string uri, string[] trail)
        {
            this.navigationManager.NavigateTo(uri, new NavigationOptions
            {
                HistoryEntryState = JsonSerializer.Serialize(new InspectionHistory(uri, this.navigationManager.Uri, trail))
            });
        }

        /// <summary>Pushes an available earlier step only while its rendered trail and URI remain current.</summary>
        /// <param name="index">The earlier step's zero-based index in the current trail.</param>
        /// <param name="elementId">The rendered stable identifier used to reject stale actions.</param>
        /// <returns>The canonical resolution and navigation operation.</returns>
        public async Task InspectTrailStepAsync(int index, string elementId)
        {
            if (this.isDisposed || this.backPending)
            {
                return;
            }

            var source = this.navigationManager.Uri;
            var state = this.navigationManager.HistoryEntryState;
            var query = QueryHelpers.ParseQuery(this.navigationManager.ToAbsoluteUri(source).Query);
            var selectedId = query.TryGetValue(SelectedElementParameterName, out var selected) && selected.Count == 1 ? selected[0] : null;
            var inspectedId = query.TryGetValue(InspectedElementParameterName, out var inspected) && inspected.Count == 1 ? inspected[0] : null;
            if (selectedId != GetStableElementId(this.elementSelectionService.SelectedElement)
                || inspectedId != GetStableElementId(this.elementSelectionService.InspectedElement))
            {
                return;
            }

            var trail = ReadTrailIds(source, state, GetStableElementId(this.elementSelectionService.SelectedElement),
                GetStableElementId(this.elementSelectionService.InspectedElement));
            if (index < 0 || index >= trail.Length - 1 || trail[index] != elementId)
            {
                return;
            }

            try
            {
                var element = await this.elementIdResolver.ResolveAsync(trail[index], this.lifetimeCancellation.Token);
                if (this.isDisposed || element is null || this.backPending
                    || !string.Equals(GetStableElementId(element), trail[index], StringComparison.Ordinal)
                    || source != this.navigationManager.Uri || state != this.navigationManager.HistoryEntryState)
                {
                    return;
                }

                var uri = this.GetUriWithInspection(source, trail[index]);
                if (this.IsDifferentFromCurrentUri(uri))
                {
                    this.PushInspection(uri, trail.Take(index + 1).ToArray());
                }
            }
            catch (OperationCanceledException) when (this.isDisposed)
            {
                // The owning workspace ended during model resolution.
            }
            catch (Exception exception)
            {
                this.logger.LogWarning(exception, "Inspection trail step could not be resolved.");
            }
        }

        /// <summary>Bounds snapshots without discarding the selected root.</summary>
        /// <param name="steps">The stable identifiers in reference-navigation order.</param>
        /// <returns>The root followed by at most the newest thirty-one steps.</returns>
        private static string[] BoundTrail(IEnumerable<string> steps)
        {
            var ids = steps.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
            return ids.Length <= MaximumTrailLength
                ? ids : new[] { ids[0] }.Concat(ids.TakeLast(MaximumTrailLength - 1)).ToArray();
        }

        /// <summary>Accepts history metadata only when its endpoints agree with the authoritative URI context.</summary>
        /// <param name="location">The authoritative absolute location.</param>
        /// <param name="entryState">The optional current history-entry metadata.</param>
        /// <param name="selectedId">The canonical main selection identifier.</param>
        /// <param name="inspectedId">The distinct inspected identifier, or <see langword="null" />.</param>
        /// <returns>A validated bounded snapshot, falling back to the real URI endpoints.</returns>
        private static string[] ReadTrailIds(string location, string entryState, string selectedId, string inspectedId)
        {
            if (selectedId is null)
            {
                return Array.Empty<string>();
            }

            if (inspectedId is null || string.Equals(selectedId, inspectedId, StringComparison.Ordinal))
            {
                return new[] { selectedId };
            }

            try
            {
                var state = JsonSerializer.Deserialize<InspectionHistory>(entryState ?? "null");
                if (state is not null && state.CurrentUri == location && state.Trail is { Length: >= 2 and <= MaximumTrailLength } ids
                    && ids[0] == selectedId && ids[^1] == inspectedId && ids.All(id => !string.IsNullOrWhiteSpace(id)))
                {
                    return ids;
                }
            }
            catch (JsonException)
            {
                // Malformed or older entry metadata falls back to the two real URI endpoints.
            }

            return new[] { selectedId, inspectedId };
        }

        /// <summary>Resolves labels for the current entry without letting metadata override either URI identity.</summary>
        /// <param name="location">The parsed authoritative URI and history entry.</param>
        /// <param name="cancellationToken">Cancels superseded model resolution.</param>
        /// <returns>The endpoint restoration with its resolved presentation-only trail.</returns>
        private async Task<WorkspaceUrlContextRestoration> ResolveTrailRestorationAsync(ParsedLocation location, CancellationToken cancellationToken)
        {
            var restoration = await this.ResolveRestorationAsync(location, cancellationToken);
            var ids = ReadTrailIds(location.Location, location.EntryState,
                GetStableElementId(restoration.SelectedElement), GetStableElementId(restoration.InspectedElement));
            var steps = new List<InspectionTrailStep>();
            foreach (var id in ids)
            {
                IElement element = null;
                var label = id;
                try
                {
                    if (id == GetStableElementId(restoration.SelectedElement))
                    {
                        element = restoration.SelectedElement;
                    }
                    else if (id == GetStableElementId(restoration.InspectedElement))
                    {
                        element = restoration.InspectedElement;
                    }
                    else
                    {
                        element = await this.elementIdResolver.ResolveAsync(id, cancellationToken);
                    }

                    if (GetStableElementId(element) != id)
                    {
                        element = null;
                    }

                    label = string.IsNullOrWhiteSpace(element?.name) ? id : element.name;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    this.logger.LogWarning(exception, "Inspection trail label could not be resolved.");
                }

                steps.Add(new InspectionTrailStep(id, label, element is not null));
            }

            return new WorkspaceUrlContextRestoration(restoration.SelectedElement, restoration.CanonicalUri,
                restoration.ShouldFocusSelectedElement, restoration.InspectedElement)
            { InspectionTrail = steps.ToArray() };
        }

        /// <summary>
        /// Stops URL-context publication and releases the replay connection.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.lifetimeCancellation.Cancel();
            this.restorationConnection.Dispose();
            this.lifetimeCancellation.Dispose();
        }

        /// <summary>
        /// Parses the selected-element parameter without assigning meaning to malformed browser input.
        /// </summary>
        /// <param name="transition">The current browser location and its path relationship to the prior location.</param>
        /// <returns>The parsed candidate and whether input requires canonicalization.</returns>
        private ParsedLocation ParseLocation(LocationTransition transition)
        {
            try
            {
                var query = QueryHelpers.ParseQuery(
                    this.navigationManager.ToAbsoluteUri(transition.Location).Query);

                if (!query.TryGetValue(SelectedElementParameterName, out var values))
                {
                    return new ParsedLocation(transition.Location, null, false, false);
                }

                var candidates = values.ToArray();

                if (candidates.Length == 0
                    || candidates.Any(string.IsNullOrWhiteSpace))
                {
                    return new ParsedLocation(transition.Location, null, true, false);
                }

                var candidate = candidates[0];
                var duplicateValuesAreIdentical =
                    candidates.All(value => string.Equals(value, candidate, StringComparison.Ordinal));
                var shouldFocusSelectedElement = transition.RouteChanged
                                                 || !string.Equals(
                                                     candidate,
                                                     GetStableElementId(this.elementSelectionService.SelectedElement),
                                                     StringComparison.Ordinal);

                return duplicateValuesAreIdentical
                    ? new ParsedLocation(
                        transition.Location,
                        candidate,
                        candidates.Length > 1,
                        shouldFocusSelectedElement)
                    : new ParsedLocation(transition.Location, null, true, false);
            }
            catch (Exception exception) when (exception is ArgumentException or UriFormatException)
            {
                return new ParsedLocation(transition.Location, null, false, false);
            }
        }

        /// <summary>
        /// Projects one framework location into immutable route-comparison state.
        /// </summary>
        /// <param name="location">The absolute framework location.</param>
        /// <returns>The transition candidate, treated as an initial route until paired by the stream.</returns>
        private LocationTransition CreateLocationTransition(string location)
        {
            return new LocationTransition(
                location,
                this.navigationManager.ToAbsoluteUri(location).AbsolutePath,
                true);
        }

        /// <summary>
        /// Resolves one parsed location while keeping unavailable model data non-fatal to navigation.
        /// </summary>
        /// <param name="parsedLocation">The parsed URL state.</param>
        /// <param name="cancellationToken">Cancels stale resolution.</param>
        /// <returns>The resolved selection and optional canonical replacement URI.</returns>
        private async Task<WorkspaceUrlContextRestoration> ResolveSelectionAsync(
            ParsedLocation parsedLocation,
            CancellationToken cancellationToken)
        {
            if (parsedLocation.ElementId is null)
            {
                var emptyCanonicalUri = parsedLocation.RequiresCanonicalization
                    ? this.GetUriWithSelectedElement(parsedLocation.Location, null)
                    : null;

                return new WorkspaceUrlContextRestoration(null, emptyCanonicalUri, false);
            }

            try
            {
                var element = await this.elementIdResolver.ResolveAsync(
                    parsedLocation.ElementId,
                    cancellationToken);
                string canonicalUri;

                if (element is null)
                {
                    canonicalUri = this.GetUriWithSelectedElement(parsedLocation.Location, null);
                }
                else if (parsedLocation.RequiresCanonicalization)
                {
                    canonicalUri = this.GetUriWithSelectedElement(
                        parsedLocation.Location,
                        parsedLocation.ElementId);
                }
                else
                {
                    canonicalUri = null;
                }

                return new WorkspaceUrlContextRestoration(
                    element,
                    canonicalUri,
                    element is not null && parsedLocation.ShouldFocusSelectedElement);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Selected-element URL context could not be resolved.");

                return new WorkspaceUrlContextRestoration(null, null, false);
            }
        }

        /// <summary>Resolves inspection separately so failures cannot discard a valid selection.</summary>
        /// <param name="parsedLocation">The selected-element candidate and original browser location.</param>
        /// <param name="cancellationToken">Cancels superseded endpoint resolution.</param>
        /// <returns>The canonical selected and inspected endpoints with any required URI replacement.</returns>
        private async Task<WorkspaceUrlContextRestoration> ResolveRestorationAsync(
            ParsedLocation parsedLocation, CancellationToken cancellationToken)
        {
            var selection = await this.ResolveSelectionAsync(parsedLocation, cancellationToken);
            var query = QueryHelpers.ParseQuery(this.navigationManager.ToAbsoluteUri(parsedLocation.Location).Query);
            if (!query.TryGetValue(InspectedElementParameterName, out var values))
            {
                return selection;
            }

            var candidates = values.ToArray();
            var candidate = candidates.FirstOrDefault();
            IElement inspected = null;
            if (!string.IsNullOrWhiteSpace(candidate)
                && candidates.All(value => string.Equals(value, candidate, StringComparison.Ordinal)))
            {
                try
                {
                    inspected = await this.elementIdResolver.ResolveAsync(candidate, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    this.logger.LogWarning(exception, "Inspected-element URL context could not be resolved.");
                }
            }

            if (string.Equals(GetStableElementId(inspected), GetStableElementId(selection.SelectedElement), StringComparison.Ordinal))
            {
                inspected = null;
            }

            var canonical = selection.CanonicalUri;
            if (inspected is null || candidates.Length > 1
                || !string.Equals(candidate, GetStableElementId(inspected), StringComparison.Ordinal))
            {
                canonical = this.GetUriWithInspection(canonical ?? parsedLocation.Location, GetStableElementId(inspected));
            }

            return new WorkspaceUrlContextRestoration(selection.SelectedElement, canonical,
                selection.ShouldFocusSelectedElement, inspected);
        }

        /// <summary>Changes only the reference parameter, preserving all other URI parts.</summary>
        /// <param name="uri">The source absolute URI.</param>
        /// <param name="elementId">The stable inspected identifier, or <see langword="null" /> to remove inspection.</param>
        /// <returns>The canonical URI with redundant equal inspection omitted.</returns>
        private string GetUriWithInspection(string uri, string elementId)
        {
            var query = QueryHelpers.ParseQuery(this.navigationManager.ToAbsoluteUri(uri).Query);
            if (query.TryGetValue(SelectedElementParameterName, out var selected)
                && selected.Count == 1 && string.Equals(elementId, selected[0], StringComparison.Ordinal))
            {
                elementId = null;
            }

            return this.navigationManager.GetUriWithQueryParameters(uri, new Dictionary<string, object>
            {
                [InspectedElementParameterName] = elementId
            });
        }

        /// <summary>
        /// Adds, replaces, or removes only the selected-element parameter in one URI.
        /// </summary>
        /// <param name="uri">The source URI whose path, unrelated query, and fragment are preserved.</param>
        /// <param name="elementId">The stable identifier to write, or <see langword="null" /> to remove it.</param>
        /// <returns>The URI with selected-element context reconciled.</returns>
        private string GetUriWithSelectedElement(string uri, string elementId)
        {
            IReadOnlyDictionary<string, object> parameter = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [SelectedElementParameterName] = elementId
            };

            return this.navigationManager.GetUriWithQueryParameters(uri, parameter);
        }

        /// <summary>
        /// Determines whether a derived location differs from the current authoritative browser URI.
        /// </summary>
        /// <param name="uri">The derived absolute URI.</param>
        /// <returns><see langword="true" /> when navigation is required.</returns>
        private bool IsDifferentFromCurrentUri(string uri)
        {
            return !string.Equals(uri, this.navigationManager.Uri, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets a usable exact element identifier from shared selection.
        /// </summary>
        /// <param name="element">The selected model element.</param>
        /// <returns>The exact identifier, or <see langword="null" /> when unavailable.</returns>
        private static string GetStableElementId(SysML2.NET.Core.POCO.Root.Elements.IElement element)
        {
            return string.IsNullOrWhiteSpace(element?.ElementId) ? null : element.ElementId;
        }

        /// <summary>
        /// Represents selected-element input parsed from one exact browser location.
        /// </summary>
        /// <param name="Location">The source browser URI.</param>
        /// <param name="ElementId">The unambiguous exact candidate, or <see langword="null" />.</param>
        /// <param name="RequiresCanonicalization">Whether the selected-element parameter must be rewritten.</param>
        /// <param name="ShouldFocusSelectedElement">Whether the browser transition requests local focus.</param>
        /// <param name="EntryState">The originating browser history metadata.</param>
        private sealed record ParsedLocation(
            string Location,
            string ElementId,
            bool RequiresCanonicalization,
            bool ShouldFocusSelectedElement,
            string EntryState = null);

        /// <summary>
        /// Carries immutable location state between adjacent framework navigation events.
        /// </summary>
        /// <param name="Location">The current absolute framework location.</param>
        /// <param name="AbsolutePath">The current escaped absolute path.</param>
        /// <param name="RouteChanged">Whether the route differs from the prior location.</param>
        /// <param name="EntryState">The originating browser history metadata.</param>
        private sealed record LocationTransition(
            string Location,
            string AbsolutePath,
            bool RouteChanged,
            string EntryState = null);
    }
}
