// ------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom
{
    using BlazorBlueprint.Components;

    using Mycelium.Bloom.Components;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using OpenTelemetry.Resources;

    using ReactiveUI.Builder;

    /// <summary>
    /// Provides the entry point for the Mycelium Bloom web application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Configures and starts the Blazor web application.
        /// </summary>
        /// <param name="args">
        /// The command-line arguments provided when starting the application.
        /// </param>
        public static void Main(string[] args)
        {
            const string serviceName = "Mycelium.Bloom";

            RxAppBuilder.CreateReactiveUIBuilder()
                .WithBlazor()
                .BuildApp();

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithLogging();

            builder.Services.AddMemoryCache();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddBlazorBlueprintComponents();

            // Add application services.
            builder.Services.AddScoped<IModelLoaderService, ModelLoaderService>();
            builder.Services.AddScoped<IElementSelectionService, ElementSelectionService>();
            builder.Services.AddTransient<IProjectBrowserViewModel, ProjectBrowserViewModel>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", true);
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
