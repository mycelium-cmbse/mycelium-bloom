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
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.POCO.Root.Namespaces;

    [TestFixture]
    public sealed class DetailsPanelViewModelTestFixture
    {
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
            selection.LifecycleState = ProjectLifecycleState.Open;
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
            var first = new Namespace { DeclaredName = "First", Id = Guid.NewGuid() };
            var second = new Namespace { DeclaredName = "Second", Id = first.Id };
            var selection = new ContextAwareService { SelectedElement = first };
            using var viewModel = new DetailsPanelViewModel(selection);
            viewModel.Inspect(second);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(second));
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
        public void VerifySynchronousSelectionObserverCanReplaceAnInspectedElement()
        {
            var initial = new Namespace { DeclaredName = "Initial" };
            var inspected = new Namespace { DeclaredName = "Inspected" };
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
                Assert.That(selectionChanges, Is.EqualTo(2));
                Assert.That(selection.SelectedElement, Is.SameAs(replacement));
                Assert.That(viewModel.Presentation.Element, Is.SameAs(replacement));
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
