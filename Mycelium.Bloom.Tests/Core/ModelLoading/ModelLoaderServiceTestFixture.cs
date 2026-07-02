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
    using System.IO;

    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Moq;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;

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
        public void LoadQuantitiesModel_LoadsRealModelAndReusesCachedInstance()
        {
            var repositoryPath = TestRepository.GetRootPath();
            var applicationPath = Path.Combine(repositoryPath, "Mycelium.Bloom");

            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.Setup(x => x.ContentRootPath).Returns(applicationPath);

            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            using var loggerFactory = LoggerFactory.Create(_ => { });

            var service = new ModelLoaderService(hostEnvironment.Object, loggerFactory, memoryCache);

            var model = service.LoadQuantitiesModel();
            var cachedModel = service.LoadQuantitiesModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(model, Is.Not.Null);
                Assert.That(model.GetType().FullName, Is.EqualTo("SysML2.NET.Core.POCO.Root.Namespaces.Namespace"));
                Assert.That(cachedModel, Is.SameAs(model));
            }
        }
    }
}
