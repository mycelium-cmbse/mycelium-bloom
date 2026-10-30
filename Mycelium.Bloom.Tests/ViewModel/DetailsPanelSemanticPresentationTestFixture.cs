// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelSemanticPresentationTestFixture.cs" company="Starion Group S.A.">
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
    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Metadata;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.Systems.States;
    using SysML2.NET.Exceptions;
    using static Mycelium.Bloom.Tests.Common.BrowserAcceptanceModelTestFactory;

    /// <summary>Verifies semantic Properties destinations against canonical SDK containment and references.</summary>
    [TestFixture]
    public sealed class DetailsPanelSemanticPresentationTestFixture
    {
        [Test]
        public void VerifyPartAndAttributeSectionOrderAndUnitValue()
        {
            var part = new PartUsage { DeclaredName = "Equipment", IsComposite = true };
            var quantity = CreateQuantity();
            Own<FeatureMembership>(part, quantity.Attribute);
            Own<OwningMembership>(part, new Documentation { Body = "A documented part." });
            Own<OwningMembership>(quantity.Attribute, new Documentation { Body = "Mass including margin." });
            var partView = new DetailsPanelPresentation(part);
            var attributeView = new DetailsPanelPresentation(quantity.Attribute);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(partView.ContentSections.Select(section => section.Title).Take(4),
                    Is.EqualTo(new[] { "Identity", "Attributes", "Documentation", "Classification" }));
                Assert.That(attributeView.ContentSections.Select(section => section.Title).Take(4),
                    Is.EqualTo(new[] { "Identity", "Documentation", "Value", "Classification" }));
                Assert.That(partView.ContentSections.Single(section => section.Title == "Attributes").Links.Single().Element,
                    Is.SameAs(quantity.Attribute));
                Assert.That(attributeView.Summary, Is.EqualTo("3[SI::kg]"));
                Assert.That(attributeView.ContentSections.Single(section => section.Title == "Value references").Links.Single(link => link.Role == "Reference").Element,
                    Is.SameAs(quantity.Library.ownedElement.OfType<AttributeUsage>().Single()));
            }
        }

        [Test]
        public void VerifyStateStructureActionsAndAllTransitionRoles()
        {
            var states = CreateStates();
            var transition = new TransitionUsage { DeclaredName = "Stop" };
            Own<FeatureMembership>(states.Owner, transition);
            AddRelationship(transition, new Membership { MemberElement = states.Initial });
            var succession = new SuccessionAsUsage();
            Own<FeatureMembership>(transition, succession);
            AddEnd(succession, states.Initial);
            AddEnd(succession, states.First);
            var trigger = new AcceptActionUsage { DeclaredName = "StopCommand" };
            Own<TransitionFeatureMembership>(transition, trigger).Kind = TransitionFeatureKind.Trigger;
            var guard = new LiteralBoolean { Value = true };
            Own<TransitionFeatureMembership>(transition, guard).Kind = TransitionFeatureKind.Guard;
            var effect = new ActionUsage { DeclaredName = "Shutdown" };
            Own<TransitionFeatureMembership>(transition, effect).Kind = TransitionFeatureKind.Effect;
            Own<TransitionFeatureMembership>(transition, new ActionUsage { DeclaredName = "Log" }).Kind = TransitionFeatureKind.Effect;
            var presentation = new DetailsPanelPresentation(states.Owner);
            var structure = presentation.ContentSections.Single(section => section.Title == "State structure");
            var roles = presentation.ContentSections.Single(section => section.Title == "Transitions").Links;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(structure.Links.Single(link => link.Role == "Initial state").Element, Is.SameAs(states.Initial));
                Assert.That(structure.Links.Where(link => link.Role == "Nested state").Select(link => link.Element),
                    Is.EqualTo(new[] { states.First, states.Initial }));
                Assert.That(presentation.ContentSections.Single(section => section.Title == "State behaviour").Links.Single().Element,
                    Is.SameAs(states.Owner.entryAction));
                Assert.That(roles.Single(link => link.Role == "Source").Element, Is.SameAs(states.Initial));
                Assert.That(roles.Single(link => link.Role == "Target").Element, Is.SameAs(states.First));
                Assert.That(roles.Single(link => link.Role == "Guard").Label, Is.EqualTo("true"));
                Assert.That(roles.Single(link => link.Role == "Trigger").Element, Is.SameAs(trigger));
                Assert.That(roles.Count(link => link.Role == "Effect"), Is.EqualTo(2));
                Assert.That(states.Owner.ownedElement, Does.Contain(transition));
            }
            states.Owner.IsParallel = true;
            Assert.That(new DetailsPanelPresentation(states.Owner).ContentSections.Single(section => section.Title == "State structure")
                .Links.Any(link => link.Role == "Initial state"), Is.False);
        }

        [Test]
        public void VerifyEndsRetainReferentMultiplicityAndDirection()
        {
            var connection = new ConnectionUsage { DeclaredName = "PowerLink" };
            var target = new Feature { DeclaredName = "powerIn" };
            var end = new Feature { IsEnd = true, Direction = FeatureDirectionKind.In };
            Own<EndFeatureMembership>(connection, end);
            AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = target });
            var range = new MultiplicityRange();
            Own<OwningMembership>(end, range);
            Own<OwningMembership>(range, new LiteralInteger { Value = 2 });
            var section = new DetailsPanelPresentation(connection).ContentSections.Single(item => item.Title == "Ends");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(section.Links.Single(link => link.Role == "Referent").Element, Is.SameAs(target));
                Assert.That(section.Links.Single(link => link.Role == "End").Element, Is.SameAs(end));
                Assert.That(section.Properties.Single(field => field.Key == "Multiplicity").Value, Is.EqualTo("[2]"));
                Assert.That(section.Properties.Single(field => field.Key == "Direction").Value, Is.EqualTo("in"));
            }
        }

        [Test]
        public void VerifyAppliedMetadataCommentsAndRepresentationUseTheirSemanticDestinations()
        {
            var part = new PartUsage();
            var comment = new Comment { Body = "<script>plain model text</script>", Locale = "en" };
            Own<Annotation>(part, comment);
            Own<OwningMembership>(part, new TextualRepresentation { Body = "part equipment;", Language = "sysml" });
            var metadata = new MetadataUsage { DeclaredName = "risk" };
            var definition = new MetadataDefinition { DeclaredName = "RiskLevel" };
            AddRelationship(metadata, new FeatureTyping { Type = definition });
            Own<Annotation>(part, metadata);
            var risk = new AttributeUsage { DeclaredName = "risk" };
            Own<FeatureMembership>(metadata, risk);
            Own<FeatureValue>(risk, new LiteralString { Value = "medium" });
            var sections = new DetailsPanelPresentation(part).ContentSections;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(sections.Single(section => section.Title == "Comments").Tab, Is.EqualTo("Comments"));
                Assert.That(sections.Single(section => section.Title == "Comments").Paragraphs.Single(), Is.EqualTo(comment.Body));
                Assert.That(sections.Single(section => section.Title == "Textual representation").Properties.Single().Value, Is.EqualTo("sysml"));
                Assert.That(sections.Single(section => section.Title == "Metadata").Links.Single(link => link.Role == "Definition").Element,
                    Is.SameAs(definition));
                Assert.That(sections.Single(section => section.Title == "Metadata").Properties.Single().Value, Does.Contain("medium"));
            }
        }

        [Test]
        public void VerifyImportsAndSpecializationsPreserveCanonicalTargetsAndFlags()
        {
            var owner = new PartDefinition { DeclaredName = "Specialized" };
            var imported = new Namespace { DeclaredName = "Library" };
            var general = new PartDefinition { DeclaredName = "Base" };
            var root = new Namespace { DeclaredName = "Model" };
            Own<OwningMembership>(root, general);
            Own<OwningMembership>(root, owner);
            AddRelationship(owner, new NamespaceImport
            {
                ImportedNamespace = imported,
                IsRecursive = true,
                IsImportAll = true,
                Visibility = SysML2.NET.Core.Root.Namespaces.VisibilityKind.Private
            });
            AddRelationship(owner, new Specialization { General = general, Specific = owner });
            var sections = new DetailsPanelPresentation(owner).ContentSections;
            var import = sections.Single(section => section.Title == "Imports");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(import.Tab, Is.EqualTo("Relations"));
                Assert.That(import.Links.Single().Element, Is.SameAs(imported));
                Assert.That(import.Properties.Select(field => field.Value), Is.EqualTo(new[] { "Namespace import", "true", "true", "private" }));
                Assert.That(sections.Single(section => section.Title == "Specializations / Type hierarchy").Links.Single().Element, Is.SameAs(general));
                Assert.That(new DetailsPanelPresentation(general).ContentSections.Single(section => section.Title == "Specializations / Type hierarchy")
                    .Links.Single(link => link.Role == "Specialized by").Element, Is.SameAs(owner));
            }
        }

        [Test]
        public void VerifyIncompleteTransitionRoleDoesNotHideValidSiblings()
        {
            var transition = new Mock<ITransitionUsage>();
            transition.SetupGet(element => element.DeclaredName).Returns("Transition");
            transition.SetupGet(element => element.triggerAction).Throws(new IncompleteModelException("Missing trigger"));
            var effect = new ActionUsage { DeclaredName = "Retained" };
            transition.SetupGet(element => element.effectAction).Returns([effect]);
            var roles = new DetailsPanelPresentation(transition.Object).ContentSections.Single(section => section.Title == "Transitions").Links;
            Assert.That(roles.Single(link => link.Role == "Trigger").Label, Does.Contain("incomplete"));
            Assert.That(roles.Single(link => link.Role == "Effect").Element, Is.SameAs(effect));
        }

        [Test]
        public void VerifyStateCardsPreserveInitialParallelAndActionTargets()
        {
            var states = CreateStates();
            var action = new ActionUsage { DeclaredName = "Maintain" };
            Own<StateSubactionMembership>(states.Initial, action).Kind = StateSubactionKind.Do;
            var region = new StateUsage { DeclaredName = "Housekeeping", IsParallel = true };
            Own<FeatureMembership>(states.Owner, region);
            var nested = new StateUsage { DeclaredName = "Thermal" };
            Own<FeatureMembership>(region, nested);
            var presentation = new DetailsPanelPresentation(states.Owner);
            var cards = presentation.DisplaySections.Single(section => section.Title == "State usages").Cards;
            var initial = cards.Single(card => ReferenceEquals(card.Heading.Element, states.Initial));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.ShowSelectionSummary, Is.False);
                Assert.That(presentation.DisplaySections.Where(section => section.Tab == "Properties").Take(3).Select(section => section.Title),
                    Is.EqualTo(new[] { "Identity", "State machine", "State usages" }));
                Assert.That(initial.Badge, Is.EqualTo("initial"));
                Assert.That(initial.Links.Single(link => link.Role == "Do").Element, Is.SameAs(action));
                Assert.That(presentation.DisplaySections.Single(section => section.Title == "Parallel region · Housekeeping")
                    .Cards.Single().Heading.Element, Is.SameAs(nested));
            }
        }

        [Test]
        public void VerifyUnboundedReferenceMatchesMultiplicityWithoutChangingCanonicalTarget()
        {
            var attribute = new AttributeUsage { DeclaredName = "values" };
            var range = new MultiplicityRange();
            Own<OwningMembership>(attribute, range);
            var infinity = new LiteralInfinity();
            Own<OwningMembership>(range, infinity);
            var presentation = new DetailsPanelPresentation(attribute);
            var bound = presentation.DisplaySections.Single(section => section.Title == "Multiplicity").Links.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.ContentSections.Single(section => section.Title == "Classification").Properties,
                    Does.Contain(new System.Collections.Generic.KeyValuePair<string, string>("Multiplicity", "[*]")));
                Assert.That(bound.Label, Is.EqualTo("*"));
                Assert.That(bound.Element, Is.SameAs(infinity));
            }
        }

        [Test]
        public void VerifyRelationRowsRetainTypingAndOwnedFeatureIdentity()
        {
            var part = new PartUsage { DeclaredName = "Harness" };
            var definition = new PartDefinition { DeclaredName = "HarnessDef" };
            AddRelationship(part, new FeatureTyping { Type = definition });
            var attribute = new AttributeUsage { DeclaredName = "mass" };
            Own<FeatureMembership>(part, attribute);
            var relations = new DetailsPanelPresentation(part).DisplaySections.Where(section => section.Tab == "Relations").ToArray();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(relations.Single(section => section.Title == "Typing").Links.Single().Element, Is.SameAs(definition));
                Assert.That(relations.Single(section => section.Title == "Owned features").Links.Single().Element, Is.SameAs(attribute));
                Assert.That(relations.SelectMany(section => section.Links).Count(link => ReferenceEquals(link.Element, definition)), Is.EqualTo(1));
            }
        }

        /// <summary>Adds an owned end feature to a succession and links it to the target through reference subsetting.</summary>
        /// <param name="succession">The succession that owns the new end through an end-feature membership.</param>
        /// <param name="target">The canonical feature referenced by the new end.</param>
        private static void AddEnd(SuccessionAsUsage succession, IFeature target)
        {
            var end = new Feature { IsEnd = true };
            Own<EndFeatureMembership>(succession, end);
            AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = target });
        }
    }
}
