// ------------------------------------------------------------------------------------------------
// <copyright file="SysmlModelElementKindExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.Common
{
    using Mycelium.Bloom.Model.Enum;

    /// <summary>
    /// Provides UI presentation helpers for <see cref="SysmlModelElementKind" />.
    /// </summary>
    public static class SysmlModelElementKindExtensions
    {
        /// <summary>
        /// The fallback design token used when no more specific element color is available.
        /// </summary>
        private const string FallbackColorToken = "var(--mb-color-neutral-600)";

        /// <summary>
        /// Converts the element kind into its UI color design token.
        /// </summary>
        /// <param name="elementKind">The SysML model element kind.</param>
        /// <returns>The UI color design token.</returns>
        public static string ToColorToken(this SysmlModelElementKind elementKind)
        {
            return elementKind switch
            {
                SysmlModelElementKind.Namespace => "var(--mb-color-sysml-structure-header)",
                SysmlModelElementKind.Import => "var(--mb-color-sysml-allocations-header)",
                SysmlModelElementKind.Membership => "var(--mb-color-sysml-metadata-header)",
                SysmlModelElementKind.Relationship => "var(--mb-color-sysml-connections-header)",
                SysmlModelElementKind.Definition => "var(--mb-color-sysml-attributes-header)",
                SysmlModelElementKind.Usage => "var(--mb-color-sysml-behavior-header)",
                SysmlModelElementKind.Feature => "var(--mb-color-sysml-requirements-header)",
                SysmlModelElementKind.Type => "var(--mb-color-sysml-verification-header)",
                SysmlModelElementKind.Annotation => "var(--mb-color-info-500)",
                SysmlModelElementKind.Unknown => FallbackColorToken,
                _ => FallbackColorToken
            };
        }
    }
}
