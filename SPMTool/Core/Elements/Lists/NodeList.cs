using System.Collections.Generic;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using SPMTool.Extensions;
using static SPMTool.Core.Model;

#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Nodes class.
	/// </summary>
	public class NodeList : SPMObjectList<NodeObject, Point>
	{

		#region Constructors

		private NodeList()
		{
		}

		private NodeList(IEnumerable<NodeObject> nodeObjects)
			: base(nodeObjects)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the collection of <see cref="DBPoint" />'s in the drawing, based in the <see cref="NodeType" />.
		/// </summary>
		/// <remarks>
		///     Leave <paramref name="type" /> null to get all <see cref="NodeType.Internal" /> and
		///     <see cref="NodeType.External" /> nodes.
		/// </remarks>
		/// <param name="type">The <see cref="NodeType" />.</param>
		public static IEnumerable<DBPoint?>? GetDBPoints(NodeType? type = null) =>
			type switch
			{
				NodeType.Internal => Layer.IntNode.GetDBObjects<DBPoint>(),
				NodeType.External => Layer.ExtNode.GetDBObjects<DBPoint>(),
				_                 => new[] { Layer.IntNode, Layer.ExtNode }.GetDBObjects<DBPoint>()
			};

		/// <summary>
		///     Get the layer name based on <paramref name="nodeType" />.
		/// </summary>
		/// <param name="nodeType">The <see cref="NodeType" /> (excluding <see cref="NodeType.All" />).</param>
		public static Layer GetLayer(NodeType nodeType) =>
			nodeType switch
			{
				NodeType.Internal => Layer.IntNode,
				NodeType.External => Layer.ExtNode,
				_                 => Layer.Displacements
			};

		/// <summary>
		///     Read all <see cref="NodeObject" />'s from drawing.
		/// </summary>
		public static NodeList ReadFromDrawing() => ReadFromPoints(GetDBPoints()?.ToArray());

		/// <summary>
		///     Read <see cref="NodeObject" />'s from a collection of <see cref="DBPoint" />'s.
		/// </summary>
		/// <param name="nodePoints">The collection containing the <see cref="DBPoint" />'s of drawing.</param>
		public static NodeList ReadFromPoints(IEnumerable<DBPoint>? nodePoints) =>
			nodePoints.IsNullOrEmpty()
				? new NodeList()
				: new NodeList(nodePoints.Select(NodeObject.GetFromPoint));

		/// <inheritdoc cref="Add(NodeObject, bool, bool)" />
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="nodeType">The <see cref="NodeType" />.</param>
		public bool Add(Point position, NodeType nodeType, bool raiseEvents = true, bool sort = true) => Add(new NodeObject(position, nodeType), raiseEvents, sort);

		/// <summary>
		///     Add nodes in all necessary positions, based on a collection of <seealso cref="StringerGeometry" />'s.
		/// </summary>
		/// <remarks>
		///     Nodes are added at each <see cref="StringerGeometry.InitialPoint" /> , <see cref="StringerGeometry.CenterPoint" />
		///     and <see cref="StringerGeometry.EndPoint" />.
		/// </remarks>
		/// <param name="geometries">The collection of <see cref="StringerGeometry" />'s for adding nodes.</param>
		/// <inheritdoc cref="AddRange(IEnumerable{NodeObject}, bool, bool)" />
		public int AddNecessary(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true)
		{
			if (geometries.IsNullOrEmpty())
				return 0;

			// Create a list
			var nds = geometries.SelectMany(g => new[]
			{
				new NodeObject(g.InitialPoint, NodeType.External),
				new NodeObject(g.CenterPoint, NodeType.Internal),
				new NodeObject(g.EndPoint, NodeType.External)
			}).Distinct().ToList();

			//// Add external nodes
			//extNds.AddRange(geoList.Select(str => str.InitialPoint));
			//extNds.AddRange(geoList.Select(str => str.EndPoint));
			//var c = AddRange(extNds, NodeType.External, raiseEvents, false);

			// Add internal nodes
			return
				AddRange(nds, raiseEvents, sort);
		}

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <param name="positions">The collection of <see cref="Point" /> positions.</param>
		/// <param name="nodeType">The <see cref="NodeType" />.</param>
		public int AddRange(IEnumerable<Point>? positions, NodeType nodeType, bool raiseEvents = true, bool sort = true) =>
			AddRange(positions?.Distinct()?.Select(p => new NodeObject(p, nodeType))?.ToList(), raiseEvents, sort);

		/// <summary>
		///     Get a list of nodes' <see cref="Point" /> positions.
		/// </summary>
		public List<Point> GetPositions() => GetProperties();

		public int RemoveRange(IEnumerable<Point>? positions, bool raiseEvents = true, bool sort = true) =>
			positions.IsNullOrEmpty()
				? 0
				: RemoveAll(n => positions.Contains(n.Position), raiseEvents, sort);

		/// <summary>
		///     Remove unnecessary nodes from this collection based on a collection of <see cref="StringerGeometry" />'s.
		/// </summary>
		/// <remarks>
		///     Nodes are removed at positions that doesn't match any <see cref="StringerGeometry.InitialPoint" />,
		///     <see cref="StringerGeometry.CenterPoint" /> and <see cref="StringerGeometry.EndPoint" /> in
		///     <paramref name="geometries" />.
		/// </remarks>
		/// <param name="geometries">The collection of <see cref="StringerGeometry" />'s for removing nodes.</param>
		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		public int RemoveUnnecessary(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true)
		{
			if (geometries.IsNullOrEmpty())
			{
				// There is no stringers, remove all nodes.
				var c = Count;

				Clear(raiseEvents);

				return c;
			}

			// Get the stringer points
			var toRemove = geometries.SelectMany(g => new[]
			{
				g.InitialPoint,
				g.CenterPoint,
				g.EndPoint
			}).Distinct().ToList();

			return
				RemoveAll(n => !toRemove.Contains(n.Position), raiseEvents, sort);
		}

		/// <summary>
		///     Update all the nodes in this collection.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public void Update(bool addNodes = true, bool removeNodes = true)
		{
			var geometries = Stringers.GetGeometries();

			// Add nodes to all needed positions
			if (addNodes)
				AddNecessary(geometries, true, false);

			// Remove nodes at unnecessary positions
			if (removeNodes)
				RemoveUnnecessary(geometries);

			// Set the style for all point objects in the drawing
			SetPointSize();
		}

		#endregion

	}
}