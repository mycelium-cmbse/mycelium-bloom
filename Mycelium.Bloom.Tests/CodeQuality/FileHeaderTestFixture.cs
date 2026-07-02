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

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        private static string NormalizeEmptyCommentLines(string text)
        {
            return text.Replace("// \n", "//\n", StringComparison.Ordinal);
        }

        private static bool IsCodeRelatedFile(string file)
        {
            return file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                   file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
        }

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
