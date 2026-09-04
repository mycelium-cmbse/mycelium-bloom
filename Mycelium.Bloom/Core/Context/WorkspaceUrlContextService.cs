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
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Routing;
    using Microsoft.AspNetCore.WebUtilities;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model;
    using ReactiveUI;

    /// <summary>
    /// Coordinates selected-element context around Blazor's authoritative navigation service.
    /// </summary>
    public sealed class WorkspaceUrlContextService : IWorkspaceUrlContextService
    {
        /// <summary>
        /// The sole query parameter used for stable selected-element identity.
        /// </summary>
        public const string SelectedElementParameterName = "selectedElement";

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
        public WorkspaceUrlContextService(
            NavigationManager navigationManager,
            IElementIdResolver elementIdResolver,
            IElementSelectionService elementSelectionService,
            ILogger<WorkspaceUrlContextService> logger)
        {
            ArgumentNullException.ThrowIfNull(navigationManager);
            ArgumentNullException.ThrowIfNull(elementIdResolver);
            ArgumentNullException.ThrowIfNull(elementSelectionService);
            ArgumentNullException.ThrowIfNull(logger);

            this.navigationManager = navigationManager;
            this.elementIdResolver = elementIdResolver;
            this.elementSelectionService = elementSelectionService;
            this.logger = logger;

            var restorations = Observable
                .FromEventPattern<LocationChangedEventArgs>(
                    handler => this.navigationManager.LocationChanged += handler,
                    handler => this.navigationManager.LocationChanged -= handler)
                .Select(_ => this.navigationManager.Uri)
                .StartWith(this.navigationManager.Uri)
                .DistinctUntilChanged(StringComparer.Ordinal)
                .Select(this.CreateLocationTransition)
                .Scan((previous, current) => current with
                {
                    RouteChanged = !string.Equals(
                        previous.AbsolutePath,
                        current.AbsolutePath,
                        StringComparison.OrdinalIgnoreCase)
                })
                .Select(this.ParseLocation)
                .Select(parsedLocation => Observable.FromAsync(cancellationToken =>
                    this.ResolveRestorationAsync(parsedLocation, cancellationToken)))
                .Switch()
                .Replay(1);

            this.Restorations = restorations;

            var selectionNavigationRequests = this.elementSelectionService
                .WhenAnyValue(selection => selection.SelectedElement)
                .Select(GetStableElementId)
                .DistinctUntilChanged(StringComparer.Ordinal)
                .Skip(1)
                .SkipUntil(restorations.Take(1))
                .Select(elementId => this.GetUriWithSelectedElement(this.navigationManager.Uri, elementId))
                .Where(this.IsDifferentFromCurrentUri);

            var canonicalNavigationRequests = restorations
                .Select(restoration => restoration.CanonicalUri)
                .Where(uri => uri is not null)
                .Where(this.IsDifferentFromCurrentUri);

            this.NavigationRequests = canonicalNavigationRequests
                .Merge(selectionNavigationRequests)
                .DistinctUntilChanged(StringComparer.Ordinal);
            this.restorationConnection = restorations.Connect();
        }

        /// <inheritdoc />
        public IObservable<WorkspaceUrlContextRestoration> Restorations { get; }

        /// <inheritdoc />
        public IObservable<string> NavigationRequests { get; }

        /// <inheritdoc />
        public string GetDestinationUri(string canonicalHref)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(canonicalHref);

            var elementId = GetStableElementId(this.elementSelectionService.SelectedElement);

            return elementId is null
                ? canonicalHref
                : QueryHelpers.AddQueryString(canonicalHref, SelectedElementParameterName, elementId);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.restorationConnection.Dispose();
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
        private async Task<WorkspaceUrlContextRestoration> ResolveRestorationAsync(
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
        private sealed record ParsedLocation(
            string Location,
            string ElementId,
            bool RequiresCanonicalization,
            bool ShouldFocusSelectedElement);

        /// <summary>
        /// Carries immutable location state between adjacent framework navigation events.
        /// </summary>
        /// <param name="Location">The current absolute framework location.</param>
        /// <param name="AbsolutePath">The current escaped absolute path.</param>
        /// <param name="RouteChanged">Whether the route differs from the prior location.</param>
        private sealed record LocationTransition(
            string Location,
            string AbsolutePath,
            bool RouteChanged);
    }
}
