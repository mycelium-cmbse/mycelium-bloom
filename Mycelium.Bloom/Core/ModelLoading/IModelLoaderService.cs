// ------------------------------------------------------------------------------------------------
// <copyright file="IModelLoaderService.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ModelLoading
{
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Defines operations to load SysML model files.
    /// </summary>
    public interface IModelLoaderService
    {
        /// <summary>
        /// Loads a SysML model from the provided file URI.
        /// </summary>
        /// <param name="modelUri">The URI of the model file to load.</param>
        /// <returns>The loaded model content.</returns>
        INamespace LoadModel(Uri modelUri);

        /// <summary>
        /// Loads the SysML Quantities standard library model.
        /// </summary>
        /// <returns>The loaded SysML Quantities model.</returns>
        INamespace LoadQuantitiesModel();
    }
}
