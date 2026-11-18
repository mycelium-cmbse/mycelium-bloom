// ------------------------------------------------------------------------------------------------
// <copyright file="SemanticIconResolverTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Presentation
{
    using Mycelium.Bloom.Core.Presentation;

    [TestFixture]
    public sealed class SemanticIconResolverTestFixture
    {
        [TestCase("Attribute", "hash")]
        [TestCase("Attribute definition", "hash")]
        [TestCase("Port definition", "circle-dot")]
        [TestCase("Item", "box")]
        [TestCase("Interface", "plug")]
        [TestCase("Interface definition", "plug")]
        [TestCase("Connection", "link")]
        [TestCase("Connection definition", "link")]
        [TestCase("Binding connector", "link")]
        [TestCase("Association", "link")]
        [TestCase("Flow", "move-right")]
        [TestCase("Flow definition", "move-right")]
        [TestCase("Succession flow", "move-right")]
        [TestCase("Allocation", "git-merge")]
        [TestCase("Allocation definition", "git-merge")]
        [TestCase("View definition", "network")]
        [TestCase("Metadata definition", "tags")]
        [TestCase("Enumeration definition", "list")]
        [TestCase("Verification case", "clipboard-check")]
        [TestCase("Verification case definition", "clipboard-check")]
        [TestCase("Analysis case", "chart-no-axes-combined")]
        [TestCase("Analysis case definition", "chart-no-axes-combined")]
        [TestCase("Satisfy requirement", "list-checks")]
        [TestCase("Requirement", "file-check")]
        [TestCase("Requirement definition", "file-check")]
        [TestCase("State", "circle-dot")]
        [TestCase("State definition", "circle-dot")]
        [TestCase("Exhibit state", "circle-dot")]
        [TestCase("Calculation", "calculator")]
        [TestCase("Calculation definition", "calculator")]
        [TestCase("Action", "activity")]
        [TestCase("Action definition", "activity")]
        [TestCase("Perform action", "activity")]
        [TestCase("Constraint", "square-equal")]
        [TestCase("Constraint definition", "square-equal")]
        [TestCase("Constraint assertion", "square-equal")]
        [TestCase("Feature", "diamond")]
        [TestCase("Namespace", "folder")]
        public void VerifyGetElementSymbol(string semanticKind, string expectedSymbol)
        {
            Assert.That(SemanticIconResolver.GetElementSymbol(semanticKind), Is.EqualTo(expectedSymbol));
        }

        [TestCase("Unknown kind")]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t\r\n")]
        [TestCase("attribute")]
        [TestCase("ATTRIBUTE")]
        [TestCase("Attribute Definition")]
        [TestCase(" Attribute")]
        [TestCase("Attribute ")]
        [TestCase("Package")]
        [TestCase("Library package")]
        [TestCase("Part definition")]
        [TestCase("Part")]
        [TestCase("Item definition")]
        [TestCase("Port")]
        [TestCase("View")]
        public void VerifyGetElementSymbolReturnsFallback(string semanticKind)
        {
            Assert.That(SemanticIconResolver.GetElementSymbol(semanticKind), Is.EqualTo("shapes"));
        }

        [TestCase("Imported", "arrow-down-to-line")]
        [TestCase("Library", "library")]
        [TestCase("Standard library", "badge-check")]
        [TestCase("Metadata", "tag")]
        [TestCase("Variation", "git-branch")]
        [TestCase("Conjugated", "rotate-cw")]
        [TestCase("Parallel", "columns-2")]
        public void VerifyGetIndicatorIcon(string indicator, string expectedIcon)
        {
            Assert.That(SemanticIconResolver.GetIndicatorIcon(indicator), Is.EqualTo(expectedIcon));
        }

        [TestCase("Unknown indicator")]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t\r\n")]
        [TestCase("imported")]
        [TestCase("IMPORTED")]
        [TestCase("Standard Library")]
        [TestCase(" Imported")]
        [TestCase("Imported ")]
        [TestCase("Documentation")]
        [TestCase("Verdict: pass")]
        public void VerifyGetIndicatorIconReturnsNull(string indicator)
        {
            Assert.That(SemanticIconResolver.GetIndicatorIcon(indicator), Is.Null);
        }
    }
}
