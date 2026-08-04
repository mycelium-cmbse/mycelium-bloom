// ------------------------------------------------------------------------------------------------
// <copyright file="ReactiveUiTestSetup.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests
{
    using ReactiveUI.Builder;

    /// <summary>
    /// Configures ReactiveUI for the test assembly.
    /// </summary>
    [SetUpFixture]
    public sealed class ReactiveUiTestSetup
    {
        /// <summary>
        /// Configures the same ReactiveUI Blazor platform used by the application.
        /// </summary>
        [OneTimeSetUp]
        public void ConfigureReactiveUi()
        {
            RxAppBuilder.CreateReactiveUIBuilder()
                .WithBlazor()
                .BuildApp();
        }
    }
}
