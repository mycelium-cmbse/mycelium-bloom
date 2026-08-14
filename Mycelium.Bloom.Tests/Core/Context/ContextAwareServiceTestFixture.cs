// ------------------------------------------------------------------------------------------------
// <copyright file="ContextAwareServiceTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Core.Context
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.Model.Enum;

    using ReactiveUI;

    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class ContextAwareServiceTestFixture
    {
        private static readonly string[] ExpectedFirstSelectionCallbacks =
            ["changing:null", "changed:selected"];

        private static readonly ProjectLifecycleState[] ExpectedLifecycleStates =
        [
            ProjectLifecycleState.Preparation,
            ProjectLifecycleState.Open,
            ProjectLifecycleState.Review
        ];

        [Test]
        public void VerifyInitialStateAndCurrentValueObservables()
        {
            var service = new ContextAwareService();
            var observedElements = new List<IElement>();
            var observedLifecycleStates = new List<ProjectLifecycleState>();

            using var selectionSubscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(context => context.SelectedElement),
                observedElements.Add);
            using var lifecycleSubscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(context => context.LifecycleState),
                observedLifecycleStates.Add);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.SelectedElement, Is.Null);
                Assert.That(service.LifecycleState, Is.EqualTo(ProjectLifecycleState.Preparation));
                Assert.That(observedElements, Is.EqualTo(new IElement[] { null }));
                Assert.That(observedLifecycleStates,
                    Is.EqualTo(new[] { ProjectLifecycleState.Preparation }));
            }
        }

        [Test]
        public void VerifySelectedElementPublishesAfterStateChanges()
        {
            var service = new ContextAwareService();
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

        [Test]
        public void VerifySelectedElementIsSilentForSameReference()
        {
            var service = new ContextAwareService();
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

        [Test]
        public void VerifySelectedElementPublishesForDistinctReferencesWithSameElementId()
        {
            var service = new ContextAwareService();
            var firstElement = new Namespace { ElementId = "shared-id" };
            var secondElement = new Namespace { ElementId = "shared-id" };
            var observedValues = new List<IElement>();

            using var subscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(context => context.SelectedElement),
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

        [Test]
        public void VerifySelectedElementPublishesForDistinctReferencesThatCompareEqual()
        {
            var service = new ContextAwareService();
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

        [Test]
        public void VerifySelectedElementNullPublishesOnlyWhenSelectionExists()
        {
            var service = new ContextAwareService();
            var observedValues = new List<IElement>();

            using var subscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(context => context.SelectedElement),
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

        [Test]
        public void VerifySelectedElementSupportsMultipleSubscribersAndUnsubscribe()
        {
            var service = new ContextAwareService();
            var firstSubscriberCount = 0;
            var secondSubscriberCount = 0;

            var firstSubscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(context => context.SelectedElement),
                _ => firstSubscriberCount++);

            using var secondSubscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(context => context.SelectedElement),
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

        [Test]
        public void VerifyLifecycleStatePublishesDistinctValidValues()
        {
            var service = new ContextAwareService();
            var observedValues = new List<ProjectLifecycleState>();

            using var subscription = System.ObservableExtensions.Subscribe(
                service.WhenAnyValue(context => context.LifecycleState),
                observedValues.Add);

            service.LifecycleState = ProjectLifecycleState.Open;
            service.LifecycleState = ProjectLifecycleState.Open;
            service.LifecycleState = ProjectLifecycleState.Review;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.LifecycleState, Is.EqualTo(ProjectLifecycleState.Review));
                Assert.That(observedValues, Is.EqualTo(ExpectedLifecycleStates));
            }
        }

        [Test]
        public void VerifyLifecycleStateRejectsUndefinedValues()
        {
            var service = new ContextAwareService();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.LifecycleState = (ProjectLifecycleState)999);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception.ParamName, Is.EqualTo("value"));
                Assert.That(service.LifecycleState, Is.EqualTo(ProjectLifecycleState.Preparation));
            }
        }

        [Test]
        public void VerifyScopedInterfacesResolveTheSameContextInstance()
        {
            var services = new ServiceCollection();
            services.AddScoped<ContextAwareService>();
            services.AddScoped<IContextAwareService>(
                serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());
            services.AddScoped<IElementSelectionService>(
                serviceProvider => serviceProvider.GetRequiredService<ContextAwareService>());

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();

            var concreteService = firstScope.ServiceProvider.GetRequiredService<ContextAwareService>();
            var contextService = firstScope.ServiceProvider.GetRequiredService<IContextAwareService>();
            var selectionService = firstScope.ServiceProvider.GetRequiredService<IElementSelectionService>();
            var secondContextService = secondScope.ServiceProvider.GetRequiredService<IContextAwareService>();
            var element = new Namespace();

            selectionService.SelectedElement = element;
            contextService.LifecycleState = ProjectLifecycleState.Open;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(contextService, Is.SameAs(concreteService));
                Assert.That(selectionService, Is.SameAs(concreteService));
                Assert.That(secondContextService, Is.Not.SameAs(concreteService));
                Assert.That(contextService.SelectedElement, Is.SameAs(element));
                Assert.That(selectionService.SelectedElement, Is.SameAs(element));
                Assert.That(concreteService.LifecycleState, Is.EqualTo(ProjectLifecycleState.Open));
                Assert.That(secondContextService.SelectedElement, Is.Null);
                Assert.That(secondContextService.LifecycleState, Is.EqualTo(ProjectLifecycleState.Preparation));
            }
        }

        [Test]
        public void VerifyServiceImplementsReactiveObjectContracts()
        {
            var service = new ContextAwareService();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(service, Is.AssignableTo<IContextAwareService>());
                Assert.That(service, Is.AssignableTo<IElementSelectionService>());
                Assert.That(service, Is.AssignableTo<IReactiveObject>());
            }
        }
    }
}
