// ------------------------------------------------------------------------------------------------
// <copyright file="ElementSelectionServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Selection
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Core.Selection;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests the <see cref="ElementSelectionService" />.
    /// </summary>
    [TestFixture]
    public sealed class ElementSelectionServiceTestFixture
    {
        /// <summary>
        /// The expected notification order for the first selection.
        /// </summary>
        private static readonly string[] ExpectedFirstSelectionCallbacks =
            ["changing:null", "changed:selected"];

        /// <summary>
        /// Verifies the initial state and current-value observable behavior.
        /// </summary>
        [Test]
        public void VerifySelectedElementInitialState()
        {
            var service = new ElementSelectionService();
            var observedValues = new List<IElement>();

            using var subscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(selection => selection.SelectedElement),
                observedValues.Add);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.SelectedElement, Is.Null);
                Assert.That(observedValues, Is.EqualTo(new IElement[] { null }));
            }
        }

        /// <summary>
        /// Verifies first-selection state and notification ordering.
        /// </summary>
        [Test]
        public void VerifySelectedElementPublishesAfterStateChanges()
        {
            var service = new ElementSelectionService();
            var element = new Namespace();
            var callbacks = new List<string>();

            service.PropertyChanging += (_, args) =>
            {
                if (args.PropertyName == nameof(service.SelectedElement))
                {
                    callbacks.Add(service.SelectedElement == null ? "changing:null" : "changing:selected");
                }
            };

            service.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(service.SelectedElement))
                {
                    callbacks.Add(ReferenceEquals(service.SelectedElement, element)
                        ? "changed:selected"
                        : "changed:other");
                }
            };

            service.SelectedElement = element;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.SelectedElement, Is.SameAs(element));
                Assert.That(callbacks, Is.EqualTo(ExpectedFirstSelectionCallbacks));
            }
        }

        /// <summary>
        /// Verifies that selecting the same object reference twice is silent.
        /// </summary>
        [Test]
        public void VerifySelectedElementIsSilentForSameReference()
        {
            var service = new ElementSelectionService();
            var element = new Namespace();
            var propertyChangedCount = 0;

            service.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(service.SelectedElement))
                {
                    propertyChangedCount++;
                }
            };

            service.SelectedElement = element;
            service.SelectedElement = element;

            Assert.That(propertyChangedCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that distinct references with the same ElementId still publish.
        /// </summary>
        [Test]
        public void VerifySelectedElementPublishesForDistinctReferencesWithSameElementId()
        {
            var service = new ElementSelectionService();
            var firstElement = new Namespace { ElementId = "shared-id" };
            var secondElement = new Namespace { ElementId = "shared-id" };
            var observedValues = new List<IElement>();

            using var subscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(selection => selection.SelectedElement),
                observedValues.Add);

            service.SelectedElement = firstElement;
            service.SelectedElement = secondElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstElement.ElementId, Is.EqualTo(secondElement.ElementId));
                Assert.That(service.SelectedElement, Is.SameAs(secondElement));
                Assert.That(observedValues, Is.EqualTo(new IElement[] { null, firstElement, secondElement }));
            }
        }

        /// <summary>
        /// Verifies that distinct references publish even when value equality returns true.
        /// </summary>
        [Test]
        public void VerifySelectedElementPublishesForDistinctReferencesThatCompareEqual()
        {
            var service = new ElementSelectionService();
            var firstElement = new Mock<IElement>();
            var secondElement = new Mock<IElement>();
            var propertyChangedCount = 0;

            firstElement.Setup(element => element.Equals(secondElement.Object)).Returns(true);
            secondElement.Setup(element => element.Equals(firstElement.Object)).Returns(true);

            service.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(service.SelectedElement))
                {
                    propertyChangedCount++;
                }
            };

            service.SelectedElement = firstElement.Object;
            service.SelectedElement = secondElement.Object;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstElement.Object, Is.Not.SameAs(secondElement.Object));
                Assert.That(firstElement.Object, Is.EqualTo(secondElement.Object));
                Assert.That(service.SelectedElement, Is.SameAs(secondElement.Object));
                Assert.That(propertyChangedCount, Is.EqualTo(2));
            }
        }

        /// <summary>
        /// Verifies clear and repeated-clear semantics.
        /// </summary>
        [Test]
        public void VerifySelectedElementNullPublishesOnlyWhenSelectionExists()
        {
            var service = new ElementSelectionService();
            var observedValues = new List<IElement>();

            using var subscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(selection => selection.SelectedElement),
                observedValues.Add);

            service.SelectedElement = null;
            service.SelectedElement = new Namespace();
            service.SelectedElement = null;
            service.SelectedElement = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.SelectedElement, Is.Null);
                Assert.That(observedValues, Has.Count.EqualTo(3));
                Assert.That(observedValues[0], Is.Null);
                Assert.That(observedValues[1], Is.Not.Null);
                Assert.That(observedValues[2], Is.Null);
            }
        }

        /// <summary>
        /// Verifies multiple subscribers and deterministic unsubscription.
        /// </summary>
        [Test]
        public void VerifySelectedElementSupportsMultipleSubscribersAndUnsubscribe()
        {
            var service = new ElementSelectionService();
            var firstSubscriberCount = 0;
            var secondSubscriberCount = 0;

            var firstSubscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(selection => selection.SelectedElement),
                _ => firstSubscriberCount++);

            using var secondSubscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(selection => selection.SelectedElement),
                _ => secondSubscriberCount++);

            service.SelectedElement = new Namespace();
            firstSubscription.Dispose();
            service.SelectedElement = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstSubscriberCount, Is.EqualTo(2));
                Assert.That(secondSubscriberCount, Is.EqualTo(3));
            }
        }

        /// <summary>
        /// Verifies independent scoped service instances do not leak selection.
        /// </summary>
        [Test]
        public void VerifyScopedInstancesKeepIndependentSelection()
        {
            var services = new ServiceCollection();
            services.AddScoped<IElementSelectionService, ElementSelectionService>();

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();

            var firstService = firstScope.ServiceProvider.GetRequiredService<IElementSelectionService>();
            var secondService = secondScope.ServiceProvider.GetRequiredService<IElementSelectionService>();
            var element = new Namespace();

            firstService.SelectedElement = element;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstService, Is.Not.SameAs(secondService));
                Assert.That(firstService.SelectedElement, Is.SameAs(element));
                Assert.That(secondService.SelectedElement, Is.Null);
            }
        }

        /// <summary>
        /// Verifies the service explicitly fulfills the ReactiveUI object contract.
        /// </summary>
        [Test]
        public void VerifyServiceImplementsReactiveObjectContract()
        {
            IElementSelectionService service = new ElementSelectionService();

            Assert.That(service, Is.AssignableTo<IReactiveObject>());
        }
    }
}
