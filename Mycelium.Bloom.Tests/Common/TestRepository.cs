// ------------------------------------------------------------------------------------------------
// <copyright file="TestRepository.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides repository path helpers for tests.
    /// </summary>
    public static class TestRepository
    {
        /// <summary>
        /// Gets the repository root path from the test output directory.
        /// </summary>
        /// <returns>The repository root path.</returns>
        public static string GetRootPath()
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
