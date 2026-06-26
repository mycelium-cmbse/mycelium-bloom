// ------------------------------------------------------------------------------------------------
// <copyright file="ModelLoaderService.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using System.Diagnostics;

    using Microsoft.Extensions.Caching.Memory;

    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Serializer.Xmi;

    /// <summary>
    /// Provides operations to load SysML model files.
    /// </summary>
    public sealed class ModelLoaderService : IModelLoaderService
    {
        /// <summary>
        /// The cache key used for the loaded Quantities standard library model.
        /// </summary>
        private const string QuantitiesModelCacheKey = "SysML2.QuantitiesModel";

        /// <summary>
        /// The host environment used to resolve application content paths.
        /// </summary>
        private readonly IHostEnvironment hostEnvironment;

        /// <summary>
        /// The logger used to write model loading messages.
        /// </summary>
        private readonly ILogger<ModelLoaderService> logger;

        /// <summary>
        /// The logger factory passed to the SysML XMI deserializer.
        /// </summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// The memory cache used to cache loaded standard library models.
        /// </summary>
        private readonly IMemoryCache memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelLoaderService" /> class.
        /// </summary>
        /// <param name="hostEnvironment">The web host environment used to resolve application paths.</param>
        /// <param name="loggerFactory">The logger factory used by the service and the SysML XMI deserializer.</param>
        /// <param name="memoryCache">The memory cache used to cache loaded standard library models.</param>
        public ModelLoaderService(
            IHostEnvironment hostEnvironment,
            ILoggerFactory loggerFactory,
            IMemoryCache memoryCache)
        {
            this.hostEnvironment = hostEnvironment;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<ModelLoaderService>();
            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// Loads a SysML model from the provided file URI.
        /// </summary>
        /// <param name="modelUri">The URI of the SysML model file to load.</param>
        /// <returns>The loaded SysML model.</returns>
        public INamespace LoadModel(Uri modelUri)
        {
            var stopwatch = Stopwatch.StartNew();

            var deSerializer = new DeSerializer(this.loggerFactory);
            var model = deSerializer.DeSerialize(modelUri);
            stopwatch.Stop();

            if (this.logger.IsEnabled(LogLevel.Information))
            {
                this.logger.LogInformation(
                    "Loaded SysML model from {ModelUri} in {ElapsedMilliseconds} ms",
                    modelUri,
                    stopwatch.ElapsedMilliseconds);
            }

            return model;
        }

        /// <summary>
        /// Loads the SysML Quantities standard library model.
        /// </summary>
        /// <returns>The loaded SysML Quantities model.</returns>
        public INamespace LoadQuantitiesModel()
        {
            var model = this.memoryCache.GetOrCreate(
                QuantitiesModelCacheKey,
                entry =>
                {
                    entry.Priority = CacheItemPriority.NeverRemove;

                    var filePath = Path.Combine(
                        this.hostEnvironment.ContentRootPath,
                        "Resources",
                        "Domain Libraries",
                        "Quantities and Units",
                        "Quantities.sysmlx");

                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException(
                            "The Quantities.sysmlx file could not be found.",
                            filePath);
                    }

                    return this.LoadModel(new Uri(filePath));
                });

            return model;
        }
    }
}
