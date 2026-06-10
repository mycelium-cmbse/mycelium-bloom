namespace Mycelium.Bloom
{
    using Mycelium.Bloom.Components;

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
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
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