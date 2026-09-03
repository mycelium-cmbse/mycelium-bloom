// ------------------------------------------------------------------------------------------------
// <copyright file="BlueprintTestSetup.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using BlazorBlueprint.Components;
    using BlazorBlueprint.Primitives.Services;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Configures the services and browser-bound interop required by Blazor Blueprint in bUnit.
    /// </summary>
    internal static class BlueprintTestSetup
    {
        /// <summary>
        /// Adds Blueprint Components services and allows its internal browser modules to be represented by bUnit.
        /// </summary>
        /// <param name="context">The active bUnit context.</param>
        internal static void Configure(BunitContext context)
        {
            context.Services.AddLogging();
            context.Services.AddBlazorBlueprintComponents(
                configureTheme: options =>
                {
                    options.DetectSystemPreference = false;
                    options.DefaultRadius = 0.375d;
                });
            context.JSInterop.Mode = JSRuntimeMode.Loose;
        }

        /// <summary>
        /// Configures Blueprint and renders the two-category portal host used by overlay components.
        /// </summary>
        /// <param name="context">The active bUnit context.</param>
        /// <returns>The rendered portal host.</returns>
        internal static IRenderedComponent<BbPortalHost> ConfigureWithPortalHost(BunitContext context)
        {
            Configure(context);
            return context.Render<BbPortalHost>();
        }
    }
}
