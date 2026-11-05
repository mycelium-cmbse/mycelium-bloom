// ------------------------------------------------------------------------------------------------
// <copyright file="ElementPresentationTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using Moq;

    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.ViewModel;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Exceptions;

    using static Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory;

    [TestFixture]
    public sealed class ElementPresentationTestFixture
    {
        [Test]
        public void VerifyExpressionExtensionPreservesNullAndLiteralText()
        {
            IExpression missing = null;
            IExpression literal = new LiteralInteger { Value = 42 };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(missing.GetExpressionText(), Is.Null);
                Assert.That(literal.GetExpressionText(), Is.EqualTo("42"));
                Assert.That(ElementPresentation.GetExpressionText(literal), Is.EqualTo(literal.GetExpressionText()));
            }
        }

        [TestCase(typeof(PartUsage), "Part usage")]
        [TestCase(typeof(IPartUsage), "Part usage")]
        [TestCase(typeof(FeatureReferenceExpression), "Feature reference expression")]
        [TestCase(typeof(ILiteralInteger), "Literal integer")]
        [TestCase(typeof(ElementPresentationTestFixture), "Element")]
        public void VerifyGetKindName(System.Type type, string expected)
        {
            Assert.That(ElementPresentation.GetKindName(type), Is.EqualTo(expected));
        }

        [TestCase(null, 4, "[4]")]
        [TestCase(0, 4, "[0..4]")]
        [TestCase(4, 4, "[4]")]
        [TestCase(0, 0, "[0]")]
        [TestCase(0, null, "[0..*]")]
        [TestCase(null, null, "[*]")]
        [TestCase(-1, 4, null)]
        [TestCase(5, 4, null)]
        [TestCase(null, -1, null)]
        public void VerifyGetMultiplicity(int? lower, int? upper, string expected)
        {
            var range = new Mock<IMultiplicityRange>();
            range.SetupGet(value => value.lowerBound)
                .Returns(lower.HasValue ? new LiteralInteger { Value = lower.Value } : null);
            range.SetupGet(value => value.upperBound)
                .Returns(upper.HasValue ? new LiteralInteger { Value = upper.Value } : new LiteralInfinity());
            var part = new Mock<IPartUsage>();
            part.SetupGet(value => value.multiplicity).Returns(range.Object);

            Assert.That(ElementPresentation.GetMultiplicity(part.Object), Is.EqualTo(expected));
        }

        [Test]
        public void VerifyGetMultiplicityOmitsMissingAndUnresolvedBounds()
        {
            var range = new Mock<IMultiplicityRange>();
            var part = new Mock<IPartUsage>();
            part.SetupGet(value => value.multiplicity).Returns(range.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ElementPresentation.GetMultiplicity(new PartUsage()), Is.Null);
                Assert.That(ElementPresentation.GetMultiplicity(part.Object), Is.Null);
            }

            range.SetupGet(value => value.upperBound).Returns(Mock.Of<IExpression>());
            Assert.That(ElementPresentation.GetMultiplicity(part.Object), Is.Null);

            range.SetupGet(value => value.upperBound).Returns(new LiteralInteger { Value = 3 });
            range.SetupGet(value => value.lowerBound).Returns(Mock.Of<IExpression>());
            Assert.That(ElementPresentation.GetMultiplicity(part.Object), Is.Null);
        }

        [TestCase(63, true)]
        [TestCase(64, false)]
        public void VerifyInlineExpressionDepthBoundary(int depth, bool complete)
        {
            var reference = CreateInlineReference();
            IElement child = Mock.Of<IElement>();

            for (var index = 1; index < depth; index++)
            {
                var owner = new Mock<IElement>();
                owner.SetupGet(element => element.ownedElement).Returns([child]);
                child = owner.Object;
            }

            reference.SetupGet(element => element.ownedElement).Returns([child]);

            Assert.That(reference.Object.GetExpressionText(), complete ? Is.EqualTo("target") : Is.Null);
        }

        [Test]
        public void VerifyInlineExpressionCyclesAndSharedBranches()
        {
            var reference = CreateInlineReference();
            var shared = new Mock<IElement>();
            shared.SetupGet(element => element.ownedElement).Returns([reference.Object]);
            reference.SetupGet(element => element.ownedElement).Returns([shared.Object]);
            Assert.That(reference.Object.GetExpressionText(), Is.Null);

            shared.SetupGet(element => element.ownedElement).Returns([]);
            var first = new Mock<IElement>();
            var second = new Mock<IElement>();
            first.SetupGet(element => element.ownedElement).Returns([shared.Object]);
            second.SetupGet(element => element.ownedElement).Returns([shared.Object]);
            reference.SetupGet(element => element.ownedElement).Returns([first.Object, second.Object]);
            Assert.That(reference.Object.GetExpressionText(), Is.EqualTo("target"));
        }

        [Test]
        public void VerifyInlineExpressionGetterFailureAndRecovery()
        {
            var reference = CreateInlineReference();
            reference.SetupGet(element => element.ownedElement).Throws(new IncompleteModelException("Missing ownership"));
            Assert.That(reference.Object.GetExpressionText(), Is.Null);

            reference.SetupGet(element => element.ownedElement).Throws(new System.InvalidOperationException("Unexpected failure"));
            Assert.That(() => reference.Object.GetExpressionText(), Throws.TypeOf<System.InvalidOperationException>());

            reference.SetupGet(element => element.ownedElement).Returns([]);
            Assert.That(reference.Object.GetExpressionText(), Is.EqualTo("target"));
        }

        [Test]
        public void VerifyInlineInvalidArityDoesNotReadArguments()
        {
            var argument = new Mock<IParameterMembership>(MockBehavior.Strict);
            var operation = new Mock<IOperatorExpression>();
            operation.SetupGet(element => element.Operator).Returns("*");
            operation.SetupGet(element => element.OwnedRelationship).Returns([argument.Object]);

            Assert.That(operation.Object.GetExpressionText(), Is.Null);
            operation.VerifyGet(element => element.Operator, Times.Exactly(3));
            argument.VerifyNoOtherCalls();
        }

        private static Mock<IFeatureReferenceExpression> CreateInlineReference()
        {
            var reference = new Mock<IFeatureReferenceExpression>();
            var scope = new Namespace();
            var target = new Feature { DeclaredName = "target" };
            Own<OwningMembership>(scope, target);
            var canonical = Reference(target);
            reference.SetupGet(element => element.OwnedRelationship).Returns(canonical.OwnedRelationship);
            reference.SetupGet(element => element.owningNamespace).Returns(scope);
            reference.SetupGet(element => element.referent).Returns(target);

            return reference;
        }
    }
}
