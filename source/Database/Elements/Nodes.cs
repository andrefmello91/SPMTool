using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Extensions.AutoCAD;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Database.Conditions;
using SPMTool.Enums;
using SPMTool.Extensions;
using SPMTool.Database.Elements;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Database.SettingsData;
using static SPMTool.Database.Model;
using static SPMTool.Database.Elements.SPMObjects<SPMTool.Database.Elements.NodeObject>;

#nullable enable

namespace SPMTool.Database.Elements
{
	/// <summary>
	/// Nodes class.
	/// </summary>
	public class Nodes : SPMObjects<NodeObject>
	{
		/// <summary>
		/// List of nodes' <see cref="Point"/> positions.
		/// </summary>
		public List<Point> Positions => this.Select(n => n.Position).ToList();

		/// <summary>
		/// Add nodes in all necessary positions (stringer start, mid and end points).
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
		/// Add a node in this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The <see cref="Point"/> position.</param>
		/// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public void Add(Point position, NodeType nodeType) => Add(new NodeObject(position, nodeType));

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)"/>
		/// <remarks>
		///		If node type is <see cref="NodeType.Displaced"/>, the node is only added to drawing.
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
		/// Add nodes in these <paramref name="positions"/>.
		/// </summary>
		/// <param name="positions">The collection of <see cref="Point"/> positions.</param>
		/// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public void AddRange(IEnumerable<Point> positions, NodeType nodeType) => AddRange(positions.Distinct().Select(p => new NodeObject(p, nodeType)));

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)"/>
		/// <inheritdoc cref="Add(NodeObject, bool, bool)"/>
		public new void AddRange(IEnumerable<NodeObject> collection, bool raiseEvents = true, bool sort = true)
		{
			// Get displaced nodes
			var dispNodes = collection.Where(n => n.Type is NodeType.Displaced).ToList();

			if (dispNodes.Any())
				AddToDrawing(dispNodes);

			base.AddRange(collection.Where(n => n.Type != NodeType.Displaced), raiseEvents, sort);
		}

		/// <summary>
		/// Remove unnecessary nodes from the drawing.
		/// </summary>
		public void Remove()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
				Clear();

			else
			{
				// Get the stringer points
				var intPts = strList.Select(str => str.CenterPoint).ToList();
				var extPts = strList.Select(str => str.InitialPoint).ToList();
				extPts.AddRange(strList.Select(str => str.EndPoint));

				// Get positions not needed
				var toRemove = this.Where(n => n.Type is NodeType.Internal && !intPts.Contains(n.Position)).ToList();
				toRemove.AddRange(this.Where(n => n.Type is NodeType.External && !extPts.Contains(n.Position)));

                // Get duplicated positions
                //toRemove.AddRange(this.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key));

                RemoveRange(toRemove);
			}
		}

		/// <summary>
		/// Return a <see cref="NodeObject"/> located at this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The required <see cref="Point"/> position.</param>
		public NodeObject? GetNodeAtPosition(Point position) =>
			this.All(n => n.Position != position)
				? null
				: Find(n => n.Position == position);

		/// <summary>
		/// Return a collection of <see cref="NodeObject"/>'s located at these <paramref name="positions"/>.
		/// </summary>
		/// <param name="positions">The required <see cref="Point3d"/> positions.</param>
		public List<NodeObject> GetNodesAtPositions(IEnumerable<Point> positions) =>
			this.Where(n => positions.Contains(n.Position)).ToList();

		/// <summary>
		/// Get the collection of internal nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint>? GetIntNodes() => Layer.IntNode.GetDBObjects()?.ToPoints();

		/// <summary>
		/// Get the collection of external nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint>? GetExtNodes() => Layer.ExtNode.GetDBObjects()?.ToPoints();

		/// <summary>
		/// Get the collection of internal and external nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint>? GetAllNodes() => GetIntNodes()?.Concat(GetExtNodes());

		/// <summary>
		/// Update all the nodes in this collection.
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

			// Update node list
			//UpdateList();

			//// Get all the nodes as points
			//var ndObjs = GetAllNodes()?.ToList() ?? new List<DBPoint>();

			//// Access the nodes on the document
			//if (ndObjs.Any())
			//{
			//	// Get the Xdata size
			//	int size = Enum.GetNames(typeof(NodeIndex)).Length;

			//             // Order nodes
			//             ndObjs = ndObjs.OrderBy(nd => nd.Position.Y).ThenBy(nd => nd.Position.X).ToList();

			//	for (var i = 0; i < ndObjs.Count; i++)
			//	{
			//		// Get the node number
			//		double ndNum = i + 1;

			//		// Initialize the array of typed values for XData
			//		var data = ndObjs[i].XData?.AsArray();
			//		data = data?.Length == size ? data : NewXData();

			//		// Set the updated number
			//		data[(int) NodeIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, ndNum);

			//                 // Add the new XData
			//                 ndObjs[i].SetXData(data);
			//	}
			//}

			// Save positions
			//Positions = GetPositions(ndObjs);

			// Set the style for all point objects in the drawing
			SetPointSize();
		}

		private Nodes()
		{
		}

		private Nodes(IEnumerable<NodeObject> nodeObjects)
			: base(nodeObjects)
		{
		}

		/// <summary>
		/// Read all <see cref="NodeObject"/>'s from drawing.
		/// </summary>
		public static Nodes ReadFromDrawing() => ReadFromPoints(GetAllNodes());

		/// <summary>
		/// Read <see cref="NodeObject"/>'s from a collection of <see cref="DBPoint"/>'s.
		/// </summary>
		/// <param name="nodePoints">The collection containing the <see cref="DBPoint"/>'s of drawing.</param>
		public static Nodes ReadFromPoints(IEnumerable<DBPoint>? nodePoints) =>
			nodePoints is null || !nodePoints.Any()
				? new Nodes()
				: new Nodes(nodePoints.Select(NodeObject.ReadFromPoint));

		/// <summary>
		/// Get a node from the list with corresponding <see cref="ObjectId"/>.
		/// </summary>
		public NodeObject? GetByObjectId(ObjectId objectId) =>
			this.Any(n => n.ObjectId == objectId)
				? Find(n => n.ObjectId == objectId)
				: null;

		/// <summary>
		/// Get <see cref="NodeType"/>.
		/// </summary>
		/// <param name="nodePoint">The <see cref="Entity"/> object.</param>
		public static NodeType GetNodeType(Entity nodePoint) =>
			nodePoint.Layer == $"{Layer.ExtNode}"
				? NodeType.External
				: nodePoint.Layer == $"{Layer.IntNode}"
					? NodeType.Internal
					: NodeType.Displaced;

		/// <summary>
		/// Get the layer name based on <paramref name="nodeType"/>.
		/// </summary>
		/// <param name="nodeType">The <see cref="NodeType"/> (excluding <see cref="NodeType.All"/>).</param>
		public static Layer GetLayer(NodeType nodeType) =>
			nodeType switch
			{
				NodeType.Internal  => Layer.IntNode,
				NodeType.Displaced => Layer.Displacements,
				_                  => Layer.ExtNode
			};

		/// <summary>
		/// Event to execute when a node is erased.
		/// </summary>
		public static void On_NodeErase(object sender, ObjectErasedEventArgs e)
		{
			if (!Model.Nodes.Any() || !(sender is DBPoint nd))
				return;

			Model.Nodes.RemoveAll(n => n.Position == nd.Position.ToPoint(SavedUnits.Geometry), false);
		}
	}
}