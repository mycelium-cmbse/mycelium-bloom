// ------------------------------------------------------------------------------------------------
// <copyright file="KeyboardNavigationTestHelper.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using Bunit;

    /// <summary>
    /// Provides JavaScript interop setup helpers for keyboard-navigation component tests.
    /// </summary>
    public static class KeyboardNavigationTestHelper
    {
        /// <summary>
        /// Sets up the keyboard navigation JavaScript module.
        /// </summary>
        /// <param name="context">The bUnit test context.</param>
        public static void SetupModule(BunitContext context)
        {
            var module = context.JSInterop.SetupModule("/js/keyboardNavigation.js");

            module.SetupVoid("registerNavigationKeyPrevention", _ => true).SetVoidResult();
            module.SetupVoid("disposeNavigationKeyPrevention", _ => true).SetVoidResult();
        }
    }
}
