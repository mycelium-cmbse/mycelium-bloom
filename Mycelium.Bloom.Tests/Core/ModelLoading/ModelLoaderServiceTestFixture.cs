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

            var model = service.LoadQuantitiesModel();
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
    }
}
