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
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.ViewModel.NavigationRail;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.WorkspaceEditor;

    /// <summary>
    /// Provides dependency-injection registration extensions for Mycelium Bloom application services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Mycelium Bloom application services to the provided service collection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The configured service collection.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="services" /> is <see langword="null" />.
        /// </exception>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<IModelLoaderService, ModelLoaderService>();
            services.AddScoped<ContextAwareService>();
            services.AddScoped<IContextAwareService>(
                serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());
            services.AddScoped<IElementSelectionService>(
                serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());
            services.AddTransient<IProjectBrowserViewModel, ProjectBrowserViewModel>();
            services.AddSingleton<INavigationRailItemProvider, NavigationRailItemProvider>();
            services.AddTransient<INavigationRailViewModel, NavigationRailViewModel>();
            services.AddTransient<IWorkspaceEditorViewModel, WorkspaceEditorViewModel>();

            return services;
        }
    }
}
