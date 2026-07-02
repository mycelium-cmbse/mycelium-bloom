// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserElementKind.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.ProjectBrowser
{
    /// <summary>
    /// Identifies the broad kind of SysML element displayed by the project browser.
    /// </summary>
    public enum ProjectBrowserElementKind
    {
        /// <summary>
        /// The element kind could not be inferred from the SysML runtime type.
        /// </summary>
        Unknown,

        /// <summary>
        /// The element is a namespace.
        /// </summary>
        Namespace,

        /// <summary>
        /// The element is an import.
        /// </summary>
        Import,

        /// <summary>
        /// The element is a membership.
        /// </summary>
        Membership,

        /// <summary>
        /// The element is a relationship.
        /// </summary>
        Relationship,

        /// <summary>
        /// The element is a definition.
        /// </summary>
        Definition,

        /// <summary>
        /// The element is a usage.
        /// </summary>
        Usage,

        /// <summary>
        /// The element is a feature.
        /// </summary>
        Feature,

        /// <summary>
        /// The element is a type.
        /// </summary>
        Type,

        /// <summary>
        /// The element is an annotation.
        /// </summary>
        Annotation
    }
}
