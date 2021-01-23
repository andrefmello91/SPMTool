using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using Extensions;
using Extensions.AutoCAD;
using Extensions.Number;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Database.Conditions;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Database.Elements
{
	/// <summary>
	/// Node class.
	/// </summary>
	public static class Nodes
	{
		/// <summary>
		/// The equality comparer for <see cref="Point3d"/>.
		/// </summary>
		public static readonly Point3dComparer Comparer = new Point3dComparer { Tolerance = Tolerance };

		/// <summary>
		/// Get the node list;
		/// </summary>
		public static EList<NodeObject> NodeList { get; private set; } = GetNodeList();

		/// <summary>
		/// List of nodes' <see cref="Point3d"/> positions.
		/// </summary>
		public static List<Point3d> Positions => NodeList.Select(n => n.Position).ToList();

		/// <summary>
		/// Get the tolerance for <see cref="Point3d"/> equality comparer.
		/// </summary>
		public static double Tolerance => SettingsData.SavedUnits.Tolerance;

		/// <summary>
		/// Add nodes in all necessary positions (stringer start, mid and end points).
		/// </summary>
		public static void Add()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
				return;

			// Add external nodes
			var extNds = strList.Select(str => str.InitialPoint).ToList();
			extNds.AddRange(strList.Select(str => str.EndPoint));
			Add(extNds, NodeType.External);

			// Add internal nodes
			Add(strList.Select(str => str.CenterPoint).ToArray(), NodeType.Internal);
		}

		/// <summary>
		/// Add a node to drawing in this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The <see cref="Point3d"/> position.</param>
		/// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public static void Add(Point3d position, NodeType nodeType) => Add(new NodeObject(position, nodeType));

		/// <summary>
		/// Add nodes to drawing in these <paramref name="positions"/>.
		/// </summary>
		/// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
		/// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public static void Add(IEnumerable<Point3d> positions, NodeType nodeType) => Add(positions.Distinct(Comparer).Select(p => new NodeObject(p, nodeType)));

		/// <summary>
		/// Add a <see cref="NodeObject"/> if it doesn't exist in <see cref="NodeList"/>.
		/// </summary>
		public static void Add(NodeObject node)
		{
			if (NodeList.Contains(node))
				return;

			NodeList.AddAndSort(node);
		}

		/// <summary>
		/// Add a collection of <see cref="NodeObject"/>'s if they don't exist in <see cref="NodeList"/>.
		/// </summary>
		public static void Add(IEnumerable<NodeObject> nodes)
		{
			var newNodes = NodeList.Any() ? nodes.Distinct().Where(n => !NodeList.Contains(n)).ToList() : nodes.ToList();

			NodeList.AddRangeAndSort(newNodes);
		}


		/// <summary>
		/// Remove unnecessary nodes from the drawing.
		/// </summary>
		public static void Remove()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
				NodeList.Clear();

			else
			{
				// Get the stringer points
				var intPts = strList.Select(str => str.CenterPoint).ToList();
				var extPts = strList.Select(str => str.InitialPoint).ToList();
				extPts.AddRange(strList.Select(str => str.EndPoint));

				// Get positions not needed
				var toRemove = NodeList.Where(n => n.Type is NodeType.Internal && !intPts.Contains(n.Position, Comparer)).ToList();
				toRemove.AddRange(NodeList.Where(n => n.Type is NodeType.External && !extPts.Contains(n.Position, Comparer)));

                // Get duplicated positions
                toRemove.AddRange(NodeList.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key));

                Remove(toRemove);
			}
		}

		/// <summary>
		/// Remove a <see cref="NodeObject"/> if it exists in <see cref="NodeList"/>.
		/// </summary>
		public static void Remove(NodeObject node)
		{
			if (!NodeList.Contains(node))
				return;

			NodeList.RemoveAndSort(node);
		}

		/// <summary>
		/// Remove a collection of <see cref="NodeObject"/>'s if they exist in <see cref="NodeList"/>.
		/// </summary>
		public static void Remove(IEnumerable<NodeObject> nodes) => NodeList.RemoveAllAndSort(nodes.Contains);

		/// <summary>
		/// Add a <see cref="NodeObject"/> to drawing and set it's <see cref="ObjectId"/>.
		/// </summary>
		/// <param name="node">The node to add.</param>
		private static void AddToDrawing(NodeObject node)
		{
			// Create the node and set the layer
			var dbPoint = new DBPoint(node.Position)
			{
				Layer = $"{GetLayer(node.Type)}"
			};

			dbPoint.AddToDrawing();

			node.ObjectId = dbPoint.ObjectId;
		}

		/// <summary>
		/// Add a collection of <see cref="NodeObject"/>'s to drawing and set their <see cref="ObjectId"/>.
		/// </summary>
		/// <param name="nodes">The node to add.</param>
		private static void AddToDrawing(IEnumerable<NodeObject> nodes)
		{
			if (nodes is null || !nodes.Any())
				return;

			var points = new List<Entity>();

			foreach (var node in nodes)
			{
				if (node is null)
					continue;

				// Create the node and set the layer
				var dbPoint = new DBPoint(node.Position)
				{
					Layer = $"{GetLayer(node.Type)}"
				};

				node.ObjectId = dbPoint.ObjectId;

				points.Add(dbPoint);
			}

			// Add objects to drawing
			var objIds = points.AddToDrawing().ToArray();

			// Set object ids
			for (int i = 0; i < nodes.Count(); i++)
				nodes.ElementAt(i).ObjectId = objIds[i];
		}

		/// <summary>
		/// Return a <see cref="DBPoint"/> in the drawing located at this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The required <see cref="Point3d"/> position.</param>
		/// <param name="type">The <see cref="NodeType"/> (excluding <see cref="NodeType.Displaced"/>). </param>
		public static DBPoint GetNodeAtPosition(Point3d position, NodeType type = NodeType.All)
		{
			var nodes =
				(type is NodeType.External
					? GetExtNodes()
					: type is NodeType.Internal
						? GetIntNodes()
						: GetAllNodes()).ToArray();

			return
				nodes.FirstOrDefault(p => p.Position.Approx(position, Tolerance));
		}

		/// <summary>
		/// Return a collection of <see cref="DBPoint"/>'s in the drawing located at this <paramref name="positions"/>.
		/// </summary>
		/// <param name="positions">The required <see cref="Point3d"/> positions.</param>
		/// <param name="type">The <see cref="NodeType"/> (excluding <see cref="NodeType.Displaced"/>). </param>
		public static IEnumerable<DBPoint> GetNodesAtPositions(IEnumerable<Point3d> positions, NodeType type = NodeType.All)
		{
			var nodes =
				(type is NodeType.External
					? GetExtNodes()
					: type is NodeType.Internal
						? GetIntNodes()
						: GetAllNodes()).ToArray();

			return
				nodes.Where(n => positions.Any(p => p.Approx(n.Position, Tolerance)));
		}

		/// <summary>
		/// Get the collection of internal nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint> GetIntNodes() => Layer.IntNode.GetDBObjects()?.ToPoints();

		/// <summary>
		/// Get the collection of external nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint> GetExtNodes() => Layer.ExtNode.GetDBObjects()?.ToPoints();

		/// <summary>
		/// Get the collection of internal and external nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint> GetAllNodes() => GetIntNodes()?.Concat(GetExtNodes());

		/// <summary>
		/// Enumerate all the nodes in the model and return the collection of nodes.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public static void Update(bool addNodes = true, bool removeNodes = true)
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
			Model.SetPointSize();
		}

		/// <summary>
		/// Get the list of node positions ordered.
		/// </summary>
		/// <param name="nodeType"></param>
		/// <returns></returns>
		public static IEnumerable<Point3d> NodePositions(NodeType nodeType)
		{
			// Initialize an object collection
			List<DBPoint> ndObjs;

			// Select the node type
			switch (nodeType)
			{
				case NodeType.All:
					ndObjs = GetAllNodes()?.ToList();
					break;

				case NodeType.Internal:
					ndObjs = GetIntNodes()?.ToList();
					break;

				case NodeType.External:
					ndObjs = GetExtNodes()?.ToList();
					break;

				default:
					ndObjs = new List<DBPoint>();
					break;
			}

			return ndObjs?.Select(nd => nd.Position).Order() ?? new List<Point3d>();
		}

		/// <summary>
		/// Read <see cref="NodeObject"/>'s from a collection.
		/// </summary>
		/// <param name="nodePoints">The collection containing the nodes of drawing.</param>
		public static IEnumerable<NodeObject> ReadFromDrawing(IEnumerable<DBPoint> nodePoints) => nodePoints?.Select(ReadFromDrawing);

		/// <summary>
		/// Read a <see cref="NodeObject"/> in the drawing.
		/// </summary>
		/// <param name="nodePoint">The <see cref="DBPoint"/> object of the node.</param>
		public static NodeObject ReadFromDrawing(DBPoint nodePoint) => new NodeObject(nodePoint.Position, GetNodeType(nodePoint)) { ObjectId = nodePoint.ObjectId };

		/// <summary>
		/// Get a node from the list with corresponding <see cref="ObjectId"/>.
		/// </summary>
		public static NodeObject GetFromList(ObjectId objectId) => NodeList.Find(n => n.ObjectId == objectId);

		/// <summary>
		/// Update <see cref="NodeList"/> by reading objects in the drawing.
		/// </summary>
		public static void UpdateList() => NodeList = GetNodeList();

		/// <summary>
		/// Get the node list from elements in drawing.
		/// </summary>
		private static EList<NodeObject> GetNodeList()
		{
			var list  = new EList<NodeObject>(ReadFromDrawing(GetAllNodes()?.ToArray())?.ToList() ?? new List<NodeObject>());

			// Add events
			SetEvents(list);

			// Sort list
			list.Sort();

			return list;
		}

		/// <summary>
		/// Set events on <paramref name="list"/>.
		/// </summary>
		private static void SetEvents(EList<NodeObject> list)
		{
			list.ItemAdded    += On_NodeAdded;
			list.ItemRemoved  += On_NodeRemoved;
			list.RangeAdded   += On_NodesAdded;
			list.RangeRemoved += On_NodesRemoved;
			list.ListSorted   += On_ListSort;
		}

		/// <summary>
		/// Get node positions in the drawing.
		/// </summary>
		/// <param name="nodes">The collection of <see cref="DBPoint"/>'s.</param>
		private static List<Point3d> GetPositions(IEnumerable<DBPoint> nodes = null) => (nodes ?? GetAllNodes())?.Select(nd => nd.Position).ToList() ?? new List<Point3d>();

		/// <summary>
		/// Get <see cref="NodeType"/>.
		/// </summary>
		/// <param name="nodePoint">The <see cref="Entity"/> object.</param>
		public static NodeType GetNodeType(Entity nodePoint) => nodePoint.Layer == $"{Layer.ExtNode}" ? NodeType.External : NodeType.Internal;

		/// <summary>
		/// Get the layer name based on <paramref name="nodeType"/>.
		/// </summary>
		/// <param name="nodeType">The <see cref="NodeType"/> (excluding <see cref="NodeType.All"/>).</param>
		private static Layer GetLayer(NodeType nodeType)
		{
			switch (nodeType)
			{
				case NodeType.Internal:
					return Layer.IntNode;

				case NodeType.Displaced:
					return Layer.Displacements;

				default:
					return Layer.ExtNode;
			}
		}

		/// <summary>
		/// Get the node number at this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The <see cref="Point3d"/> position.</param>
		/// <param name="nodeObjects">The collection of node <see cref="DBObject"/>'s</param>
		public static int? GetNumber(Point3d position, IEnumerable<DBPoint> nodeObjects = null) => (nodeObjects ?? GetAllNodes())?.First(nd => nd.Position.Approx(position))?.ReadXData()?[(int)NodeIndex.Number].ToInt();

		/// <summary>
		/// Create node XData.
		/// </summary>
		private static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Node Data";

			// Get the Xdata size
			int size = Enum.GetNames(typeof(NodeIndex)).Length;

			// Initialize the array of typed values for XData
			var data = new TypedValue[size];

			// Set the initial parameters
			data[(int)NodeIndex.AppName] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			data[(int)NodeIndex.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int)NodeIndex.Ux] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			data[(int)NodeIndex.Uy] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

			return data;
		}

		/// <summary>
		/// Set displacements to the collection of <see cref="Node"/>'s.
		/// </summary>
		/// <param name="nodes">The collection of <see cref="Node"/>'s.</param>
		public static void SetDisplacements(IEnumerable<Node> nodes)
		{
			foreach (var nd in nodes)
				SetDisplacements(nd);
		}

		/// <summary>
		/// Set displacements to a <see cref="Node"/>.
		/// </summary>
		/// <param name="node">The <see cref="Node"/>.</param>
		public static void SetDisplacements(Node node)
		{
			// Get extended data
			var data = node.ObjectId.ReadXData();

			// Save the displacements on the XData
			data[(int)NodeIndex.Ux] = new TypedValue((int)DxfCode.ExtendedDataReal, node.Displacement.ComponentX);
			data[(int)NodeIndex.Uy] = new TypedValue((int)DxfCode.ExtendedDataReal, node.Displacement.ComponentY);

			// Save new XData
			node.ObjectId.SetXData(data);
		}

		/// <summary>
		/// Event to execute when a node is erased.
		/// </summary>
		private static void On_NodeErase(object sender, ObjectErasedEventArgs e)
		{
			if (Positions is null || !Positions.Any() || !(sender is DBPoint nd))
				return;

			Positions.RemoveAll(p => p.Approx(nd.Position, Tolerance));
		}

		/// <summary>
		/// Event to execute when a node is added.
		/// </summary>
		private static void On_NodeAdded(object sender, ItemEventArgs<NodeObject> e)
		{
			var node = e.Item;

			if (node is null)
				return;

			AddToDrawing(node);
		}

		/// <summary>
		/// Event to execute when a range of nodes is added.
		/// </summary>
		private static void On_NodesAdded(object sender, RangeEventArgs<NodeObject> e)
		{
			var nodes = e.ItemCollection;

			if (nodes is null)
				return;

			AddToDrawing(nodes);
		}

		/// <summary>
		/// Event to execute when a <see cref="NodeObject"/> is removed.
		/// </summary>
		public static void On_NodeRemoved(object sender, ItemEventArgs<NodeObject> e) => Model.RemoveFromDrawing(e.Item);

		/// <summary>
		/// Event to execute when a range of <see cref="NodeObject"/>'s is removed.
		/// </summary>
		public static void On_NodesRemoved(object sender, RangeEventArgs<NodeObject> e) => Model.RemoveFromDrawing(e.ItemCollection);

		/// <summary>
		/// Event to execute when an <see cref="ISPMObject"/> list is sorted.
		/// </summary>
		public static void On_ListSort(object sender, EventArgs e)
		{
			if (!(sender is EList<NodeObject> list))
				return;

			Model.SetNumbers(list);
		}
	}
}