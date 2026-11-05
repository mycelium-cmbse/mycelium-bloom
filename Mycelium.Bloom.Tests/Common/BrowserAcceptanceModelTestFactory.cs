// ------------------------------------------------------------------------------------------------
// <copyright file="BrowserAcceptanceModelTestFactory.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System;

    using SysML2.NET.Core.Core.Types;
    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Core.Types;
    using SysML2.NET.Core.POCO.Kernel.Behaviors;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Multiplicities;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Systems.Actions;
    using SysML2.NET.Core.POCO.Systems.Attributes;
    using SysML2.NET.Core.POCO.Systems.Connections;
    using SysML2.NET.Core.POCO.Systems.DefinitionAndUsage;
    using SysML2.NET.Core.POCO.Systems.Enumerations;
    using SysML2.NET.Core.POCO.Systems.States;
    using SysML2.NET.Core.POCO.Systems.VerificationCases;
    using SysML2.NET.Extensions;

    internal static class BrowserAcceptanceModelTestFactory
    {
        internal static (StateDefinition Owner, StateUsage First, StateUsage Initial, SuccessionAsUsage Succession) CreateStates()
        {
            var owner = new StateDefinition { ElementId = "states", DeclaredName = "ControllerStates" };
            var first = new StateUsage { ElementId = "standby", DeclaredName = "Standby" };
            var initial = new StateUsage { ElementId = "operating", DeclaredName = "Operating" };
            Own<FeatureMembership>(owner, first);
            Own<FeatureMembership>(owner, initial);
            var entry = new ActionUsage { ElementId = "entry", DeclaredName = "Initialize" };
            Own<StateSubactionMembership>(owner, entry).Kind = SysML2.NET.Core.Systems.States.StateSubactionKind.Entry;
            var succession = new SuccessionAsUsage { ElementId = "initial-succession" };
            Own<OwningMembership>(owner, succession);
            AddEnd(succession, entry);
            AddEnd(succession, initial);

            return (owner, first, initial, succession);
        }

        internal static (LibraryPackage Library, VerificationCaseUsage Verification) CreateVerification(string value)
        {
            var library = new LibraryPackage { DeclaredName = "VerificationCases", IsStandard = true };
            var verdict = new EnumerationDefinition { DeclaredName = "VerdictKind" };
            Own<OwningMembership>(library, verdict);
            var literal = new EnumerationUsage { ElementId = "verdict-" + value, DeclaredName = value };
            Own<VariantMembership>(verdict, literal);
            var verification = new VerificationCaseUsage { ElementId = "verification", DeclaredName = "MassVerification" };
            var root = new Namespace();
            Own<OwningMembership>(root, library);
            Own<OwningMembership>(root, verification);
            var result = new AttributeUsage { DeclaredName = "verdict", Direction = FeatureDirectionKind.Out };
            Own<ReturnParameterMembership>(verification, result);
            AddRelationship(result, new FeatureTyping { Type = verdict });
            Own<FeatureValue>(result, Reference(literal));

            return (library, verification);
        }

        internal static (LibraryPackage Library, AttributeUsage Attribute, OperatorExpression Value) CreateQuantity()
        {
            // SI.sysml declares attribute <kg> kilogram : MassUnit. Only that needed subset is reproduced here.
            var library = new LibraryPackage { DeclaredName = "SI", IsStandard = true };
            var unitType = new AttributeDefinition { DeclaredName = "MassUnit" };
            var unit = new AttributeUsage { DeclaredName = "kilogram", DeclaredShortName = "kg" };
            Own<OwningMembership>(library, unitType);
            Own<OwningMembership>(library, unit);
            AddRelationship(unit, new FeatureTyping { Type = unitType });
            var attribute = new AttributeUsage { ElementId = "mass", DeclaredName = "assembledEquipmentMassWithMargin" };
            var value = new OperatorExpression { Operator = "[" };
            Argument(value, new LiteralInteger { Value = 3 });
            Own<FeatureMembership>(value, Reference(unit));
            Own<FeatureValue>(attribute, value);

            return (library, attribute, value);
        }

        internal static FeatureReferenceExpression Reference(IFeature target)
        {
            var reference = new FeatureReferenceExpression();
            AddRelationship(reference, new Membership { MemberElement = target });

            return reference;
        }

        internal static OperatorExpression Binary(string symbol, IExpression left, IExpression right)
        {
            var expression = new OperatorExpression { Operator = symbol };
            Argument(expression, left);

            if (symbol is "??" or "or" or "and" or "implies")
            {
                // ArgumentExpressionValue is an owned expression reference, not an ordinary argument value.
                var deferred = new FeatureReferenceExpression();
                Own<FeatureMembership>(deferred, right);
                Argument(expression, deferred);
            }
            else
            {
                Argument(expression, right);
            }

            return expression;
        }

        private static void Argument(OperatorExpression owner, IExpression expression)
        {
            var parameter = new Feature { Direction = FeatureDirectionKind.In };
            Own<ParameterMembership>(owner, parameter);
            Own<FeatureValue>(parameter, expression);
        }

        private static void AddEnd(SuccessionAsUsage succession, IFeature target)
        {
            var end = new Feature { IsEnd = true };
            Own<EndFeatureMembership>(succession, end);
            AddRelationship(end, new ReferenceSubsetting { ReferencedFeature = target });
        }

        internal static TMembership Own<TMembership>(IElement owner, IElement child) where TMembership : IRelationship, new()
        {
            var membership = new TMembership();

            if (child.Id == Guid.Empty)
            {
                child.Id = Guid.NewGuid();
            }

            if (owner.Id == Guid.Empty)
            {
                owner.Id = Guid.NewGuid();
            }

            if (membership.Id == Guid.Empty)
            {
                membership.Id = Guid.NewGuid();
            }

            owner.AssignOwnership(membership, child);

            return membership;
        }

        internal static void AddRelationship(IElement owner, IRelationship relationship)
        {
            if (owner.Id == Guid.Empty)
            {
                owner.Id = Guid.NewGuid();
            }

            if (relationship.Id == Guid.Empty)
            {
                relationship.Id = Guid.NewGuid();
            }

            owner.AssignOwnership(relationship);
        }
    }
}
