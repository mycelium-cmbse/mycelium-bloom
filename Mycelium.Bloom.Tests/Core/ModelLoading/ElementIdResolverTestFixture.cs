// ------------------------------------------------------------------------------------------------
// <copyright file="ElementIdResolverTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.ModelLoading
{
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Tests.Common;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests stable element identity resolution against the canonical loaded model.
    /// </summary>
    [TestFixture]
    public sealed class ElementIdResolverTestFixture
    {
        /// <summary>
        /// Verifies exact non-Guid identifiers return canonical model objects and unknown values remain unresolved.
        /// </summary>
        [Test]
        public async Task VerifyResolveAsyncUsesExactStableElementIdentity()
        {
            var child = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("part/alpha value", "Child");
            var root = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("root", "Root", child);
            var modelLoader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            modelLoader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var resolver = new ElementIdResolver(modelLoader.Object);

            var resolved = await resolver.ResolveAsync("part/alpha value", CancellationToken.None);
            var wrongCase = await resolver.ResolveAsync("PART/ALPHA VALUE", CancellationToken.None);
            var unknown = await resolver.ResolveAsync("unknown", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved, Is.SameAs(child));
                Assert.That(wrongCase, Is.Null);
                Assert.That(unknown, Is.Null);
                modelLoader.Verify(service => service.LoadQuantitiesModel(), Times.Once);
            }
        }

        /// <summary>
        /// Verifies duplicate stable identifiers are unresolved without affecting other canonical identities.
        /// </summary>
        [Test]
        public async Task VerifyResolveAsyncRejectsDuplicateElementIdentifiers()
        {
            var firstDuplicate = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("duplicate", "First");
            var secondDuplicate = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("duplicate", "Second");
            var unique = ProjectBrowserNodeTestFactory.CreateElement<Namespace>("unique", "Unique");
            var root = ProjectBrowserNodeTestFactory.CreateElement<Namespace>(
                "root",
                "Root",
                firstDuplicate,
                unique,
                secondDuplicate);
            var modelLoader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            modelLoader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var resolver = new ElementIdResolver(modelLoader.Object);

            var duplicate = await resolver.ResolveAsync("duplicate", CancellationToken.None);
            var resolvedUnique = await resolver.ResolveAsync("unique", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(duplicate, Is.Null);
                Assert.That(resolvedUnique, Is.SameAs(unique));
            }
        }

        /// <summary>
        /// Verifies empty runtime identifiers remain unresolved without loading the model.
        /// </summary>
        [Test]
        public async Task VerifyResolveAsyncIgnoresEmptyRuntimeInput()
        {
            var modelLoader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            var resolver = new ElementIdResolver(modelLoader.Object);

            var resolved = await resolver.ResolveAsync(" ", CancellationToken.None);

            Assert.That(resolved, Is.Null);
            modelLoader.Verify(service => service.LoadQuantitiesModel(), Times.Never);
        }
    }
}
