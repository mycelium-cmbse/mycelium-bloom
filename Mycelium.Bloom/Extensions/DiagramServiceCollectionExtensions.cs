// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramServiceCollectionExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Registers the bounded diagram feature without changing circuit service identity.</summary>
    public static class DiagramServiceCollectionExtensions
    {
        /// <summary>Registers canonical model contexts and caller-owned diagram ViewModels.</summary>
        /// <param name="services">The application service collection.</param>
        /// <returns>The same collection for composition.</returns>
        public static IServiceCollection AddInterconnection(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddLogging();
            services.AddOptions<DiagramViewportOptions>();
            services.AddOptions<DiagramDataOptions>();
            services.AddScoped<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            services.AddScoped<Mycelium.Bloom.Core.Context.WorkspaceConnectionHandler>();
            services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>(provider =>
                provider.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceConnectionHandler>());
            services.AddScoped<Func<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>>(provider =>
                () => ActivatorUtilities.CreateInstance<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>(provider));
            services.AddScoped<Func<DiagramViewModel>>(provider => () => ActivatorUtilities.CreateInstance<DiagramViewModel>(provider));
            return services;
        }

        /// <summary>Configures bounded regional projection and viewport settings.</summary>
        /// <param name="services">The application services.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddDiagramOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DiagramDataOptions>()
                .Bind(configuration.GetSection("DiagramData"))
                .Validate(options => options.CapacityBytes > 0 && options.ExpirationSeconds > 0, "Invalid diagram read bounds.")
                .ValidateOnStart();
            services.AddOptions<DiagramViewportOptions>()
                .Bind(configuration.GetSection("DiagramViewport"))
                .Validate(options => double.IsFinite(options.MinimumZoom) && options.MinimumZoom is >= 0.1 and <= 4
                    && double.IsFinite(options.Margin) && options.Margin is >= 0 and <= 1000
                    && double.IsFinite(options.PanPadding) && options.PanPadding is >= 0 and <= 10000, "Diagram viewport settings are outside their supported bounds.")
                .ValidateOnStart();
            return services;
        }

    }
}
