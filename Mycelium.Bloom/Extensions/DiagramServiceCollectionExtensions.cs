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
    using System.Security.Cryptography;
    using System.Text;

    using Microsoft.Extensions.Options;

    using Mycelium.Bloom.Core.Configuration;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.ViewModel.Diagram;

    /// <summary>Registers the bounded diagram feature without changing circuit service identity.</summary>
    public static class DiagramServiceCollectionExtensions
    {
        /// <summary>Registers shared demonstration loading and caller-owned diagram ViewModels.</summary>
        /// <param name="services">The application service collection.</param>
        /// <returns>The same collection for composition.</returns>
        public static IServiceCollection AddDiagramPrototype(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddLogging();
            services.AddOptions<DiagramBenchmarkOptions>();
            services.AddOptions<DiagramViewportOptions>();
            services.AddOptions<DiagramDataOptions>();
            services.AddScoped<InterconnectionPrototype>();
            services.AddScoped<Mycelium.Bloom.Core.Context.WorkspaceModelSession>();
            services.AddScoped<Mycelium.Bloom.Core.Context.WorkspaceConnectionHandler>();
            services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>(provider =>
                provider.GetRequiredService<Mycelium.Bloom.Core.Context.WorkspaceConnectionHandler>());
            services.AddScoped<Func<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>>(provider =>
                () => ActivatorUtilities.CreateInstance<Mycelium.Bloom.ViewModel.ModelOpeningViewModel>(provider));
            services.AddScoped<Func<DiagramViewModel>>(provider => () => ActivatorUtilities.CreateInstance<DiagramViewModel>(provider));
            return services;
        }

        /// <summary>Configures isolated benchmark access and rejects an incomplete diagnostic deployment.</summary>
        /// <param name="services">The application services.</param>
        /// <param name="configuration">The explicit diagnostic settings.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddDiagramBenchmarkOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DiagramDataOptions>()
                .Bind(configuration.GetSection("DiagramData"))
                .Validate(options => options.CapacityBytes > 0 && options.ExpirationSeconds > 0
                    && options.BatchSize is >= 1 and <= 32 && options.ConcurrentRequests is >= 1 and <= 4, "Invalid diagram read bounds.")
                .ValidateOnStart();
            services.AddOptions<DiagramViewportOptions>()
                .Bind(configuration.GetSection("DiagramViewport"))
                .Validate(options => double.IsFinite(options.MinimumZoom) && options.MinimumZoom is >= 0.1 and <= 4
                    && double.IsFinite(options.Margin) && options.Margin is >= 0 and <= 1000
                    && double.IsFinite(options.PanPadding) && options.PanPadding is >= 0 and <= 10000
                    && options.DensityWarningCount > 0, "Diagram viewport settings are outside their supported bounds.")
                .ValidateOnStart();
            services.AddOptions<DiagramBenchmarkOptions>()
                .Bind(configuration.GetSection(DiagramBenchmarkOptions.SectionName))
                .Validate(options => options.OnDemandTiles is >= 0 and <= 1428 && (options.OnDemandTiles == 0 || options.LargeSceneEnabled),
                    "On-demand fixtures require isolated large-scene mode.")
                .Validate(options => !options.PreloadOnDemandFixture || options.OnDemandTiles > 0,
                    "The eager diagnostic control requires the on-demand fixture.")
                .Validate(options => !options.LargeSceneEnabled || options.Enabled,
                    "Large diagram scenes require isolated benchmark mode.")
                .Validate(options => !options.AsyncRendering || options.LargeSceneEnabled,
                    "Asynchronous rendering experiments require isolated large-scene mode.")
                .Validate(options => !options.SimpleRouting || options.LargeSceneEnabled,
                    "Routing controls require isolated large-scene mode.")
                .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.AccessKey) && options.AccessKey.Length >= 32,
                    "Enabled diagram benchmarks require a per-run access key containing at least 32 characters.")
                .ValidateOnStart();
            return services;
        }

        /// <summary>Requires a per-run capability for every diagnostic application request except readiness probes.</summary>
        /// <param name="app">The application pipeline.</param>
        /// <returns>The same application pipeline.</returns>
        public static IApplicationBuilder UseDiagramBenchmarkAccess(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<DiagramBenchmarkOptions>>().Value;
            if (!options.Enabled)
            {
                return app;
            }

            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(options.AccessKey));
            return app.Use(async (context, next) =>
            {
                if (context.Request.Path != "/healthz" && context.Request.Path != "/ready")
                {
                    var provided = context.Request.Cookies[DiagramBenchmarkOptions.CookieName] ?? string.Empty;
                    if (!CryptographicOperations.FixedTimeEquals(expected, SHA256.HashData(Encoding.UTF8.GetBytes(provided))))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                }

                await next(context);
            });
        }
    }
}
