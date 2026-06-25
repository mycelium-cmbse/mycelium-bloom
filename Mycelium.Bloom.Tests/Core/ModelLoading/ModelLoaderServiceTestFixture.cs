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

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    using Mycelium.Bloom.Core.ModelLoading;

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
            var repositoryPath = GetRepositoryPath();
            var applicationPath = Path.Combine(repositoryPath, "Mycelium.Bloom");

            var webApplicationBuilder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    ApplicationName = "Mycelium.Bloom",
                    ContentRootPath = applicationPath,
                    EnvironmentName = "Development"
                });

            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            using var loggerFactory = LoggerFactory.Create(_ => { });

            var service = new ModelLoaderService(webApplicationBuilder.Environment, loggerFactory, memoryCache);

            var model = service.LoadQuantitiesModel();
            var cachedModel = service.LoadQuantitiesModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(model, Is.Not.Null);
                Assert.That(model.GetType().FullName, Is.EqualTo("SysML2.NET.Core.POCO.Root.Namespaces.Namespace"));
                Assert.That(cachedModel, Is.SameAs(model));
            }
        }

        private static string GetRepositoryPath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Mycelium.Bloom.sln")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Could not locate repository root from the test output directory.");
            }

            return directory.FullName;
        }
    }
}
