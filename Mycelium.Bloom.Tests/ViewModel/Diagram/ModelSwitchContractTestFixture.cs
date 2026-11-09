// ------------------------------------------------------------------------------------------------
// <copyright file="ModelSwitchContractTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.Diagram
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Extensions;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel;
    using Mycelium.Bloom.ViewModel.Diagram;
    using SysML2.NET.Dal;

    [TestFixture]
    public sealed class ModelSwitchContractTestFixture
    {
        private static ServiceProvider Services()
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath)
                .Returns(TestRepository.GetDirectoryPath("Mycelium.Bloom"));
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(environment.Object);
            services.AddApplicationServices();
            return services.BuildServiceProvider();
        }

        [TestCase("identical")]
        [TestCase("edited")]
        [TestCase("independent-overlap")]
        public async Task VerifyRepeatedOrOverlappingFilesNeverMergeModels(string variant)
        {
            using var services = Services();
            using var file = new WorkspaceModelFile("First", "pump");
            var bytes = await File.ReadAllBytesAsync(file.Uri.LocalPath);
            using var opening = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            Assert.That(await opening.OpenAsync("first.json", bytes.Length, _ => new MemoryStream(bytes)), Is.True);
            var session = services.GetRequiredService<WorkspaceModelSession>();
            var original = session.Current;
            using var tab = services.GetRequiredService<Func<DiagramViewModel>>()();
            tab.BindWorkspace(session);
            await tab.Preparation;
            Assert.That(tab.AddExisting(original.Root.ownedElement.Single().ElementId), Is.True);
            await tab.SelectAsync([tab.State.Occurrences[0].Id], tab.State.Occurrences[0].Id);
            var presentation = tab.State;
            var selected = services.GetRequiredService<IElementSelectionService>().SelectedElement;
            var cache = services.GetRequiredService<IAssembler>().Cache
                .ToDictionary(pair => pair.Key, pair => pair.Value.Value);
            var incoming = bytes;
            if (variant == "edited")
            {
                incoming = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("pump", "changed"));
            }

            using var independent = new WorkspaceModelFile("Independent", "other");
            if (variant == "independent-overlap")
            {
                var text = await File.ReadAllTextAsync(independent.Uri.LocalPath);
                using var json = JsonDocument.Parse(text);
                var partId = json.RootElement[2].GetProperty("@id").GetString();
                incoming = Encoding.UTF8.GetBytes(text.Replace(partId, selected.ElementId));
            }

            Assert.That(await opening.OpenAsync("incoming.json", incoming.Length, _ => new MemoryStream(incoming)),
                Is.EqualTo(variant == "identical"));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(session.Current, Is.SameAs(original));
                Assert.That(tab.State, Is.SameAs(presentation));
                Assert.That(tab.IsScopeAvailable, Is.True);
                Assert.That(services.GetRequiredService<IElementSelectionService>().SelectedElement,
                    Is.SameAs(selected));
                Assert.That(selected.DeclaredName, Is.EqualTo("pump"));
                Assert.That(services.GetRequiredService<IAssembler>().Cache.Keys, Is.EquivalentTo(cache.Keys));
                foreach (var pair in cache)
                {
                    Assert.That(services.GetRequiredService<IAssembler>().Cache[pair.Key].Value, Is.SameAs(pair.Value));
                }

                if (variant != "identical")
                {
                    Assert.That(opening.State.Error, Does.Contain("conflicts"));
                }
            }
        }

        [Test]
        public async Task VerifySwitchRetainsOldTabScopeWithoutReactivationOnReopen()
        {
            using var services = Services();
            using var first = new WorkspaceModelFile("First", "pump");
            using var second = new WorkspaceModelFile("Second", "valve");
            var bytes = await File.ReadAllBytesAsync(first.Uri.LocalPath);
            var other = await File.ReadAllBytesAsync(second.Uri.LocalPath);
            using var opening = services.GetRequiredService<Func<ModelOpeningViewModel>>()();
            var session = services.GetRequiredService<WorkspaceModelSession>();
            Assert.That(await opening.OpenAsync("first.json", bytes.Length, _ => new MemoryStream(bytes)), Is.True);
            var firstContext = session.Current;
            using var tab = services.GetRequiredService<Func<DiagramViewModel>>()();
            tab.BindWorkspace(session);
            await tab.Preparation;
            var presentation = tab.State;
            Assert.That(await opening.OpenAsync("second.json", other.Length, _ => new MemoryStream(other)), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(tab.IsScopeAvailable, Is.False);
                Assert.That(tab.CanEditPresentation, Is.False);
                Assert.That(tab.State, Is.SameAs(presentation));
                Assert.That(tab.State.ModelScope, Is.EqualTo(firstContext.Root.ElementId));
                Assert.That(tab.IndexedElementCount, Is.Zero);
                Assert.That(tab.WorkspaceDetailCache, Is.Null);
                Assert.That(tab.ApplyPresentation(presentation), Is.False);
            }

            Assert.That(await opening.OpenAsync("first.json", bytes.Length, _ => new MemoryStream(bytes)), Is.True);
            Assert.That(session.Current.Root, Is.SameAs(firstContext.Root));
            Assert.That(session.Current.Id, Is.Not.EqualTo(firstContext.Id));
            Assert.That(tab.IsScopeAvailable, Is.False);
            Assert.That(tab.State, Is.SameAs(presentation));
        }
    }
}