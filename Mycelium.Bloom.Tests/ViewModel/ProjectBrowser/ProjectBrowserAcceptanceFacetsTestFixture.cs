// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserAcceptanceFacetsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using System.Linq;
    using Moq;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.POCO.Systems.VerificationCases;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.Core.Types;
    using static Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory;

    /// <summary>Verifies semantic Browser facets using canonical SDK model data.</summary>
    [TestFixture]
    public sealed class ProjectBrowserAcceptanceFacetsTestFixture
    {
        [Test]
        public void VerifyInitialStateComesFromEntrySuccessionRatherThanNameOrOrder()
        {
            var model = CreateStates();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(model.Owner.entryAction, Is.Not.Null);
                Assert.That(model.Succession.sourceFeature, Is.SameAs(model.Owner.entryAction));
                Assert.That(model.Succession.targetFeature.Single(), Is.SameAs(model.Initial));
                Assert.That(ProjectBrowserRowPresentation.For(model.Initial).Indicators, Does.Contain("Initial"));
                Assert.That(ProjectBrowserRowPresentation.For(model.First).Indicators, Does.Not.Contain("Initial"));
            }
            model.Owner.IsParallel = true;
            Assert.That(ProjectBrowserRowPresentation.For(model.Initial).Indicators, Does.Not.Contain("Initial"));
            model.Owner.IsParallel = false;
            AddRelationship(model.Owner, new OwningMembership()); // An unrelated membership changes nothing.
            model.Owner.DeclaredName = "Renamed";
            model.Initial.DeclaredName = "NotNamedInitial";
            Assert.That(ProjectBrowserRowPresentation.For(model.Initial).Indicators, Does.Contain("Initial"));
        }

        [Test]
        public void VerifyUnguardedEntryTransitionUsesItsCanonicalTarget()
        {
            var model = CreateStates();
            var owner = new Mock<IStateDefinition>();
            owner.SetupGet(element => element.entryAction).Returns(model.Owner.entryAction);
            var state = new Mock<IStateUsage>();
            state.SetupGet(element => element.owner).Returns(owner.Object);
            var transition = new Mock<ITransitionUsage>();
            transition.SetupGet(element => element.source).Returns(model.Owner.entryAction);
            transition.SetupGet(element => element.target).Returns(state.Object);
            transition.SetupGet(element => element.guardExpression).Returns([]);
            transition.SetupGet(element => element.triggerAction).Returns([]);
            owner.SetupGet(element => element.ownedElement).Returns([transition.Object]);
            Assert.That(ProjectBrowserRowPresentation.For(state.Object).Indicators, Does.Contain("Initial"));
            transition.SetupGet(element => element.guardExpression).Returns([new BooleanExpression()]);
            Assert.That(ProjectBrowserRowPresentation.For(state.Object).Indicators, Does.Not.Contain("Initial"));
        }

        [Test]
        public void VerifyNoInitialMarkerWithoutExplicitOrUniqueSuccession()
        {
            var model = CreateStates();
            var transition = new Mock<ITransitionUsage>();
            transition.SetupGet(element => element.source).Returns(model.Owner.entryAction);
            var owner = new Mock<IStateDefinition>();
            owner.SetupGet(element => element.entryAction).Returns(model.Owner.entryAction);
            owner.SetupGet(element => element.ownedElement).Returns([model.Succession, transition.Object]);
            var state = new Mock<IStateUsage>();
            state.SetupGet(element => element.owner).Returns(owner.Object);
            Assert.That(ProjectBrowserRowPresentation.For(state.Object).Indicators, Does.Not.Contain("Initial"));
            Assert.That(ProjectBrowserRowPresentation.For(new StateUsage { DeclaredName = "Initial" }).Indicators,
                Does.Not.Contain("Initial"));
        }

        [TestCase("pass")]
        [TestCase("fail")]
        [TestCase("inconclusive")]
        [TestCase("error")]
        public void VerifyVerdictIsAnExplicitStandardEnumerationResult(string value)
        {
            var model = CreateVerification(value);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(model.Verification.result, Is.Not.Null);
                Assert.That(model.Library.ownedElement.OfType<SysML2.NET.Core.POCO.Systems.Enumerations.IEnumerationDefinition>()
                    .Single().qualifiedName, Is.EqualTo("VerificationCases::VerdictKind"));
                Assert.That(ProjectBrowserRowPresentation.For(model.Verification).Indicators, Does.Contain("Verdict: " + value));
            }
            model.Library.DeclaredName = "UserDefinedResults";
            Assert.That(ProjectBrowserRowPresentation.For(model.Verification).Indicators.Any(indicator =>
                indicator.StartsWith("Verdict:", System.StringComparison.Ordinal)), Is.False);
            model.Library.DeclaredName = "VerificationCases";
            model.Library.IsStandard = false;
            Assert.That(ProjectBrowserRowPresentation.For(model.Verification).Indicators.Any(indicator =>
                indicator.StartsWith("Verdict:", System.StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void VerifyExplicitVerdictOnVerificationDefinition()
        {
            var model = CreateVerification("inconclusive");
            var literal = model.Library.ownedElement.OfType<IEnumerationDefinition>().Single()
                .ownedElement.OfType<IEnumerationUsage>().Single();
            var definition = new VerificationCaseDefinition { DeclaredName = "VerificationDefinition" };
            var result = new AttributeUsage { Direction = FeatureDirectionKind.Out };
            Own<ReturnParameterMembership>(definition, result);
            Own<FeatureValue>(result, Reference(literal));
            Assert.That(ProjectBrowserRowPresentation.For(definition).Indicators, Does.Contain("Verdict: inconclusive"));
        }

        [Test]
        public void VerifyUnrecordedOrAmbiguousVerdictsAreNotGuessed()
        {
            var model = CreateVerification("pass");
            Own<FeatureValue>(model.Verification.result, new FeatureReferenceExpression());
            Assert.That(ProjectBrowserRowPresentation.For(model.Verification).Indicators.Any(indicator =>
                indicator.StartsWith("Verdict:", System.StringComparison.Ordinal)), Is.False);
            Assert.That(ProjectBrowserRowPresentation.For(new VerificationCaseUsage()).Indicators.Any(indicator =>
                indicator.StartsWith("Verdict:", System.StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void VerifyQuantityValueUsesCanonicalUnitReference()
        {
            var model = CreateQuantity();
            var root = new Package { DeclaredName = "Acceptance" };
            Own<OwningMembership>(root, model.Library);
            Own<OwningMembership>(root, model.Attribute);
            var text = ProjectBrowserRowPresentation.For(model.Attribute).ValueLabel;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Is.Not.Null);
                Assert.That(text, Does.StartWith("3").And.Contain("[").And.Contain("]").And.Contain("SI::"));
                Assert.That(text, Does.Contain("kilogram").Or.Contain("kg"));
                Assert.That(model.Value.ownedElement, Is.Not.Empty);
            }
        }

        [TestCase("+", "Assembly::count + 1")]
        [TestCase("??", "Assembly::count ?? 1")]
        public void VerifyExpressionBoundsPreserveSingletonRangeAndUnboundedNotation(string operation, string expected)
        {
            var parameter = new Feature { DeclaredName = "count" };
            var root = new Package { DeclaredName = "Assembly" };
            Own<OwningMembership>(root, parameter);
            var feature = new Feature();
            Own<OwningMembership>(root, feature);
            var upper = Binary(operation, Reference(parameter), new LiteralInteger { Value = 1 });
            var range = new MultiplicityRange();
            Own<OwningMembership>(feature, range);
            Own<OwningMembership>(range, new LiteralInteger { Value = 0 });
            Own<OwningMembership>(range, upper);
            var text = ElementPresentation.GetMultiplicity(feature);
            Assert.That(text, Is.EqualTo($"[0..{expected}]"));

            var singleton = new Mock<IMultiplicityRange>();
            singleton.SetupGet(element => element.upperBound).Returns(upper);
            var otherFeature = new Mock<IFeature>();
            otherFeature.SetupGet(element => element.multiplicity).Returns(singleton.Object);
            Assert.That(ElementPresentation.GetMultiplicity(otherFeature.Object), Is.EqualTo($"[{expected}]"));
            singleton.SetupGet(element => element.lowerBound).Returns(upper);
            Assert.That(ElementPresentation.GetMultiplicity(otherFeature.Object), Is.EqualTo($"[{expected}]"));
            singleton.SetupGet(element => element.upperBound).Returns(new LiteralInfinity());
            Assert.That(ElementPresentation.GetMultiplicity(otherFeature.Object), Is.EqualTo($"[{expected}..*]"));
        }

        [Test]
        public void VerifyMalformedExpressionsAreOmittedRatherThanInvented()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ElementPresentation.GetExpressionText(new FeatureReferenceExpression()), Is.Null);
                Assert.That(ElementPresentation.GetExpressionText(new OperatorExpression { Operator = "+" }), Is.Null);
                Assert.That(ElementPresentation.GetExpressionText(new OperatorExpression { Operator = "[" }), Is.Null);
                var malformedConditional = Binary("+", new LiteralInteger { Value = 1 }, new LiteralInteger { Value = 2 });
                malformedConditional.Operator = "??";
                Assert.That(ElementPresentation.GetExpressionText(malformedConditional), Is.Null,
                    "The SDK's deferred-operand form must be complete; truncated notation must not be shown.");
            }
        }
    }
}
