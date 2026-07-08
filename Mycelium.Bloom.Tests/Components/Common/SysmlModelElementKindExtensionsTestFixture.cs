// ------------------------------------------------------------------------------------------------
// <copyright file="SysmlModelElementKindExtensionsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Common
{
    using System;
    using System.Collections.Generic;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Tests the <see cref="SysmlModelElementKindExtensions" /> class.
    /// </summary>
    [TestFixture]
    public sealed class SysmlModelElementKindExtensionsTestFixture
    {
        /// <summary>
        /// The expected color tokens for each SysML model element kind.
        /// </summary>
        private static readonly IReadOnlyDictionary<SysmlModelElementKind, string> ExpectedColorTokens =
            new Dictionary<SysmlModelElementKind, string>
            {
                { SysmlModelElementKind.Unknown, "var(--mb-color-neutral-600)" },
                { SysmlModelElementKind.Namespace, "var(--mb-color-sysml-structure-header)" },
                { SysmlModelElementKind.Import, "var(--mb-color-sysml-allocations-header)" },
                { SysmlModelElementKind.Membership, "var(--mb-color-sysml-metadata-header)" },
                { SysmlModelElementKind.Relationship, "var(--mb-color-sysml-connections-header)" },
                { SysmlModelElementKind.Definition, "var(--mb-color-sysml-attributes-header)" },
                { SysmlModelElementKind.Usage, "var(--mb-color-sysml-behavior-header)" },
                { SysmlModelElementKind.Feature, "var(--mb-color-sysml-requirements-header)" },
                { SysmlModelElementKind.Type, "var(--mb-color-sysml-verification-header)" },
                { SysmlModelElementKind.Annotation, "var(--mb-color-info-500)" }
            };

        /// <summary>
        /// Verifies that all SysML model element kinds have an expected color token.
        /// </summary>
        [Test]
        public void VerifyToColorTokenDefinesExpectedColorTokensForAllElementKinds()
        {
            var elementKinds = Enum.GetValues<SysmlModelElementKind>();

            Assert.That(ExpectedColorTokens.Keys, Is.EquivalentTo(elementKinds));
        }

        /// <summary>
        /// Verifies that each SysML model element kind returns its expected color token.
        /// </summary>
        [Test]
        public void VerifyToColorTokenReturnsExpectedColorTokens()
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var expectedColorToken in ExpectedColorTokens)
                {
                    Assert.That(
                        expectedColorToken.Key.ToColorToken(),
                        Is.EqualTo(expectedColorToken.Value),
                        $"{expectedColorToken.Key} must return the expected UI color token.");
                }
            }
        }
    }
}
