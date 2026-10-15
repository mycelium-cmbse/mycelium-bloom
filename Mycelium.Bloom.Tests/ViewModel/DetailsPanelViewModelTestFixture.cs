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
        public void VerifyBackPreservesCanonicalIdentityAndRepeatedInspectionDoesNotLoop()
        {
            var first = new Namespace { DeclaredName = "First" };
            var second = new Namespace { DeclaredName = "Second", Id = first.Id };
            var third = new Namespace { DeclaredName = "Third" };
            var selection = new ContextAwareService { SelectedElement = first };
            using var viewModel = new DetailsPanelViewModel(selection);
            viewModel.Inspect(second);
            viewModel.Inspect(second);
            viewModel.Inspect(third);
            viewModel.GoBack();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(second));
                Assert.That(viewModel.Presentation.Element, Is.SameAs(second));
                Assert.That(viewModel.CanGoBack, Is.True);
            }
            viewModel.Inspect(first);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(selection.SelectedElement, Is.SameAs(first));
                Assert.That(viewModel.CanGoBack, Is.False);
            }
            viewModel.GoBack();
            Assert.That(selection.SelectedElement, Is.SameAs(first));
        }

        [Test]
        public void VerifyExternalSelectionClearAndExplicitReselectionResetOnlyLocalHistory()
        {
            var first = new Namespace();
            var second = new Namespace();
            var external = new Namespace();
            var selection = new ContextAwareService { SelectedElement = first };
            using var viewModel = new DetailsPanelViewModel(selection);
            using var other = new DetailsPanelViewModel(selection);
            viewModel.Inspect(second);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.CanGoBack, Is.True);
                Assert.That(other.CanGoBack, Is.False);
            }
            viewModel.Refresh();
            Assert.That(viewModel.CanGoBack, Is.True);
            selection.SelectedElement = external;
            Assert.That(viewModel.CanGoBack, Is.False);
            viewModel.Inspect(first);
            viewModel.ResetInspectionHistory();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.CanGoBack, Is.False);
                Assert.That(selection.SelectedElement, Is.SameAs(first));
            }
            viewModel.Inspect(second);
            viewModel.ClearSelection();
            viewModel.GoBack();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.CanGoBack, Is.False);
                Assert.That(selection.SelectedElement, Is.Null);
            }
        }

        [Test]
        public void VerifyInspectionHistoryIsBoundedAndDisposedPanelCannotNavigate()
        {
            var elements = Enumerable.Range(0, 40).Select(index => new Namespace { DeclaredName = $"Element {index}" }).ToArray();
            var selection = new ContextAwareService { SelectedElement = elements[0] };
            using var viewModel = new DetailsPanelViewModel(selection);
            foreach (var element in elements.Skip(1)) viewModel.Inspect(element);
            var count = 0;
            while (viewModel.CanGoBack)
            {
                viewModel.GoBack();
                Assert.That(++count, Is.LessThanOrEqualTo(32));
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(count, Is.EqualTo(32));
                Assert.That(selection.SelectedElement, Is.SameAs(elements[7]));
            }
            viewModel.Inspect(elements[39]);
            viewModel.Dispose();
            viewModel.GoBack();
            viewModel.ResetInspectionHistory();
            Assert.That(selection.SelectedElement, Is.SameAs(elements[39]));
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
