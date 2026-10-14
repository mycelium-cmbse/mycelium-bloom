// ------------------------------------------------------------------------------------------------
// <copyright file="ElementPresentation.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel
{
    using System.Globalization;

    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>
    /// Formats supported SDK data without evaluating expressions or changing the model.
    /// </summary>
    public static class ElementPresentation
    {
        /// <summary>
        /// Gets the first available element name for display.
        /// </summary>
        /// <param name="element">The element to describe.</param>
        /// <returns>The available name or concrete SDK type.</returns>
        public static string GetDisplayName(IElement element)
        {
            return new[] { element.DeclaredName, element.name, element.qualifiedName }
                       .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
                   ?? element.GetType().Name;
        }

        /// <summary>
        /// Formats a multiplicity whose bounds are supported nonnegative literals.
        /// </summary>
        /// <param name="element">The element whose multiplicity is displayed.</param>
        /// <returns>The bracketed multiplicity, or null when it cannot be resolved.</returns>
        public static string GetMultiplicity(IElement element)
        {
            if (element is not IType { multiplicity: IMultiplicityRange range })
            {
                return null;
            }

            var upper = GetBound(range.upperBound);

            if (upper is null)
            {
                return null;
            }

            if (range.lowerBound is null)
            {
                return $"[{upper}]";
            }

            if (range.lowerBound is not ILiteralInteger { Value: >= 0 } lower
                || (range.upperBound is ILiteralInteger upperInteger && lower.Value > upperInteger.Value))
            {
                return null;
            }

            return $"[{lower.Value.ToString(CultureInfo.InvariantCulture)}..{upper}]";
        }

        /// <summary>
        /// Formats a supported scalar literal without implying units or computed values.
        /// </summary>
        /// <param name="expression">The expression to display.</param>
        /// <returns>The literal value, or null for an unsupported expression.</returns>
        public static string GetLiteral(IExpression expression)
        {
            return expression switch
            {
                ILiteralInteger integer => integer.Value.ToString(CultureInfo.InvariantCulture),
                ILiteralRational rational when double.IsFinite(rational.Value) => rational.Value.ToString(CultureInfo.InvariantCulture),
                ILiteralBoolean boolean => boolean.Value ? "true" : "false",
                ILiteralString text => text.Value,
                _ => null
            };
        }

        /// <summary>
        /// Formats a nonnegative integer or unbounded multiplicity endpoint.
        /// </summary>
        private static string GetBound(IExpression expression)
        {
            return expression switch
            {
                ILiteralInteger { Value: >= 0 } integer => integer.Value.ToString(CultureInfo.InvariantCulture),
                ILiteralInfinity => "*",
                _ => null
            };
        }
    }
}
