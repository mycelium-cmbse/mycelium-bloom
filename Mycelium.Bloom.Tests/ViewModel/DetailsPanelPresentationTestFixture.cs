// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelPresentationTestFixture.cs" company="Starion Group S.A.">
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

    using Mycelium.Bloom.ViewModel.DetailsPanel;

    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.Associations;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Flows;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Exceptions;

    using static Mycelium.Bloom.Tests.Common.ProjectBrowserNodeTestFactory;

    [TestFixture]
    public sealed class DetailsPanelPresentationTestFixture
    {
        private static readonly string[] ExpectedDocumentation = ["Retained documentation"];
        [Test]
        public void VerifyAttributesPreserveUnsetUnresolvedUnsupportedAndMultipleValues()
        {
            var unset = new AttributeUsage { DeclaredName = "unset" };
            var missing = new Mock<IAttributeUsage>();
            missing.SetupGet(element => element.DeclaredName).Returns("missing");
            missing.SetupGet(element => element.OwnedRelationship).Returns([new FeatureValue()]);
            var unsupported = new AttributeUsage { DeclaredName = "expression" };
            var multiple = new AttributeUsage { DeclaredName = "multiple" };
            var empty = new AttributeUsage { DeclaredName = "empty" };
            Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory.Attach<FeatureValue>(unsupported, new LiteralExpression());
            Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory.Attach<FeatureValue>(multiple, new LiteralInteger { Value = 2 }, new LiteralInteger { Value = 3 });
            Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory.Attach<FeatureValue>(empty, new LiteralString { Value = "" });
            var parent = new Mock<IPartUsage>();
            parent.SetupGet(element => element.ownedElement).Returns([unset, missing.Object, unsupported, multiple, empty]);
            var values = new DetailsPanelPresentation(parent.Object).Sections.Single(section => section.Title == "Attributes")
                .Properties.ToDictionary(property => property.Key, property => property.Value);
            Assert.That(values, Has.Count.EqualTo(5));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(values["unset"], Is.EqualTo("No local value; inherited/default values not resolved"));
                Assert.That(values["missing"], Is.EqualTo("Unresolved value"));
                Assert.That(values["expression"], Does.Contain("SDK produced no textual notation"));
                Assert.That(values["multiple"], Is.EqualTo("Multiple values (2): 1: Local binding: 2; 2: Local binding: 3"));
                Assert.That(values["empty"], Is.EqualTo("Local binding: Empty string"));
            }
        }

        [Test]
        public void VerifyIncompletePropertiesPreserveValidSiblingsAndUnrelatedErrors()
        {
            var selected = new Mock<IPartUsage>();
            selected.SetupGet(element => element.DeclaredName).Returns("Retained");
            selected.SetupGet(element => element.qualifiedName).Throws(new IncompleteModelException("Missing owner"));
            selected.SetupGet(element => element.type).Throws(new IncompleteModelException("Missing type"));
            selected.SetupGet(element => element.multiplicity).Throws(new IncompleteModelException("Missing bound"));
            var value = new Mock<IFeatureValue>();
            value.SetupGet(element => element.value).Throws(new IncompleteModelException("Missing value"));
            var attribute = new Mock<IAttributeUsage>();
            attribute.SetupGet(element => element.DeclaredName).Returns("mass");
            attribute.SetupGet(element => element.OwnedRelationship).Returns([value.Object]);
            var brokenMembership = new Mock<IMembership>();
            brokenMembership.SetupGet(element => element.ownedElement).Throws(new IncompleteModelException("Missing member"));
            selected.SetupGet(element => element.ownedElement).Returns([brokenMembership.Object, attribute.Object]);
            selected.SetupGet(element => element.documentation).Returns([new Documentation { Body = "Retained documentation" }]);
            var result = new DetailsPanelPresentation(selected.Object);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DisplayName, Is.EqualTo("Retained"));
                Assert.That(result.Documentation, Is.EqualTo(ExpectedDocumentation));
                Assert.That(result.Sections.Single(section => section.Title == "Identity").Properties
                    .Single(property => property.Key == "Qualified name").Value, Does.Contain("incomplete"));
                Assert.That(result.Sections.Single(section => section.Title == "Attributes").Properties
                    .Single(property => property.Key == "mass").Value, Is.EqualTo("Unresolved value"));
                Assert.That(result.Sections.Single(section => section.Title == "Classification").Properties
                    .Single(property => property.Key == "Typed by").Value, Does.Contain("incomplete"));
            }
            selected.SetupGet(element => element.qualifiedName).Throws(new System.InvalidOperationException("Unexpected"));
            Assert.That(() => new DetailsPanelPresentation(selected.Object), Throws.TypeOf<System.InvalidOperationException>());
        }

        [TestCase(TypeArgs = [typeof(IAssociation)])]
        [TestCase(TypeArgs = [typeof(IConnectionUsage)])]
        [TestCase(TypeArgs = [typeof(IFlowUsage)])]
        public void VerifyAttributesDoNotLeakThroughNestedNavigableRelationships<TElement>()
            where TElement : class, IElement
        {
            var value = new Mock<IFeatureValue>();
            value.SetupGet(featureValue => featureValue.value).Returns(new LiteralInteger { Value = 5 });
            var attribute = new Mock<IAttributeUsage>();
            attribute.SetupGet(element => element.DeclaredName).Returns("rating");
            attribute.SetupGet(element => element.OwnedRelationship).Returns([value.Object]);
            var nested = new Mock<TElement>();
            nested.SetupGet(element => element.ownedElement).Returns([attribute.Object]);
            var hidden = new Mock<IMembership>();
            hidden.SetupGet(element => element.ownedElement).Returns([nested.Object]);
            var parent = new Mock<IPartUsage>();
            parent.SetupGet(element => element.ownedElement).Returns([hidden.Object]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(new DetailsPanelPresentation(parent.Object).Sections.Select(section => section.Title),
                    Does.Not.Contain("Attributes"));
                Assert.That(new DetailsPanelPresentation(nested.Object).Sections
                    .Single(section => section.Title == "Attributes").Properties[0].Value, Is.EqualTo("Local binding: 5"));
            }

            hidden.SetupGet(element => element.ownedElement).Returns([attribute.Object]);
            Assert.That(new DetailsPanelPresentation(parent.Object).Sections
                .Single(section => section.Title == "Attributes").Properties[0].Value, Is.EqualTo("Local binding: 5"));
        }

        [Test]
        public void VerifyClassificationUsesResolvedSdkProperties()
        {
            var definition = new PartDefinition { DeclaredName = "HarnessDef" };
            var part = new Mock<IPartUsage>();
            part.SetupGet(element => element.DeclaredName).Returns("Harness");
            part.SetupGet(element => element.type).Returns([definition]);
            part.SetupGet(element => element.IsComposite).Returns(true);
            part.SetupGet(element => element.IsVariation).Returns(true);
            part.SetupGet(element => element.owningMembership).Returns(new OwningMembership());
            var multiplicity = CreateElement<MultiplicityRange>("range", null, new LiteralInteger { Value = 2 });
            part.SetupGet(element => element.multiplicity).Returns(multiplicity);
            var presentation = new DetailsPanelPresentation(part.Object);
            var classification = presentation.Sections.Single(section => section.Title == "Classification")
                .Properties.ToDictionary(property => property.Key, property => property.Value);
            var identity = presentation.Sections.Single(section => section.Title == "Identity")
                .Properties.ToDictionary(property => property.Key, property => property.Value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(classification["Typed by"], Is.EqualTo("HarnessDef"));
                Assert.That(classification["Multiplicity"], Is.EqualTo("[2]"));
                Assert.That(classification["Composition"], Is.EqualTo("composite"));
                Assert.That(presentation.AdvancedProperties.Single(property => property.Key == "Is variation").Value, Is.EqualTo("true"));
                Assert.That(classification.Keys, Does.Not.Contain("Is variation"));
                Assert.That(identity["Visibility"], Is.EqualTo(part.Object.owningMembership.Visibility.ToString().ToLowerInvariant()));
                Assert.That(classification.Keys, Does.Not.Contain("Is read only"));
            }
        }

        [Test]
        public void VerifyAttributeValuesRetainUnsupportedExpressions()
        {
            var featureValue = new Mock<IFeatureValue>();
            featureValue.SetupGet(value => value.value).Returns(new LiteralRational { Value = 12.5 });
            var attribute = new Mock<IAttributeUsage>();
            attribute.SetupGet(element => element.DeclaredName).Returns("mass");
            attribute.SetupGet(element => element.OwnedRelationship).Returns([featureValue.Object]);
            var part = new Mock<IPartUsage>();
            part.SetupGet(element => element.ownedElement).Returns([attribute.Object]);
            var presentation = new DetailsPanelPresentation(part.Object);
            var attributes = presentation.Sections.Single(section => section.Title == "Attributes").Properties;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attributes, Has.Count.EqualTo(1));
                Assert.That(attributes[0].Key, Is.EqualTo("mass"));
                Assert.That(attributes[0].Value, Is.EqualTo("Local binding: 12.5"));
                Assert.That(new DetailsPanelPresentation(attribute.Object).Sections
                    .Single(section => section.Title == "Value").Properties[0].Value, Is.EqualTo("Local binding: 12.5"));
            }

            featureValue.SetupGet(value => value.value).Returns(new LiteralExpression());
            using (Assert.EnterMultipleScope())
            {
                Assert.That(new DetailsPanelPresentation(part.Object).Sections.Single(section => section.Title == "Attributes").Properties[0].Value,
                    Does.Contain("SDK produced no textual notation"));
                Assert.That(new DetailsPanelPresentation(attribute.Object).Sections.Single(section => section.Title == "Value").Properties[0].Value,
                    Does.Contain("SDK produced no textual notation"));
            }
        }

        [Test]
        public void VerifyDocumentationUsesCanonicalBodiesWithoutMutation()
        {
            var documentation = new Documentation { Body = "First line\nSecond line" };
            var part = CreateElement<PartUsage>("part", "Part", documentation);
            var presentation = new DetailsPanelPresentation(part);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.Documentation, Is.EqualTo(new[] { documentation.Body }));
                Assert.That(part.ownedElement.Single(), Is.SameAs(documentation));
                Assert.That(new DetailsPanelPresentation(documentation).Documentation,
                    Is.EqualTo(presentation.Documentation));
            }
        }
    }
}
