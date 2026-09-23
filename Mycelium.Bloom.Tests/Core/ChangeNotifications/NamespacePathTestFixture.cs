// ------------------------------------------------------------------------------------------------
// <copyright file="NamespacePathTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.ChangeNotifications
{
    using System;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;
    using Mycelium.Bloom.Tests.Common;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Annotations;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class NamespacePathTestFixture
    {
        [Test]
        public void VerifyConstructorCopiesAncestryAndSupportsRoots()
        {
            var nearest = Guid.NewGuid();
            var root = Guid.NewGuid();
            var input = new[] { nearest, root };
            var path = new NamespacePath(input);
            input[0] = Guid.NewGuid();
            var differentPath = path.NamespaceIds.SetItem(0, Guid.NewGuid());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(path.NamespaceIds, Is.EqualTo(new[] { nearest, root }));
                Assert.That(path.ParentNamespaceId, Is.EqualTo(nearest));
                Assert.That(differentPath[0], Is.Not.EqualTo(nearest));
                Assert.That(new NamespacePath([]).ParentNamespaceId, Is.Null);
            }
        }

        [Test]
        public void VerifyConstructorRejectsInvalidAncestry()
        {
            var id = Guid.NewGuid();
            Assert.That(() => new NamespacePath(null), Throws.ArgumentNullException);
            Assert.That(() => new NamespacePath([Guid.Empty]), Throws.ArgumentException);
            Assert.That(() => new NamespacePath([id, id]), Throws.ArgumentException);
        }

        [Test]
        public void VerifyCaptureFollowsRealSdkOwnership()
        {
            var leaf = new Documentation { Id = Guid.NewGuid() };
            var parent = ProjectBrowserNodeTestFactory.CreateElement<Package>("parent", "Parent", leaf);
            var root = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("root", "Root", parent);
            parent.Id = Guid.NewGuid();
            root.Id = Guid.NewGuid();

            var path = NamespacePath.Capture(leaf);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(path.NamespaceIds, Is.EqualTo(new[] { parent.Id, root.Id }));
                Assert.That(NamespacePath.Capture(root).NamespaceIds, Is.Empty);
            }
        }

        [Test]
        public void VerifyCaptureCrossesNonNamespaceOwners()
        {
            var root = new Mock<INamespace>(MockBehavior.Strict);
            var intermediate = new Mock<IElement>(MockBehavior.Strict);
            var leaf = new Mock<IElement>(MockBehavior.Strict);
            var rootId = Guid.NewGuid();
            root.SetupGet(x => x.Id).Returns(rootId);
            root.SetupGet(x => x.owner).Returns((IElement)null);
            intermediate.SetupGet(x => x.owner).Returns(root.Object);
            leaf.SetupGet(x => x.owner).Returns(intermediate.Object);

            Assert.That(NamespacePath.Capture(leaf.Object).NamespaceIds, Is.EqualTo(new[] { rootId }));
        }

        [Test]
        public void VerifyCaptureRejectsCyclesAndNullElements()
        {
            var element = new Mock<IElement>(MockBehavior.Strict);
            element.SetupGet(x => x.owner).Returns(element.Object);

            Assert.That(() => NamespacePath.Capture(element.Object), Throws.InvalidOperationException);
            Assert.That(() => NamespacePath.Capture(null), Throws.ArgumentNullException);
        }
    }
}
