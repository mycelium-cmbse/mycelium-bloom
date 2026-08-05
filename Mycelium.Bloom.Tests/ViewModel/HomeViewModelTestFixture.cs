// ------------------------------------------------------------------------------------------------
// <copyright file="HomeViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System;
    using System.Collections.Generic;

    using Moq;

    using Mycelium.Bloom.Core.Selection;
    using Mycelium.Bloom.ViewModel;

    using SysML2.NET.Core.POCO.Kernel.Packages;
    using SysML2.NET.Core.POCO.Root.Elements;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests the <see cref="HomeViewModel" />.
    /// </summary>
    [TestFixture]
    public sealed class HomeViewModelTestFixture
    {
        /// <summary>
        /// Verifies the constructor rejects a null selection service.
        /// </summary>
        [Test]
        public void VerifyConstructorRejectsNullSelectionService()
        {
            Assert.That(
                () => new HomeViewModel(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property("ParamName").EqualTo("elementSelectionService"));
        }

        /// <summary>
        /// Verifies the initial no-selection projection.
        /// </summary>
        [Test]
        public void VerifySelectedElementNameReturnsNoneInitially()
        {
            var viewModel = new HomeViewModel(new ElementSelectionService());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedElement, Is.Null);
                Assert.That(viewModel.SelectedElementName, Is.EqualTo("None"));
            }
        }

        /// <summary>
        /// Verifies the declared name takes precedence over all other display-name candidates.
        /// </summary>
        [Test]
        public void VerifySelectedElementNamePrefersDeclaredName()
        {
            var service = new ElementSelectionService();
            var viewModel = new HomeViewModel(service);

            service.SelectedElement = CreateElement("Declared", "Name", "Qualified");

            Assert.That(viewModel.SelectedElementName, Is.EqualTo("Declared"));
        }

        /// <summary>
        /// Verifies the effective name is used before the qualified name when no declared name is available.
        /// </summary>
        [Test]
        public void VerifySelectedElementNamePrefersNameOverQualifiedName()
        {
            var service = new ElementSelectionService();
            var viewModel = new HomeViewModel(service);

            service.SelectedElement = CreateElement(" ", "Name", "Qualified");

            Assert.That(viewModel.SelectedElementName, Is.EqualTo("Name"));
        }

        /// <summary>
        /// Verifies the qualified name is used when declared and effective names are unavailable.
        /// </summary>
        [Test]
        public void VerifySelectedElementNameFallsBackToQualifiedName()
        {
            var service = new ElementSelectionService();
            var viewModel = new HomeViewModel(service);

            service.SelectedElement = CreateElement(" ", " ", "Qualified");

            Assert.That(viewModel.SelectedElementName, Is.EqualTo("Qualified"));
        }

        /// <summary>
        /// Verifies the runtime type is used when no model name is available.
        /// </summary>
        [Test]
        public void VerifySelectedElementNameFallsBackToRuntimeType()
        {
            var service = new ElementSelectionService();
            var viewModel = new HomeViewModel(service);

            service.SelectedElement = new Namespace { DeclaredName = " " };

            Assert.That(viewModel.SelectedElementName, Is.EqualTo(nameof(Namespace)));
        }

        /// <summary>
        /// Verifies an activated view model reacts to shared selection changes.
        /// </summary>
        [Test]
        public void VerifyActivatedViewModelReactsToSelection()
        {
            var service = new ElementSelectionService();
            var viewModel = new HomeViewModel(service);
            var changedProperties = new List<string>();

            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            using var activation = viewModel.Activator.Activate();

            var element = new LibraryPackage();
            service.SelectedElement = element;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.SelectedElement, Is.SameAs(element));
                Assert.That(viewModel.SelectedElementName, Is.EqualTo(nameof(LibraryPackage)));
                Assert.That(changedProperties, Does.Contain(nameof(viewModel.SelectedElement)));
                Assert.That(changedProperties, Does.Contain(nameof(viewModel.SelectedElementName)));
            }
        }

        /// <summary>
        /// Verifies two activated consumers observe the same shared selection.
        /// </summary>
        [Test]
        public void VerifyTwoConsumersObserveSameSelection()
        {
            var service = new ElementSelectionService();
            var firstViewModel = new HomeViewModel(service);
            var secondViewModel = new HomeViewModel(service);

            using var firstActivation = firstViewModel.Activator.Activate();
            using var secondActivation = secondViewModel.Activator.Activate();

            var element = new Namespace();
            service.SelectedElement = element;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstViewModel.SelectedElement, Is.SameAs(element));
                Assert.That(secondViewModel.SelectedElement, Is.SameAs(element));
                Assert.That(firstViewModel.SelectedElementName, Is.EqualTo(secondViewModel.SelectedElementName));
            }
        }

        /// <summary>
        /// Verifies deactivation deterministically removes selection subscriptions.
        /// </summary>
        [Test]
        public void VerifyDeactivatedViewModelDoesNotReact()
        {
            var service = new ElementSelectionService();
            var viewModel = new HomeViewModel(service);
            var selectionNotificationCount = 0;

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(viewModel.SelectedElement))
                {
                    selectionNotificationCount++;
                }
            };

            var activation = viewModel.Activator.Activate();
            selectionNotificationCount = 0;
            activation.Dispose();
            service.SelectedElement = new Namespace();

            Assert.That(selectionNotificationCount, Is.Zero);
        }

        /// <summary>
        /// Creates a minimal element with controlled derived display-name values.
        /// </summary>
        /// <param name="declaredName">The declared name.</param>
        /// <param name="name">The effective name.</param>
        /// <param name="qualifiedName">The qualified name.</param>
        /// <returns>The configured element.</returns>
        private static IElement CreateElement(string declaredName, string name, string qualifiedName)
        {
            var element = new Mock<IElement>();
            element.SetupGet(x => x.DeclaredName).Returns(declaredName);
            element.SetupGet(x => x.name).Returns(name);
            element.SetupGet(x => x.qualifiedName).Returns(qualifiedName);

            return element.Object;
        }
    }
}
