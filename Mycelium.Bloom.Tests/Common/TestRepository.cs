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
    /// Provides repository path helpers for tests that need project files.
    /// </summary>
    internal static class TestRepository
    {
        /// <summary>
        /// Gets the repository root path from the current test output directory.
        /// </summary>
        /// <returns>The repository root path.</returns>
        internal static string GetRootPath()
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

        /// <summary>
        /// Gets the path to a repository child directory.
        /// </summary>
        /// <param name="directoryName">The repository child directory name.</param>
        /// <returns>The child directory path.</returns>
        internal static string GetDirectoryPath(string directoryName)
        {
            return Path.Combine(GetRootPath(), directoryName);
        }
    }
}
