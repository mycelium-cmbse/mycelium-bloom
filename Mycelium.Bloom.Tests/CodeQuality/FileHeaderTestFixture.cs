// ------------------------------------------------------------------------------------------------
// <copyright file="FileHeaderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.CodeQuality
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Verifies that code-related files contain the expected file header.
    /// </summary>
    [TestFixture]
    public sealed class FileHeaderTestFixture
    {
        private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

        /// <summary>
        /// Verifies that every C# and Razor source file has the expected file header.
        /// </summary>
        [Test]
        public void CodeRelatedFiles_HaveExpectedFileHeader()
        {
            var repositoryPath = TestRepository.GetRootPath();
            var sourceFiles = new[]
                {
                    Path.Combine(repositoryPath, "Mycelium.Bloom"),
                    Path.Combine(repositoryPath, "Mycelium.Bloom.Tests")
                }
                .SelectMany(path => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                .Where(IsCodeRelatedFile)
                .Where(file => !IsGeneratedOrBuildOutput(repositoryPath, file))
                .OrderBy(file => file)
                .ToArray();

            var filesMissingHeaders = sourceFiles
                .Where(file => !HasExpectedHeader(file))
                .Select(file => Path.GetRelativePath(repositoryPath, file))
                .ToArray();

            Assert.That(filesMissingHeaders, Is.Empty, $"Missing or invalid file headers:{Environment.NewLine}{string.Join(Environment.NewLine, filesMissingHeaders)}");
        }

        /// <summary>
        /// Checks whether the provided file starts with the expected header.
        /// </summary>
        /// <param name="file">The file path to inspect.</param>
        /// <returns>A value indicating whether the file has the expected header.</returns>
        private static bool HasExpectedHeader(string file)
        {
            var content = File.ReadAllText(file, Encoding.UTF8);

            if (content.Length > 0 && content[0] == '\uFEFF')
            {
                content = content[1..];
            }

            var fileName = Path.GetFileName(file);
            content = NormalizeEmptyCommentLines(NormalizeLineEndings(content));

            var expectedHeader = NormalizeEmptyCommentLines(NormalizeLineEndings(GetExpectedHeader(fileName)));

            return content.StartsWith(expectedHeader, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the expected header text for the provided file name.
        /// </summary>
        /// <param name="fileName">The source file name.</param>
        /// <returns>The expected normalized header text.</returns>
        private static string GetExpectedHeader(string fileName)
        {
            if (fileName.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(
                    Environment.NewLine,
                    "@* ------------------------------------------------------------------------------------------------",
                    $"<copyright file=\"{fileName}\" company=\"Starion Group S.A.\">",
                    string.Empty,
                    "  Copyright 2026 Starion Group S.A.",
                    "  SPDX-License-Identifier: Apache-2.0",
                    string.Empty,
                    "</copyright>",
                    "------------------------------------------------------------------------------------------------ *@",
                    string.Empty,
                    string.Empty);
            }

            return string.Join(
                Environment.NewLine,
                "// ------------------------------------------------------------------------------------------------",
                $"// <copyright file=\"{fileName}\" company=\"Starion Group S.A.\">",
                "//",
                "//   Copyright 2026 Starion Group S.A.",
                "//   SPDX-License-Identifier: Apache-2.0",
                "//",
                "// </copyright>",
                "// ------------------------------------------------------------------------------------------------",
                string.Empty,
                string.Empty);
        }

        /// <summary>
        /// Normalizes Windows line endings to Unix line endings.
        /// </summary>
        /// <param name="text">The text to normalize.</param>
        /// <returns>The text with normalized line endings.</returns>
        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        /// <summary>
        /// Normalizes empty C# comment lines in header text.
        /// </summary>
        /// <param name="text">The text to normalize.</param>
        /// <returns>The text with normalized empty comment lines.</returns>
        private static string NormalizeEmptyCommentLines(string text)
        {
            return text.Replace("// \n", "//\n", StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks whether a file is a source file covered by the header rule.
        /// </summary>
        /// <param name="file">The file path to inspect.</param>
        /// <returns>A value indicating whether the file is code-related.</returns>
        private static bool IsCodeRelatedFile(string file)
        {
            return file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                   file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks whether a file belongs to generated or build output folders.
        /// </summary>
        /// <param name="repositoryPath">The repository root path.</param>
        /// <param name="file">The file path to inspect.</param>
        /// <returns>A value indicating whether the file should be excluded.</returns>
        private static bool IsGeneratedOrBuildOutput(string repositoryPath, string file)
        {
            var relativePath = Path.GetRelativePath(repositoryPath, file);
            var pathSegments = relativePath.Split(DirectorySeparators, StringSplitOptions.None);

            return pathSegments.Contains("bin", StringComparer.OrdinalIgnoreCase) ||
                   pathSegments.Contains("obj", StringComparer.OrdinalIgnoreCase) ||
                   pathSegments.Contains("artifacts", StringComparer.OrdinalIgnoreCase);
        }
    }
}
