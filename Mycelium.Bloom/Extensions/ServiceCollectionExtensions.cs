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
    using Mycelium.Bloom.Core.MockedData;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;
    using Mycelium.Bloom.ViewModel.Home;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.Session;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;
    using Mycelium.SDK.REST;

    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;

    using SySML2.NET.REST;

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
                    AllowsResize = true,
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

            /// <summary>
            /// Binds the organization options and registers the mocked data of the circuit, the organization services, which
            /// answer with it, the ViewModels of the organization context and of its header, one of each per circuit (the layout, the header
            /// and the page content must share the same context, which a transient registration would split in three), and
            /// the factories of the projects and members page ViewModels, which build one ViewModel per page instance, and of the
            /// pending invitations block, one per members page, and the shared time provider. Replacing a placeholder service
            /// changes this registration only.
            /// </summary>
            /// <param name="configuration">The application configuration containing the organization section.</param>
            /// <returns>The original service collection for continued registration chaining.</returns>
            /// <exception cref="ArgumentNullException">
            /// Thrown when <paramref name="services" /> or <paramref name="configuration" /> is <see langword="null" />.
            /// </exception>
            public IServiceCollection AddOrganizationServices(IConfiguration configuration)
            {
                ArgumentNullException.ThrowIfNull(services);
                ArgumentNullException.ThrowIfNull(configuration);

                services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
                services.AddScoped<IMockedDataService, MockedDataService>();
                services.AddScoped<IOrganizationContextService, PlaceholderOrganizationContextService>();
                services.AddScoped<IOrganizationContextViewModel, OrganizationContextViewModel>();
                services.AddScoped<IOrganizationHeaderViewModel, OrganizationHeaderViewModel>();
                services.AddScoped<IOrganizationProjectService, PlaceholderOrganizationProjectService>();
                services.AddScoped<Func<IProjectsViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<ProjectsViewModel>(serviceProvider));
                services.AddScoped<IOrganizationMemberService, PlaceholderOrganizationMemberService>();
                services.AddScoped<Func<IMembersViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<MembersViewModel>(serviceProvider));
                services.AddScoped<IOrganizationInvitationService, PlaceholderOrganizationInvitationService>();
                services.AddScoped<Func<IPendingInvitationsViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<PendingInvitationsViewModel>(serviceProvider));
                services.TryAddSingleton(TimeProvider.System);

                return services;
            }

            /// <summary>
            /// Registers the session of the SysML2.NET REST client and the session service of the Mycelium SDK that holds it, one
            /// per circuit so that every page of the user works against the same server, and the factories of the start page and
            /// Home page ViewModels, each of which builds one ViewModel per page instance. The session assembles the server's model
            /// in an assembler of its own, not the circuit's <see cref="IAssembler" />, because closing the session drops what it
            /// assembled and must not drop the locally loaded libraries. The limits of the session service keep the defaults of
            /// <see cref="SessionServiceOptions" /> until Bloom needs to set them.
            /// </summary>
            /// <returns>The original service collection for continued registration chaining.</returns>
            /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is <see langword="null" />.</exception>
            public IServiceCollection AddSessionServices()
            {
                ArgumentNullException.ThrowIfNull(services);

                services.AddHttpClient();
                services.AddScoped<ISession>(serviceProvider => new Session(serviceProvider.GetRequiredService<IHttpClientFactory>(), new DeSerializer(), new Serializer(), new Assembler(), serviceProvider.GetRequiredService<ILoggerFactory>()));
                services.AddOptions<SessionServiceOptions>();
                services.AddScoped<ISessionService, SessionService>();
                services.AddScoped<Func<IConnectViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<ConnectViewModel>(serviceProvider));
                services.AddScoped<Func<IHomeViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<HomeViewModel>(serviceProvider));

                return services;
            }
        }
    }
}
