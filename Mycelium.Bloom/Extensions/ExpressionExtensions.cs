// ------------------------------------------------------------------------------------------------
// <copyright file="ExpressionExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Extensions
{
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.DetailsPanel;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Exceptions;

    /// <summary>
    /// Formats supported expression notation without evaluating or changing the model.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Formats a literal or supported SDK expression inline, without evaluating or adding diagnostic UI.
        /// </summary>
        /// <param name="expression">The canonical value or bound expression.</param>
        /// <returns>The model's value/notation, or null for unsupported or incomplete structure.</returns>
        public static string GetExpressionText(this IExpression expression)
        {
            if (expression == null)
            {
                return null;
            }

            try
            {
                if (expression is ILiteralExpression)
                {
                    return ElementPresentation.GetLiteral(expression);
                }

                return HasCompleteInlineStructure(expression, new HashSet<IElement>(ReferenceEqualityComparer.Instance), 0)
                    ? SdkExpressionText.Create(expression).Text : null;
            }
            catch (IncompleteModelException)
            {
                return null;
            }
        }

        /// <summary>
        /// Rejects incomplete reference/operand structures before invoking the bounded SDK writer.
        /// </summary>
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
                    var validArity = HasSupportedOperatorArity(operation, arguments.Length);
                    var acceptsTypeArgument = operation.Operator is "as" or "istype" or "hastype" or "@" or "@@" or "meta" or "all";
                    var hasDeferredArguments = operation.Operator is "??" or "or" or "and" or "implies" or "if";

                    if (!validArity || !HasCompleteArguments(arguments, acceptsTypeArgument, hasDeferredArguments))
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
        /// Checks supported operator arity, including the separate quantity operand.
        /// </summary>
        /// <param name="operation">The canonical operator expression.</param>
        /// <param name="count">The number of non-return parameter memberships.</param>
        /// <returns>Whether the operator has a supported operand structure.</returns>
        private static bool HasSupportedOperatorArity(IOperatorExpression operation, int count)
        {
            return operation.Operator switch
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
        }

        /// <summary>
        /// Checks ordinary arguments before inspecting any deferred argument bodies.
        /// </summary>
        /// <param name="arguments">The non-return parameter memberships in SDK order.</param>
        /// <param name="acceptsTypeArgument">Whether a single resolved typing can supply an argument.</param>
        /// <param name="hasDeferredArguments">Whether arguments after the first require deferred bodies.</param>
        /// <returns>Whether every required value, type or deferred body is available.</returns>
        private static bool HasCompleteArguments(IParameterMembership[] arguments, bool acceptsTypeArgument, bool hasDeferredArguments)
        {
            return !arguments.Any(argument =>
                argument.ownedMemberParameter?.OwnedRelationship?.OfType<IFeatureValue>().ToArray() is not [{ value: not null }]
                && !(acceptsTypeArgument && argument.ownedMemberParameter?.OwnedRelationship?
                    .OfType<IFeatureTyping>().Count(typing => typing.Type != null) == 1))
                && !(hasDeferredArguments && arguments.Skip(1).Any(argument =>
                    argument.ownedMemberParameter?.OwnedRelationship?.OfType<IFeatureValue>().FirstOrDefault()?.value
                        is not IFeatureReferenceExpression deferred
                    || deferred.OwnedRelationship?.OfType<IFeatureMembership>()
                        .Any(member => member.OwnedRelatedElement?.OfType<IExpression>().Count() == 1) != true));
        }
    }
}
