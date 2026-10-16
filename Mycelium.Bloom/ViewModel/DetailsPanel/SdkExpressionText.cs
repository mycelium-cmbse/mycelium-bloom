// ------------------------------------------------------------------------------------------------
// <copyright file="SdkExpressionText.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.DetailsPanel
{
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Exceptions;
    using SysML2.NET.Serializer.TextualNotation.Writers;

    /// <summary>Adapts the pinned SDK writer to a bounded read-only expression presentation.</summary>
    /// <param name="Text">The SDK-generated notation, when available.</param>
    /// <param name="UnavailableReason">The presentation reason when serialization cannot produce text.</param>
    public sealed record SdkExpressionText(string Text, string UnavailableReason)
    {
        /// <summary>Serializes canonical expression data without treating generated notation as authored source.</summary>
        /// <param name="expression">The canonical expression approved for serialization by the projection.</param>
        /// <returns>SDK-generated text or an explicit unavailable state.</returns>
        public static SdkExpressionText Create(IExpression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            try
            {
                var declaration = IsDeclaration(expression);
                if (!HasSupportedStructure(expression, declaration, new HashSet<IElement>(ReferenceEqualityComparer.Instance), 0))
                {
                    return new(null, "Notation unavailable for this expression structure; inspect its canonical elements.");
                }
                var scope = expression.owner?.owningNamespace ?? expression.owningNamespace ?? expression;
                using var context = new TextualNotationWriterContext(scope);
                var builder = new IndentedStringBuilder();
                if (declaration)
                {
                    if (expression is IInvariant invariant)
                        InvariantTextualNotationBuilder.BuildInvariant(invariant, context, builder);
                    else if (expression is IBooleanExpression boolean)
                        BooleanExpressionTextualNotationBuilder.BuildBooleanExpression(boolean, context, builder);
                    else
                        ExpressionTextualNotationBuilder.BuildExpression(expression, context, builder);
                }
                else
                {
                    ExpressionTextualNotationBuilder.BuildOwnedExpression(expression, context, builder);
                }
                var text = builder.ToString().Trim();
                return string.IsNullOrWhiteSpace(text)
                    ? new(null, "The SDK produced no textual notation for this expression.")
                    : new(text, null);
            }
            catch (IncompleteModelException)
            {
                return new(null, "Notation unavailable (incomplete model).");
            }
            catch (NotSupportedException)
            {
                return new(null, "This expression is not supported by the SDK textual writer.");
            }
        }

        /// <summary>Recognizes declarations by their canonical membership rather than their Boolean result type alone.</summary>
        private static bool IsDeclaration(IExpression expression) =>
            !IsValueExpression(expression) && expression is not IUsage
            && expression.owningMembership is IOwningMembership and not IFeatureValue
                and not IResultExpressionMembership and not IParameterMembership
            && expression.owner is INamespace and not IExpression and not IMultiplicity;

        /// <summary>Identifies public SDK expression forms that do not enter the generic sequence fallback.</summary>
        private static bool IsValueExpression(IExpression expression) =>
            expression is INullExpression or ILiteralExpression or IFeatureReferenceExpression
                or IMetadataAccessExpression or IInvocationExpression or IConstructorExpression;

        /// <summary>Rejects unsupported writer shapes and cycles before invoking recursive SDK code.</summary>
        private static bool HasSupportedStructure(IElement element, bool declaration, HashSet<IElement> path, int depth)
        {
            if (depth >= 64 || !path.Add(element)) return false;
            try
            {
                if (element is ILiteralString) return false;
                if (element is IExpression expression && !(declaration || IsValueExpression(expression))) return false;
                return (element.ownedElement ?? []).All(child => child is not null
                    && HasSupportedStructure(child, child is IExpression nested && IsDeclaration(nested), path, depth + 1));
            }
            finally
            {
                path.Remove(element);
            }
        }
    }
}
