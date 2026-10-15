// ------------------------------------------------------------------------------------------------
// <copyright file="ExpressionPresentationTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Moq;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.Root.Namespaces;
    using SysML2.NET.Exceptions;
    using SysML2.NET.Extensions;
    using SysML2.NET.Serializer.TextualNotation.Writers;
    using static Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory;

    [TestFixture]
    public sealed class ExpressionPresentationTestFixture
    {
        [Test]
        public void VerifyInspectionOmitsEmptyQuantitiesWrappersWithoutRemovingMetadata()
        {
            var dimensions = Descendants(LoadQuantities()).Single(element => element.name == "dimensions");
            var groups = ExpressionPresentation.Create(dimensions).SelectMany(node => node.OwnedGroups).ToArray();
            var empty = groups.Where(node => node.Role is "Result" or "Owned element").ToArray();
            Assert.That(empty, Has.Length.EqualTo(3));
            foreach (var group in empty)
            {
                Assert.That(group.CanInspectElement, Is.False);
                Assert.That(group.CanInspectMembership, Is.False);
                Assert.That(group.Element, Is.Not.Null);
                Assert.That(group.Membership, Is.Not.Null);
                Assert.That(group.Properties, Is.Not.Empty);
            }
            Assert.That(groups.Where(node => node.Role is "Operand 1" or "Value")
                .All(node => node.CanInspectElement && node.CanInspectMembership), Is.True);
        }

        [Test]
        public void VerifyInspectionRetainsNamedTypedUnresolvedAndLiteralElements()
        {
            var input = new Mock<IFeature>();
            input.SetupGet(element => element.owningMembership).Returns(new ParameterMembership());
            input.SetupGet(element => element.type).Throws(new IncompleteModelException("Missing type"));
            Assert.That(ExpressionPresentation.Create(input.Object).Single().CanInspectElement, Is.True);
            input.SetupGet(element => element.type).Returns([new PartDefinition()]);
            Assert.That(ExpressionPresentation.Create(input.Object).Single().CanInspectElement, Is.True);
            var operation = new OperatorExpression { Operator = "+" };
            Attach<ParameterMembership>(operation, new Feature { DeclaredName = "input" });
            Assert.That(ExpressionPresentation.Create(operation).Single().Children.Single().CanInspectElement, Is.True);
            Assert.That(ExpressionPresentation.Create(new LiteralRational { Value = 53.4 }).Single().CanInspectElement, Is.True);
        }

        [Test]
        public void VerifyInspectionRetainsAnonymousFeaturesWithAdditionalFlags()
        {
            var operation = new OperatorExpression { Operator = "+" };
            Attach<ParameterMembership>(operation, new Feature { IsUnique = false });
            Attach<ParameterMembership>(operation, new Feature { IsDerived = true });
            Assert.That(ExpressionPresentation.Create(operation).Single().Children.Select(node => node.CanInspectElement),
                Is.All.True);
        }

        [Test]
        public void VerifyMembershipProjectionUsesCanonicalOwnedRelatedElements()
        {
            var dimensions = Descendants(LoadQuantities()).Single(element => element.name == "dimensions");
            var chain = Descendants(dimensions).OfType<IFeatureChainExpression>().Single();
            var membership = chain.OwnedRelationship.OfType<IReturnParameterMembership>().Single();
            Assert.That(membership.ownedElement, Is.Empty);
            var projected = ExpressionPresentation.Create(membership).Single();
            Assert.That(projected.Element, Is.SameAs(membership.OwnedRelatedElement.Single()));
            Assert.That(projected.Membership, Is.SameAs(membership));
            Assert.That(projected.Role, Is.EqualTo("Result"));
            Assert.That(projected.ValueState, Is.EqualTo("No explicit value"));
            Assert.That(projected.Properties.Single(property => property.Key == "Direction").Value, Is.EqualTo("out"));
            Assert.That(projected.Children, Is.Empty);
        }

        [Test]
        public void VerifyLiteralAndInputValueStatesDoNotInventComputedResults()
        {
            var literal = new LiteralRational { Value = 53.4 };
            var projected = ExpressionPresentation.Create(literal).Single();
            Assert.That(projected.LiteralText, Is.EqualTo("53.4"));
            Assert.That(projected.TextualNotation.Text, Is.EqualTo("53.4"));
            Assert.That(projected.ValueState, Is.Null);
            var dimensions = Descendants(LoadQuantities()).Single(element => element.name == "dimensions");
            var reference = Descendants(dimensions).OfType<IFeatureReferenceExpression>().Single();
            var input = ExpressionPresentation.Create(reference.owner).Single();
            Assert.That(input.ValueState, Is.Null);
            Assert.That(input.OwnedGroups.Single(node => node.ReferenceRole == "Reference").Reference,
                Is.SameAs(reference.referent));
        }

        [Test]
        public void VerifyUnresolvedInputStructureAndMembershipStayDistinctFromAbsentValues()
        {
            var input = new Mock<IFeature>();
            input.SetupGet(element => element.owningMembership).Returns(new ParameterMembership());
            input.SetupGet(element => element.OwnedRelationship).Throws(new IncompleteModelException("Missing structure"));
            var projected = ExpressionPresentation.Create(input.Object).Single();
            Assert.That(projected.ValueState, Does.StartWith("Unresolved"));
            Assert.That(projected.Children.Single().Notice, Does.Contain("incomplete model"));
            var membership = new Mock<IReturnParameterMembership>();
            membership.SetupGet(element => element.OwnedRelatedElement).Throws(new IncompleteModelException("Missing result"));
            var missing = ExpressionPresentation.Create(membership.Object).Single();
            Assert.That(missing.Notice, Does.StartWith("Unresolved"));
            Assert.That(missing.Membership, Is.SameAs(membership.Object));
            Assert.That(missing.Element, Is.Null);
        }

        [TestCase("dimensions", false)]
        [TestCase("orderSum", true)]
        [TestCase("boundMatch", true)]
        public void VerifyCompactGroupsPreserveEveryOwnedIdentityAndOperandOrder(string name, bool combinesValues)
        {
            var selected = Descendants(LoadQuantities()).Single(element => element.name == name);
            var roots = ExpressionPresentation.Create(selected);
            var original = roots.SelectMany(Flatten).ToArray();
            var compact = roots.SelectMany(ExpandDisplayPaths).ToArray();
            Assert.That(compact, Is.EqualTo(original));
            Assert.That(original.Any(node => node.DisplayPath.Count > 1), Is.EqualTo(combinesValues));
            foreach (var node in original)
            {
                Assert.That(node.DisplayPath[0], Is.SameAs(node));
                Assert.That(node.Element?.owner, Is.SameAs(node.Membership?.OwningRelatedElement));
            }
        }

        [Test]
        public void VerifyCompactGroupsKeepMultipleValuesAndNamedFeaturesVisible()
        {
            var input = new Feature { DeclaredName = "input" };
            Attach<FeatureValue>(input, new LiteralInteger { Value = 1 });
            Attach<FeatureValue>(input, new LiteralInteger { Value = 2 });
            var operation = new OperatorExpression { Operator = "+" };
            Attach<ParameterMembership>(operation, input);
            var group = ExpressionPresentation.Create(operation)[0].Children[0];
            Assert.That(group.DisplayPath, Has.Count.EqualTo(1));
            Assert.That(group.Children, Has.Count.EqualTo(2));
            Assert.That(group.HasSummaryLabel, Is.True);
        }

        private static IEnumerable<ExpressionPresentation> ExpandDisplayPaths(ExpressionPresentation node)
        {
            var path = node.DisplayPath;
            return path.Concat(path[^1].Children.SelectMany(ExpandDisplayPaths));
        }

        [TestCase("dimensions", "mRef.dimensions")]
        [TestCase("orderSum", "contravariantOrder + covariantOrder == order")]
        [TestCase("boundMatch", "isBound == mRef.isBound or (not isBound and mRef.isBound)")]
        public void VerifyCreateUsesSdkGeneratedNotation(string name, string expected)
        {
            var selected = Descendants(LoadQuantities()).Single(element => element.name == name);
            var nodes = ExpressionPresentation.Create(selected).SelectMany(Flatten).ToArray();
            var notation = nodes.Single(node => node.TextualNotation is not null).TextualNotation;
            Assert.That(notation.Text, Is.EqualTo(expected));
            Assert.That(notation.UnavailableReason, Is.Null);
        }

        [TestCase("dimensions")]
        [TestCase("orderSum")]
        [TestCase("boundMatch")]
        public void VerifyCreateResolvesNotationReferencesToCanonicalQuantitiesTargets(string name)
        {
            var elements = Descendants(LoadQuantities()).ToArray();
            var selected = elements.Single(element => element.name == name);
            var scope = elements.Single(element => element.name == "TensorQuantityValue") as INamespace;
            using var context = new TextualNotationWriterContext(scope);
            var references = ExpressionPresentation.Create(selected).SelectMany(Flatten)
                .Where(node => node.Element is IFeatureReferenceExpression && node.Reference is not IExpression).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Empty);
                Assert.That(elements.Count(element => element.name == "mRef"), Is.GreaterThan(1));
                foreach (var reference in references)
                {
                    var emittedName = context.NameResolutionCache.Resolve(reference.Reference, reference.Element);
                    Assert.That(scope.Resolve(emittedName)?.MemberElement, Is.SameAs(reference.Reference));
                }
            }
        }

        [Test]
        public void VerifyCreateQualifiesShadowedReferenceWithNarrowWriterContext()
        {
            var globalScope = new Namespace();
            var model = new Namespace { DeclaredName = "Model" };
            var outer = new Namespace { DeclaredName = "Outer" };
            var inner = new Namespace { DeclaredName = "Inner" };
            var target = new Feature { DeclaredName = "value" };
            var shadow = new Feature { DeclaredName = "value" };
            var expression = new FeatureReferenceExpression();
            Attach<OwningMembership>(globalScope, model);
            Attach<OwningMembership>(model, outer);
            Attach<OwningMembership>(outer, target);
            Attach<OwningMembership>(outer, inner);
            Attach<OwningMembership>(inner, shadow);
            Attach<OwningMembership>(inner, expression);
            expression.AssignOwnership(new Membership { MemberElement = target });
            var node = ExpressionPresentation.Create(expression)[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(inner.Resolve("value").MemberElement, Is.SameAs(shadow));
                Assert.That(node.TextualNotation.UnavailableReason, Is.Null);
                Assert.That(node.TextualNotation.Text, Is.Not.EqualTo("value"));
                Assert.That(inner.Resolve(node.TextualNotation.Text)?.MemberElement, Is.SameAs(target));
                Assert.That(node.Reference, Is.SameAs(target));
            }
        }

        [Test]
        public void VerifyCreateUsesSdkParenthesesForNestedOperatorPrecedence()
        {
            var addition = new OperatorExpression { Operator = "+" };
            Attach<FeatureValue>(Attach<ParameterMembership>(addition,
                new Feature { Direction = FeatureDirectionKind.In }).ownedMemberParameter, new LiteralInteger { Value = 1 });
            Attach<FeatureValue>(Attach<ParameterMembership>(addition,
                new Feature { Direction = FeatureDirectionKind.In }).ownedMemberParameter, new LiteralInteger { Value = 2 });
            var multiplication = new OperatorExpression { Operator = "*" };
            Attach<FeatureValue>(Attach<ParameterMembership>(multiplication,
                new Feature { Direction = FeatureDirectionKind.In }).ownedMemberParameter, addition);
            Attach<FeatureValue>(Attach<ParameterMembership>(multiplication,
                new Feature { Direction = FeatureDirectionKind.In }).ownedMemberParameter, new LiteralInteger { Value = 3 });
            foreach (var element in Descendants(multiplication))
            {
                element.Id = Guid.NewGuid();
                foreach (var relationship in element.OwnedRelationship)
                {
                    relationship.Id = Guid.NewGuid();
                }
            }
            var text = ExpressionPresentation.Create(multiplication)[0].TextualNotation;
            Assert.That(text.Text, Is.EqualTo("(1+ 2) * 3"));
            Assert.That(text.UnavailableReason, Is.Null);
        }

        [TestCase("dimensions", 1)]
        [TestCase("orderSum", 4)]
        [TestCase("boundMatch", 9)]
        public void VerifyCreatePreservesQuantitiesInputsAndCanonicalIdentity(string name, int inputCount)
        {
            var elements = Descendants(LoadQuantities()).ToArray();
            var selected = elements.Single(element => element.name == name);
            var presentation = new DetailsPanelPresentation(selected);
            var nodes = presentation.Expressions.SelectMany(Flatten).ToArray();
            var inputs = nodes.Where(node => node.Membership is IParameterMembership and not IReturnParameterMembership).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(inputs, Has.Length.EqualTo(inputCount));
                Assert.That(inputs.SelectMany(node => node.Properties).Where(p => p.Key == "Direction").Select(p => p.Value),
                    Has.All.EqualTo("in"));
                Assert.That(inputs.SelectMany(node => node.Properties).Where(p => p.Key == "Visibility").Select(p => p.Value),
                    Has.All.EqualTo("private"));
                foreach (var node in nodes.Where(node => node.Element is not null))
                {
                    Assert.That(elements.Single(element => element.Id == node.Element.Id), Is.SameAs(node.Element));
                    Assert.That(node.Membership, Is.SameAs(node.Element.owningMembership));
                }
                Assert.That(nodes.Where(node => node.Notice is not null), Is.Empty);
            }
        }

        [Test]
        public void VerifyCreateUsesOwnedOperandsWhenArgumentIsEmpty()
        {
            var root = Descendants(LoadQuantities()).Single(element => element.name == "orderSum");
            var equality = ExpressionPresentation.Create(root)[0].Children[0];
            var addition = equality.Children[0].Children[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(((IOperatorExpression)equality.Element).argument, Is.Empty);
                Assert.That(equality.Operator, Is.EqualTo("=="));
                Assert.That(addition.Operator, Is.EqualTo("+"));
                Assert.That(addition.Children.Where(node => node.Role.StartsWith("Operand", StringComparison.Ordinal))
                    .Select(node => node.Children[0].ReferenceLabel), Is.EqualTo(new[] { "contravariantOrder", "covariantOrder" }));
                Assert.That(equality.Children[1].Children[0].ReferenceLabel, Is.EqualTo("order"));
                Assert.That(equality.Children[^1].Role, Is.EqualTo("Result"));
                Assert.That(equality.Children[^1].Properties.Single(p => p.Key == "Direction").Value, Is.EqualTo("out"));
            }
        }

        [Test]
        public void VerifyCreatePreservesChainsAndOwnedReferencedExpressionBodies()
        {
            var elements = Descendants(LoadQuantities()).ToArray();
            var chain = ExpressionPresentation.Create(elements.Single(element => element.name == "dimensions"))[0];
            var boolean = ExpressionPresentation.Create(elements.Single(element => element.name == "boundMatch"))[0];
            var nodes = Flatten(boolean).ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(chain.Operator, Is.EqualTo("."));
                Assert.That(chain.Reference, Is.SameAs(((IFeatureChainExpression)chain.Element).targetFeature));
                Assert.That(chain.ReferenceLabel, Is.EqualTo("dimensions"));
                Assert.That(chain.Children[0].Children[0].ReferenceLabel, Is.EqualTo("mRef"));
                Assert.That(nodes.Where(node => node.Operator is not null).Select(node => node.Operator),
                    Is.EqualTo(new[] { "or", "==", ".", "and", "not", "." }));
                var embedded = nodes.Where(node => node.Reference is IExpression).ToArray();
                Assert.That(embedded, Has.Length.EqualTo(2));
                foreach (var reference in embedded)
                {
                    Assert.That(reference.Children[0].Element, Is.SameAs(reference.Reference));
                }
            }
        }

        [Test]
        public void VerifyCreatePreservesMultipleValuesWithoutFollowingExternalReferences()
        {
            var input = new Feature { Direction = FeatureDirectionKind.In };
            var first = new LiteralInteger { Value = 1 };
            var second = new LiteralInteger { Value = 2 };
            Attach<FeatureValue>(input, first).IsDefault = true;
            Attach<FeatureValue>(input, second).IsInitial = true;
            var expression = new OperatorExpression { Operator = "+" };
            var membership = Attach<ParameterMembership>(expression, input);
            membership.Visibility = VisibilityKind.Private;
            var external = new Feature { DeclaredName = "Target" };
            Attach<FeatureValue>(external, new LiteralInteger { Value = 99 });
            var reference = new FeatureReferenceExpression();
            reference.AssignOwnership(new Membership { MemberElement = external });
            var node = ExpressionPresentation.Create(expression)[0].Children[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.Element, Is.SameAs(input));
                Assert.That(node.Membership, Is.SameAs(membership));
                Assert.That(node.Children.Select(child => child.Element), Is.EqualTo(new IElement[] { first, second }));
                Assert.That(node.Children[0].Properties.Single(p => p.Key == "Default").Value, Is.EqualTo("true"));
                Assert.That(node.Children[1].Properties.Single(p => p.Key == "Initial").Value, Is.EqualTo("true"));
                Assert.That(ExpressionPresentation.Create(reference)[0].Reference, Is.SameAs(external));
                Assert.That(ExpressionPresentation.Create(reference)[0].Children, Is.Empty);
                var notationRoots = new DetailsPanelPresentation(input).ExpressionNotations;
                Assert.That(notationRoots.Select(group => group.Element), Is.EqualTo(new IElement[] { first, second }));
                Assert.That(notationRoots.Select(group => group.TextualNotation.Text), Is.EqualTo(new[] { "1", "2" }));
            }
        }

        [Test]
        public void VerifyCreateKeepsValidPropertiesWhenReferenceIsIncomplete()
        {
            var chain = new Mock<IFeatureChainExpression>();
            chain.SetupGet(element => element.Operator).Returns(".");
            chain.SetupGet(element => element.targetFeature).Throws(new IncompleteModelException("Missing target"));
            var node = ExpressionPresentation.Create(chain.Object)[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(node.Operator, Is.EqualTo("."));
                Assert.That(node.Reference, Is.Null);
                Assert.That(node.ReferenceLabel, Is.EqualTo("Unresolved reference"));
            }
            chain.SetupGet(element => element.targetFeature).Returns((IFeature)null);
            Assert.That(ExpressionPresentation.Create(chain.Object)[0].ReferenceLabel, Is.EqualTo("Not specified"));
            chain.SetupGet(element => element.targetFeature).Throws(new InvalidOperationException("Unexpected failure"));
            Assert.That(() => ExpressionPresentation.Create(chain.Object), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void VerifyCreateExposesIncompleteMembershipAndPreservesSiblings()
        {
            var root = new Mock<IExpression>();
            var missing = new ParameterMembership();
            var valid = new Feature { DeclaredName = "Valid" };
            var membership = new Mock<IParameterMembership>();
            membership.SetupGet(element => element.OwnedRelatedElement).Returns([valid]);
            root.SetupGet(element => element.OwnedRelationship).Returns([missing, membership.Object]);
            var nodes = ExpressionPresentation.Create(root.Object)[0].Children;
            Assert.That(nodes[0].Notice, Does.Contain("no resolved owned element"));
            Assert.That(nodes[0].Membership, Is.SameAs(missing));
            Assert.That(nodes[1].Element, Is.SameAs(valid));
        }

        [Test]
        public void VerifyCreateStopsOwnershipCyclesAndBoundsDeepAndWideStructures()
        {
            var expression = new Mock<IExpression>();
            var value = new Mock<IFeatureValue>();
            value.SetupGet(element => element.OwnedRelatedElement).Returns([expression.Object]);
            expression.SetupGet(element => element.OwnedRelationship).Returns([value.Object]);
            Assert.That(ExpressionPresentation.Create(expression.Object)[0].Children[0].Notice, Does.Contain("cycle"));

            var root = new OperatorExpression();
            IElement parent = root;
            for (var index = 0; index < 20; index++)
            {
                var child = new OperatorExpression();
                Attach<FeatureValue>(parent, child);
                parent = child;
            }
            var deep = Flatten(ExpressionPresentation.Create(root)[0]).ToArray();
            Assert.That(deep, Has.Length.EqualTo(13));
            Assert.That(deep[^1].Notice, Does.Contain("limit"));
            Assert.That(deep[^1].Element, Is.Not.Null);
            Assert.That(deep[0].TextualNotation.Text, Is.Null);
            Assert.That(deep[0].TextualNotation.UnavailableReason, Does.Contain("display limit"));

            var wide = new OperatorExpression();
            for (var index = 0; index < 150; index++)
            {
                Attach<ParameterMembership>(wide, new Feature());
            }
            var nodes = Flatten(ExpressionPresentation.Create(wide)[0]).ToArray();
            Assert.That(nodes.Length, Is.LessThanOrEqualTo(130));
            Assert.That(nodes[^1].Notice, Does.Contain("limit"));
        }

        [Test]
        public void VerifyCreateDoesNotAddExpressionsToOrdinaryPartsOrMutateSelection()
        {
            var part = new PartUsage { DeclaredName = "Harness" };
            var attribute = new AttributeUsage { DeclaredName = "Mass" };
            Attach<FeatureMembership>(part, attribute);
            Assert.That(new DetailsPanelPresentation(part).Expressions, Is.Empty);
            Assert.That(part.ownedElement[0], Is.SameAs(attribute));
            Assert.That(attribute.owner, Is.SameAs(part));
        }

        private static IEnumerable<ExpressionPresentation> Flatten(ExpressionPresentation node)
        {
            yield return node;
            foreach (var child in node.Children.SelectMany(Flatten))
            {
                yield return child;
            }
        }
    }
}
