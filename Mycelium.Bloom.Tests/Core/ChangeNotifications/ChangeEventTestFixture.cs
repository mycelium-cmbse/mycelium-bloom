// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeEventTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.ChangeNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reflection;

    using Moq;

    using Mycelium.Bloom.Core.ChangeNotifications;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Namespaces;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Decorators;

    [TestFixture]
    public sealed class ChangeEventTestFixture
    {
        private static readonly string[] ExpectedProperties = ["DeclaredName"];

        [TestCase("elementId")]
        [TestCase("commitId")]
        public void VerifyConstructorRejectsEmptyIdentifiers(string invalidParameter)
        {
            var exception = Assert.Throws<ArgumentException>(() => new ChangeEvent(ChangeKind.Updated, ChangeSource.Local,
                invalidParameter == "elementId" ? Guid.Empty : Guid.NewGuid(), typeof(Package), new NamespacePath([]), [],
                invalidParameter == "commitId" ? Guid.Empty : Guid.NewGuid()));

            Assert.That(exception.ParamName, Is.EqualTo(invalidParameter));
        }

        [TestCase(typeof(string))]
        [TestCase(typeof(object))]
        [TestCase(typeof(SysML2.NET.Core.DTO.Kernel.Packages.Package))]
        public void VerifyConstructorRejectsNonPocoMetaclasses(Type type)
        {
            Assert.That(() => new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), type,
                new NamespacePath([]), [], Guid.NewGuid()), Throws.ArgumentException);
            Assert.That(() => ChangeTarget.ForType(type), Throws.ArgumentException);
        }

        [Test]
        public void VerifyConstructorRejectsNullRequiredValues()
        {
            Assert.That(() => new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), null,
                new NamespacePath([]), [], Guid.NewGuid()), Throws.ArgumentNullException);
            Assert.That(() => new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                null, [], Guid.NewGuid()), Throws.ArgumentNullException);
            Assert.That(() => new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), null, Guid.NewGuid()), Throws.ArgumentNullException);
            Assert.That(() => ChangeTarget.ForType(null), Throws.ArgumentNullException);
        }

        [Test]
        public void VerifyConstructorRejectsUndefinedKindAndSource()
        {
            Assert.That(() => new ChangeEvent((ChangeKind)100, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), [], Guid.NewGuid()), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ChangeEvent(ChangeKind.Updated, (ChangeSource)100, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), [], Guid.NewGuid()), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void VerifyConstructorRejectsBlankPropertyNames(string name)
        {
            Assert.That(() => new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), [name], Guid.NewGuid()), Throws.ArgumentException);
        }

        [Test]
        public void VerifyConstructorRequiresPreviousContainmentExactlyForMoves()
        {
            Assert.That(() => new ChangeEvent(ChangeKind.Moved, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), [], Guid.NewGuid()), Throws.ArgumentException);
            Assert.That(() => new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), [], Guid.NewGuid(), new NamespacePath([])), Throws.ArgumentException);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerifyConstructorRejectsSelfContainment(bool previous)
        {
            var elementId = Guid.NewGuid();
            var self = new NamespacePath([elementId]);
            Assert.That(() => new ChangeEvent(ChangeKind.Moved, ChangeSource.Local, elementId, typeof(Package),
                previous ? new NamespacePath([]) : self, [], Guid.NewGuid(), previous ? self : new NamespacePath([])),
                Throws.ArgumentException);
        }

        [Test]
        public void VerifyChangedPropertiesRemainImmutableAfterPublication()
        {
            var scheduler = new HistoricalScheduler();
            using var service = new ChangeNotificationService(new Mock<IModelRefreshCoordinator>(MockBehavior.Strict).Object, scheduler);
            var input = new List<string> { "DeclaredName", "DeclaredName" };
            var change = new ChangeEvent(ChangeKind.Updated, ChangeSource.Local, Guid.NewGuid(), typeof(Package),
                new NamespacePath([]), input, Guid.NewGuid());
            var received = new List<ChangeEvent>();
            using var subscription = service.Listen().Subscribe(received.Add);
            service.Publish(change);
            input.Clear();
            input.Add("Unpublished");
            var differentSet = change.ChangedProperties.Add("OtherProperty");
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(75));

            Assert.That(received, Has.Count.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(received[0].ChangedProperties, Is.EquivalentTo(ExpectedProperties));
                Assert.That(differentSet, Does.Contain("OtherProperty"));
            }
            service.Dispose();
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        }

        [Test]
        public void VerifyTargetsNormalizeEquivalentTypesAndDistinguishScopes()
        {
            var id = Guid.NewGuid();
            var identifierTargets = new HashSet<ChangeTarget> { ChangeTarget.Element(id), ChangeTarget.Subtree(id) };
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ChangeTarget.ForType(typeof(Package)), Is.EqualTo(ChangeTarget.ForType(typeof(IPackage))));
                Assert.That(ChangeTarget.ForType(typeof(Namespace)), Is.EqualTo(ChangeTarget.ForType(typeof(INamespace))));
                Assert.That(identifierTargets, Does.Contain(ChangeTarget.Element(id)));
                Assert.That(identifierTargets, Does.Contain(ChangeTarget.Subtree(id)));
                Assert.That(ChangeTarget.Element(id), Is.Not.EqualTo(ChangeTarget.Subtree(id)));
                Assert.That(ChangeTarget.Element(id), Is.Not.EqualTo(ChangeTarget.Global));
            }
            Assert.That(() => ChangeTarget.Element(Guid.Empty), Throws.ArgumentException);
            Assert.That(() => ChangeTarget.Subtree(Guid.Empty), Throws.ArgumentException);
        }

        [Test]
        public void VerifyTargetsNormalizeEveryGeneratedPocoThroughImplementedMetaclass()
        {
            var generated = typeof(IElement).Assembly.GetTypes()
                .Where(typeof(IElement).IsAssignableFrom)
                .Select(type => (Type: type, Metadata: type.GetCustomAttribute<ClassAttribute>(false)))
                .Where(entry => entry.Metadata != null)
                .ToArray();
            var interfaces = generated.Where(entry => entry.Type.IsInterface)
                .ToDictionary(entry => entry.Metadata.XmiId, entry => entry.Type, StringComparer.Ordinal);
            var classes = generated.Where(entry => entry.Type.IsClass).ToArray();
            Assert.That(classes, Is.Not.Empty);

            using (Assert.EnterMultipleScope())
            {
                foreach (var (type, metadata) in classes)
                {
                    var metaclass = interfaces[metadata.XmiId];
                    Assert.That(metaclass.IsAssignableFrom(type), Is.True, type.FullName);
                    Assert.That(ChangeTarget.ForType(type), Is.EqualTo(ChangeTarget.ForType(metaclass)), type.FullName);
                }
            }
            TestContext.Out.WriteLine($"Verified {classes.Length} generated POCO classes against {interfaces.Count} metaclass interfaces.");
        }
    }
}
