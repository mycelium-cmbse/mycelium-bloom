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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;

    using Microsoft.Extensions.Logging;

    using Mycelium.Bloom.Tests.Common;

    using SysML2.NET.Dal;

    using DtoElement = SysML2.NET.Core.DTO.Root.Elements.IElement;
    using JsonDeSerializer = SysML2.NET.Serializer.Json.DeSerializer;
    using JsonSerializer = SysML2.NET.Serializer.Json.Serializer;
    using PocoElement = SysML2.NET.Core.POCO.Root.Elements.IElement;
    using PocoFeature = SysML2.NET.Core.POCO.Core.Features.IFeature;
    using PocoNamespace = SysML2.NET.Core.POCO.Root.Namespaces.INamespace;
    using PocoRedefinition = SysML2.NET.Core.POCO.Core.Features.IRedefinition;
    using SerializationModeKind = SysML2.NET.Serializer.Json.SerializationModeKind;
    using SerializationTargetKind = SysML2.NET.Serializer.Json.SerializationTargetKind;
    using XmiDeSerializer = SysML2.NET.Serializer.Xmi.DeSerializer;

    /// <summary>
    /// Verifies the API-shaped Quantities payload against its authoritative XMI source.
    /// </summary>
    [TestFixture]
    public sealed class QuantitiesJsonResourceTestFixture
    {
        /// <summary>
        /// Verifies deterministic DTO generation and canonical assembly preserve the Bloom-consumed QUDV model.
        /// </summary>
        [Test]
        public void VerifyQuantitiesJsonResourceMatchesAuthoritativeXmiModel()
        {
            var resourceDirectory = Path.Combine(
                TestRepository.GetDirectoryPath("Mycelium.Bloom"),
                "Resources",
                "Domain Libraries",
                "Quantities and Units");
            using var loggerFactory = LoggerFactory.Create(_ => { });
            var sourceResult = new XmiDeSerializer(loggerFactory)
                .DeSerialize(new Uri(Path.Combine(resourceDirectory, "Quantities.sysmlx")));
            var sourceElements = EnumerateContainment([sourceResult.RootNamespace]).ToList();
            var availableElements = EnumerateContainment(
                    new[] { sourceResult.RootNamespace }.Concat(sourceResult.ReferencedNamespaces))
                .ToDictionary(element => element.Id);
            var sourceElementIds = sourceElements.Select(element => element.Id).ToHashSet();
            var conversionMethods = GetDtoConversionMethods();
            var sourceDtos = sourceElements
                .Select(element => ConvertToDto(element, conversionMethods))
                .ToList();
            var referencedElementIds = sourceDtos
                .SelectMany(GetReferencedIdentifiers)
                .Distinct()
                .ToList();

            Assert.That(
                referencedElementIds.Where(identifier => !availableElements.ContainsKey(identifier)),
                Is.Empty);

            var supportingElementIds = referencedElementIds
                .Where(identifier => !sourceElementIds.Contains(identifier))
                .OrderBy(identifier => identifier)
                .ToList();

            var generatedDtos = sourceDtos
                .Concat(supportingElementIds.Select(identifier => ConvertToDto(
                    availableElements[identifier],
                    conversionMethods)))
                .ToList();
            using var generatedJson = new MemoryStream();

            new JsonSerializer().Serialize(
                generatedDtos,
                SerializationModeKind.JSON,
                false,
                generatedJson,
                new JsonWriterOptions { Indented = true, NewLine = "\n" });

            var committedJson = File.ReadAllBytes(Path.Combine(resourceDirectory, "Quantities.json"));
            using var jsonDocument = JsonDocument.Parse(committedJson);
            generatedJson.Position = 0;
            var deserializedData = new JsonDeSerializer(loggerFactory)
                .DeSerialize(
                    generatedJson,
                    SerializationModeKind.JSON,
                    SerializationTargetKind.PSM,
                    false)
                .ToList();
            var deserializedDtos = deserializedData.OfType<DtoElement>().ToList();
            var assembler = new Assembler(loggerFactory);
            assembler.Synchronize(deserializedDtos);
            var canonicalRoot = (PocoNamespace)assembler.Cache[sourceResult.RootNamespace.Id].Value;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sourceElements, Has.Count.EqualTo(281));
                Assert.That(supportingElementIds, Has.Count.EqualTo(24));
                Assert.That(generatedJson.ToArray(), Is.EqualTo(committedJson));
                Assert.That(jsonDocument.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
                Assert.That(jsonDocument.RootElement.GetArrayLength(), Is.EqualTo(generatedDtos.Count));
                Assert.That(jsonDocument.RootElement[0].TryGetProperty("@type", out _), Is.True);
                Assert.That(jsonDocument.RootElement[0].TryGetProperty("@id", out _), Is.True);
                Assert.That(deserializedDtos, Has.Count.EqualTo(deserializedData.Count));
                Assert.That(assembler.Cache, Has.Count.EqualTo(generatedDtos.Count));
                Assert.That(canonicalRoot, Is.SameAs(assembler.Cache[canonicalRoot.Id].Value));
                Assert.That(
                    supportingElementIds.All(identifier => assembler.Cache.ContainsKey(identifier)),
                    Is.True);
            }

            AssertEquivalentModel(sourceElements, canonicalRoot, assembler);
            AssertExternalRedefinitionIsCanonical(assembler);
        }

        /// <summary>
        /// Gets the SDK-provided concrete POCO-to-DTO conversions keyed by their source type.
        /// </summary>
        /// <returns>The available SDK conversion methods.</returns>
        private static Dictionary<Type, MethodInfo> GetDtoConversionMethods()
        {
            return typeof(IAssembler).Assembly
                .GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(IsDtoConversionMethod)
                .ToDictionary(method => method.GetParameters()[0].ParameterType);
        }

        /// <summary>
        /// Determines whether a method is an SDK POCO-to-DTO conversion.
        /// </summary>
        /// <param name="method">The method to inspect.</param>
        /// <returns><see langword="true" /> when the method converts one concrete POCO element to its DTO.</returns>
        private static bool IsDtoConversionMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();

            return method.Name == "ToDto"
                   && typeof(DtoElement).IsAssignableFrom(method.ReturnType)
                   && parameters.Length == 2
                   && parameters[1].ParameterType == typeof(bool);
        }

        /// <summary>
        /// Converts one concrete POCO using its generated SDK conversion method.
        /// </summary>
        /// <param name="element">The source POCO element.</param>
        /// <param name="conversionMethods">The SDK conversions keyed by concrete POCO type.</param>
        /// <returns>The corresponding PSM DTO.</returns>
        private static DtoElement ConvertToDto(
            PocoElement element,
            Dictionary<Type, MethodInfo> conversionMethods)
        {
            if (!conversionMethods.TryGetValue(element.GetType(), out var conversionMethod))
            {
                throw new InvalidOperationException($"No SDK DTO conversion exists for {element.GetType().FullName}.");
            }

            return (DtoElement)conversionMethod.Invoke(null, [element, false]);
        }

        /// <summary>
        /// Enumerates identifiers referenced by one generated SDK DTO.
        /// </summary>
        /// <param name="element">The DTO whose reference properties are inspected.</param>
        /// <returns>The identifiers carried by scalar and collection reference properties.</returns>
        private static IEnumerable<Guid> GetReferencedIdentifiers(DtoElement element)
        {
            foreach (var property in element.GetType().GetProperties())
            {
                switch (property.GetValue(element))
                {
                    case Guid identifier when identifier != Guid.Empty && identifier != element.Id:
                        yield return identifier;
                        break;
                    case IEnumerable<Guid> identifiers:
                        foreach (var identifier in identifiers.Where(identifier => identifier != Guid.Empty))
                        {
                            yield return identifier;
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Enumerates elements and their owned relationships in deterministic containment order.
        /// </summary>
        /// <param name="roots">The roots whose containment closures are enumerated.</param>
        /// <returns>The distinct containment closure in pre-order.</returns>
        private static IEnumerable<PocoElement> EnumerateContainment(IEnumerable<PocoElement> roots)
        {
            var visitedIdentifiers = new HashSet<Guid>();
            var pendingElements = new Stack<PocoElement>(roots.Reverse());

            while (pendingElements.TryPop(out var element))
            {
                if (!visitedIdentifiers.Add(element.Id))
                {
                    continue;
                }

                yield return element;

                PushInReverse(element.ownedElement, pendingElements);
                PushInReverse(element.OwnedRelationship, pendingElements);
            }
        }

        /// <summary>
        /// Pushes model elements in reverse so stack traversal preserves SDK collection order.
        /// </summary>
        /// <typeparam name="TElement">The concrete SDK element interface in the collection.</typeparam>
        /// <param name="elements">The ordered model elements to push.</param>
        /// <param name="pendingElements">The traversal stack receiving the elements.</param>
        private static void PushInReverse<TElement>(
            IReadOnlyList<TElement> elements,
            Stack<PocoElement> pendingElements)
            where TElement : PocoElement
        {
            if (elements is null)
            {
                return;
            }

            for (var index = elements.Count - 1; index >= 0; index--)
            {
                if (elements[index] is { } element)
                {
                    pendingElements.Push(element);
                }
            }
        }

        /// <summary>
        /// Verifies the fields and containment references Bloom consumes against assembled canonical objects.
        /// </summary>
        /// <param name="sourceElements">The authoritative XMI POCO containment closure.</param>
        /// <param name="canonicalRoot">The root produced by JSON deserialization and assembly.</param>
        /// <param name="assembler">The assembler that owns the target POCO identities.</param>
        private static void AssertEquivalentModel(
            List<PocoElement> sourceElements,
            PocoNamespace canonicalRoot,
            Assembler assembler)
        {
            var canonicalElements = EnumerateContainment([canonicalRoot]).ToDictionary(element => element.Id);

            Assert.That(canonicalElements, Has.Count.EqualTo(sourceElements.Count));

            foreach (var sourceElement in sourceElements)
            {
                Assert.That(canonicalElements.TryGetValue(sourceElement.Id, out var canonicalElement), Is.True);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(canonicalElement, Is.SameAs(assembler.Cache[sourceElement.Id].Value));
                    Assert.That(canonicalElement.GetType(), Is.EqualTo(sourceElement.GetType()));
                    Assert.That(canonicalElement.ElementId, Is.EqualTo(sourceElement.ElementId));
                    Assert.That(canonicalElement.DeclaredName, Is.EqualTo(sourceElement.DeclaredName));
                    Assert.That(canonicalElement.DeclaredShortName, Is.EqualTo(sourceElement.DeclaredShortName));
                    Assert.That(canonicalElement.name, Is.EqualTo(sourceElement.name));
                    Assert.That(canonicalElement.shortName, Is.EqualTo(sourceElement.shortName));
                    Assert.That(canonicalElement.qualifiedName, Is.EqualTo(sourceElement.qualifiedName));
                    Assert.That(GetOwnedElementIds(canonicalElement), Is.EqualTo(GetOwnedElementIds(sourceElement)));
                    Assert.That(
                        GetOwnedRelationshipIds(canonicalElement),
                        Is.EqualTo(GetOwnedRelationshipIds(sourceElement)));
                    Assert.That(canonicalElement.OwningRelationship?.Id, Is.EqualTo(sourceElement.OwningRelationship?.Id));
                }
            }
        }

        /// <summary>
        /// Gets the ordered identifiers of an element's derived owned elements.
        /// </summary>
        /// <param name="element">The element whose owned elements are projected.</param>
        /// <returns>The ordered owned-element identifiers.</returns>
        private static Guid[] GetOwnedElementIds(PocoElement element)
        {
            return element.ownedElement?.Select(ownedElement => ownedElement.Id).ToArray() ?? [];
        }

        /// <summary>
        /// Gets the ordered identifiers of an element's owned relationships.
        /// </summary>
        /// <param name="element">The element whose relationships are projected.</param>
        /// <returns>The ordered owned-relationship identifiers.</returns>
        private static Guid[] GetOwnedRelationshipIds(PocoElement element)
        {
            return element.OwnedRelationship?.Select(relationship => relationship.Id).ToArray() ?? [];
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
                Assert.That(redefinition.RedefinedFeature, Is.SameAs(assembler.Cache[redefinedFeatureId].Value));
                Assert.That(redefiningFeature.name, Is.EqualTo("dimensions"));
                Assert.That(
                    redefiningFeature.qualifiedName,
                    Is.EqualTo("Quantities::TensorQuantityValue::dimensions"));
            }
        }
    }
}
