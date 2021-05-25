using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.PanelProperties;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;

using UnitsNet;
using UnitsNet.Units;
#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Panels class.
	/// </summary>
	public class PanelList : SPMObjectList<PanelObject, PanelGeometry>
	{

		#region Constructors

		/// <summary>
		///		Create a panel list.
		/// </summary>
		/// <inheritdoc />
		private PanelList(ObjectId blockTableId)
			: base(blockTableId)
		{
		}

		/// <summary>
		///		Create a panel list.
		/// </summary>
		/// <inheritdoc />
		private PanelList(IEnumerable<PanelObject> panelObjects, ObjectId blockTableId)
			: base(panelObjects, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the collection of panels in the drawing.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		private static IEnumerable<Solid?>? GetObjects(Document document) => document.GetObjects(Layer.Panel)?.Cast<Solid?>();

		/// <summary>
		///     Read all the <see cref="PanelObject" />'s in the drawing.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static PanelList From(Document document, LengthUnit unit)
		{
			var solids = GetObjects(document)?
				.Where(s => s is not null)
				.ToArray();
			var bId    = document.Database.BlockTableId;

			return solids.IsNullOrEmpty() 
				? new PanelList(bId)
				: new PanelList(solids.Select(s => PanelObject.From(s!, unit)), bId);
		}

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="vertices">The collection of four <see cref="Point" /> vertices, in any order.</param>
		public bool Add(IEnumerable<Point>? vertices, bool raiseEvents = true, bool sort = true) => vertices is not null && Add(new PanelObject(vertices, BlockTableId), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="vertices">The panel <see cref="Vertices" /> object.</param>
		public bool Add(Vertices vertices, bool raiseEvents = true, bool sort = true) => Add(new PanelObject(vertices, BlockTableId), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <param name="verticesCollection">The collection of <see cref="Vertices" />'s that represents the panels.</param>
		public int AddRange(IEnumerable<Vertices>? verticesCollection, bool raiseEvents = true, bool sort = true) => AddRange(verticesCollection?.Select(v => new PanelObject(v, BlockTableId)), raiseEvents, sort);

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
		public IEnumerable<PanelObject?> GetByVertices(IEnumerable<Vertices> vertices) =>
			this.Where(p => vertices.Contains(p.Vertices));

		/// <summary>
		///     Get the <see cref="Panel" />'s associated to objects in this collection.
		/// </summary>
		/// <inheritdoc cref="PanelObject.GetElement(IEnumerable{Node}, ElementModel)" />
		[return: NotNull]
		public IEnumerable<Panel> GetElements(IEnumerable<Node> nodes, ElementModel elementModel = ElementModel.Elastic) =>
			this.Select(s => s.GetElement(nodes, elementModel));

		/// <summary>
		///     Get the list of <see cref="PanelGeometry" />'s from this collection.
		/// </summary>
		public IEnumerable<PanelGeometry> GetGeometries() => GetProperties();

		/// <summary>
		///     Get the list of distinct <see cref="WebReinforcementDirection" />'s in this collection.
		/// </summary>
		public IEnumerable<WebReinforcementDirection> GetReinforcementDirections() => 
			this.SelectMany(p => new[] { p.Reinforcement?.DirectionX, p.Reinforcement?.DirectionY })
				.Where(r => r is not null)
				.Distinct()
				.OrderBy(r => r)!;

		/// <summary>
		///     Get the list of distinct <see cref="WebReinforcement" />'s in this collection.
		/// </summary>
		public IEnumerable<WebReinforcement> GetReinforcements() => 
			this.Select(p => p.Reinforcement)
				.Where(r => r is not null)
				.Distinct()
				.OrderBy(r => r)!;

		/// <summary>
		///     Get the list of distinct <see cref="Steel" />'s in this collection.
		/// </summary>
		public IEnumerable<Steel> GetSteels() => 
			this.SelectMany(p => new[] { p.Reinforcement?.DirectionX?.Steel, p.Reinforcement?.DirectionY?.Steel })
				.Where(s => s is not null)
				.Distinct()
				.OrderBy(r => r)!;

		/// <summary>
		///     Get the list of <see cref="Vertices" />'s from this collection.
		/// </summary>
		public IEnumerable<Vertices> GetVertices() => 
			GetGeometries()
				.Select(g => g.Vertices);

		/// <summary>
		///     Get the list of distinct widths from this collection.
		/// </summary>
		public IEnumerable<Length> GetWidths() => 
			GetGeometries()
				.Select(g => g.Width)
				.Distinct()
				.OrderBy(w => w);

		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		/// <param name="vertices">The <see cref="Vertices" /> of panel to remove.</param>
		public bool Remove(Vertices vertices, bool raiseEvents = true, bool sort = true) => Remove(new PanelObject(vertices, BlockTableId), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		/// <param name="vertices">The collection of <see cref="Vertices" /> of panels to remove.</param>
		public int RemoveRange(IEnumerable<Vertices>? vertices, bool raiseEvents = true, bool sort = true) => RemoveRange(vertices?.Select(v => new PanelObject(v, BlockTableId)), raiseEvents, sort);

		/// <summary>
		///     Update all the panels in this collection from drawing.
		/// </summary>
		public void Update()
		{
			Clear(false);

			var model = SPMModel.GetOpenedModel(BlockTableId)!;

			AddRange(From(model.AcadDocument, model.Settings.Units.Geometry), false);
		}

		#endregion

	}
}