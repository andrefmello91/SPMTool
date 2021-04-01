using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
using andrefmello91.Extensions;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.PanelProperties;
using andrefmello91.SPMElements.StringerProperties;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;
using static SPMTool.Units;

using Force = UnitsNet.Force;

#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Stringers class.
	/// </summary>
	public class StringerList : SPMObjectList<StringerObject, StringerGeometry>
	{
		#region Constructors

		private StringerList()
			: base()
		{
		}

		private StringerList(IEnumerable<StringerObject> stringerObjects)
			: base(stringerObjects)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get the collection of stringers in the drawing.
		/// </summary>
		public static IEnumerable<Line>? GetObjects() => Layer.Stringer.GetDBObjects<Line>();

		/// <summary>
		///     Read all the <see cref="StringerObject" />'s in the drawing.
		/// </summary>
		public static StringerList ReadFromDrawing() => ReadFromLines(GetObjects());

		/// <summary>
		///     Read <see cref="StringerObject" />'s from a collection of <see cref="Line" />'s.
		/// </summary>
		/// <param name="stringerLines">The collection containing the <see cref="Line" />'s of drawing.</param>
		public static StringerList ReadFromLines(IEnumerable<Line>? stringerLines) =>
			stringerLines.IsNullOrEmpty()
				? new StringerList()
				: new StringerList(stringerLines.Select(StringerObject.ReadFromLine));

		/// <summary>
		///     Get the list of <see cref="StringerGeometry" />'s from objects in this collection.
		/// </summary>
		public List<StringerGeometry> GetGeometries() => GetProperties();

		/// <summary>
		///     Get the list of distinct <see cref="CrossSection" />'s from objects in this collection.
		/// </summary>
		public List<CrossSection> GetCrossSections() => GetGeometries().Select(g => g.CrossSection).Distinct().OrderBy(c => c).ToList();

		/// <summary>
		///     Get the list of distinct widths from this collection.
		/// </summary>
		public List<Length> GetWidths() => GetCrossSections().Select(c => c.Width).Distinct().OrderBy(w => w).ToList();

		/// <summary>
		///		Get the list of distinct <see cref="UniaxialReinforcement"/>'s of this collection.
		/// </summary>
		public List<UniaxialReinforcement?> GetReinforcements() => this.Select(s => s.Reinforcement).Distinct().OrderBy(r => r).ToList();

		/// <summary>
		///		Get the list of distinct <see cref="Steel"/>'s of this collection.
		/// </summary>
		public List<Steel?> GetSteels() => this.Select(s => s.Reinforcement?.Steel).Distinct().OrderBy(s => s).ToList();

		/// <summary>
		///     Update all the stringers in this collection from drawing.
		/// </summary>
		public void Update()
		{
			Clear(false);

			AddRange(ReadFromDrawing(), false);
		}

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="startPoint">The start <see cref="Point" />.</param>
		/// <param name="endPoint">The end <see cref="Point" />.</param>
		public bool Add(Point startPoint, Point endPoint, bool raiseEvents = true, bool sort = true)
		{
			// Order points
			var pts = new[] { startPoint, endPoint }.OrderBy(p => p).ToList();
			pts.Sort();

			return
				Add(new StringerObject(pts[0], pts[1]), raiseEvents, sort);
		}

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="geometry">The <see cref="StringerGeometry" /> to add.</param>
		public bool Add(StringerGeometry geometry, bool raiseEvents = true, bool sort = true) => Add(new StringerObject(geometry), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <param name="geometries">The <see cref="StringerGeometry" />'s to add.</param>
		public int AddRange(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true) => AddRange(geometries?.Select(g => new StringerObject(g)), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		/// <param name="geometry">The <see cref="StringerGeometry" /> to remove from this list.</param>
		public bool Remove(StringerGeometry geometry, bool raiseEvents = true, bool sort = true) => Remove(new StringerObject(geometry), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		/// <param name="geometries">The <see cref="StringerGeometry" />'s to remove from drawing.</param>
		public int RemoveRange(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true) => RemoveRange(geometries.Select(g => new StringerObject(g)), raiseEvents, sort);

		/// <summary>
		///     Get the <see cref="Stringer" />'s associated to objects in this collection.
		/// </summary>
		/// <inheritdoc cref="StringerObject.GetElement(IEnumerable{Node}, ElementModel)" />
		[return:NotNull]
		public IEnumerable<Stringer> GetElements(IEnumerable<Node> nodes, ElementModel elementModel = ElementModel.Elastic) => this.Select(s => s.GetElement(nodes, elementModel));

		/// <summary>
		///		Get a <see cref="StringerObject"/> from this collection that matches <paramref name="panelEdge"/>.
		/// </summary>
		/// <param name="panelEdge">A panel's <see cref="Edge"/>.</param>
		public StringerObject? GetFromPanelEdge(Edge panelEdge) => Find(s => s.Geometry.CenterPoint == panelEdge.CenterPoint);

		/// <summary>
		///		Get a collection of <see cref="StringerObject"/>'s from this collection that matches a <paramref name="panelGeometry"/>.
		/// </summary>
		/// <param name="panelGeometry">A <see cref="PanelGeometry"/>.</param>
		public IEnumerable<StringerObject?> GetFromPanelGeometry(PanelGeometry panelGeometry) => panelGeometry.Edges.Select(GetFromPanelEdge);

		/// <summary>
		///		Get a collection of <see cref="StringerObject"/>'s from this collection that matches any of <paramref name="panelGeometries"/>.
		/// </summary>
		/// <param name="panelGeometries">A collection of <see cref="PanelGeometry"/>'s.</param>
		public IEnumerable<StringerObject?> GetFromPanelGeometries(IEnumerable<PanelGeometry> panelGeometries) => panelGeometries.SelectMany(GetFromPanelGeometry);

		#endregion
	}
}