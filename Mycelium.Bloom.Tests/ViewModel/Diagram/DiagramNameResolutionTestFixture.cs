// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramNameResolutionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System.Linq;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Diagram;

    using SysML2.NET.Core.POCO.Core.Features;
    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;

    [TestFixture]
    public sealed class DiagramNameResolutionTestFixture
    {
        [Test]
        public void VerifyBundledModelProjectionReportsUnavailableNamesWithoutMutatingCanonicalElements()
        {
            var environment = new Mock<IHostEnvironment>(MockBehavior.Strict);
            environment.SetupGet(value => value.ContentRootPath).Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            using var loggerFactory = LoggerFactory.Create(_ => { });
            var loader = new ModelLoaderService(environment.Object, new DeSerializer(loggerFactory), new Assembler(loggerFactory),
                loggerFactory.CreateLogger<ModelLoaderService>());
            var model = loader.LoadQuantitiesModel();
            var package = model.ownedElement.Single();
            var relationships = package.OwnedRelationship.ToArray();

            var projection = DiagramTestFactory.CreateProjector().CreateForRoot(model);
            var presentation = projection.Nodes.Single(node => ReferenceEquals(node.Occurrence.Element, package));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(projection.Nodes, Has.Length.EqualTo(2));
                Assert.That(presentation.Name.Declaration, Is.EqualTo("Notation unavailable (incomplete model)"));
                Assert.That(package.OwnedRelationship, Is.EqualTo(relationships));
                Assert.That(package.owningNamespace, Is.SameAs(model));
            }
        }

        [Test]
        public void VerifyRootProjectionUsesViewLocalAliasWithoutReparentingCanonicalPackage()
        {
            var root = new Namespace();
            var external = new Namespace { DeclaredName = "External" };
            var local = new Namespace { DeclaredName = "Local" };
            var target = new Package { DeclaredName = "ReferencedPackage" };
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, external);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, local);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(external, target);
            BrowserAcceptanceModelTestFactory.AddRelationship(local, new Membership { MemberName = "shortcut", MemberElement = target });

            var projection = DiagramTestFactory.CreateProjector().CreateForRoot(local);
            var presentation = projection.Nodes.Single(node => ReferenceEquals(node.Occurrence.Element, target));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(presentation.Name.Declaration, Is.EqualTo("shortcut"));
                Assert.That(presentation.Name.Aliases, Does.Contain("shortcut"));
                Assert.That(target.owningNamespace, Is.SameAs(external));
                Assert.That(local.Resolve(presentation.Name.Declaration)?.MemberElement, Is.SameAs(target));
            }
        }

        [TestCase("shortcut", "shortcut")]
        [TestCase("alias", "'alias'")]
        public void VerifyReferenceNameUsesAliasVisibleAtTheReferenceSite(string alias, string expected)
        {
            var root = new Namespace();
            var external = new Namespace { DeclaredName = "External" };
            var local = new Namespace { DeclaredName = "Local" };
            var target = new Feature { DeclaredName = "LongTargetName" };
            var site = new Feature { DeclaredName = "site" };
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, external);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, local);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(external, target);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(local, site);
            BrowserAcceptanceModelTestFactory.AddRelationship(local, new Membership { MemberName = alias, MemberElement = target });

            var name = DiagramNotation.ReferenceName(target, site);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(name, Is.EqualTo(expected));
                Assert.That(local.Resolve(name)?.MemberElement, Is.SameAs(target));
                Assert.That(target.owningNamespace, Is.SameAs(external));
                Assert.That(target.DeclaredName, Is.EqualTo("LongTargetName"));
            }
        }

        [Test]
        public void VerifyReferenceNameQualifiesShadowedTargetWithoutLeakingBetweenContexts()
        {
            var root = new Namespace();
            var outer = new Namespace { DeclaredName = "Outer" };
            var inner = new Namespace { DeclaredName = "Inner" };
            var target = new Feature { DeclaredName = "value" };
            var shadow = new Feature { DeclaredName = "value" };
            var outerSite = new Feature { DeclaredName = "outerSite" };
            var innerSite = new Feature { DeclaredName = "innerSite" };
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(root, outer);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(outer, inner);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(outer, target);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(outer, outerSite);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(inner, shadow);
            BrowserAcceptanceModelTestFactory.Own<OwningMembership>(inner, innerSite);

            var innerName = DiagramNotation.ReferenceName(target, innerSite);
            var outerName = DiagramNotation.ReferenceName(target, outerSite);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(innerName, Is.Not.EqualTo("value"));
                Assert.That(inner.Resolve(innerName)?.MemberElement, Is.SameAs(target));
                Assert.That(outerName, Is.EqualTo("value"));
                Assert.That(inner.Resolve("value")?.MemberElement, Is.SameAs(shadow));
                Assert.That(target.owningNamespace, Is.SameAs(outer));
            }
        }
    }
}
