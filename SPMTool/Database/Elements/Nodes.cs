using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
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
		/// The equality comparer for nodes.
		/// </summary>
		private static readonly NodeComparer IntNodeComparer = new NodeComparer
		{
			Type      = NodeType.Internal,
			Tolerance = Tolerance
		};

		/// <summary>
		/// List of nodes' <see cref="Point3d"/> positions.
		/// </summary>
		public static List<Point3d> Positions { get; private set; } = GetPositions();
		
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

			// Get points
			var intNds = strList.Select(str => str.CenterPoint).Distinct(Comparer).ToList();
			var extNds = strList.Select(str => str.InitialPoint).Distinct(Comparer).ToList();
			extNds.AddRange(strList.Select(str => str.EndPoint).Distinct(Comparer));

            // Add nodes
			Add(intNds, NodeType.Internal);
			Add(extNds, NodeType.External);
		}

        /// <summary>
        /// Add a node to drawing in this <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The <see cref="Point3d"/> position.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public static void Add(Point3d position, NodeType nodeType)
		{
            // Check if a node already exists at the position. If not, its created
            if (Positions.Exists(p => p.Approx(position, Tolerance)))
				return;

            // Add to the list
            Positions.Add(position);

			// Create the node and set the layer
			var dbPoint = new DBPoint(position)
			{
				Layer = $"{GetLayer(nodeType)}"
			};
			
			// Add the new object
			if (nodeType != NodeType.Displaced)
				dbPoint.Add(On_NodeErase);
			else
				dbPoint.Add();
		}

        /// <summary>
        /// Add nodes to drawing in these <paramref name="positions"/>.
        /// </summary>
        /// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        public static void Add(IEnumerable<Point3d> positions, NodeType nodeType)
		{
			// Get the positions that don't exist in the drawing
			var newNds = positions.Distinct(Comparer).Where(p => !Positions.Any(p2 => p2.Approx(p, Tolerance))).ToArray();
			Positions.AddRange(newNds);

			// Get the layer
			var layer = $"{GetLayer(nodeType)}";

			// Create the nodes
			var nodes = newNds.Select(p => new DBPoint(p) { Layer = layer }).ToArray();

			if (nodeType != NodeType.Displaced)
				nodes.Add(On_NodeErase);
			else
				nodes.Add();
        }

		/// <summary>
		/// Remove unnecessary nodes from the drawing.
		/// </summary>
        public static void Remove()
        {
	        // Get stringers
	        var strList = Stringers.Geometries;

	        List<Point3d> toRemove;
			
			if (strList is null || !strList.Any())
		        toRemove = Positions;

			else
			{
				// Get points
				var nodes = strList.Select(str => str.CenterPoint).ToList();
				nodes.AddRange(strList.Select(str => str.InitialPoint));
				nodes.AddRange(strList.Select(str => str.EndPoint));

				// Get positions not needed
				toRemove = Positions.Where(p => !nodes.Any(n => n.Approx(p, Tolerance))).ToList();
			}

			// Add nodes
			Remove(toRemove);
        }

		/// <summary>
		/// Remove the node at this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The <see cref="Point3d"/> position.</param>
		public static void Remove(Point3d position)
        {
	        if (Positions.Contains(position))
		        Positions.Remove(position);

			// Remove from drawing
			GetNodeAtPosition(position).Remove();
        }

		/// <summary>
		/// Remove the nodes at these <paramref name="positions"/>.
		/// </summary>
		/// <param name="positions">The <see cref="Point3d"/> positions.</param>
        public static void Remove(IEnumerable<Point3d> positions)
        {
	        // Get positions to remove
	        var toRemove = Positions.Distinct(Comparer).Where(p => positions.Any(n => n.Approx(p, Tolerance))).ToList();

			// Update positions
			Positions = Positions.Where(p => !toRemove.Any(n => n.Approx(p, Tolerance))).ToList();

			// Remove from drawing
			GetNodesAtPositions(toRemove).ToArray().Remove();
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

			// Get all the nodes as points
			var ndObjs = GetAllNodes()?.ToList() ?? new List<DBPoint>();
			
			// Access the nodes on the document
			if (ndObjs.Any())
			{
				// Get the Xdata size
				int size = Enum.GetNames(typeof(NodeIndex)).Length;

                // Order nodes
                ndObjs = ndObjs.OrderBy(nd => nd.Position.Y).ThenBy(nd => nd.Position.X).ToList();

				for (var i = 0; i < ndObjs.Count; i++)
				{
					// Get the node number
					double ndNum = i + 1;

					// Initialize the array of typed values for XData
					var data = ndObjs[i].XData?.AsArray();
					data = data?.Length == size ? data : NewXData();

					// Set the updated number
					data[(int) NodeIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, ndNum);

                    // Add the new XData
                    ndObjs[i].SetXData(data);
				}
			}

			// Save positions
			Positions = GetPositions(ndObjs);

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
        public static IEnumerable<Node> Read(IEnumerable<DBPoint> nodeObjects, Units units) => nodeObjects?.Select(nd => Read(nd, units)).OrderBy(node => node.Number);

        /// <summary>
        /// Read a <see cref="Node"/> in the drawing.
        /// </summary>
        /// <param name="nodeObject">The <see cref="DBPoint"/> object of the node.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static Node Read(DBPoint nodeObject, Units units)
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
        /// Get node positions in the drawing.
        /// </summary>
        /// <param name="nodes">The collection of <see cref="DBPoint"/>'s.</param>
        private static List<Point3d> GetPositions(IEnumerable<DBPoint> nodes = null) => (nodes ?? GetAllNodes())?.Select(nd => nd.Position).ToList() ?? new List<Point3d>();
		//{
		// _positions = (nodes ?? GetAllNodes())?.Select(nd => nd.Position).ToList() ?? new List<Point3d>();
		// return _positions;
		//}

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
		/// Node comparer class.
		/// </summary>
		private class NodeComparer : IEqualityComparer<Point3d>
		{
			public NodeType Type { get; set; } = NodeType.External;
			public double Tolerance { get; set; } = 1E-3;

			/// <summary>
			/// Verify if two nodes are equal.
			/// </summary>
			public bool Equals(Point3d node, Point3d otherNode) => node.Approx(otherNode, Tolerance);

			/// <summary>
			/// Verify if two nodes are equal.
			/// </summary>
			public bool Equals(Point3d node, Point3d otherNode, NodeType otherNodeType) => Type == otherNodeType && node.Approx(otherNode, Tolerance);

			public int GetHashCode(Point3d obj) => obj.GetHashCode();
		}
	}
}