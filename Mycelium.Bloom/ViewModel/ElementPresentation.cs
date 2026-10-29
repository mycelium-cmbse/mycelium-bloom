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
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Exceptions;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using Mycelium.Bloom.ViewModel.DetailsPanel;

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
            if (!string.IsNullOrWhiteSpace(element.DeclaredName))
            {
                return element.DeclaredName;
            }
            if (!string.IsNullOrWhiteSpace(element.DeclaredShortName))
            {
                return element.DeclaredShortName;
            }
            var incomplete = false;
            foreach (var read in new Func<string>[] { () => element.name, () => element.shortName })
            {
                try
                {
                    var name = read();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }
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
            if (type.Assembly != typeof(IElement).Assembly)
            {
                return "Element";
            }
            var name = type.IsInterface ? type.Name[1..] : type.Name;
            var words = Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return words.Length == 0 ? "Element" : words[..1] + words[1..].ToLowerInvariant();
        }

        /// <summary>
        /// Formats literal, unbounded and safely serializable expression multiplicity bounds without evaluation.
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

            var lower = GetBound(range.lowerBound);
            if (lower is null || range.lowerBound is ILiteralInfinity
                || (range.lowerBound is ILiteralInteger lowerInteger
                    && range.upperBound is ILiteralInteger upperInteger && lowerInteger.Value > upperInteger.Value))
            {
                return null;
            }

            return ReferenceEquals(range.lowerBound, range.upperBound)
                || (range.lowerBound is ILiteralInteger lowerBound
                    && range.upperBound is ILiteralInteger upperBound && lowerBound.Value == upperBound.Value)
                ? $"[{upper}]"
                : $"[{lower}..{upper}]";
        }

        /// <summary>Formats a literal or supported SDK expression inline, without evaluating or adding diagnostic UI.</summary>
        /// <param name="expression">The canonical value or bound expression.</param>
        /// <returns>The model's value/notation, or null for unsupported or incomplete structure.</returns>
        public static string GetExpressionText(IExpression expression)
        {
            if (expression == null)
            {
                return null;
            }

            try
            {
                if (expression is ILiteralExpression)
                {
                    return GetLiteral(expression);
                }
                return HasCompleteInlineStructure(expression, new HashSet<IElement>(ReferenceEqualityComparer.Instance), 0)
                    ? SdkExpressionText.Create(expression).Text : null;
            }
            catch (IncompleteModelException)
            {
                return null;
            }
        }

        /// <summary>Rejects incomplete reference/operand structures before invoking the bounded SDK writer.</summary>
        /// <param name="element">The canonical expression structure.</param>
        /// <param name="path">Identities on the current ownership path.</param>
        /// <param name="depth">The current nesting depth.</param>
        /// <returns>Whether the available structure is safe to present as inline notation.</returns>
        private static bool HasCompleteInlineStructure(IElement element, HashSet<IElement> path, int depth)
        {
            if (element == null || depth >= 64 || !path.Add(element))
            {
                return false;
            }
            try
            {
                if (element is IFeatureReferenceExpression reference && reference.referent == null
                    || element is IFeatureChainExpression chain && chain.targetFeature == null
                    || element is IFeatureValue { value: null })
                {
                    return false;
                }
                if (element is IOperatorExpression operation and not IFeatureChainExpression)
                {
                    var arguments = operation.OwnedRelationship?.OfType<IParameterMembership>()
                        .Where(membership => membership is not IReturnParameterMembership).ToArray() ?? [];
                    var count = arguments.Length;
                    var validArity = operation.Operator switch
                    {
                        "+" or "-" => count is 1 or 2,
                        "not" or "~" => count == 1,
                        "if" => count == 3,
                        "[" => count == 1 && operation.OwnedRelationship.OfType<IFeatureMembership>()
                            .Any(member => member is not IParameterMembership
                                && member.OwnedRelatedElement?.OfType<IExpression>().Count() == 1),
                        "*" or "/" or "**" or "%" or "^" or "and" or "or" or "xor"
                            or "==" or "!=" or "===" or "!==" or "<" or ">" or "<=" or ">="
                            or "|" or "&" or ".." or "??" or "implies" => count == 2,
                        "as" or "istype" or "hastype" or "@" or "@@" or "meta" => count is 1 or 2,
                        "all" => count == 1,
                        _ => false
                    };
                    var acceptsTypeArgument = operation.Operator is "as" or "istype" or "hastype" or "@" or "@@" or "meta" or "all";
                    var hasDeferredArguments = operation.Operator is "??" or "or" or "and" or "implies" or "if";
                    if (!validArity || arguments.Any(argument =>
                        argument.ownedMemberParameter?.OwnedRelationship?.OfType<IFeatureValue>().ToArray() is not [{ value: not null }]
                        && !(acceptsTypeArgument && argument.ownedMemberParameter?.OwnedRelationship?
                            .OfType<IFeatureTyping>().Count(typing => typing.Type != null) == 1))
                        || (hasDeferredArguments && arguments.Skip(1).Any(argument =>
                            argument.ownedMemberParameter?.OwnedRelationship?.OfType<IFeatureValue>().FirstOrDefault()?.value
                                is not IFeatureReferenceExpression deferred
                            || deferred.OwnedRelationship?.OfType<IFeatureMembership>()
                                .Any(member => member.OwnedRelatedElement?.OfType<IExpression>().Count() == 1) != true)))
                    {
                        return false;
                    }
                }
                return (element.ownedElement ?? []).All(child => HasCompleteInlineStructure(child, path, depth + 1));
            }
            finally
            {
                path.Remove(element);
            }
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
        /// Formats a nonnegative integer, an unbounded endpoint or supported expression text.
        /// </summary>
        private static string GetBound(IExpression expression)
        {
            return expression switch
            {
                ILiteralInteger { Value: >= 0 } integer => integer.Value.ToString(CultureInfo.InvariantCulture),
                ILiteralInfinity => "*",
                ILiteralExpression => null,
                _ => GetExpressionText(expression)
            };
        }
    }
}
