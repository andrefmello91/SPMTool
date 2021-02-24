﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using MathNet.Numerics;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;
using static SPMTool.Units;

#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Panels class.
	/// </summary>
	public class PanelList : SPMObjectList<PanelObject, PanelGeometry, Panel>
	{
		#region Constructors

		private PanelList()
			: base()
        {
		}

		private PanelList(IEnumerable<PanelObject> panelObjects)
			: base(panelObjects)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get the collection of panels in the drawing.
		/// </summary>
		public static IEnumerable<Solid>? GetObjects() => Layer.Panel.GetDBObjects()?.ToSolids();

		/// <summary>
		///     Read all the <see cref="PanelObject" />'s in the drawing.
		/// </summary>
		[return: NotNull]
		public static PanelList ReadFromDrawing() => ReadFromSolids(GetObjects());

		/// <summary>
		///     Read <see cref="PanelObject" />'s from a collection of <see cref="Solid" />'s.
		/// </summary>
		/// <param name="panelSolids">The collection containing the <see cref="Solid" />'s of drawing.</param>
		[return: NotNull]
		public static PanelList ReadFromSolids(IEnumerable<Solid>? panelSolids) =>
			panelSolids.IsNullOrEmpty()
				? new PanelList()
				: new PanelList(panelSolids.Select(PanelObject.ReadFromSolid));

		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		/// <param name="vertices">The <see cref="Vertices" /> of panel to remove.</param>
		public bool Remove(Vertices vertices, bool raiseEvents = true, bool sort = true) => Remove(new PanelObject(vertices), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		/// <param name="vertices">The collection of <see cref="Vertices" /> of panels to remove.</param>
		public int RemoveRange(IEnumerable<Vertices>? vertices, bool raiseEvents = true, bool sort = true) => RemoveRange(vertices?.Select(v => new PanelObject(v)), raiseEvents, sort);

		/// <summary>
		///     Get a <see cref="PanelObject" /> in this collection with the corresponding <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices">The <see cref="Vertices" /> required.</param>
		public PanelObject? GetByVertices(Vertices vertices) => Find(p => p.Vertices == vertices);

		/// <summary>
		///     Get a collection of <see cref="PanelObject" />'s in this collection with the corresponding
		///     <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices">The collection of <see cref="Vertices" /> required.</param>
		public IEnumerable<PanelObject>? GetByVertices(IEnumerable<Vertices>? vertices) => this.Where(p => vertices.Contains(p.Vertices));

		/// <summary>
		///     Update all the panels in this collection from drawing.
		/// </summary>
		public void Update()
		{
			Clear(false);

			AddRange(ReadFromDrawing(), false);
		}

		/// <summary>
		///     Get the list of <see cref="PanelGeometry" />'s from this collection.
		/// </summary>
		public List<PanelGeometry> GetGeometries() => GetProperties();

		/// <summary>
		///     Get the list of <see cref="Vertices" />'s from this collection.
		/// </summary>
		public List<Vertices> GetVertices() => GetGeometries().Select(g => g.Vertices).ToList();

		/// <summary>
		///     Get the list of distinct widths from this collection.
		/// </summary>
		public List<Length> GetWidths() => GetGeometries().Select(g => g.Width).Distinct().ToList();

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="vertices">The collection of four <see cref="Point" /> vertices, in any order.</param>
		public bool Add(IEnumerable<Point>? vertices, bool raiseEvents = true, bool sort = true) => !(vertices is null) && Add(new PanelObject(vertices), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="vertices">The panel <see cref="Vertices" /> object.</param>
		public bool Add(Vertices vertices, bool raiseEvents = true, bool sort = true) => Add(new PanelObject(vertices), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <param name="verticesCollection">The collection of <see cref="Vertices" />'s that represents the panels.</param>
		public int AddRange(IEnumerable<Vertices>? verticesCollection, bool raiseEvents = true, bool sort = true) => AddRange(verticesCollection?.Select(v => new PanelObject(v)), raiseEvents, sort);

		/// <summary>
		///     Get the <see cref="Panel" />'s associated to objects in this collection.
		/// </summary>
		/// <inheritdoc cref="PanelObject.GetElement(IEnumerable{Node}, AnalysisType)" />
		[return: NotNull]
		public List<Panel> GetElements(IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) => this.Select(s => s.GetElement(nodes, analysisType)).ToList();

		#endregion
	}
}