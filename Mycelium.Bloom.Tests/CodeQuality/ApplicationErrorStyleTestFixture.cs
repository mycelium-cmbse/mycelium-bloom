// ------------------------------------------------------------------------------------------------
// <copyright file="ApplicationErrorStyleTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.CodeQuality
{
    using System.IO;
    using System.Text.RegularExpressions;

    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Verifies that error presentation shares the generated theme contract.
    /// </summary>
    [TestFixture]
    public sealed partial class ApplicationErrorStyleTestFixture
    {
        /// <summary>
        /// Verifies visual values resolve through supplied tokens without a separate palette or mode overrides.
        /// </summary>
        /// <param name="stylesheet">The component-owned stylesheet.</param>
        [TestCase("Components/UI/Molecules/ApplicationErrorState/ApplicationErrorState.razor.css")]
        [TestCase("Components/Layout/ErrorLayout.razor.css")]
        public void VerifyErrorStylesUseCanonicalTokens(string stylesheet)
        {
            var project = TestRepository.GetDirectoryPath("Mycelium.Bloom");
            var css = File.ReadAllText(Path.Combine(project, stylesheet));
            var tokens = File.ReadAllText(Path.Combine(project, "wwwroot", "css", "tokens.css"));
            var references = TokenReference().Matches(css);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(references, Is.Not.Empty);
                foreach (Match reference in references)
                {
                    Assert.That(tokens, Does.Contain(reference.Groups[1].Value + ":"));
                }

                Assert.That(css, Does.Not.Match(@"#[0-9a-fA-F]{3,8}\b|\b(?:rgb|hsl|oklch|color)\("));
                Assert.That(css, Does.Not.Contain(".dark").And.Not.Contain("prefers-color-scheme"));
                Assert.That(css, Does.Not.Match(@"--m[b]-"));
            }
        }

        [GeneratedRegex(@"var\((--[\w-]+)\)")]
        private static partial Regex TokenReference();
    }
}
