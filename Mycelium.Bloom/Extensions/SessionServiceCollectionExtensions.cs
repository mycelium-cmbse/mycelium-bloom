// ------------------------------------------------------------------------------------------------
// <copyright file="SessionServiceCollectionExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Core.Session;
    using Mycelium.Bloom.ViewModel.Home;
    using Mycelium.Bloom.ViewModel.Session;
    using Mycelium.Bloom.ViewModel.Workspace;

    using SysML2.NET.Serializer.Json;

    using SySML2.NET.REST;

    /// <summary>
    /// Provides dependency-injection registration extensions for the connection to a model server.
    /// </summary>
    public static class SessionServiceCollectionExtensions
    {
        /// <summary>
        /// Defines service registration extensions for an <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Registers the session of the SysML2.NET REST client and the service that holds it, one per circuit so that every
            /// page of the user works against the same server, and the factories of the start page, Home page and workspace
            /// breadcrumb ViewModels, each of which builds one ViewModel per instance of its view.
            /// </summary>
            /// <returns>The original service collection for continued registration chaining.</returns>
            /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is <see langword="null" />.</exception>
            public IServiceCollection AddSessionServices()
            {
                ArgumentNullException.ThrowIfNull(services);

                services.AddHttpClient();
                services.AddScoped<ISession>(serviceProvider => new Session(serviceProvider.GetRequiredService<IHttpClientFactory>(), new DeSerializer(), new Serializer(), serviceProvider.GetRequiredService<ILoggerFactory>()));
                services.AddScoped<IServerSessionService, ServerSessionService>();
                services.AddScoped<Func<IConnectViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<ConnectViewModel>(serviceProvider));
                services.AddScoped<Func<IHomeViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<HomeViewModel>(serviceProvider));
                services.AddScoped<Func<IWorkspaceBreadcrumbViewModel>>(serviceProvider => () => ActivatorUtilities.CreateInstance<WorkspaceBreadcrumbViewModel>(serviceProvider));

                return services;
            }
        }
    }
}
