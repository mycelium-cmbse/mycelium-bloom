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
                "house",
                "grid-2x2",
                "file-text",
                "ellipsis",
                "eye",
                "copy",
                "user",
                "settings-2",
                "log-out",
                "trash-2",
                "menu",
                "x",
                "share-2",
                "undo-2",
                "mouse-pointer-2",
                "sticky-note",
                "pencil",
                "link-2",
                "move",
                "focus",
                "minus",
                "plus",
                "scan-line",
                "maximize",
                "info",
                "check"
            ];

            var actualNames = System.Enum
                .GetValues<SymbolIconName>()
                .Select(symbol => symbol.ToLucideName())
                .ToArray();

            Assert.That(actualNames, Is.EqualTo(expectedNames));
        }
    }
}
