// ------------------------------------------------------------------------------------------------
// <copyright file="KeyboardNavigationBunitContext.cs" company="Starion Group S.A.">
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
    /// Provides a bUnit test context with keyboard-navigation JavaScript interop setup.
    /// </summary>
    public abstract class KeyboardNavigationBunitContext : BunitContext
    {
        /// <summary>
        /// Sets up JavaScript interop used by keyboard navigation.
        /// </summary>
        [SetUp]
        public void SetUpKeyboardNavigation()
        {
            KeyboardNavigationTestHelper.SetupModule(this);
        }
    }
}
