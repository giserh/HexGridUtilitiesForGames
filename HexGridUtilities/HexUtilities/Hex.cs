﻿#region The MIT License - Copyright (C) 2012-2013 Pieter Geerkens
/////////////////////////////////////////////////////////////////////////////////////////
//                PG Software Solutions Inc. - Hex-Grid Utilities
/////////////////////////////////////////////////////////////////////////////////////////
// The MIT License:
// ----------------
// 
// Copyright (c) 2012-2013 Pieter Geerkens (email: pgeerkens@hotmail.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to the following 
// conditions:
//     The above copyright notice and this permission notice shall be 
//     included in all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//     EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//     OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
//     NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//     HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
//     WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
//     FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
//     OTHER DEALINGS IN THE SOFTWARE.
/////////////////////////////////////////////////////////////////////////////////////////
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PGNapoleonics.HexUtilities.Common;
using PGNapoleonics.HexUtilities.PathFinding;

namespace PGNapoleonics.HexUtilities {
  public interface IHex {
    /// <summary>The <c>IBoard&lt;IHex></c> on which this hex is located.</summary>
    IBoard<IHex> Board            { get; }

    /// <summary>The <c>HexCoords</c> coordinates for this hex on <c>Board</c>.</summary>
    HexCoords    Coords           { get; }

    /// <summary>Elevation "Above Sea Level" in <i>game units</i> of the ground in this hex.</summary>
    int          ElevationASL     { get; }

    /// <summary>Height ASL in <i>game units</i> of observer's eyes for FOV calculations.</summary>
    int          HeightObserver   { get; }

    /// <summary>Height ASL in <i>game units</i> of target above ground level to be spotted.</summary>
    int          HeightTarget     { get; }

    /// <summary>Height ASL in <i>game units</i> of any blocking terrian in this hex.</summary>
    int          HeightTerrain    { get; }

    /// <summary>The precalculated <b><c>PathShortcut</c>s</b> from this hex.</summary>
    IList<PathShortcut> Shortcuts { get; }

    IHex Neighbour(Hexside hexside);

    /// <summary>Cost to extend the path with the hex located across the <c>Hexside</c> at <c>direction</c>.</summary>
    int  StepCost(Hexside direction);

    /// <summary>Cost to exit this hex through the <c>Hexside</c> <c>hexsideExit</c>.</summary>
    int  DirectedStepCost(Hexside hexsideExit);
  }

  public abstract class Hex : IHex, IEquatable<Hex> {
    protected Hex(IBoard<IHex> board, HexCoords coords) { 
      Board     = board;
      Coords    = coords; 
      Shortcuts = new List<PathShortcut>(0);
    }

    /// <inheritdoc/>
    public          IBoard<IHex> Board           { get; private set; }

    /// <inheritdoc/>
    public          HexCoords    Coords          { get; private set; }

    /// <inheritdoc/>
    public abstract int          ElevationASL    { get; }

    /// <inheritdoc/>
    public virtual  int          HeightObserver  { get { return ElevationASL + 1; } }

    /// <inheritdoc/>
    public virtual  int          HeightTarget    { get { return ElevationASL + 1; } }

    /// <inheritdoc/>
    public abstract int          HeightTerrain   { get; }

    /// <inheritdoc/>
    public virtual IList<PathShortcut> Shortcuts { get; private set; }

    /// <inheritdoc/>
    public abstract int  StepCost(Hexside direction);

    /// <inheritdoc/>
    public virtual  int  DirectedStepCost(Hexside hexsideExit) {
      return Board[Coords.GetNeighbour(hexsideExit)].StepCost(hexsideExit);
    }

    public          IHex Neighbour(Hexside hexside) { return Board[Coords.GetNeighbour(hexside)]; }

    /// <inheritdoc/>
    public IEnumerator<NeighbourHex> GetEnumerator() { return this.GetNeighbourHexes().GetEnumerator(); }

    #region Value Equality
    /// <inheritdoc/>
    public override bool Equals(object obj) {
      var hex = obj as Hex;
      return hex!=null && Coords.Equals(hex.Coords);
    }

    /// <inheritdoc/>
    public override int GetHashCode()       { return Coords.GetHashCode(); }

    /// <inheritdoc/>
    bool IEquatable<Hex>.Equals(Hex rhs)    { return rhs!=null  &&  this.Coords.Equals(rhs.Coords); }
    #endregion
  }

  public static partial class HexExtensions {
    /// <summary>All neighbours of this hex, as an <c>IEnumerable&lt;NeighbourHex></c></summary>
    public static IEnumerable<NeighbourHex> GetAllNeighbours(this IHex @this) {
      return HexsideList.Select(i => new NeighbourHex(@this.Board[@this.Coords.GetNeighbour(i)], i));
    }

    /// <summary>All <i>OnBoard</i> neighbours of this hex, as an <c>IEnumerable&lt;NeighbourHex></c></summary>
    public static IEnumerable<NeighbourHex> GetNeighbourHexes(this IHex @this) { 
      return @this.GetAllNeighbours().Where(n => n.Hex!=null);
    }

    /// <inheritdoc/>
    public static IEnumerator GetEnumerator(this IHex @this) { 
      return @this.GetNeighbourHexes().GetEnumerator();
    }

    /// <summary>Returns the field-of-view on <c>board</c> from the hex specified by coordinates <c>coords</c>.</summary>
    public static IFov GetFov(this IFovBoard<IHex> @this, HexCoords origin) {
      return FovFactory.GetFieldOfView(@this,origin);
    }

    /// <summary>The <i>Manhattan</i> distance from this hex to that at <c>coords</c>.</summary>
    public static int Range(this IHex @this, IHex target) { 
      if (@this==null) throw new ArgumentNullException("this");
      if (target==null) throw new ArgumentNullException("target");
      return @this.Coords.Range(target.Coords); 
    }

    /// <summary>Returns a least-cost path from this hex to the hex <c>goal.</c></summary>
    public static IPath GetPath(this IHex @this, IHex goal) {
      if (@this==null) throw new ArgumentNullException("this");
      if (goal==null) throw new ArgumentNullException("goal");

      return Pathfinder.FindPath(@this.Coords, goal.Coords,  @this.Board);
    }

    /// <summary>Returns a least-cost path from this hex to the hex <c>goal.</c></summary>
    public static IDirectedPath GetDirectedPath(this IHex @this, IHex goal) {
      if (@this==null) throw new ArgumentNullException("this");
      if (goal==null) throw new ArgumentNullException("goal");
      if (! @this.Board.IsPassable(goal.Coords) || ! @this.Board.IsPassable(@this.Coords))
        return null;

      return goal.Coords.Range(@this.Coords) > @this.Board.RangeCutoff
            ? BidirectionalPathfinder.FindDirectedPathFwd(@this, goal, @this.Board)
            : Pathfinder.FindDirectedPath(@this, goal, @this.Board);
    }

    /// <summary>Returns whether this hex is "On Board".</summary>
    public static bool IsOnboard(this IHex @this) {
      if (@this==null) throw new ArgumentNullException("this");
      return @this.Board.IsOnboard(@this.Coords);
    }
  }
}
