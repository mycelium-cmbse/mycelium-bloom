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
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Core.Configuration;
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
    /// Defines service registration extensions for an <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    extension(IServiceCollection services)
    {
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

            services.AddScoped<IModelLoaderService, ModelLoaderService>();
            services.AddScoped<ContextAwareService>();
            services.AddScoped<IContextAwareService>(
                serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());
            services.AddScoped<IElementSelectionService>(
                serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());
            services.AddScoped<IProjectBrowserViewModelFactory, ProjectBrowserViewModelFactory>();
            services.AddSingleton<INavigationRailItemProvider, NavigationRailItemProvider>();
            services.AddTransient<INavigationRailViewModel, NavigationRailViewModel>();
            services.AddTransient<IWorkspaceEditorViewModel, WorkspaceEditorViewModel>();

            return services;
        }
    }
}
}
