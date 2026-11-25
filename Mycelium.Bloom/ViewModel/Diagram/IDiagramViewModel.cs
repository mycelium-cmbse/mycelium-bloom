// ------------------------------------------------------------------------------------------------
// <copyright file="IDiagramViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel.Diagram
{
    using System.ComponentModel;

    using Mycelium.Bloom.Model.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Exposes tab-owned diagram presentation without transferring ownership to a renderer.</summary>
    public interface IDiagramViewModel : INotifyPropertyChanged, IDisposable
    {
        /// <summary>Gets this diagram's retained pointer mode.</summary>
        IDiagramToolState Tools { get; }

        /// <summary>Gets whether lifecycle and local read-only policy permit graphical movement.</summary>
        bool CanMoveNodes { get; }

        /// <summary>Gets or sets whether presentation movement is prohibited locally.</summary>
        bool IsReadOnly { get; set; }


        /// <summary>Updates a permitted occurrence position without changing canonical data.</summary>
        /// <param name="occurrenceId">The view-local occurrence identity.</param>
        /// <param name="x">The finite horizontal model-space coordinate.</param>
        /// <param name="y">The finite vertical model-space coordinate.</param>
        /// <returns>Whether movement was accepted.</returns>
        bool MoveNode(Guid occurrenceId, double x, double y);

        /// <summary>Adds a new occurrence of an existing canonical object without changing its semantic model.</summary>
        /// <param name="element">The borrowed Browser element.</param>
        /// <param name="x">The finite horizontal model-space position.</param>
        /// <param name="y">The finite vertical model-space position.</param>
        /// <returns>Whether this view accepted the graphical addition.</returns>
        bool AddElement(IElement element, double x, double y);

        /// <summary>Gets whether the current tool and editing policy permit element placement.</summary>
        bool CanAddElements { get; }

        /// <summary>Updates permitted occurrence dimensions without changing canonical data.</summary>
        /// <param name="occurrenceId">The view-local occurrence identity.</param>
        /// <param name="width">The finite width, constrained to the minimum readable rectangle.</param>
        /// <param name="height">The finite height, constrained to the minimum readable rectangle.</param>
        /// <returns>Whether resizing was accepted.</returns>
        bool ResizeNode(Guid occurrenceId, double width, double height);

        /// <summary>Gets the immutable notation projected from canonical model objects.</summary>
        DiagramProjection Projection { get; }

        /// <summary>Gets the coherent viewport retained independently of any mounted renderer.</summary>
        DiagramViewport Viewport { get; }

        /// <summary>Gets retained reactive presentation for one graphical node occurrence.</summary>
        /// <param name="occurrenceId">The graphical occurrence identity.</param>
        /// <returns>The tab-owned node state, borrowed by mounted components.</returns>
        DiagramNodeViewModel GetNodeViewModel(Guid occurrenceId);

        /// <summary>Gets retained reactive presentation for one graphical relationship occurrence.</summary>
        /// <param name="occurrenceId">The graphical relationship identity.</param>
        /// <returns>The tab-owned edge state, borrowed by mounted components.</returns>
        DiagramEdgeViewModel GetEdgeViewModel(Guid occurrenceId);

        /// <summary>Replaces validated presentation from composition without editing canonical model data.</summary>
        /// <param name="projection">The new immutable snapshot, with stable identities for retained occurrences.</param>
        void UpdateProjection(DiagramProjection projection);

        /// <summary>Selects an element represented by this view.</summary>
        /// <param name="element">The exact canonical object captured by the UI.</param>
        void Select(IElement element);

        /// <summary>Changes the requested zoom while preserving pan.</summary>
        /// <param name="percent">The finite zoom percentage requested by the controls.</param>
        void ChangeZoom(double percent);

        /// <summary>Restores native scale while preserving pan.</summary>
        void ResetZoom();

        /// <summary>Fits occurrence rectangles, bounded boundary labels and bends, within 0.1% to 100% scale; content scrolls inside each rectangle.</summary>
        /// <param name="width">The measured canvas width in pixels.</param>
        /// <param name="height">The measured canvas height in pixels.</param>
        void FitToView(double width, double height);

        /// <summary>Retains viewport changes reported by a mounted renderer.</summary>
        /// <param name="panX">The finite horizontal translation.</param>
        /// <param name="panY">The finite vertical translation.</param>
        /// <param name="zoom">The finite scale, constrained to the supported navigation range.</param>
        /// <param name="isInitialized">Whether the canvas has established its initial viewport.</param>
        void SetViewport(double panX, double panY, double zoom, bool isInitialized = true);

        /// <summary>Gets child state owned by a specific graphical occurrence in this tab.</summary>
        /// <param name="occurrenceId">The parent occurrence identity, independent of semantic identity.</param>
        /// <param name="projection">The child view's immutable notation.</param>
        /// <returns>The retained child state for this occurrence and projection.</returns>
        IDiagramViewModel GetNestedView(Guid occurrenceId, DiagramProjection projection);
    }
}
