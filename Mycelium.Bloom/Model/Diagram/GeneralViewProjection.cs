// ------------------------------------------------------------------------------------------------
// <copyright file="GeneralViewProjection.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Diagram
{
    using System.Collections.Immutable;

    using SysML2.NET.Core.POCO.Root.Elements;

    /// <summary>Identifies one graphical occurrence independently of its canonical semantic element.</summary>
    /// <param name="Id">The view-local graphical identity.</param>
    /// <param name="Element">The borrowed canonical SDK object.</param>
    /// <param name="X">The horizontal model-space coordinate.</param>
    /// <param name="Y">The vertical model-space coordinate.</param>
    /// <param name="Width">The positive occurrence width.</param>
    /// <param name="Height">The positive occurrence height.</param>
    public sealed record DiagramOccurrence(Guid Id, IElement Element, double X, double Y, double Width = 240, double Height = 160);

    /// <summary>Selects a reusable general-node shape.</summary>
    public enum DiagramNodeKind
    {

        /// <summary>A square-cornered definition.</summary>
        Definition,

        /// <summary>A rounded usage.</summary>
        Usage,

        /// <summary>A dotted reference usage.</summary>
        Reference,

        /// <summary>A tabbed package.</summary>
        Package,

        /// <summary>A folded-corner comment.</summary>
        Comment,

        /// <summary>A general element without specialized notation.</summary>
        Element
    }

    /// <summary>Identifies one of the four occurrence boundaries.</summary>
    public enum BoundarySide
    {

        /// <summary>The left boundary.</summary>
        Left,

        /// <summary>The right boundary.</summary>
        Right,

        /// <summary>The top boundary.</summary>
        Top,

        /// <summary>The bottom boundary.</summary>
        Bottom
    }

    /// <summary>Selects an endpoint symbol from the shared graphical vocabulary.</summary>
    public enum DiagramMarker
    {

        /// <summary>No endpoint symbol.</summary>
        None,

        /// <summary>Composite membership diamond.</summary>
        FilledDiamond,

        /// <summary>Noncomposite membership diamond.</summary>
        HollowDiamond,

        /// <summary>Hollow specialization triangle.</summary>
        Triangle,

        /// <summary>Open dependency arrow.</summary>
        OpenArrow,

        /// <summary>Crossed open arrow.</summary>
        CrossedArrow,

        /// <summary>Feature typing triangle with two dots.</summary>
        TypingTriangle,

        /// <summary>Redefinition triangle with a bar.</summary>
        BarredTriangle
    }

    /// <summary>Selects supported generic relationship notation.</summary>
    public enum DiagramRelationshipKind
    {

        /// <summary>Composite feature membership.</summary>
        CompositeMembership,

        /// <summary>Noncomposite feature membership.</summary>
        NoncompositeMembership,

        /// <summary>Classifier specialization.</summary>
        Subclassification,

        /// <summary>Feature typing.</summary>
        Definition,

        /// <summary>Feature subsetting.</summary>
        Subsetting,

        /// <summary>Feature redefinition.</summary>
        Redefinition,

        /// <summary>Binary connection.</summary>
        Connection,

        /// <summary>Binding connector.</summary>
        Binding,

        /// <summary>Binary dependency.</summary>
        Dependency,

        /// <summary>Comment annotation link.</summary>
        Annotation
    }

    /// <summary>Describes a titled textual or graphical compartment without owning semantic data.</summary>
    /// <param name="Title">The caller-selected compartment heading.</param>
    /// <param name="Rows">The already-formatted textual member rows.</param>
    /// <param name="IsTruncated">Whether additional textual members are omitted.</param>
    /// <param name="GraphicalView">The optional nested graphical presentation.</param>
    public sealed record DiagramCompartmentPresentation(string Title, ImmutableArray<string> Rows, bool IsTruncated = false, GeneralViewProjection GraphicalView = null);

    /// <summary>Places a canonical port or parameter on one occurrence boundary.</summary>
    /// <param name="Element">The borrowed canonical SDK object.</param>
    /// <param name="Label">The already-formatted visible notation.</param>
    /// <param name="Side">The occurrence boundary that carries this feature.</param>
    /// <param name="Offset">The normalized position along that boundary.</param>
    /// <param name="Direction">The optional canonical direction name.</param>
    public sealed record DiagramBoundaryPresentation(IElement Element, string Label, BoundarySide Side, double Offset, string Direction = null);

    /// <summary>Carries SDK declaration text and caller-selected graphical name metadata.</summary>
    /// <param name="Keyword">The graphical BNF keyword, without guillemets.</param>
    /// <param name="Declaration">The declaration emitted by the SDK.</param>
    /// <param name="Aliases">The selected alias names.</param>
    /// <param name="SymbolName">The optional Bloom symbol name.</param>
    /// <param name="AdditionalKeywords">Optional subsequent BNF keyword lines for a specialized name compartment.</param>
    public sealed record DiagramNamePresentation(string Keyword, string Declaration, ImmutableArray<string> Aliases, string SymbolName = null,
        ImmutableArray<string> AdditionalKeywords = default);

    /// <summary>Combines reusable node slots for one canonical element occurrence.</summary>
    /// <param name="Occurrence">The graphical identity and geometry.</param>
    /// <param name="Kind">The graphical notation variant.</param>
    /// <param name="Name">The formatted name compartment.</param>
    /// <param name="Compartments">The compartment stack in caller-specified order.</param>
    /// <param name="Boundary">The boundary ports and parameters.</param>
    /// <param name="Body">The optional comment text.</param>
    /// <param name="NestedView">The optional graphical view preceding the compartment stack.</param>
    public sealed record DiagramNodePresentation(DiagramOccurrence Occurrence, DiagramNodeKind Kind, DiagramNamePresentation Name,
        ImmutableArray<DiagramCompartmentPresentation> Compartments, ImmutableArray<DiagramBoundaryPresentation> Boundary, string Body = null,
            GeneralViewProjection NestedView = null);

    /// <summary>Combines optional role, multiplicity and adornment notation at a relationship end.</summary>
    /// <param name="Role">The optional end role name.</param>
    /// <param name="Multiplicity">The optional SDK multiplicity notation.</param>
    /// <param name="Adornments">The optional end adornments.</param>
    public sealed record DiagramEndLabel(string Role = null, string Multiplicity = null, string Adornments = null)
    {

        /// <summary>Formats the supplied end notation without inventing absent labels.</summary>
        /// <returns>The nonempty end labels separated by spaces.</returns>
        public override string ToString() => string.Join(" ", new[] { this.Role, this.Multiplicity, this.Adornments }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    /// <summary>Stores a model-space routing point without modifying canonical geometry.</summary>
    /// <param name="X">The horizontal model-space coordinate.</param>
    /// <param name="Y">The vertical model-space coordinate.</param>
    public sealed record DiagramPoint(double X, double Y);

    /// <summary>Connects graphical occurrences while retaining the canonical relationship or annotation.</summary>
    /// <param name="Id">The view-local graphical identity.</param>
    /// <param name="Element">The borrowed canonical SDK object.</param>
    /// <param name="Source">The source graphical occurrence identity.</param>
    /// <param name="Target">The target graphical occurrence identity.</param>
    /// <param name="Kind">The graphical notation variant.</param>
    /// <param name="Label">The already-formatted visible notation.</param>
    /// <param name="SourceLabel">The optional source-end notation.</param>
    /// <param name="TargetLabel">The optional target-end notation.</param>
    /// <param name="SourceFeature">The optional canonical boundary endpoint or source node element.</param>
    /// <param name="TargetFeature">The optional canonical boundary endpoint or target node element.</param>
    /// <param name="Bends">The optional model-space polyline bends.</param>
    public sealed record DiagramEdgePresentation(Guid Id, IElement Element, Guid Source, Guid Target, DiagramRelationshipKind Kind,
        string Label, DiagramEndLabel SourceLabel = null, DiagramEndLabel TargetLabel = null, IElement SourceFeature = null, IElement TargetFeature = null,
            ImmutableArray<DiagramPoint> Bends = default)
    {

        /// <summary>Gets the marker located at the canonical source end.</summary>
        public DiagramMarker SourceMarker => this.Kind switch
        {
            DiagramRelationshipKind.CompositeMembership => DiagramMarker.FilledDiamond,
            DiagramRelationshipKind.NoncompositeMembership => DiagramMarker.HollowDiamond,
            _ => DiagramMarker.None
        };

        /// <summary>Gets the marker located at the canonical target end.</summary>
        public DiagramMarker TargetMarker => this.Kind switch
        {
            DiagramRelationshipKind.Subclassification or DiagramRelationshipKind.Subsetting => DiagramMarker.Triangle,
            DiagramRelationshipKind.Definition => DiagramMarker.TypingTriangle,
            DiagramRelationshipKind.Dependency => DiagramMarker.OpenArrow,
            DiagramRelationshipKind.Redefinition => DiagramMarker.BarredTriangle,
            _ => DiagramMarker.None
        };

        /// <summary>Gets whether the generic relationship uses a dashed line.</summary>
        public bool IsDashed => this.Kind is DiagramRelationshipKind.Dependency or DiagramRelationshipKind.Annotation;
    }

    /// <summary>Describes an explicit immutable general-view slice and any omitted content.</summary>
    /// <param name="Nodes">The initialized node presentation collection.</param>
    /// <param name="Edges">The initialized edge presentation collection.</param>
    /// <param name="ViewName">The optional name displayed in the view frame.</param>
    /// <param name="HasOmittedContent">Whether requested content was unavailable or omitted.</param>
    public sealed record GeneralViewProjection(ImmutableArray<DiagramNodePresentation> Nodes, ImmutableArray<DiagramEdgePresentation> Edges,
        string ViewName = null, bool HasOmittedContent = false);
}
