// ------------------------------------------------------------------------------------------------
// <copyright file="JavaScriptInteropTestSetup.cs" company="Starion Group S.A.">
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
    /// Configures shared JavaScript modules used by component regression fixtures.
    /// </summary>
    internal static class JavaScriptInteropTestSetup
    {
        /// <summary>
        /// Configures the shared outside-click module.
        /// </summary>
        /// <param name="jsInterop">The bUnit JavaScript interop instance.</param>
        /// <returns>The configured module interop for optional invocation assertions.</returns>
        internal static BunitJSModuleInterop SetUpOutsideClick(BunitJSInterop jsInterop)
        {
            var module = jsInterop.SetupModule("./js/outside-click.js");
            var registerHandler = module.SetupVoid("registerOutsideClick", invocation => true);
            var disposeHandler = module.SetupVoid("disposeOutsideClick", invocation => true);

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            return module;
        }

        /// <summary>
        /// Configures the shared element-scoped keyboard-default module.
        /// </summary>
        /// <param name="jsInterop">The bUnit JavaScript interop instance.</param>
        /// <returns>The configured module interop for optional invocation assertions.</returns>
        internal static BunitJSModuleInterop SetUpKeyboardDefaults(BunitJSInterop jsInterop)
        {
            var module = jsInterop.SetupModule("./js/keyboard-defaults.js");
            var registerHandler = module.SetupVoid("registerKeyPrevention", invocation => true);
            var disposeHandler = module.SetupVoid("disposeKeyPrevention", invocation => true);

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            return module;
        }
    }
}
