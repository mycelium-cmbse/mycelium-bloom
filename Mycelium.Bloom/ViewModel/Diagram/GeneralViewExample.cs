// ------------------------------------------------------------------------------------------------
// <copyright file="GeneralViewExample.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.Collections.Immutable;

    using Mycelium.Bloom.Model.Diagram;

    using SysML2.NET.Core.POCO.Core.Classifiers;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Dependencies;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.Items;
    using SysML2.NET.Core.POCO.Systems.Parts;
    using SysML2.NET.Core.POCO.Systems.Ports;
    using SysML2.NET.Core.POCO.Systems.Views;
    using SysML2.NET.Extensions;

    /// <summary>An explicit in-memory example made of canonical SDK objects, with no persistence claim.</summary>
    public static class GeneralViewExample
    {
        /// <summary>Creates the representative session-only heterogeneous SDK model and its presentation.</summary>
        /// <returns>The immutable projected view and its omission state.</returns>
        public static GeneralViewProjection Create()
        {
            var package = new Package { Id = Guid.NewGuid(), ElementId = "example-package", DeclaredName = "Vehicle" };
            var vehicleDefinition = new PartDefinition { Id = Guid.NewGuid(), ElementId = "example-vehicle-definition", DeclaredName = "VehicleType" };
            var definition = new PartDefinition { Id = Guid.NewGuid(), ElementId = "example-definition", DeclaredName = "Engine" };
            var part = new PartUsage { Id = Guid.NewGuid(), ElementId = "example-part", DeclaredName = "engine", IsComposite = true };
            var item = new ItemUsage { Id = Guid.NewGuid(), ElementId = "example-item", DeclaredName = "fuel", IsComposite = false };
            var attribute = new AttributeUsage { Id = Guid.NewGuid(), ElementId = "example-attribute", DeclaredName = "heaters" };
            var attributeDefinition = new AttributeDefinition { Id = Guid.NewGuid(), ElementId = "example-attribute-definition", DeclaredName = "Count" };
            var portDefinition = new PortDefinition { Id = Guid.NewGuid(), ElementId = "example-port-definition", DeclaredName = "PowerPort" };
            var port = new PortUsage { Id = Guid.NewGuid(), ElementId = "example-port", DeclaredName = "powerIn", Direction = SysML2.NET.Core.Core.Types.FeatureDirectionKind.In };
            var comment = new Comment { Id = Guid.NewGuid(), ElementId = "example-comment", Body = "Session example: mixed General View notation." };
            Own(package, new OwningMembership(), definition);
            Own(package, new OwningMembership(), vehicleDefinition);
            Own(vehicleDefinition, new FeatureMembership(), part);
            Own(vehicleDefinition, new FeatureMembership(), item);
            Own(package, new OwningMembership(), attributeDefinition);
            Own(package, new OwningMembership(), portDefinition);
            Own(package, new OwningMembership(), comment);
            Own(part, new FeatureMembership(), attribute);
            Own(part, new FeatureMembership(), new AttributeUsage { Id = Guid.NewGuid(), ElementId = "example-hidden-attribute", DeclaredName = "coolant" });
            Own(part, new FeatureMembership(), port);
            Own(attribute, new FeatureValue(), new LiteralInteger { Id = Guid.NewGuid(), Value = 8 });
            Own(part, new FeatureTyping { TypedFeature = part, Type = definition });
            Own(attribute, new FeatureTyping { TypedFeature = attribute, Type = attributeDefinition });
            Own(port, new FeatureTyping { TypedFeature = port, Type = portDefinition });
            Own(comment, new Annotation { AnnotatedElement = part });
            var dependency = new Dependency { Id = Guid.NewGuid(), ElementId = "example-dependency", DeclaredName = "requires" };
            dependency.Client.Add(item);
            dependency.Supplier.Add(part);
            Own(package, dependency);
            var backupDefinition = new PartDefinition { Id = Guid.NewGuid(), ElementId = "example-backup-definition", DeclaredName = "BackupEngine" };
            var backup = new PartUsage { Id = Guid.NewGuid(), ElementId = "example-backup", DeclaredName = "reserve", IsComposite = true };
            var counter = new AttributeUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "example-counter",
                DeclaredName = "heaterCount",
                Direction = SysML2.NET.Core.Core.Types.FeatureDirectionKind.In
            };
            var action = new ActionUsage { Id = Guid.NewGuid(), ElementId = "example-action", DeclaredName = "control", IsComposite = true };
            var view = new ViewUsage { Id = Guid.NewGuid(), ElementId = "example-view", DeclaredName = "context", IsComposite = true };
            var output = new PortUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "example-output",
                DeclaredName = "powerOut",
                Direction = SysML2.NET.Core.Core.Types.FeatureDirectionKind.Out
            };
            var sparePort = new PortUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "example-spare-port",
                DeclaredName = "powerIn",
                Direction = SysML2.NET.Core.Core.Types.FeatureDirectionKind.In
            };
            var top = new AttributeUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "example-top",
                DeclaredName = "command",
                Direction = SysML2.NET.Core.Core.Types.FeatureDirectionKind.In
            };
            var bottom = new AttributeUsage
            {
                Id = Guid.NewGuid(),
                ElementId = "example-bottom",
                DeclaredName = "status",
                Direction = SysML2.NET.Core.Core.Types.FeatureDirectionKind.Out
            };
            Own(package, new OwningMembership(), backupDefinition);
            Own(package, new OwningMembership(), backup);
            Own(package, new OwningMembership(), action);
            Own(package, new OwningMembership(), view);
            Own(part, new FeatureMembership(), output);
            Own(action, new ParameterMembership(), top);
            Own(action, new ParameterMembership(), bottom);
            Own(backup, new FeatureMembership(), sparePort);
            Own(backup, new FeatureMembership(), counter);
            Own(backup, new FeatureTyping { TypedFeature = backup, Type = backupDefinition });
            Own(output, new FeatureTyping { TypedFeature = output, Type = portDefinition });
            Own(sparePort, new FeatureTyping { TypedFeature = sparePort, Type = portDefinition });
            Own(top, new FeatureTyping { TypedFeature = top, Type = attributeDefinition });
            Own(bottom, new FeatureTyping { TypedFeature = bottom, Type = attributeDefinition });
            var classification = new Subclassification
            {
                Id = Guid.NewGuid(),
                ElementId = "example-subclassification",
                Subclassifier = backupDefinition,
                Superclassifier = definition
            };
            var subset = new Subsetting { Id = Guid.NewGuid(), ElementId = "example-subsetting", SubsettingFeature = backup, SubsettedFeature = part };
            var redefine = new Redefinition { Id = Guid.NewGuid(), ElementId = "example-redefinition", RedefiningFeature = counter, RedefinedFeature = attribute };
            var alias = new Membership { Id = Guid.NewGuid(), MemberElement = definition, MemberName = "PowerUnit" };
            Own(package, alias);
            var connectionType = new ConnectionDefinition { Id = Guid.NewGuid(), ElementId = "example-connection-type", DeclaredName = "PowerBus" };
            Own(package, new OwningMembership(), connectionType);
            var connection = new ConnectionUsage { Id = Guid.NewGuid(), ElementId = "example-connection", DeclaredName = "supply" };
            Own(package, new OwningMembership(), connection);
            Own(connection, new FeatureTyping { TypedFeature = connection, Type = connectionType });
            var binding = new BindingConnectorAsUsage { Id = Guid.NewGuid(), ElementId = "example-binding" };
            Own(package, new OwningMembership(), binding);
            AddEnd(connection, output);
            AddEnd(connection, sparePort);
            AddEnd(binding, top);
            AddEnd(binding, counter);
            var projection = GeneralViewProjector.Create([package, vehicleDefinition, definition, backupDefinition, part, backup, action, item, attributeDefinition,
                portDefinition, connectionType, comment, view, attribute, dependency, classification, subset, redefine, connection, binding], "VehicleGeneralView", true);
            var placements = new Dictionary<string, (double X, double Y)>
            {
                ["example-package"] = (40, 40),
                ["example-vehicle-definition"] = (40, 350),
                ["example-definition"] = (400, 40),
                ["example-backup-definition"] = (800, 40),
                ["example-port-definition"] = (1200, 40),
                ["example-part"] = (220, 390),
                ["example-backup"] = (800, 390),
                ["example-action"] = (1200, 390),
                ["example-item"] = (400, 820),
                ["example-attribute-definition"] = (40, 820),
                ["example-attribute"] = (400, 660),
                ["example-connection-type"] = (40, 650),
                ["example-comment"] = (800, 820),
                ["example-view"] = (1200, 820)
            };
            var nodes = LayoutNodes(projection, placements, vehicleDefinition, view, attribute, part, item);
            var edges = projection.Edges.Select(edge => edge with
            {
                Bends = edge.Kind switch
                {
                    DiagramRelationshipKind.NoncompositeMembership => [new(10, 420), new(10, 1120), new(520, 1120)],
                    DiagramRelationshipKind.Dependency => [new(680, 910), new(680, 300), new(340, 300)],
                    DiagramRelationshipKind.Subsetting => [new(920, 730), new(340, 730)],
                    DiagramRelationshipKind.Redefinition => [new(740, 600), new(520, 600)],
                    DiagramRelationshipKind.Binding => [new(1120, 330), new(740, 330), new(740, 534)],
                    _ => edge.Bends
                }
            }).ToImmutableArray();

            return projection with { Nodes = nodes, Edges = edges };

        }

        /// <summary>Applies authored example placements and slot contents to the canonical projection.</summary>
        /// <param name="projection">The immutable projected notation.</param>
        /// <param name="placements">The authored example coordinates keyed by example element identity.</param>
        /// <param name="vehicleDefinition">The canonical example definition shown with compact geometry.</param>
        /// <param name="view">The canonical example view containing nested graphical content.</param>
        /// <param name="attribute">The canonical example attribute shown as a compact standalone occurrence.</param>
        /// <param name="part">The canonical example part with a truncated attribute compartment.</param>
        /// <param name="item">The canonical example item repeated in a nested view.</param>
        /// <returns>The example occurrences retaining the original canonical references.</returns>
        private static ImmutableArray<DiagramNodePresentation> LayoutNodes(GeneralViewProjection projection, Dictionary<string, (double X, double Y)> placements,
            PartDefinition vehicleDefinition, ViewUsage view, AttributeUsage attribute,
            PartUsage part, ItemUsage item)
        {
            var nodes = projection.Nodes.Select(node =>
            {
                var position = placements[node.Occurrence.Element.ElementId];
                var boundary = node.Boundary.Select(feature => feature with
                {
                    Side = feature.Element.ElementId switch
                    {
                        "example-port" => BoundarySide.Left,
                        "example-output" => BoundarySide.Right,
                        "example-top" => BoundarySide.Top,
                        "example-bottom" => BoundarySide.Bottom,
                        _ => BoundarySide.Left
                    },
                    Offset = feature.Element.ElementId == "example-counter" ? 0.8 : 0.5
                }).ToImmutableArray();
                var element = node.Occurrence.Element;
                var height = element switch
                {
                    _ when ReferenceEquals(element, vehicleDefinition) => 140,
                    _ when ReferenceEquals(element, view) => 260,
                    _ when ReferenceEquals(element, attribute) => 120,
                    ConnectionDefinition => 120,
                    _ => 180
                };

                return node with
                {
                    Occurrence = node.Occurrence with { X = position.X, Y = position.Y, Width = ReferenceEquals(element, vehicleDefinition) ? 160 : 240, Height = height },
                    Boundary = boundary,
                    Compartments = node.Occurrence.Element == part ? [node.Compartments[0] with { Rows = node.Compartments[0].Rows.Take(1).ToImmutableArray(),
                        IsTruncated = true }] : node.Compartments,
                    NestedView = node.Occurrence.Element == view ? GeneralViewProjector.Create([item], "FuelContext") : null
                };
            }).ToImmutableArray();

            return nodes;
        }

        /// <summary>Authors one canonical example connector end and its multiplicity.</summary>
        /// <param name="connector">The canonical connector being authored for this example.</param>
        /// <param name="target">The canonical target element or target occurrence adapter.</param>
        private static void AddEnd(IElement connector, IFeature target)
        {
            var end = new Feature
            {
                Id = Guid.NewGuid(),
                IsEnd = true,
                IsOrdered = target.DeclaredName == "powerOut",
                DeclaredName = target.DeclaredName switch { "powerOut" => "source", "powerIn" => "sink", _ => null }
            };
            Own(connector, new EndFeatureMembership(), end);
            Own(end, new ReferenceSubsetting { ReferencedFeature = target });
            var range = new MultiplicityRange { Id = Guid.NewGuid() };
            Own(end, new OwningMembership(), range);
            Own(range, new OwningMembership(), new LiteralInteger { Id = Guid.NewGuid(), Value = 1 });
        }

        /// <summary>Assigns ownership only while constructing the explicit example model.</summary>
        /// <param name="owner">The caller-owned presentation authority or example element being authored.</param>
        /// <param name="relationship">The canonical SDK relationship.</param>
        /// <param name="child">The optional canonical example member.</param>
        private static void Own(IElement owner, IRelationship relationship, IElement child = null)
        {
            relationship.Id = Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(relationship.ElementId))
            {
                relationship.ElementId = relationship.Id.ToString();
            }

            if (child is null)
            {
                owner.AssignOwnership(relationship);
            }
            else
            {
                owner.AssignOwnership(relationship, child);
            }
        }
    }
}
