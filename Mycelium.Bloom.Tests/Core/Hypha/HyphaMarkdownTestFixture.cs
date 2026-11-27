// ------------------------------------------------------------------------------------------------
// <copyright file="HyphaMarkdownTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Hypha
{
    using Mycelium.Bloom.Core.Hypha;

    [TestFixture]
    public sealed class HyphaMarkdownTestFixture
    {
        [Test]
        public void VerifyAnAnswerIsRenderedWithEmphasisListsAndTables()
        {
            var html = HyphaMarkdown.ToSafeHtml("The dry mass is **125.8 kg**.\n\n- AOCS\n- Power\n\n| Subsystem | Mass |\n|---|---|\n| AOCS | 18.5 kg |");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(html, Does.Contain("<strong>125.8 kg</strong>"));
                Assert.That(html, Does.Contain("<li>AOCS</li>"));
                Assert.That(html, Does.Contain("<table>"));
                Assert.That(html, Does.Contain("<td>18.5 kg</td>"));
            }
        }

        [Test]
        public void VerifyDangerousHtmlOfAnAnswerIsNotRendered()
        {
            var html = HyphaMarkdown.ToSafeHtml("Hello <script>alert('x')</script> <img src=x onerror=\"alert(1)\"> [link](javascript:alert(1))");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(html, Does.Not.Contain("<script"));
                Assert.That(html, Does.Not.Contain("<img"));
                Assert.That(html, Does.Not.Contain("javascript:"));
                Assert.That(html, Does.Contain("Hello"));
            }
        }

        [Test]
        public void VerifyALinkOfAnAnswerOpensInANewTab()
        {
            var html = HyphaMarkdown.ToSafeHtml("See [ECSS](https://ecss.nl).");

            Assert.That(html, Does.Contain("href=\"https://ecss.nl\"").And.Contain("target=\"_blank\"").And.Contain("rel=\"noopener noreferrer\""));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void VerifyAnEmptyAnswerRendersNothing(string markdown)
        {
            Assert.That(HyphaMarkdown.ToSafeHtml(markdown), Is.Empty);
        }
    }
}
