// ------------------------------------------------------------------------------------------------
// <copyright file="QuantitiesJsonResourceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.ModelLoading
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    using Microsoft.Extensions.Logging;

    using Mycelium.Bloom.Tests.Common;

    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;

    using DtoElement = SysML2.NET.Core.DTO.Root.Elements.IElement;
    using DtoNamespace = SysML2.NET.Core.DTO.Root.Namespaces.INamespace;
    using PocoFeature = SysML2.NET.Core.POCO.Core.Features.IFeature;
    using PocoNamespace = SysML2.NET.Core.POCO.Root.Namespaces.INamespace;
    using PocoRedefinition = SysML2.NET.Core.POCO.Core.Features.IRedefinition;

    /// <summary>
    /// Verifies the local API-shaped Quantities payload and its canonical SDK model.
    /// </summary>
    [TestFixture]
    public sealed class QuantitiesJsonResourceTestFixture
    {
        [Test]
        public void VerifyQuantitiesJsonResourceAssemblesCanonicalModel()
        {
            var resourcePath = Path.Combine(
                TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                "Resources",
                "Domain Libraries",
                "Quantities and Units",
                "Quantities.json");

            Assert.That(File.Exists(resourcePath), Is.True);

            var committedJson = File.ReadAllBytes(resourcePath);
            using var jsonDocument = JsonDocument.Parse(committedJson);

            Assert.That(jsonDocument.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));

            using var loggerFactory = LoggerFactory.Create(_ => { });
            using var jsonStream = new MemoryStream(committedJson);
            var data = new DeSerializer(loggerFactory)
                .DeSerialize(jsonStream, SerializationModeKind.JSON, SerializationTargetKind.PSM, false)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jsonDocument.RootElement.GetArrayLength(), Is.EqualTo(305));
                Assert.That(data, Has.Count.EqualTo(305));
                Assert.That(data, Is.All.InstanceOf<DtoElement>());
            }

            var elements = data.Cast<DtoElement>().ToList();
            var identifiers = elements.Select(element => element.Id).ToList();

            Assert.That(identifiers, Is.Unique);
            Assert.That(identifiers, Has.None.EqualTo(Guid.Empty));

            var rootNamespaces = elements
                .OfType<DtoNamespace>()
                .Where(element => !element.OwningRelationship.HasValue)
                .ToList();

            Assert.That(rootNamespaces, Has.Count.EqualTo(1));
            Assert.That(rootNamespaces[0].Id, Is.EqualTo(Guid.Parse("88e753b3-e75d-525f-b9ad-d5e9095b98ec")));

            var assembler = new Assembler(loggerFactory);

            Assert.That(() => assembler.Synchronize(elements), Throws.Nothing);
            Assert.That(assembler.Cache, Has.Count.EqualTo(elements.Count));

            var canonicalElements = assembler.Cache.ToDictionary(entry => entry.Key, entry => entry.Value.Value);

            foreach (var element in elements)
            {
                var canonicalElement = canonicalElements[element.Id];

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(canonicalElement.Id, Is.EqualTo(element.Id));
                    Assert.That(canonicalElement.ElementId, Is.EqualTo(element.ElementId));
                }
            }

            Assert.That(canonicalElements[rootNamespaces[0].Id], Is.InstanceOf<PocoNamespace>());

            var root = (PocoNamespace)canonicalElements[rootNamespaces[0].Id];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root, Is.SameAs(assembler.Cache[root.Id].Value));
                Assert.That(root.ElementId, Is.EqualTo("88e753b3-e75d-525f-b9ad-d5e9095b98ec"));
                Assert.That(root.DeclaredName, Is.Null);
                Assert.That(root.ownedElement, Has.Count.EqualTo(1));
            }

            var quantities = root.ownedElement[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(quantities.Id, Is.EqualTo(Guid.Parse("80db4152-4332-52fd-ab62-911a6515fc29")));
                Assert.That(quantities, Is.SameAs(canonicalElements[quantities.Id]));
                Assert.That(quantities.DeclaredName, Is.EqualTo("Quantities"));
                Assert.That(quantities.name, Is.EqualTo("Quantities"));
                Assert.That(quantities.qualifiedName, Is.EqualTo("Quantities"));
            }

            var tensorQuantity = quantities.ownedElement.Single(element => element.name == "TensorQuantityValue");
            var scalarQuantity = quantities.ownedElement.Single(element => element.name == "ScalarQuantityValue");
            var dimensionsId = Guid.Parse("334779bf-6357-5adf-a7c5-ffb32c2ffd17");
            var dimensions = tensorQuantity.ownedElement.Single(element => element.Id == dimensionsId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tensorQuantity, Is.SameAs(canonicalElements[tensorQuantity.Id]));
                Assert.That(tensorQuantity.qualifiedName, Is.EqualTo("Quantities::TensorQuantityValue"));
                Assert.That(scalarQuantity, Is.SameAs(canonicalElements[scalarQuantity.Id]));
                Assert.That(scalarQuantity.qualifiedName, Is.EqualTo("Quantities::ScalarQuantityValue"));
                Assert.That(dimensions, Is.SameAs(canonicalElements[dimensions.Id]));
            }

            AssertExternalRedefinitionIsCanonical(assembler);

            Assert.That(() => assembler.Synchronize(elements), Throws.Nothing);
            Assert.That(assembler.Cache, Has.Count.EqualTo(canonicalElements.Count));

            foreach (var element in canonicalElements.Values)
            {
                Assert.That(assembler.Cache[element.Id].Value, Is.SameAs(element));
            }

            AssertExternalRedefinitionIsCanonical(assembler);
        }

        /// <summary>
        /// Verifies the supporting SDK element that supplies the inherited QUDV dimensions name is canonical.
        /// </summary>
        /// <param name="assembler">The assembler owning the synchronized model.</param>
        private static void AssertExternalRedefinitionIsCanonical(Assembler assembler)
        {
            var redefiningFeatureId = Guid.Parse("334779bf-6357-5adf-a7c5-ffb32c2ffd17");
            var redefinedFeatureId = Guid.Parse("b81e8170-64bd-590e-869f-6de1ad059600");
            var redefiningFeature = (PocoFeature)assembler.Cache[redefiningFeatureId].Value;
            var redefinition = redefiningFeature.OwnedRelationship.OfType<PocoRedefinition>().Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(redefinition, Is.SameAs(assembler.Cache[redefinition.Id].Value));
                Assert.That(redefinition.RedefinedFeature, Is.SameAs(assembler.Cache[redefinedFeatureId].Value));
                Assert.That(redefiningFeature.DeclaredName, Is.Null);
                Assert.That(redefinition.RedefinedFeature.name, Is.EqualTo("dimensions"));
                Assert.That(redefiningFeature.name, Is.EqualTo("dimensions"));
                Assert.That(
                    redefiningFeature.qualifiedName,
                    Is.EqualTo("Quantities::TensorQuantityValue::dimensions"));
            }
        }
    }
}
