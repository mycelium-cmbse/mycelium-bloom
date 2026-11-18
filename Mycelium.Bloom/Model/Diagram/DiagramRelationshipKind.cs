// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramRelationshipKind.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    /// <summary>Names the built-in relationship notations without closing extension to other keys.</summary>
    public static class DiagramRelationshipKind
    {
        /// <summary>The composite membership notation key.</summary>
        public const string CompositeMembership = "CompositeMembership";

        /// <summary>The noncomposite membership notation key.</summary>
        public const string NoncompositeMembership = "NoncompositeMembership";

        /// <summary>The subclassification notation key.</summary>
        public const string Subclassification = "Subclassification";

        /// <summary>The definition notation key.</summary>
        public const string Definition = "Definition";

        /// <summary>The subsetting notation key.</summary>
        public const string Subsetting = "Subsetting";

        /// <summary>The redefinition notation key.</summary>
        public const string Redefinition = "Redefinition";

        /// <summary>The connection notation key.</summary>
        public const string Connection = "Connection";

        /// <summary>The binding notation key.</summary>
        public const string Binding = "Binding";

        /// <summary>The dependency notation key.</summary>
        public const string Dependency = "Dependency";

        /// <summary>The annotation notation key.</summary>
        public const string Annotation = "Annotation";
    }
}
