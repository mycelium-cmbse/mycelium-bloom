// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationServiceCollectionExtensions.cs" company="Starion Group S.A.">
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
    using Mycelium.Bloom.Core.Organization;
    using Mycelium.Bloom.Core.Organization.Placeholder;
    using Mycelium.Bloom.ViewModel.Organization;

    /// <summary>
    /// Provides dependency-injection registration extensions for the organization screens.
    /// </summary>
    public static class OrganizationServiceCollectionExtensions
    {
        /// <summary>
        /// Defines service registration extensions for an <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Binds the organization options and registers the organization context service.
            /// Replacing the placeholder service changes this registration only.
            /// </summary>
            /// <param name="configuration">The application configuration containing the organization section.</param>
            /// <returns>The original service collection for continued registration chaining.</returns>
            /// <exception cref="ArgumentNullException">
            /// Thrown when <paramref name="services" /> or <paramref name="configuration" /> is
            /// <see langword="null" />.
            /// </exception>
            public IServiceCollection AddOrganizationServices(IConfiguration configuration)
            {
                ArgumentNullException.ThrowIfNull(services);
                ArgumentNullException.ThrowIfNull(configuration);

                services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));

                services.AddScoped<Func<IOrganizationContextViewModel>>(serviceProvider =>
                    () => ActivatorUtilities.CreateInstance<OrganizationContextViewModel>(serviceProvider));

                services.AddScoped<Func<IOrganizationContextViewModel, IOrganizationHeaderViewModel>>(serviceProvider =>
                    context => ActivatorUtilities.CreateInstance<OrganizationHeaderViewModel>(serviceProvider, context));

                services.AddScoped<IOrganizationContextService, PlaceholderOrganizationContextService>();

                return services;
            }
        }
    }
}
