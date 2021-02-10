using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using static SPMTool.Database.SettingsData;
using static SPMTool.Database.Model;

#nullable enable

namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Nodes class.
	/// </summary>
	public class Nodes : SPMObjects<NodeObject>
	{
		#region Properties

		/// <summary>
		///     Get a list of nodes' <see cref="Point" /> positions.
		/// </summary>
		public List<Point> Positions => this.Select(n => n.Position).ToList();

		#endregion

		#region Constructors

		private Nodes()
		{
		}

		private Nodes(IEnumerable<NodeObject> nodeObjects)
			: base(nodeObjects)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get the collection of <see cref="DBPoint" />'s in the drawing, based in the <see cref="NodeType" />.
		/// </summary>
		/// <remarks>
		///     Leave <paramref name="type" /> null to get all <see cref="NodeType.Internal" /> and
		///     <see cref="NodeType.External" /> nodes.
		/// </remarks>
		/// <param name="type">The <see cref="NodeType" />.</param>
		public static IEnumerable<DBPoint>? GetDBPoints(NodeType? type = null) =>
			type switch
			{
				NodeType.Internal  => Layer.IntNode.GetDBObjects()?.ToPoints(),
				NodeType.External  => Layer.ExtNode.GetDBObjects()?.ToPoints(),
				NodeType.Displaced => Layer.Displacements.GetDBObjects()?.ToPoints(),
				_                  => new[] { Layer.IntNode, Layer.ExtNode }.GetDBObjects()?.ToPoints()
			};

		/// <summary>
		///     Read all <see cref="NodeObject" />'s from drawing.
		/// </summary>
		public static Nodes ReadFromDrawing() => ReadFromPoints(GetDBPoints());

		/// <summary>
		///     Read <see cref="NodeObject" />'s from a collection of <see cref="DBPoint" />'s.
		/// </summary>
		/// <param name="nodePoints">The collection containing the <see cref="DBPoint" />'s of drawing.</param>
		public static Nodes ReadFromPoints(IEnumerable<DBPoint>? nodePoints) =>
			nodePoints is null || !nodePoints.Any()
				? new Nodes()
				: new Nodes(nodePoints.Select(NodeObject.ReadFromPoint));

		/// <summary>
		///     Get <see cref="NodeType" />.
		/// </summary>
		/// <param name="nodePoint">The <see cref="DBPoint" /> object.</param>
		public static NodeType GetNodeType(DBPoint nodePoint) =>
			nodePoint.Layer == $"{Layer.ExtNode}"
				? NodeType.External
				: nodePoint.Layer == $"{Layer.IntNode}"
					? NodeType.Internal
					: NodeType.Displaced;

		/// <summary>
		///     Get the layer name based on <paramref name="nodeType" />.
		/// </summary>
		/// <param name="nodeType">The <see cref="NodeType" /> (excluding <see cref="NodeType.All" />).</param>
		public static Layer GetLayer(NodeType nodeType) =>
			nodeType switch
			{
				NodeType.Internal  => Layer.IntNode,
				NodeType.External  => Layer.ExtNode,
				_                  => Layer.Displacements
			};

		/// <summary>
		///     Event to execute when a node is erased.
		/// </summary>
		public static void On_NodeErase(object sender, ObjectErasedEventArgs e)
		{
			if (!Model.Nodes.Any() || !(sender is DBPoint nd))
				return;

			Model.Nodes.RemoveAll(n => n.Position == nd.Position.ToPoint(SavedUnits.Geometry), false);
		}

		/// <summary>
		///     Add nodes in all necessary positions (stringer start, mid and end points).
		/// </summary>
		public void Add()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
				return;

			// Add external nodes
			var extNds = strList.Select(str => str.InitialPoint).ToList();
			extNds.AddRange(strList.Select(str => str.EndPoint));
			AddRange(extNds, NodeType.External);

			// Add internal nodes
			AddRange(strList.Select(str => str.CenterPoint).ToArray(), NodeType.Internal);
		}

		/// <summary>
		///     Add a node in this <paramref name="position" />.
		/// </summary>
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="nodeType">The <see cref="NodeType" />.</param>
		public void Add(Point position, NodeType nodeType) => Add(new NodeObject(position, nodeType));

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <remarks>
		///     If node type is <see cref="NodeType.Displaced" />, the node is only added to drawing.
		/// </remarks>
		public new void Add(NodeObject item, bool raiseEvents = true, bool sort = true)
		{
			if (item.Type is NodeType.Displaced)
			{
				// Just add to drawing
				item.AddToDrawing();
				return;
			}

			base.Add(item, raiseEvents, sort);
		}

		/// <summary>
		///     Add nodes in these <paramref name="positions" />.
		/// </summary>
		/// <param name="positions">The collection of <see cref="Point" /> positions.</param>
		/// <param name="nodeType">The <see cref="NodeType" />.</param>
		public void AddRange(IEnumerable<Point> positions, NodeType nodeType) => AddRange(positions.Distinct().Select(p => new NodeObject(p, nodeType)));

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <inheritdoc cref="Add(NodeObject, bool, bool)" />
		public new void AddRange(IEnumerable<NodeObject> collection, bool raiseEvents = true, bool sort = true)
		{
			// Get displaced nodes
			var dispNodes = collection.Where(n => n.Type is NodeType.Displaced).ToList();

			if (dispNodes.Any())
				AddToDrawing(dispNodes);

			base.AddRange(collection.Where(n => n.Type != NodeType.Displaced), raiseEvents, sort);
		}

		/// <summary>
		///     Remove unnecessary nodes from the drawing.
		/// </summary>
		public int Remove()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
			{
				var count = Count;

				Clear();

				return count;
			}

			// Get the stringer points
			var intPts = strList.Select(str => str.CenterPoint).ToList();
			var extPts = strList.Select(str => str.InitialPoint).ToList();
			extPts.AddRange(strList.Select(str => str.EndPoint));

			// Get positions not needed
			var toRemove = this.Where(n => n.Type is NodeType.Internal && !intPts.Contains(n.Position)).ToList();
			toRemove.AddRange(this.Where(n => n.Type is NodeType.External && !extPts.Contains(n.Position)));

			// Get duplicated positions
			//toRemove.AddRange(this.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key));

			return
				RemoveRange(toRemove);
		}

		/// <summary>
		///     Return a <see cref="NodeObject" /> located at this <paramref name="position" />.
		/// </summary>
		/// <param name="position">The required <see cref="Point" /> position.</param>
		public NodeObject? GetByPosition(Point position) => Find(n => n.Position == position);

		/// <summary>
		///     Return a collection of <see cref="NodeObject" />'s located at these <paramref name="positions" />.
		/// </summary>
		/// <param name="positions">The required <see cref="Point3d" /> positions.</param>
		public List<NodeObject>? GetByPositions(IEnumerable<Point> positions) =>
			this.Where(n => positions.Contains(n.Position))?.ToList();

		/// <summary>
		///     Update all the nodes in this collection.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public void Update(bool addNodes = true, bool removeNodes = true)
		{
			// Add nodes to all needed positions
			if (addNodes)
				Add();

			// Remove nodes at unnecessary positions
			if (removeNodes)
				Remove();

			// Set the style for all point objects in the drawing
			SetPointSize();
		}

		/// <summary>
		///     Get a node from the list with corresponding <see cref="ObjectId" />.
		/// </summary>
		public NodeObject? GetByObjectId(ObjectId objectId) => Find(n => n.ObjectId == objectId);

		#endregion
	}
}