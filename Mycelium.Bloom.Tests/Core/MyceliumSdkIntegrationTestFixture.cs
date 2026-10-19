// ------------------------------------------------------------------------------------------------
// <copyright file="MyceliumSdkIntegrationTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core
{
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.ModelLoading;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.SDK;
    using Mycelium.SDK.DTO;
    using Mycelium.SDK.Serializer.Json;

    using SysML2.NET.Dal;

    using SysMLDeSerializer = SysML2.NET.Serializer.Json.IDeSerializer;

    [TestFixture]
    public sealed class MyceliumSdkIntegrationTestFixture
    {
        [TestCase(ProjectLifecycleKind.Preparation)]
        [TestCase(ProjectLifecycleKind.Open)]
        [TestCase(ProjectLifecycleKind.Review)]
        [TestCase(ProjectLifecycleKind.Archived)]
        public void VerifySdkProjectLifecycleFlowsIntoApplicationContext(ProjectLifecycleKind lifecycle)
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();
            using var stream = new MemoryStream();
            var serializer = scope.ServiceProvider.GetRequiredService<ISerializer>();
            var deSerializer = scope.ServiceProvider.GetRequiredService<IDeSerializer>();
            var context = scope.ServiceProvider.GetRequiredService<IContextAwareService>();
            var project = new FunctionalProject
            {
                Name = "Example project",
                Description = "Example project description",
                Lifecycle = lifecycle
            };

            serializer.Serialize(project, stream, default);
            stream.Position = 0;
            var restoredProject = (FunctionalProject)deSerializer.DeSerialize(stream).Single();
            context.LifecycleState = restoredProject.Lifecycle;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(restoredProject.Name, Is.EqualTo(project.Name));
                Assert.That(context.LifecycleState, Is.EqualTo(lifecycle));
                Assert.That(scope.ServiceProvider.GetRequiredService<IElementSelectionService>(), Is.SameAs(context));
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        public async Task VerifySdkServicesPreserveScopedSysMLModelIdentity()
        {
            var hostEnvironment = new Mock<IHostEnvironment>(MockBehavior.Strict);
            hostEnvironment.SetupGet(environment => environment.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var services = new ServiceCollection();
            services.AddSingleton(hostEnvironment.Object);
            services.AddApplicationServices();

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();
            var loader = firstScope.ServiceProvider.GetRequiredService<IModelLoaderService>();
            var assembler = firstScope.ServiceProvider.GetRequiredService<IAssembler>();
            var resolver = firstScope.ServiceProvider.GetRequiredService<IElementIdResolver>();

            var root = loader.LoadQuantitiesModel();
            var resolvedRoot = await resolver.ResolveAsync(root.ElementId, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loader.LoadQuantitiesModel(), Is.SameAs(root));
                Assert.That(resolvedRoot, Is.SameAs(root));
                Assert.That(assembler.Cache[root.Id].Value, Is.SameAs(root));
                Assert.That(root.ownedElement, Is.Not.Empty);
                Assert.That(firstScope.ServiceProvider.GetRequiredService<IDeSerializer>(), Is.TypeOf<DeSerializer>());
                Assert.That(firstScope.ServiceProvider.GetRequiredService<SysMLDeSerializer>(),
                    Is.TypeOf<SysML2.NET.Serializer.Json.DeSerializer>());
                Assert.That(secondScope.ServiceProvider.GetRequiredService<IAssembler>(), Is.Not.SameAs(assembler));
                Assert.That(secondScope.ServiceProvider.GetRequiredService<SysMLDeSerializer>(),
                    Is.Not.SameAs(firstScope.ServiceProvider.GetRequiredService<SysMLDeSerializer>()));
            }
        }
    }
}
