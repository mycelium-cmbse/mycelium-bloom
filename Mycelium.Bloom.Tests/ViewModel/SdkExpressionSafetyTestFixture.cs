// ------------------------------------------------------------------------------------------------
// <copyright file="SdkExpressionSafetyTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.Extensions.Logging.Abstractions;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.POCO.Kernel.Functions;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using SysML2.NET.Core.POCO.Kernel.FeatureValues;
    using SysML2.NET.Core.POCO.Root.Elements;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;
    using SysML2.NET.Dal;
    using SysML2.NET.Extensions;
    using SysML2.NET.Serializer.Json;
    using static Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory;

    [TestFixture]
    public sealed class SdkExpressionSafetyTestFixture
    {
        [Test]
        public async Task VerifySdkWritersInIsolatedProcess()
        {
            var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "serializer-probe", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var start = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add("vstest");
            start.ArgumentList.Add(typeof(SdkExpressionSafetyTestFixture).Assembly.Location);
            start.ArgumentList.Add($"/TestCaseFilter:FullyQualifiedName={typeof(SdkExpressionSafetyTestFixture).FullName}.{nameof(VerifyCanonicalLibraryInChildProcess)}");
            start.ArgumentList.Add("/Logger:trx;LogFileName=probe.trx");
            start.ArgumentList.Add($"/ResultsDirectory:{directory}");
            start.Environment["BLOOM_SERIALIZER_CHILD"] = "1";
            using var process = Process.Start(start);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new AssertionException("The isolated SDK writer probe exceeded its time limit.");
            }
            Assert.That(process.ExitCode, Is.Zero, await output + await error);
            var report = XDocument.Load(Path.Combine(directory, "probe.trx"));
            var counters = report.Descendants().Single(node => node.Name.LocalName == "Counters");
            Assert.That((string)counters.Attribute("passed"), Is.EqualTo("1"));
            Assert.That((string)counters.Attribute("failed"), Is.EqualTo("0"));
        }

        [Test, Explicit("Recursive SDK writers must execute in the isolated child runner.")]
        public void VerifyCanonicalLibraryInChildProcess()
        {
            if (Environment.GetEnvironmentVariable("BLOOM_SERIALIZER_CHILD") != "1")
                Assert.Ignore("Run the parent isolation test.");

            using var file = File.OpenRead(Path.Combine(TestRepository.GetDirectoryPath("Mycelium.Bloom.Tests"), "Resources", "SysML", "KernelLibrary.json.gz"));
            using var compressed = new GZipStream(file, CompressionMode.Decompress);
            var data = new DeSerializer(NullLoggerFactory.Instance)
                .DeSerialize(compressed, SerializationModeKind.JSON, SerializationTargetKind.PSM, false)
                .OfType<SysML2.NET.Core.DTO.Root.Elements.IElement>().ToList();
            var assembler = new Assembler(NullLoggerFactory.Instance);
            assembler.Synchronize(data);
            var elements = assembler.Cache.Values.Select(value => value.Value).ToArray();
            Assert.That(elements, Has.Length.EqualTo(16843));
            var expression = (IBooleanExpression)elements.Single(element => element.ElementId == "68c0c0ba-d0f8-513f-8243-be9ce46853da");
            var notation = SdkExpressionText.Create(expression);
            Assert.That(notation.UnavailableReason, Is.Null);
            Assert.That(notation.Text, Does.Contain("bool earlierFirstIncomingTransferSort : IncomingTransferSort"));
            Assert.That(notation.Text, Does.Contain("return t1First = includes(t1.endShot.successors, t2.endShot);"));
            Assert.That(new DetailsPanelPresentation(expression).ExpressionNotations.Single().Element, Is.SameAs(expression));

            var range = (SysML2.NET.Core.POCO.Kernel.Multiplicities.IMultiplicityRange)elements
                .Single(element => element.ElementId == "12db684f-2c52-5586-a738-29a8850903b4");
            var bound = elements.Single(element => element.ElementId == "2bd186c1-af44-570f-b037-570e90d46611");
            Assert.That(range.upperBound, Is.SameAs(bound));
            Assert.That(new DetailsPanelPresentation(range).ModelLinks.Single(link => link.Role == "Upper bound").Element, Is.SameAs(bound));
            var boundSnapshot = new DetailsPanelPresentation(bound);
            Assert.That(boundSnapshot.ExpressionNotations.Single().TextualNotation.Text, Is.Not.Empty);
            Assert.That(boundSnapshot.Expressions.Single().Reference, Is.SameAs(((IFeatureReferenceExpression)bound).referent));

            foreach (var sample in elements.GroupBy(element => element.GetType()).Select(group => group.First()))
            {
                var snapshot = new DetailsPanelPresentation(sample);
                Assert.That(snapshot.Element, Is.SameAs(sample));
                Assert.That(snapshot.DisplayName, Is.Not.Empty, sample.GetType().Name);
                foreach (var owned in sample.ownedElement.Concat(sample.OwnedRelationship))
                    Assert.That(snapshot.ModelLinks.Any(link => ReferenceEquals(link.Element, owned)), Is.True, sample.GetType().Name);
                if (sample is IRelationship relationship)
                    foreach (var related in relationship.Source.Concat(relationship.Target))
                        Assert.That(snapshot.ModelLinks.Any(link => ReferenceEquals(link.Element, related)), Is.True, sample.GetType().Name);
            }

            var sdkTypes = typeof(IElement).Assembly.GetExportedTypes().Where(type => typeof(IElement).IsAssignableFrom(type)).ToArray();
            Assert.That(sdkTypes.Count(type => type.IsInterface), Is.EqualTo(175));
            foreach (var type in sdkTypes.Where(type => type.IsInterface))
                Assert.That(ProjectBrowserElementPresentation.For(type).Kind, Is.Not.Empty, type.Name);
            var concrete = sdkTypes.Where(type => type.IsClass && !type.IsAbstract).ToArray();
            Assert.That(concrete, Has.Length.EqualTo(167));
            foreach (var type in concrete)
            {
                var element = (IElement)Activator.CreateInstance(type);
                var snapshot = new DetailsPanelPresentation(element);
                Assert.That(snapshot.Element, Is.SameAs(element));
                Assert.That(snapshot.DisplayName, Is.Not.Empty, type.Name);
                Assert.That(snapshot.DisplayName, Does.Not.StartWith("Unnamed"), type.Name);
                if (type.Name != "Element")
                    Assert.That(ProjectBrowserElementPresentation.For(type).Kind, Is.Not.EqualTo("Element"), type.Name);
            }

            var root = LoadQuantities();
            foreach (var (name, expected) in new[]
            {
                ("dimensions", "mRef.dimensions"),
                ("orderSum", "contravariantOrder + covariantOrder == order"),
                ("boundMatch", "isBound == mRef.isBound or (not isBound and mRef.isBound)")
            })
            {
                var selected = Descendants(root).Single(element => element.name == name);
                var snapshot = new DetailsPanelPresentation(selected);
                Assert.That(snapshot.ExpressionNotations.Select(node => node.TextualNotation.Text), Does.Contain(expected));
            }

            var valueOwner = new SysML2.NET.Core.POCO.Core.Features.Feature();
            var booleanValue = new BooleanExpression();
            Attach<FeatureValue>(valueOwner, booleanValue);
            Assert.That(SdkExpressionText.Create(booleanValue).UnavailableReason, Does.Contain("unavailable"));
            Assert.That(SdkExpressionText.Create(new Expression()).UnavailableReason, Does.Contain("unavailable"));
            var boundOwner = new SysML2.NET.Core.POCO.Kernel.Multiplicities.MultiplicityRange();
            var genericBound = new Expression();
            Attach<SysML2.NET.Core.POCO.Root.Namespaces.OwningMembership>(boundOwner, genericBound);
            Assert.That(SdkExpressionText.Create(genericBound).UnavailableReason, Does.Contain("unavailable"));
            Assert.That(SdkExpressionText.Create(Moq.Mock.Of<IInstantiationExpression>()).UnavailableReason, Does.Contain("unavailable"));
            Assert.That(SdkExpressionText.Create(new LiteralInteger { Value = 7 }).Text, Is.EqualTo("7"));
            Assert.That(SdkExpressionText.Create(new LiteralString { Value = "" }).UnavailableReason, Is.Not.Null);
        }
    }
}
