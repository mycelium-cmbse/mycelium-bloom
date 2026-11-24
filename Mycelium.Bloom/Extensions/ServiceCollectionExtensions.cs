// ------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using System.Net;

    using Blazor.Diagrams;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram;
    using Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools;
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;

    /// <summary>
    /// Provides dependency-injection registration extensions for Mycelium Bloom application services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Defines service registration extensions for an <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Configures forwarded headers from the optional trusted reverse proxy and validates its address at startup.
            /// </summary>
            /// <param name="configuration">The application configuration containing the reverse proxy address.</param>
            /// <returns>The original service collection for continued registration chaining.</returns>
            public IServiceCollection AddReverseProxyOptions(IConfiguration configuration)
            {
                ArgumentNullException.ThrowIfNull(services);
                ArgumentNullException.ThrowIfNull(configuration);

                var knownProxyValue = configuration["ReverseProxy:KnownProxy"];
                var isKnownProxyValid = IPAddress.TryParse(knownProxyValue, out var knownProxy);

                services.AddOptions<ForwardedHeadersOptions>()
                    .Configure(options =>
                    {
                        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                        options.RequireHeaderSymmetry = true;

                        if (isKnownProxyValid)
                        {
                            options.KnownProxies.Add(knownProxy);
                        }
                    })
                    .Validate(
                        _ => string.IsNullOrWhiteSpace(knownProxyValue) || isKnownProxyValid,
                        "ReverseProxy:KnownProxy must be a valid IP address.")
                    .ValidateOnStart();

                return services;
            }

            /// <summary>
            /// Adds, binds, and validates the Workspace Editor options required during application startup.
            /// </summary>
            /// <param name="configuration">
            /// The application configuration containing the Workspace Editor section.
            /// </param>
            /// <returns>The original service collection for continued registration chaining.</returns>
            /// <exception cref="ArgumentNullException">
            /// Thrown when <paramref name="services" /> or <paramref name="configuration" /> is
            /// <see langword="null" />.
            /// </exception>
            public IServiceCollection AddWorkspaceEditorOptions(IConfiguration configuration)
            {
                ArgumentNullException.ThrowIfNull(services);
                ArgumentNullException.ThrowIfNull(configuration);

                services.AddOptions<WorkspaceEditorOptions>()
                    .Bind(configuration.GetSection(WorkspaceEditorOptions.SectionName))
                    .Validate(
                        options => options.MaximumGroupCount >= 1,
                        $"{WorkspaceEditorOptions.SectionName}:{nameof(WorkspaceEditorOptions.MaximumGroupCount)} must be at least 1.")
                    .ValidateOnStart();

                return services;
            }

            /// <summary>
            /// Adds the Mycelium Bloom application services to the provided service collection.
            /// </summary>
            /// <returns>The configured service collection.</returns>
            /// <exception cref="ArgumentNullException">
            /// Thrown when <paramref name="services" /> is <see langword="null" />.
            /// </exception>
            public IServiceCollection AddApplicationServices()
            {
                ArgumentNullException.ThrowIfNull(services);

                services.AddDiagramServices();

                services.AddScoped<IAssembler, Assembler>();
                services.AddScoped<IDeSerializer, DeSerializer>();
                services.AddScoped<IModelLoaderService, ModelLoaderService>();
                services.AddScoped<IElementIdResolver, ElementIdResolver>();
                services.AddScoped<ContextAwareService>();
                services.AddScoped<IContextAwareService>(
                    serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());
                services.AddScoped<IElementSelectionService>(
                    serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());
                services.AddScoped<Func<IProjectBrowserViewModel>>(serviceProvider =>
                    () => ActivatorUtilities.CreateInstance<ProjectBrowserViewModel>(serviceProvider));
                services.AddScoped<Func<IDetailsPanelViewModel>>(serviceProvider =>
                    () => ActivatorUtilities.CreateInstance<DetailsPanelViewModel>(serviceProvider));
                services.AddSingleton<INavigationRailItemProvider, NavigationRailItemProvider>();
                services.AddScoped<Func<INavigationRailViewModel>>(serviceProvider =>
                    () => ActivatorUtilities.CreateInstance<NavigationRailViewModel>(serviceProvider));
                services.AddScoped<Func<IWorkspaceUrlContextService>>(serviceProvider =>
                    () => ActivatorUtilities.CreateInstance<WorkspaceUrlContextService>(serviceProvider));
                services.AddScoped<Func<IWorkspaceEditorViewModel>>(serviceProvider =>
                    () => ActivatorUtilities.CreateInstance<WorkspaceEditorViewModel>(serviceProvider));

                return services;
            }

            /// <summary>Registers extensible diagram projection, typed rendering and caller-owned ViewModel factories.</summary>
            /// <remarks>Caller edge policies precede built-in policies at equal SDK specificity, regardless of DI registration order.</remarks>
            /// <returns>The supplied service collection.</returns>
            /// <exception cref="ArgumentNullException">The service collection is null.</exception>
            public IServiceCollection AddDiagramServices()
            {
                ArgumentNullException.ThrowIfNull(services);
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagramNodeFactory, PackageDiagramNodeFactory>());
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagramNodeFactory, CommentDiagramNodeFactory>());
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagramNodeFactory, ShellDiagramNodeFactory>());
                services.TryAddSingleton<IDiagramLayout, GridDiagramLayout>();

                var defaultEdgeFactories = DiagramEdgeFactories.CreateDefault();

                foreach (var factory in defaultEdgeFactories)
                {
                    services.AddSingleton(factory);
                }

                services.TryAddScoped<IGeneralViewProjector>(serviceProvider => new GeneralViewProjector(
                    serviceProvider.GetServices<IDiagramNodeFactory>(),
                    serviceProvider.GetServices<IDiagramEdgeFactory>().OrderBy(factory => defaultEdgeFactories.Contains(factory, ReferenceEqualityComparer.Instance)),
                    serviceProvider.GetRequiredService<IDiagramLayout>()));
                services.AddSingleton(new DiagramNodeComponentRegistration(typeof(DiagramShellPresentation), typeof(DiagramShellNode)));
                services.AddSingleton(new DiagramNodeComponentRegistration(typeof(DiagramPackagePresentation), typeof(DiagramPackageNode)));
                services.AddSingleton(new DiagramNodeComponentRegistration(typeof(DiagramCommentPresentation), typeof(DiagramCommentNode)));
                services.TryAddSingleton<DiagramNodeComponentRegistry>();
                services.TryAddSingleton<IDiagramToolCatalogue, DiagramToolCatalogue>();
                services.TryAddSingleton<IDiagramToolRuntimeProvider, DiagramToolRuntimeProvider>();
                services.TryAddSingleton(DiagramToolDefaults.Layout);
                services.AddSingleton(new DiagramToolDefinition(DiagramToolDefaults.Move, "Move", "mouse-pointer-2")
                {
                    AllowsSelection = true,
                    Tooltip = "Select / Move"
                });
                services.AddSingleton(new DiagramToolDefinition(DiagramToolDefaults.Pan, "Pan", "hand")
                {
                    CapturesNestedInput = true,
                    Tooltip = "Pan (hold Space for temporary navigation)"
                });
                services.AddSingleton(new DiagramToolRuntimeRegistration(DiagramToolDefaults.Move, (context, _) => new MoveDiagramTool(context)));
                services.AddSingleton(new DiagramToolRuntimeRegistration(DiagramToolDefaults.Pan, (context, _) => new PanDiagramTool(context)));
                services.TryAddSingleton<Func<IObservable<bool>, IDiagramToolState>>(serviceProvider =>
                {
                    var catalogue = serviceProvider.GetRequiredService<IDiagramToolCatalogue>();
                    var layout = serviceProvider.GetRequiredService<DiagramToolLayout>();
                    return editingPolicy => new DiagramToolState(catalogue, layout, editingPolicy);
                });
                services.TryAddSingleton<Func<BlazorDiagram, IDiagramViewModel, DiagramPointerBehavior>>(serviceProvider =>
                {
                    var runtimes = serviceProvider.GetRequiredService<IDiagramToolRuntimeProvider>();
                    return (diagram, owner) => new DiagramPointerBehavior(diagram, owner, runtimes);
                });
                services.TryAddSingleton<Func<IDiagramViewModel, IDiagramRenderer>>(serviceProvider =>
                {
                    var createPointer = serviceProvider.GetRequiredService<Func<BlazorDiagram, IDiagramViewModel, DiagramPointerBehavior>>();
                    return owner => new DiagramRenderer(owner, createPointer);
                });

                services.TryAddScoped<Func<DiagramProjection, IDiagramViewModel>>(serviceProvider =>
                    projection => ActivatorUtilities.CreateInstance<DiagramViewModel>(serviceProvider, projection));

                services.AddScoped(serviceProvider => new DiagramEditorDescriptor("open-general-view", "General View", "general-view",
                    root => serviceProvider.GetRequiredService<Func<DiagramProjection, IDiagramViewModel>>()(
                        serviceProvider.GetRequiredService<IGeneralViewProjector>().CreateForRoot(root)), typeof(DiagramCanvas)));
                services.AddScoped(serviceProvider => new DiagramEditorDescriptor("open-general-example", "General View example", "general-view-example",
                    _ => serviceProvider.GetRequiredService<Func<DiagramProjection, IDiagramViewModel>>()(
                        GeneralViewExample.Create(serviceProvider.GetRequiredService<IGeneralViewProjector>())), typeof(DiagramCanvas))
                { IsDevelopmentOnly = true });
                services.TryAddScoped<DiagramEditorRegistry>();

                return services;
            }
        }
    }
}
