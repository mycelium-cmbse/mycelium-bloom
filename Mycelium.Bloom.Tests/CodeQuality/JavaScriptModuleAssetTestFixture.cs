// ------------------------------------------------------------------------------------------------
// <copyright file="JavaScriptModuleAssetTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.CodeQuality
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Verifies that dynamically imported JavaScript modules map to publishable static assets.
    /// </summary>
    [TestFixture]
    public sealed partial class JavaScriptModuleAssetTestFixture
    {
        /// <summary>
        /// Verifies every literal JavaScript module import used by C# interop.
        /// </summary>
        [Test]
        public void VerifyImportedJavaScriptModulesArePublishable()
        {
            var repositoryPath = TestRepository.GetRootPath();
            var projectPath = Path.Combine(repositoryPath, "Mycelium.Bloom");
            var failures = new List<string>();

            foreach (var sourceFile in Directory.EnumerateFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                         .Where(file => !IsBuildOutput(projectPath, file)))
            {
                var source = File.ReadAllText(sourceFile);

                foreach (Match match in JavaScriptImportPattern().Matches(source))
                {
                    var modulePath = match.Groups["path"].Value;
                    var relativeModulePath = modulePath[2..].Replace('/', Path.DirectorySeparatorChar);
                    string assetPath;

                    if (modulePath.StartsWith("./Components/", StringComparison.Ordinal))
                    {
                        if (!modulePath.EndsWith(".razor.js", StringComparison.Ordinal))
                        {
                            failures.Add($"{modulePath} must be a collocated .razor.js asset or live under wwwroot.");
                            continue;
                        }

                        assetPath = Path.Combine(projectPath, relativeModulePath);
                    }
                    else
                    {
                        assetPath = Path.Combine(projectPath, "wwwroot", relativeModulePath);
                    }

                    if (!File.Exists(assetPath))
                    {
                        failures.Add($"{modulePath} does not resolve to a publishable source asset.");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Checks whether a source file belongs to generated or build output folders.
        /// </summary>
        /// <param name="projectPath">The application project path.</param>
        /// <param name="file">The source file path.</param>
        /// <returns>True when the file should be excluded; otherwise, false.</returns>
        private static bool IsBuildOutput(string projectPath, string file)
        {
            var relativePath = Path.GetRelativePath(projectPath, file);
            var pathSegments = relativePath.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.None);

            return pathSegments.Contains("bin", StringComparer.OrdinalIgnoreCase)
                   || pathSegments.Contains("obj", StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the pattern used to find literal JavaScript module imports in C# source.
        /// </summary>
        /// <returns>The compiled import pattern.</returns>
        [GeneratedRegex("\\\"import\\\"\\s*,\\s*\\\"(?<path>\\./[^\\\"]+\\.js)\\\"", RegexOptions.CultureInvariant)]
        private static partial Regex JavaScriptImportPattern();
    }
}
