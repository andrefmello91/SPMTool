using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using SPM.Elements;
using SPMTool.Database.Conditions;
using SPMTool.Enums;
using UnitsNet.Units;

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Node class.
    /// </summary>
	public static class Nodes
	{
		/// <summary>
        /// Auxiliary list of nodes' <see cref="Point3d"/> positions.
        /// </summary>
		private static List<Point3d> _positions;

		/// <summary>
		/// Add nodes in all necessary positions (stringer start, mid and end points).
		/// </summary>
		public static void Add()
		{
			if (_positions is null)
				_positions = new List<Point3d>(NodePositions(NodeType.All));

			// Get stringers
			var strList = Model.StringerCollection;

			if (strList is null || !strList.Any())
				return;

			// Get points
			var intNds = strList.Select(str => str.MidPoint()).ToList();
			var extNds = strList.Select(str => str.StartPoint).ToList();
			extNds.AddRange(strList.Select(str => str.EndPoint));

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
			if (_positions is null)
				_positions = new List<Point3d>(NodePositions(NodeType.All));

            // Check if a node already exists at the position. If not, its created
            if (_positions.Contains(position))
				return;

            // Add to the list
            _positions.Add(position);

			// Create the node and set the layer
			var dbPoint = new DBPoint(position)
			{
				Layer = $"{GetLayer(nodeType)}"
			};
			
			// Add the new object
			dbPoint.Add(On_NodeErase);
		}

        /// <summary>
        /// Add nodes to drawing in these <paramref name="positions"/>.
        /// </summary>
        /// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        public static void Add(IEnumerable<Point3d> positions, NodeType nodeType)
		{
			foreach (var position in positions)
				Add(position, nodeType);
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
        public static void Update(bool addNodes = true)
		{
			// Add nodes to all needed positions
			if (addNodes)
				Add();

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
			_positions = ndObjs.Select(nd => nd.Position).ToList();

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

	        var node = new Node(nodeObject.ObjectId, number, nodeObject.Position, GetNodeType(nodeObject), units.Geometry, units.Displacements);

			// Set forces
			Forces.Set(node);

			return node;
        }

        /// <summary>
        /// Get <see cref="NodeType"/>.
        /// </summary>
        /// <param name="nodePoint">The <see cref="Entity"/> object.</param>
        private static NodeType GetNodeType(Entity nodePoint) => nodePoint.Layer == $"{Layer.ExtNode}" ? NodeType.External : NodeType.Internal;

        /// <summary>
        /// Get the layer name based on <paramref name="nodeType"/>.
        /// </summary>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        private static Layer GetLayer(NodeType nodeType)
        {
	        switch (nodeType)
	        {
		        case NodeType.External:
			        return Layer.ExtNode;

		        case NodeType.Internal:
			        return Layer.IntNode;

		        case NodeType.Displaced:
			        return Layer.Displacements;

		        default:
			        return default;
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
			if (_positions is null || !_positions.Any() || !(sender is DBPoint nd))
				return;

			if (_positions.Contains(nd.Position))
			{
				_positions.Remove(nd.Position);
				Model.Editor.WriteMessage($"\nRemoved: {nd.Position}");
			}
        }
	}
}