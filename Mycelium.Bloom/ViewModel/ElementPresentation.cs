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
    using System.Text.RegularExpressions;

    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Exceptions;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    /// <summary>
    /// Formats supported SDK data without evaluating expressions or changing the model.
    /// </summary>
    public static class ElementPresentation
    {
        /// <summary>
        /// Gets the first available element name for display.
        /// </summary>
        /// <param name="element">The element to describe.</param>
        /// <returns>An SDK name, semantic role, or explicit incomplete-name state.</returns>
        public static string GetDisplayName(IElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return GetDisplayName(element, ProjectBrowserElementPresentation.For(element.GetType()), out _);
        }

        /// <summary>
        /// Gets an SDK name or the contextual semantic fallback for an element.
        /// </summary>
        /// <param name="element">The element to describe.</param>
        /// <param name="presentation">The semantic presentation for the element.</param>
        /// <param name="isFallbackLabel">Whether the result is a UI-provided fallback rather than an SDK name.</param>
        /// <returns>An SDK name, semantic role, or explicit incomplete-name state.</returns>
        public static string GetDisplayName(
            IElement element,
            ProjectBrowserElementPresentation presentation,
            out bool isFallbackLabel)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(presentation);

            isFallbackLabel = false;
            if (!string.IsNullOrWhiteSpace(element.DeclaredName)) return element.DeclaredName;
            if (!string.IsNullOrWhiteSpace(element.DeclaredShortName)) return element.DeclaredShortName;
            var incomplete = false;
            foreach (var read in new Func<string>[] { () => element.name, () => element.shortName })
            {
                try
                {
                    var name = read();
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
                catch (IncompleteModelException)
                {
                    incomplete = true;
                }
            }
            var role = presentation.UnnamedLabel;
            isFallbackLabel = !incomplete;
            return incomplete ? $"{role} (name unresolved)" : role;
        }

        /// <summary>Labels an SDK metaclass without exposing its implementation identifier as prose.</summary>
        /// <param name="type">The SDK type being presented.</param>
        /// <returns>A readable metaclass label or the neutral element role.</returns>
        public static string GetKindName(System.Type type)
        {
            if (type.Assembly != typeof(IElement).Assembly) return "Element";
            var name = type.IsInterface ? type.Name[1..] : type.Name;
            var words = Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return words.Length == 0 ? "Element" : words[..1] + words[1..].ToLowerInvariant();
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
                ILiteralString { Value: "" } => "Empty string",
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
