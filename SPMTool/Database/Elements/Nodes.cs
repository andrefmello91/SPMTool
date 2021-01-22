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
		private static readonly Point3dEqualityComparer Comparer = new Point3dEqualityComparer { Tolerance = Tolerance };

		/// <summary>
		/// Get the node list;
		/// </summary>
		public static EList<Node> NodeList { get; private set; } = GetNodeList();

		/// <summary>
		/// List of nodes' <see cref="Point3d"/> positions.
		/// </summary>
		public static List<Point3d> Positions => NodeList.Select(n => n.Position).ToList();
		
		/// <summary>
		/// Get the geometry unit.
		/// </summary>
		private static LengthUnit GeometryUnit => SettingsData.SavedUnits.Geometry;

		/// <summary>
		/// Get the tolerance for <see cref="Point3d"/> equality comparer.
		/// </summary>
		public static double Tolerance => 0.001.ConvertFromMillimeter(GeometryUnit);

		/// <summary>
		/// Add nodes in all necessary positions (stringer start, mid and end points).
		/// </summary>
		public static void Add()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
				return;

			// Add internal nodes
			Add(strList.Select(str => str.CenterPoint).ToArray(), NodeType.Internal);

			// Add external nodes
			var extNds = strList.Select(str => str.InitialPoint).ToList();
			extNds.AddRange(strList.Select(str => str.EndPoint));
			Add(extNds, NodeType.External);

			// Get points
			//var intNds = strList.Select(str => str.CenterPoint).Distinct(Comparer).ToList();
			//var extNds = strList.Select(str => str.InitialPoint).Distinct(Comparer).ToList();
			//extNds.AddRange(strList.Select(str => str.EndPoint).Distinct(Comparer));

   //         // Add nodes
			//Add(intNds, NodeType.Internal);
			//Add(extNds, NodeType.External);
		}

		/// <summary>
		/// Add a <see cref="Node"/> if it doesn't exist in <see cref="NodeList"/>.
		/// </summary>
		public static void AddToList(Node node)
		{
			if (NodeList.Contains(node))
				return;

			NodeList.Add(node);
		}

		/// <summary>
		/// Add a collection of <see cref="Node"/>'s if they don't exist in <see cref="NodeList"/>.
		/// </summary>
		public static void AddToList(IEnumerable<Node> nodes)
		{
			var newNodes = nodes.Where(n => !NodeList.Contains(n)).ToList();

			NodeList.AddRange(newNodes.Distinct());
		}

		/// <summary>
		/// Remove a <see cref="Node"/> if it exists in <see cref="NodeList"/>.
		/// </summary>
		public static void RemoveFromList(Node node)
		{
			if (!NodeList.Contains(node))
				return;

			NodeList.Remove(node);
		}

		/// <summary>
		/// Remove a collection of <see cref="Node"/>'s if they exist in <see cref="NodeList"/>.
		/// </summary>
		public static void RemoveFromList(IEnumerable<Node> nodes) => NodeList.RemoveAll(nodes.Contains);

		/// <summary>
        /// Add a node to drawing in this <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The <see cref="Point3d"/> position.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public static void Add(Point3d position, NodeType nodeType)
		{
            // Check if a node already exists at the position. If not, its created
            if (NodeList.Any(n => position.Approx(n.Position, Tolerance)))
				return;

            // Add to the list
            var units = SettingsData.SavedUnits;
            AddToList(new Node(position, nodeType, units.Geometry, units.Displacements));
		}

        /// <summary>
        /// Add nodes to drawing in these <paramref name="positions"/>.
        /// </summary>
        /// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        public static void Add(IEnumerable<Point3d> positions, NodeType nodeType)
		{
			// Get the positions that don't exist in the drawing
			var newPts = positions.Distinct(Comparer).Where(p => !NodeList.Any(n => n.Position.Approx(p, Tolerance))).ToArray();
			var units = SettingsData.SavedUnits;
			var newNds = newPts.Select(p => new Node(p, nodeType, units.Geometry, units.Displacements)).ToArray();
			AddToList(newNds);
        }

		/// <summary>
		/// Add a <see cref="Node"/> to drawing and set it's <see cref="ObjectId"/>.
		/// </summary>
		/// <param name="node">The node to add.</param>
        private static void AddToDrawing(Node node)
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
		/// Add a collection of <see cref="Node"/>'s to drawing and set their <see cref="ObjectId"/>.
		/// </summary>
		/// <param name="nodes">The node to add.</param>
		private static void AddToDrawing(IEnumerable<Node> nodes)
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
				var points = strList.Select(str => str.CenterPoint).ToList();
				points.AddRange(strList.Select(str => str.InitialPoint));
				points.AddRange(strList.Select(str => str.EndPoint));

				// Get positions not needed
				var toRemove = NodeList.Where(n => !points.Contains(n.Position)).ToList();

				RemoveFromList(toRemove);
			}
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
				nodes.FirstOrDefault(p => p.Position.Approx( position, Tolerance));
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
			UpdateList();

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
        /// Read <see cref="Node"/> objects from an <see cref="ObjectIdCollection"/>.
        /// </summary>
        /// <param name="nodeObjects">The <see cref="ObjectIdCollection"/> containing the nodes of drawing.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static IEnumerable<Node> ReadFromDrawing(IEnumerable<DBPoint> nodeObjects, Units units) => nodeObjects?.Select(nd => ReadFromDrawing(nd, units)).Order();

        /// <summary>
        /// Read a <see cref="Node"/> in the drawing.
        /// </summary>
        /// <param name="nodeObject">The <see cref="DBPoint"/> object of the node.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static Node ReadFromDrawing(DBPoint nodeObject, Units units)
        {
	        // Read the XData and get the necessary data
	        var data = nodeObject.ReadXData();

	        // Get the node number
	        var number = data[(int)NodeIndex.Number].ToInt();

			// Get displacement
			var ux = Length.FromMillimeters(data[(int) NodeIndex.Ux].ToDouble()).ToUnit(units.Displacements);
			var uy = Length.FromMillimeters(data[(int) NodeIndex.Uy].ToDouble()).ToUnit(units.Displacements);
			var disp = new Displacement(ux, uy);

	        var node = new Node(nodeObject.ObjectId, number, nodeObject.Position, GetNodeType(nodeObject), units.Geometry, units.Displacements);

			// Set forces, support and displacements
			Forces.Set(node);
			Supports.Set(node);
			node.SetDisplacements(disp);

			return node;
        }

		/// <summary>
		/// Get a node from the list with corresponding <see cref="ObjectId"/>.
		/// </summary>
        public static Node GetFromList(ObjectId objectId) => NodeList.Find(n => n.ObjectId == objectId);

        /// <summary>
        /// Update <see cref="NodeList"/>.
        /// </summary>
        /// <param name="readFromDwg">Read nodes from drawing?</param>
        public static void UpdateList(bool readFromDwg = false) => NodeList = readFromDwg ? GetNodeList() : EnumerateList(NodeList);

        /// <summary>
		/// Enumerate a list of <see cref="Node"/>'s.
		/// </summary>
		private static EList<Node> EnumerateList(IEnumerable<Node> nodes)
		{
			var ordered = new EList<Node>(nodes.Order());

			// Set numbers
			SetNumbers(ordered);

			// Add events
			SetEvents(ordered);

			return ordered;
		}

		/// <summary>
		/// Get the node list from elements in drawing.
		/// </summary>
        private static EList<Node> GetNodeList()
        {
	        var nodes = ReadFromDrawing(GetAllNodes()?.ToArray(), SettingsData.SavedUnits)?.ToList() ?? new List<Node>();
	        var list = new EList<Node>(nodes.Order());

			// Set numbers
			SetNumbers(list);

			// Add events
			SetEvents(list);

	        return list;
        }

		/// <summary>
		/// Set events on <paramref name="list"/>.
		/// </summary>
		private static void SetEvents(EList<Node> list)
		{
			list.ItemAdded    += On_NodeAdded;
			list.ItemRemoved  += On_NodeRemoved;
			list.RangeAdded   += On_NodesAdded;
			list.RangeRemoved += On_NodesRemoved;
		}

		/// <summary>
		/// Set numbers to a collection of <see cref="Node"/>'s.
		/// </summary>
		/// <param name="nodes"></param>
		private static void SetNumbers(IEnumerable<Node> nodes)
		{
			if (nodes is null || !nodes.Any())
				return;

			var count = nodes.Count();

			for (int i = 0; i < count; i++)
				nodes.ElementAt(i).Number = i + 1;
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
		private static NodeType GetNodeType(Entity nodePoint) => nodePoint.Layer == $"{Layer.ExtNode}" ? NodeType.External : NodeType.Internal;

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
        public static int? GetNumber(Point3d position, IEnumerable<DBPoint> nodeObjects = null) => (nodeObjects ?? GetAllNodes())?.First(nd => nd.Position.Approx(position))?.ReadXData()?[(int) NodeIndex.Number].ToInt();

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
			data[(int)NodeIndex.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			data[(int)NodeIndex.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int)NodeIndex.Ux]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
			data[(int)NodeIndex.Uy]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

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
		private static void On_NodeAdded(object sender, ItemEventArgs<Node> e)
		{
			var node = e.Item;

			if (node is null)
				return;

			AddToDrawing(node);
		}

		/// <summary>
		/// Event to execute when a range of nodes is added.
		/// </summary>
		private static void On_NodesAdded(object sender, RangeEventArgs<Node> e)
		{
			var nodes = e.ItemCollection;

			if (nodes is null)
				return;

			AddToDrawing(nodes);
		}

		/// <summary>
		/// Event to execute when a <see cref="Node"/> is removed.
		/// </summary>
		public static void On_NodeRemoved(object sender, ItemEventArgs<Node> e) => Model.RemoveFromDrawing(e.Item);

		/// <summary>
		/// Event to execute when a range of <see cref="Node"/>'s is removed.
		/// </summary>
		public static void On_NodesRemoved(object sender, RangeEventArgs<Node> e) => Model.RemoveFromDrawing(e.ItemCollection);
	}
}