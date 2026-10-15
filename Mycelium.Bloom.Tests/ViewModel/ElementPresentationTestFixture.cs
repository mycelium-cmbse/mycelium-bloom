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

    using Mycelium.Bloom.ViewModel;

    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Systems.Parts;

    [TestFixture]
    public sealed class ElementPresentationTestFixture
    {
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
    }
}
