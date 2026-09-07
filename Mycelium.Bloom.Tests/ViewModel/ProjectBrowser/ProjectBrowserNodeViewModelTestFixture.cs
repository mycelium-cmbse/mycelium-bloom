// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowserNodeViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ProjectBrowser
{
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests subtree identity lookup owned by project browser nodes.
    /// </summary>
    [TestFixture]
    public sealed class ProjectBrowserNodeViewModelTestFixture
    {
        /// <summary>
        /// Verifies subtree lookup accepts canonical references and equivalent stable element identifiers.
        /// </summary>
        [Test]
        public void VerifyFindPathToUsesCanonicalOrStableElementIdentity()
        {
            var target = ProjectBrowserNodeTestFactory.CreateNamespaceNode("target", "Target");
            var branch = ProjectBrowserNodeTestFactory.CreateNamespaceNode("branch", "Branch", target);
            var sibling = ProjectBrowserNodeTestFactory.CreateNamespaceNode("sibling", "Sibling");
            var root = ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root", branch, sibling);
            var equivalentIdentity = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("target", "Equivalent");
            var unknownIdentity = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("unknown", "Unknown");

            var canonicalPath = root.FindPathTo(target.SourceElement);
            var stableIdentityPath = root.FindPathTo(equivalentIdentity);
            var unknownPath = root.FindPathTo(unknownIdentity);
            ProjectBrowserNodeViewModel[] expectedPath = [root, branch, target];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(canonicalPath, Is.EqualTo(expectedPath));
                Assert.That(stableIdentityPath, Is.EqualTo(expectedPath));
                Assert.That(unknownPath, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies subtree lookup rejects a missing model identity.
        /// </summary>
        [Test]
        public void VerifyFindPathToRejectsNullIdentity()
        {
            var root = ProjectBrowserNodeTestFactory.CreateNamespaceNode("root", "Root");

            Assert.That(() => root.FindPathTo(null), Throws.ArgumentNullException);
        }
    }
}
