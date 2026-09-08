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

    using SysML2.NET.Dal;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Serializer.Json;

    using DtoElement = SysML2.NET.Core.DTO.Root.Elements.IElement;
    using DtoNamespace = SysML2.NET.Core.DTO.Root.Namespaces.INamespace;

    /// <summary>
    /// Provides operations to load SysML model files.
    /// </summary>
    public sealed class ModelLoaderService : IModelLoaderService
    {
        /// <summary>
        /// The file name of the local SysML v2 API-shaped Quantities payload.
        /// </summary>
        private const string QuantitiesModelFileName = "Quantities.json";

        /// <summary>
        /// The SDK assembler that owns canonical POCO identity for this model session.
        /// </summary>
        private readonly IAssembler assembler;

        /// <summary>
        /// The SDK deserializer that reads SysML v2 API-shaped JSON payloads.
        /// </summary>
        private readonly IDeSerializer deSerializer;

        /// <summary>
        /// The host environment used to resolve application content paths.
        /// </summary>
        private readonly IHostEnvironment hostEnvironment;

        /// <summary>
        /// The logger used to write model loading messages.
        /// </summary>
        private readonly ILogger<ModelLoaderService> logger;

        /// <summary>
        /// The lazily loaded Quantities model for this model session.
        /// </summary>
        private readonly Lazy<INamespace> quantitiesModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelLoaderService" /> class.
        /// </summary>
        /// <param name="hostEnvironment">The web host environment used to resolve application paths.</param>
        /// <param name="deSerializer">The SDK JSON deserializer used to read PSM DTOs.</param>
        /// <param name="assembler">The SDK assembler that owns the canonical POCO graph.</param>
        /// <param name="logger">The logger used to write model loading messages.</param>
        public ModelLoaderService(
            IHostEnvironment hostEnvironment,
            IDeSerializer deSerializer,
            IAssembler assembler,
            ILogger<ModelLoaderService> logger)
        {
            this.hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            this.deSerializer = deSerializer ?? throw new ArgumentNullException(nameof(deSerializer));
            this.assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.quantitiesModel = new Lazy<INamespace>(this.LoadQuantitiesModelResource);
        }

        /// <summary>
        /// Loads a SysML v2 API-shaped JSON payload and synchronizes it into the canonical SDK model.
        /// </summary>
        /// <param name="modelUri">The URI of the SysML model file to load.</param>
        /// <returns>The canonical root namespace assembled from the payload DTOs.</returns>
        public INamespace LoadModel(Uri modelUri)
        {
            ArgumentNullException.ThrowIfNull(modelUri);

            var filePath = modelUri.LocalPath;

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The SysML JSON model file could not be found.", filePath);
            }

            var stopwatch = Stopwatch.StartNew();

            using var stream = File.OpenRead(filePath);

            var data = this.deSerializer
                .DeSerialize(stream, SerializationModeKind.JSON, SerializationTargetKind.PSM, false)
                .ToList();
            var elements = data.OfType<DtoElement>().ToList();

            if (elements.Count != data.Count)
            {
                throw new InvalidDataException("The SysML JSON model must contain only PSM element DTOs.");
            }

            var rootNamespaces = elements
                .OfType<DtoNamespace>()
                .Where(element => !element.OwningRelationship.HasValue)
                .ToList();

            if (rootNamespaces.Count != 1)
            {
                throw new InvalidDataException("The SysML JSON model must contain exactly one root namespace DTO.");
            }

            this.assembler.Synchronize(elements);

            if (!this.assembler.Cache.TryGetValue(rootNamespaces[0].Id, out var rootElement)
                || rootElement.Value is not INamespace rootNamespace)
            {
                throw new InvalidDataException("The SDK assembler did not produce the requested root namespace.");
            }

            stopwatch.Stop();

            if (this.logger.IsEnabled(LogLevel.Information))
            {
                this.logger.LogInformation(
                    "Loaded SysML model from {ModelUri} in {ElapsedMilliseconds} ms",
                    modelUri,
                    stopwatch.ElapsedMilliseconds);
            }

            return rootNamespace;
        }

        /// <summary>
        /// Loads the SysML Quantities standard library model.
        /// </summary>
        /// <returns>The loaded SysML Quantities model.</returns>
        public INamespace LoadQuantitiesModel()
        {
            return this.quantitiesModel.Value;
        }

        /// <summary>
        /// Loads the local Quantities payload through the same JSON DTO boundary used by the SDK REST client.
        /// </summary>
        /// <returns>The canonical Quantities root namespace.</returns>
        private INamespace LoadQuantitiesModelResource()
        {
            var filePath = Path.Combine(
                this.hostEnvironment.ContentRootPath,
                "Resources",
                "Domain Libraries",
                "Quantities and Units",
                QuantitiesModelFileName);

            return this.LoadModel(new Uri(filePath));
        }
    }
}
