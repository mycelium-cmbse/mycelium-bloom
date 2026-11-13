// ------------------------------------------------------------------------------------------------
// <copyright file="DetailsPanelViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel
{
    using System;
    using System.Linq;

    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using Mycelium.SDK;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class DetailsPanelViewModelTestFixture
    {
        [Test]
        public void VerifyInspectionOfSelectedStableIdentityReturnsToSelection()
        {
            var selected = new Namespace { ElementId = "selected", DeclaredName = "Selected" };
            var reference = new Namespace { ElementId = "reference", DeclaredName = "Reference" };
            var context = new ContextAwareService { SelectedElement = selected };
            using var viewModel = new DetailsPanelViewModel(context);
            viewModel.Inspect(reference);
            viewModel.Inspect(new Namespace { ElementId = selected.ElementId });

            Assert.That(context.SelectedElement, Is.SameAs(selected));
            Assert.That(context.InspectedElement, Is.Null);
            Assert.That(viewModel.Presentation.Element, Is.SameAs(selected));
            Assert.That(viewModel.IsInspecting, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void VerifyInspectionWithoutStableIdentityPreservesDisplayedElement(string elementId)
        {
            var selected = new Namespace { ElementId = "selected" };
            var context = new ContextAwareService { SelectedElement = selected };
            using var viewModel = new DetailsPanelViewModel(context);
            viewModel.Inspect(new Namespace { ElementId = elementId });

            Assert.That(context.SelectedElement, Is.SameAs(selected));
            Assert.That(context.InspectedElement, Is.Null);
            Assert.That(viewModel.Presentation.Element, Is.SameAs(selected));
        }

        [Test]
        public void VerifyPresentationCachesSelectionAndSupportsExplicitRefresh()
        {
            var element = new Namespace { DeclaredName = "Initial" };
            var selection = new ContextAwareService { SelectedElement = element };
            using var viewModel = new DetailsPanelViewModel(selection);
            var initial = viewModel.Presentation;
            Assert.That(initial.Element, Is.SameAs(element));
            element.DeclaredName = "Updated";
            selection.SelectedElement = element;
            selection.LifecycleState = ProjectLifecycleKind.Open;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Presentation, Is.SameAs(initial));
                Assert.That(initial.DisplayName, Is.EqualTo("Initial"));
            }
            viewModel.Refresh();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Presentation, Is.Not.SameAs(initial));
                Assert.That(viewModel.Presentation.DisplayName, Is.EqualTo("Updated"));
                Assert.That(viewModel.Presentation.Element, Is.SameAs(element));
            }
        }

        [Test]
        public void VerifyInspectPreservesCanonicalReplacementAndNullSelection()
        {
            var first = new Namespace { ElementId = "first", DeclaredName = "First", Id = Guid.NewGuid() };
            var second = new Namespace { ElementId = "second", DeclaredName = "Second", Id = first.Id };
            var selection = new ContextAwareService { SelectedElement = first };
            using var viewModel = new DetailsPanelViewModel(selection);
            viewModel.Inspect(second);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(first));
                Assert.That(selection.InspectedElement, Is.SameAs(second));
                Assert.That(viewModel.Presentation.Element, Is.SameAs(second));
            }
            viewModel.ClearSelection();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.Null);
                Assert.That(viewModel.Presentation, Is.Null);
            }
        }

        [Test]
        public void VerifyReferenceInspectionDoesNotNotifySelectionObservers()
        {
            var initial = new Namespace { ElementId = "initial", DeclaredName = "Initial" };
            var inspected = new Namespace { ElementId = "inspected", DeclaredName = "Inspected" };
            var replacement = new Namespace { DeclaredName = "Replacement" };
            var selection = new ContextAwareService { SelectedElement = initial };
            using var viewModel = new DetailsPanelViewModel(selection);
            var selectionChanges = 0;
            selection.PropertyChanged += (_, eventArgs) =>
            {
                if (eventArgs.PropertyName != nameof(selection.SelectedElement))
                {
                    return;
                }

                selectionChanges++;
                if (ReferenceEquals(selection.SelectedElement, inspected))
                {
                    selection.SelectedElement = replacement;
                }
            };

            viewModel.Inspect(inspected);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectionChanges, Is.Zero);
                Assert.That(selection.SelectedElement, Is.SameAs(initial));
                Assert.That(viewModel.Presentation.Element, Is.SameAs(inspected));
            }
        }

        [Test]
        public void VerifyDisposeDetachesOnlyOwnedSubscriptions()
        {
            var selection = new ContextAwareService { SelectedElement = new Namespace { DeclaredName = "Initial" } };
            var first = new DetailsPanelViewModel(selection);
            using var second = new DetailsPanelViewModel(selection);
            var retained = first.Presentation;
            first.Dispose();
            first.Dispose();
            selection.SelectedElement = new Namespace { DeclaredName = "Next" };
            first.Refresh();
            first.Inspect(new Namespace());
            first.ClearSelection();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Presentation, Is.SameAs(retained));
                Assert.That(second.Presentation.Element, Is.SameAs(selection.SelectedElement));
                Assert.That(second.Presentation.DisplayName, Is.EqualTo("Next"));
            }
        }
    }
}
