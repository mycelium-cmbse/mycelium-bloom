// ------------------------------------------------------------------------------------------------
// <copyright file="ElementSemanticsTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System.Linq;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Exceptions;
    using SysML2.NET.Extensions;
    using static Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory;

    [TestFixture]
    public sealed class ElementSemanticsTestFixture
    {
        [Test]
        public void VerifyAnnotationBodiesRemainAvailable()
        {
            var comment = new SysML2.NET.Core.POCO.Root.Annotations.Comment { Body = "Model rationale", Locale = "en" };
            var snapshot = new DetailsPanelPresentation(comment);
            Assert.That(snapshot.Sections.Single(section => section.Title == "Annotation").Properties
                .Single(property => property.Key == "Body").Value, Is.EqualTo("Model rationale"));
            var representation = new SysML2.NET.Core.POCO.Root.Annotations.TextualRepresentation { Body = "authored notation", Language = "sysml" };
            Assert.That(new DetailsPanelPresentation(representation).Sections.Single(section => section.Title == "Annotation").Properties
                .Single(property => property.Key == "Body").Value, Is.EqualTo("authored notation"));
        }

        [Test]
        public void VerifyShortAndEffectiveNameProvenance()
        {
            var shortOnly = new AttributeUsage { DeclaredShortName = "speed" };
            Assert.That(new DetailsPanelPresentation(shortOnly).DisplayName, Is.EqualTo("speed"));
            var general = new Feature { DeclaredName = "dimensions", DeclaredShortName = "dim" };
            var specific = new Feature();
            specific.AssignOwnership(new Redefinition { RedefiningFeature = specific, RedefinedFeature = general });
            var snapshot = new DetailsPanelPresentation(specific);
            var identity = snapshot.Sections.Single(section => section.Title == "Identity").Properties.ToDictionary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.DisplayName, Is.EqualTo(specific.EffectiveName()));
                Assert.That(identity["Effective name"], Is.EqualTo("dimensions"));
                Assert.That(identity["Effective short name"], Is.EqualTo("dim"));
                Assert.That(identity["Name source"], Does.Contain("SDK naming feature"));
                Assert.That(snapshot.ModelLinks.Single(link => link.Role == "Naming feature").Element, Is.SameAs(general));
                Assert.That(specific.DeclaredName, Is.Null);
                Assert.That(specific.DeclaredShortName, Is.Null);
            }
        }

        [Test]
        public void VerifyUnresolvedNameDoesNotBecomeAnonymous()
        {
            var feature = new Mock<IFeature>();
            feature.SetupGet(element => element.name).Throws(new IncompleteModelException("Missing naming feature"));
            var snapshot = new DetailsPanelPresentation(feature.Object);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.DisplayName, Does.Contain("name unresolved"));
                Assert.That(snapshot.Sections.Single(section => section.Title == "Identity").Properties
                    .Single(property => property.Key == "Name source").Value, Is.EqualTo("Unresolved name"));
            }
        }

        [Test]
        public void VerifyQuantitiesRedefinitionsAndValueNotation()
        {
            var elements = Descendants(LoadQuantities()).OfType<IFeature>().ToArray();
            var dimensions = elements.Single(element => element.name == "dimensions");
            var order = elements.Single(element => element.name == "order");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(dimensions.DeclaredName, Is.Null);
                Assert.That(dimensions.NamingFeature().name, Is.EqualTo("dimensions"));
                Assert.That(dimensions.ownedRedefinition.Single().RedefinedFeature, Is.SameAs(dimensions.NamingFeature()));
                Assert.That(new DetailsPanelPresentation(dimensions).Sections.Single(section => section.Title == "Value")
                    .Properties.Single().Value, Is.EqualTo("Local binding: mRef.dimensions"));
                Assert.That(order.DeclaredName, Is.EqualTo("order"));
                Assert.That(order.ownedRedefinition.Single().RedefinedFeature.name, Is.EqualTo("rank"));
                Assert.That(ElementPresentation.GetDisplayName(order), Is.EqualTo("order"));
            }
        }

        [TestCase(false, false, "Local binding")]
        [TestCase(false, true, "Default value")]
        [TestCase(true, false, "Initial value")]
        [TestCase(true, true, "Initial default")]
        public void VerifyValueOrigin(bool initial, bool defaultValue, string expected)
        {
            var attribute = new AttributeUsage();
            var value = Attach<FeatureValue>(attribute, new LiteralString { Value = "" });
            value.IsInitial = initial;
            value.IsDefault = defaultValue;
            Assert.That(new DetailsPanelPresentation(attribute).Sections.Single(section => section.Title == "Value")
                .Properties.Single().Value, Is.EqualTo($"{expected}: Empty string"));
        }

        [Test]
        public void VerifyNonliteralMultiplicityRetainsCanonicalInspectionTrail()
        {
            var feature = new Feature { DeclaredName = "items" };
            var range = new MultiplicityRange();
            var bound = new FeatureReferenceExpression();
            var referenced = new Feature { DeclaredName = "capacity" };
            Attach<OwningMembership>(feature, range);
            Attach<OwningMembership>(range, bound);
            bound.AssignOwnership(new Membership { MemberElement = referenced });
            Assert.That(ElementPresentation.GetMultiplicity(feature), Is.EqualTo("[capacity]"));
            var selection = new ContextAwareService { SelectedElement = feature };
            using var panel = new DetailsPanelViewModel(selection);
            var link = panel.Presentation.ModelLinks.Single(candidate => candidate.Role == "Multiplicity");
            panel.Inspect(link.Element);
            Assert.That(selection.SelectedElement, Is.SameAs(range));
            panel.Inspect(panel.Presentation.ModelLinks.Single(candidate => candidate.Role == "Upper bound").Element);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(bound));
                Assert.That(panel.Presentation.Expressions.Single().Reference, Is.SameAs(referenced));
            }
            panel.GoBack();
            Assert.That(selection.SelectedElement, Is.SameAs(range));
            panel.GoBack();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(feature));
                Assert.That(feature.ownedElement.Single(), Is.SameAs(range));
            }
        }
    }
}
