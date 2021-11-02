using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.PanelProperties;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Stringers class.
	/// </summary>
	public class StringerList : SPMObjectList<StringerObject, StringerGeometry>
	{

		#region Constructors

		/// <summary>
		///     Create a stringer list.
		/// </summary>
		/// <inheritdoc />
		private StringerList(ObjectId blockTableId)
			: base(blockTableId)
		{
		}

		/// <summary>
		///     Create a stringer list.
		/// </summary>
		/// <inheritdoc />
		private StringerList(IEnumerable<StringerObject> stringerObjects, ObjectId blockTableId)
			: base(stringerObjects, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Read all the <see cref="StringerObject" />'s in the drawing.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static StringerList From(Document document, LengthUnit unit)
		{
			var lines = GetObjects(document)?
				.Where(l => l is not null)
				.ToArray();
			var bId = document.Database.BlockTableId;

			return lines.IsNullOrEmpty()
				? new StringerList(bId)
				: new StringerList(lines.Select(l => StringerObject.From(l!, unit)), bId);
		}

		/// <summary>
		///     Get the collection of stringers in the drawing.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		private static IEnumerable<Line?>? GetObjects(Document document) => document.GetObjects(Layer.Stringer)?.Cast<Line?>();

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="startPoint">The start <see cref="Point" />.</param>
		/// <param name="endPoint">The end <see cref="Point" />.</param>
		public bool Add(Point startPoint, Point endPoint, bool raiseEvents = true, bool sort = true)
		{
			var pts = new[] { startPoint, endPoint }
				.OrderBy(p => p.Y)
				.ThenBy(p => p.X)
				.ToArray();

			// Get correct order
			var (p1, p2) = pts[1].X > pts[0].X
				? (pts[0], pts[1])
				: (pts[1], pts[0]);

			return
				Add(new StringerObject(p1, p2, BlockTableId), raiseEvents, sort);
		}

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="geometry">The <see cref="StringerGeometry" /> to add.</param>
		public bool Add(StringerGeometry geometry, bool raiseEvents = true, bool sort = true) =>
			Add(new StringerObject(geometry, BlockTableId), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <param name="geometries">The <see cref="StringerGeometry" />'s to add.</param>
		public int AddRange(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true) =>
			AddRange(geometries?.Select(g => new StringerObject(g, BlockTableId)), raiseEvents, sort);

		/// <summary>
		///     Get the list of distinct <see cref="CrossSection" />'s from objects in this collection.
		/// </summary>
		public IEnumerable<CrossSection> GetCrossSections() =>
			GetGeometries()
				.Select(g => g.CrossSection)
				.Distinct()
				.OrderBy(c => c);

		/// <summary>
		///     Get the <see cref="Stringer" />'s associated to objects in this collection.
		/// </summary>
		/// <inheritdoc cref="StringerObject.GetElement(IEnumerable{Node}, ElementModel)" />
		[return: NotNull]
		public IEnumerable<Stringer> GetElements(IEnumerable<Node> nodes, ElementModel elementModel = ElementModel.Elastic) =>
			this.Select(s => s.GetElement(nodes, elementModel));

		/// <summary>
		///     Get a <see cref="StringerObject" /> from this collection that matches <paramref name="panelEdge" />.
		/// </summary>
		/// <param name="panelEdge">A panel's <see cref="Edge" />.</param>
		public StringerObject? GetFromPanelEdge(Edge panelEdge) => Find(s => s.Geometry.CenterPoint == panelEdge.CenterPoint);

		/// <summary>
		///     Get a collection of <see cref="StringerObject" />'s from this collection that matches any of
		///     <paramref name="panelGeometries" />.
		/// </summary>
		/// <param name="panelGeometries">A collection of <see cref="PanelGeometry" />'s.</param>
		public IEnumerable<StringerObject?> GetFromPanelGeometries(IEnumerable<PanelGeometry> panelGeometries) => panelGeometries.SelectMany(GetFromPanelGeometry);

		/// <summary>
		///     Get a collection of <see cref="StringerObject" />'s from this collection that matches a
		///     <paramref name="panelGeometry" />.
		/// </summary>
		/// <param name="panelGeometry">A <see cref="PanelGeometry" />.</param>
		public IEnumerable<StringerObject?> GetFromPanelGeometry(PanelGeometry panelGeometry) => panelGeometry.Edges.Select(GetFromPanelEdge);

		/// <summary>
		///     Get the list of <see cref="StringerGeometry" />'s from objects in this collection.
		/// </summary>
		public List<StringerGeometry> GetGeometries() => GetProperties();

		/// <summary>
		///     Get the list of distinct <see cref="UniaxialReinforcement" />'s of this collection.
		/// </summary>
		public IEnumerable<UniaxialReinforcement> GetReinforcements() =>
			this.Select(s => s.Reinforcement)
				.Where(s => s is not null)
				.Distinct()
				.OrderBy(r => r)!;

		/// <summary>
		///     Get the list of distinct <see cref="Steel" />'s of this collection.
		/// </summary>
		public IEnumerable<SteelParameters> GetSteelParameters() =>
			this.Select(s => s.Reinforcement?.Steel)
				.Where(s => s is not null)
				.Select(s => s!.Parameters)
				.Distinct()
				.OrderBy(s => s)!;

		/// <summary>
		///     Get the list of distinct widths from this collection.
		/// </summary>
		public IEnumerable<Length> GetWidths() =>
			GetCrossSections()
				.Select(c => c.Width)
				.Distinct()
				.OrderBy(w => w);

		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		/// <param name="geometry">The <see cref="StringerGeometry" /> to remove from this list.</param>
		public bool Remove(StringerGeometry geometry, bool raiseEvents = true, bool sort = true) => Remove(new StringerObject(geometry, BlockTableId), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		/// <param name="geometries">The <see cref="StringerGeometry" />'s to remove from drawing.</param>
		public int RemoveRange(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true) => RemoveRange(geometries.Select(g => new StringerObject(g, BlockTableId)), raiseEvents, sort);

		/// <summary>
		///     Update all the stringers in this collection from drawing.
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