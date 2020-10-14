using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using SPM.Elements;
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
		/// Add nodes in all necessary positions (stringer start, mid and end points).
		/// </summary>
		/// <param name="data">The extended data for the node object.</param>
		public static void Add(ResultBuffer data = null)
		{
            // Get the list of nodes
            var ndList = NodePositions(NodeType.All);

			// Get stringers
			var strList = Model.StringerCollection;

			// Get points
			var intNds = strList.Where(str => !ndList.Contains(str.MidPoint())).Select(str => str.MidPoint()).ToList();
			var extNds = strList.Where(str => !ndList.Contains(str.StartPoint)).Select(str => str.StartPoint).ToList();
			extNds.AddRange(strList.Where(str => !ndList.Contains(str.EndPoint)).Select(str => str.EndPoint));

            // Add nodes
			Add(intNds, NodeType.Internal, ref ndList, data);
			Add(extNds, NodeType.External, ref ndList, data);
		}

        /// <summary>
        /// Add a node to drawing in this <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The <see cref="Point3d"/> position.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        /// <param name="data">The extended data for the node object.</param>
        public static void Add(Point3d position, NodeType nodeType, ResultBuffer data = null)
		{
            // Get the list of nodes
            var ndList = NodePositions(NodeType.All);

			Add(position, nodeType, ref ndList, data);
		}

        /// <summary>
        /// Add a node to drawing in this <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The <see cref="Point3d"/> position.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        /// <param name="existentNodes">The collection containing the position of existent nodes in the drawing.</param>
        /// <param name="data">The extended data for the node object.</param>
		public static void Add(Point3d position, NodeType nodeType, ref IEnumerable<Point3d> existentNodes, ResultBuffer data = null)
		{
            // Get the list of nodes
            var ndList = existentNodes?.ToList() ?? new List<Point3d>();

            // Check if a node already exists at the position. If not, its created
            if (ndList.Contains(position))
				return;

			// Add to the list
			ndList.Add(position);
			existentNodes = ndList;

			// Create the node and set the layer
			var dbPoint = new DBPoint(position)
			{
				Layer = $"{GetLayer(nodeType)}"
			};

			// Add the new object
			dbPoint.Add();

			// Set Xdata
			dbPoint.SetXData(data ?? new ResultBuffer(NewXData()));
		}

        /// <summary>
        /// Add nodes to drawing in these <paramref name="positions"/>.
        /// </summary>
        /// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        /// <param name="existentNodes">The collection containing the position of existent nodes in the drawing.</param>
        /// <param name="data">The extended data for the node object.</param>
        public static void Add(IEnumerable<Point3d> positions, NodeType nodeType, ref IEnumerable<Point3d> existentNodes, ResultBuffer data = null)
		{
            foreach (var position in positions)
				Add(position, nodeType, ref existentNodes, data);
		}

        /// <summary>
        /// Add nodes to drawing in these <paramref name="positions"/>.
        /// </summary>
        /// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        /// <param name="data">The extended data for the node object.</param>
        public static void Add(IEnumerable<Point3d> positions, NodeType nodeType, ResultBuffer data = null)
		{
			// Get the list of nodes
			var ndList = NodePositions(NodeType.All);

			Add(positions, nodeType, ref ndList, data);
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
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        /// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
        public static IEnumerable<DBPoint> Update(LengthUnit geometryUnit, bool addNodes = true)
		{
			// Add nodes to all needed positions
			if (addNodes)
				Add();

			// Get all the nodes as points
			var ndObjs = GetAllNodes().ToArray();

			// Get the list of nodes ordered
			var ndList = NodePositions(NodeType.All).ToList();

			// Get the Xdata size
			int size = Enum.GetNames(typeof(NodeIndex)).Length;

			// Access the nodes on the document
			foreach (var nd in ndObjs)
			{
				// Get the node number on the list
				double ndNum = ndList.IndexOf(nd.Position) + 1;

				// Initialize the array of typed values for XData
				var data = nd.XData?.AsArray();
				data = data?.Length == size ? data : NewXData();

				// Set the updated number
				data[(int) NodeIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, ndNum);

				// Add the new XData
				nd.SetXData(data);
			}

			// Set the style for all point objects in the drawing
            DataBase.Database.Pdmode = 32;
            DataBase.Database.Pdsize = 40 * geometryUnit.ScaleFactor();

            // Return the collection of nodes
            return ndObjs;
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
					ndObjs = Layer.IntNode.GetDBObjects()?.ToPoints()?.ToList();
					break;

				case NodeType.External:
					ndObjs = Layer.ExtNode.GetDBObjects()?.ToPoints()?.ToList();
					break;

				default:
					ndObjs = null;
					break;
			}

			return ndObjs?.Select(nd => nd.Position).Order();
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

	        return
		        new Node(nodeObject.ObjectId, number, nodeObject.Position, GetNodeType(nodeObject), units.Geometry, units.Displacements);
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
        public static int? GetNumber(Point3d position, IEnumerable<DBPoint> nodeObjects = null) => (nodeObjects ?? GetAllNodes())?.First(nd => nd.Position.Approx(position))?.ReadXData()[(int) NodeIndex.Number].ToInt();

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
	}
}