// // ------------------------------------------------------------------------------------------------
// // <copyright file="ModelLoaderTest.razor.cs" company="Starion Group S.A.">
// //
// //   Copyright 2026 Starion Group S.A.
// //   SPDX-License-Identifier: Apache-2.0
// //
// // </copyright>
// // ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Pages
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Core.ModelLoading;

    /// <summary>
    /// Test page used to verify that the SysML model loader can load the Quantities model.
    /// </summary>
    public partial class ModelLoaderTest : ComponentBase
    {
        /// <summary>
        /// Gets or sets the model loading result displayed by the page.
        /// </summary>
        protected string Result { get; set; } = "Loading...";

        /// <summary>
        /// Gets or sets the model loader service.
        /// </summary>
        [Inject]
        public IModelLoaderService ModelLoaderService { get; set; }

        /// <summary>
        /// Initializes the component and attempts to load the Quantities SysML model.
        /// </summary>
        protected override void OnInitialized()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var model = this.ModelLoaderService.LoadQuantitiesModel();
                stopwatch.Stop();

                this.Result =
                    $"Loaded successfully: {model.GetType().FullName}{Environment.NewLine}Elapsed: {stopwatch.ElapsedMilliseconds} ms";
            }
            catch (Exception exception)
            {
                this.Result = exception.ToString();
            }
        }
    }
}
