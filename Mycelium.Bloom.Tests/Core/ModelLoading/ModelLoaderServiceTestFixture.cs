// ------------------------------------------------------------------------------------------------
// <copyright file="ModelLoaderServiceTestFixture.cs" company="Starion Group S.A.">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;
    using SysML2.NET.Dal;
    using SysML2.NET.Serializer.Json;

    /// <summary>
    /// Integration tests for the <see cref="ModelLoaderService" />.
    /// </summary>
    [TestFixture]
    public sealed class ModelLoaderServiceTestFixture
    {
        /// <summary>
        /// Verifies that the Quantities standard library model loads from application resources and is cached.
        /// </summary>
        [Test]
        public void VerifyLoadQuantitiesModelLoadsRealModelAndReusesCachedInstance()
        {
            var applicationPath = TestRepository.GetDirectoryPath("Mycelium.Bloom");

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.Setup(x => x.ContentRootPath).Returns(applicationPath);

            using var loggerFactory = LoggerFactory.Create(_ => { });
            var assembler = new Assembler(loggerFactory);
            var service = new ModelLoaderService(
                hostEnvironment.Object,
                new DeSerializer(loggerFactory),
                assembler,
                loggerFactory.CreateLogger<ModelLoaderService>());

            Assert.That(assembler.Cache, Is.Empty);
            var model = service.LoadQuantitiesModel();
            Assert.That(assembler.Cache, Has.Count.EqualTo(305));
            var cachedModel = service.LoadQuantitiesModel();
            var modelPath = Path.Combine(
                applicationPath,
                "Resources",
                "Domain Libraries",
                "Quantities and Units",
                "Quantities.json");
            var synchronizedModel = service.LoadModel(new Uri(modelPath));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(model, Is.Not.Null);
                Assert.That(model.GetType().FullName, Is.EqualTo("SysML2.NET.Core.POCO.Root.Namespaces.Namespace"));
                Assert.That(cachedModel, Is.SameAs(model));
                Assert.That(synchronizedModel, Is.SameAs(model));
                Assert.That(assembler.Cache, Has.Count.EqualTo(305));
                Assert.That(assembler.Cache[model.Id].Value, Is.SameAs(model));
                Assert.That(model.ownedElement, Has.Count.EqualTo(1));
                Assert.That(model.ownedElement[0].DeclaredName, Is.EqualTo("Quantities"));
            }
        }

        [TestCase(1024, false)]
        [TestCase(1024, true)]
        [TestCase(1025, false)]
        [TestCase(1025, true)]
        [TestCase(1100, false)]
        [TestCase(1100, true)]
        public async Task VerifyUploadOwnershipDepth(int links, bool leafFirst)
        {
            using var logging = LoggerFactory.Create(_ => { });
            var assembler = new Assembler(logging);
            var service = new ModelLoaderService(Mock.Of<IHostEnvironment>(), new DeSerializer(logging),
                assembler, logging.CreateLogger<ModelLoaderService>());
            var elements = CreateOwnershipChain(links);
            if (leafFirst)
            {
                elements.Reverse();
            }

            using var stream =
                new MemoryStream(Encoding.UTF8.GetBytes(new JsonArray(elements.ToArray()).ToJsonString()));
            if (links > 1024)
            {
                await Assert.ThatAsync(async () => await service.OpenIndependentModelAsync(stream,
                    accept => Task.FromResult(accept()), CancellationToken.None), Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("excessively deep ownership"));
            }
            else
            {
                var model = await service.OpenIndependentModelAsync(stream,
                    accept => Task.FromResult(accept()), CancellationToken.None);
                Assert.That(model.DeclaredName, Is.EqualTo("Root"));
                service.ReleaseUploadedModel(model);
            }

            Assert.That(assembler.Cache, Is.Empty);
        }

        [TestCase(1022)]
        [TestCase(1024)]
        public async Task VerifyUploadSharedAncestry(int depth)
        {
            using var logging = LoggerFactory.Create(_ => { });
            var service = new ModelLoaderService(Mock.Of<IHostEnvironment>(), new DeSerializer(logging),
                new Assembler(logging), logging.CreateLogger<ModelLoaderService>());
            var elements = CreateOwnershipChain(depth);
            var owner = elements[^1];
            var memberships = new JsonArray();
            owner["ownedRelationship"] = memberships;
            for (var branch = 0; branch < 2; branch++)
            {
                var membershipId = $"72000000-0000-4000-8000-{branch:D12}";
                var partId = $"73000000-0000-4000-8000-{branch:D12}";
                memberships.Add(new JsonObject { ["@id"] = membershipId });
                elements.Add(new JsonObject
                {
                    ["@id"] = membershipId,
                    ["elementId"] = membershipId,
                    ["@type"] = "OwningMembership",
                    ["owningRelatedElement"] = new JsonObject { ["@id"] = owner["@id"].GetValue<string>() },
                    ["ownedRelatedElement"] = new JsonArray(new JsonObject { ["@id"] = partId }),
                    ["memberElement"] = new JsonObject { ["@id"] = partId }
                });
                elements.Add(new JsonObject
                {
                    ["@id"] = partId,
                    ["elementId"] = partId,
                    ["@type"] = "PartUsage",
                    ["owningRelationship"] = new JsonObject { ["@id"] = membershipId }
                });
            }

            using var stream =
                new MemoryStream(Encoding.UTF8.GetBytes(new JsonArray(elements.ToArray()).ToJsonString()));
            if (depth == 1024)
            {
                await Assert.ThatAsync(async () => await service.OpenIndependentModelAsync(stream,
                    accept => Task.FromResult(accept()), CancellationToken.None), Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("excessively deep ownership"));
            }
            else
            {
                var model = await service.OpenIndependentModelAsync(stream,
                    accept => Task.FromResult(accept()), CancellationToken.None);
                Assert.That(model.DeclaredName, Is.EqualTo("Root"));
                service.ReleaseUploadedModel(model);
            }
        }

        private static List<JsonNode> CreateOwnershipChain(int links)
        {
            var elements = new List<JsonNode>();
            for (var index = 0; index <= links; index++)
            {
                var id = $"71000000-0000-4000-8000-{index:D12}";
                var element = new JsonObject
                {
                    ["@id"] = id,
                    ["elementId"] = id,
                    ["@type"] = index % 2 == 0 ? "Package" : "OwningMembership",
                    ["declaredName"] = index == 0 ? "Root" : null
                };
                elements.Add(element);
                if (index == 0)
                {
                    continue;
                }

                var owner = elements[^2];
                element[index % 2 == 0 ? "owningRelationship" : "owningRelatedElement"] =
                    new JsonObject { ["@id"] = owner["@id"].GetValue<string>() };
                owner[index % 2 == 0 ? "ownedRelatedElement" : "ownedRelationship"] =
                    new JsonArray(new JsonObject { ["@id"] = id });
                if (index % 2 == 0)
                {
                    owner["memberElement"] = new JsonObject { ["@id"] = id };
                }

            }

            return elements;
        }

        [TestCase("https://example.invalid/model.json", UriKind.Absolute)]
        [TestCase("model.json", UriKind.Relative)]
        public void VerifyLoadModelRejectsUnsupportedSources(string source, UriKind kind)
        {
            using var loggerFactory = LoggerFactory.Create(_ => { });
            var assembler = new Assembler(loggerFactory);
            var service = new ModelLoaderService(Mock.Of<IHostEnvironment>(), new DeSerializer(loggerFactory),
                assembler, loggerFactory.CreateLogger<ModelLoaderService>());

            Assert.That(() => service.LoadModel(new Uri(source, kind)), Throws.TypeOf<NotSupportedException>());
            Assert.That(assembler.Cache, Is.Empty);
        }
    }
}