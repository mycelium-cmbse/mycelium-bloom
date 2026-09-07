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
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Mycelium.Bloom.Core.ModelLoading;

    using SysML2.NET.Dal;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    using DtoNamespace = SysML2.NET.Core.DTO.Root.Namespaces.Namespace;

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
            var rootId = Guid.Parse("57bd8652-a14a-4a77-94a4-51ab3df0dc1f");
            var childId = Guid.Parse("1ff92391-af9d-4d5b-957d-4363ea03d018");
            var assembler = new Assembler();
            assembler.Synchronize(
            [
                new DtoNamespace { Id = rootId, ElementId = "root", DeclaredName = "Root" },
                new DtoNamespace { Id = childId, ElementId = "part/alpha value", DeclaredName = "Child" }
            ]);
            var root = (Namespace)assembler.Cache[rootId].Value;
            var child = assembler.Cache[childId].Value;
            var modelLoader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            modelLoader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var resolver = new ElementIdResolver(modelLoader.Object, assembler);

            var resolved = await resolver.ResolveAsync("part/alpha value", CancellationToken.None);
            var wrongCase = await resolver.ResolveAsync("PART/ALPHA VALUE", CancellationToken.None);
            var unknown = await resolver.ResolveAsync("unknown", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved, Is.SameAs(child));
                Assert.That(wrongCase, Is.Null);
                Assert.That(unknown, Is.Null);
                modelLoader.Verify(service => service.LoadQuantitiesModel(), Times.Exactly(3));
            }
        }

        /// <summary>
        /// Verifies duplicate stable identifiers are unresolved without affecting other canonical identities.
        /// </summary>
        [Test]
        public async Task VerifyResolveAsyncRejectsDuplicateElementIdentifiers()
        {
            var rootId = Guid.Parse("4a8987a5-c211-4422-a3e5-50610707bc26");
            var uniqueId = Guid.Parse("c39022a6-88d0-4b05-951a-97120dbf5d17");
            var assembler = new Assembler();
            assembler.Synchronize(
            [
                new DtoNamespace { Id = rootId, ElementId = "duplicate", DeclaredName = "First" },
                new DtoNamespace
                {
                    Id = Guid.Parse("d04d2c52-0cc9-4ef9-8da3-38da9173b24f"),
                    ElementId = "duplicate",
                    DeclaredName = "Second"
                },
                new DtoNamespace { Id = uniqueId, ElementId = "unique", DeclaredName = "Unique" }
            ]);
            var root = (Namespace)assembler.Cache[rootId].Value;
            var modelLoader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            modelLoader.Setup(service => service.LoadQuantitiesModel()).Returns(root);
            var resolver = new ElementIdResolver(modelLoader.Object, assembler);

            var duplicate = await resolver.ResolveAsync("duplicate", CancellationToken.None);
            var repeatedDuplicate = await resolver.ResolveAsync("duplicate", CancellationToken.None);
            var resolvedUnique = await resolver.ResolveAsync("unique", CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(duplicate, Is.Null);
                Assert.That(repeatedDuplicate, Is.Null);
                Assert.That(resolvedUnique, Is.SameAs(assembler.Cache[uniqueId].Value));
            }
        }

        /// <summary>
        /// Verifies empty runtime identifiers remain unresolved without loading the model.
        /// </summary>
        [Test]
        public async Task VerifyResolveAsyncIgnoresEmptyRuntimeInput()
        {
            var modelLoader = new Mock<IModelLoaderService>(MockBehavior.Strict);
            var resolver = new ElementIdResolver(modelLoader.Object, new Assembler());

            var resolved = await resolver.ResolveAsync(" ", CancellationToken.None);

            Assert.That(resolved, Is.Null);
            modelLoader.Verify(service => service.LoadQuantitiesModel(), Times.Never);
        }
    }
}
