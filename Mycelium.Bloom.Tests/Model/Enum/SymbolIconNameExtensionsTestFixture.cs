// ------------------------------------------------------------------------------------------------
// <copyright file="SymbolIconNameExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Model.Enum
{
    using System.Linq;

    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Tests the application symbol mapping retained by model-driven consumers.
    /// </summary>
    [TestFixture]
    public sealed class SymbolIconNameExtensionsTestFixture
    {
        /// <summary>
        /// Verifies every application symbol maps to its supported Lucide name.
        /// </summary>
        [Test]
        public void VerifyToLucideName()
        {
            string[] expectedNames =
            [
                "file-text",
                "eye",
                "copy",
                "user",
                "settings-2",
                "log-out",
                "trash-2"
            ];

            var actualNames = System.Enum
                .GetValues<SymbolIconName>()
                .Select(symbol => symbol.ToLucideName())
                .ToArray();

            Assert.That(actualNames, Is.EqualTo(expectedNames));
        }

        /// <summary>
        /// Verifies an unknown application symbol retains the safe fallback icon.
        /// </summary>
        [Test]
        public void VerifyToLucideNameReturnsFallbackForUnknownSymbol()
        {
            var unknownSymbol = (SymbolIconName)int.MaxValue;

            Assert.That(unknownSymbol.ToLucideName(), Is.EqualTo("circle-help"));
        }
    }
}
