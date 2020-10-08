using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using SPM.Elements;
using SPMTool.Database;
using SPMTool.Enums;
using UnitsNet.Units;
using Nodes = SPMTool.Model.Elements.Nodes;

[assembly: CommandClass(typeof(Nodes))]

namespace SPMTool.Model.Elements
{
	/// <summary>
    /// Node class.
    /// </summary>
	public static class Nodes
	{
		/// <summary>
		/// Add a node to drawing in this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The <see cref="Point3d"/> position.</param>
		/// <param name="nodeType">The <see cref="NodeType"/>.</param>
		/// <param name="existentNodes">The collection containing the position of existent nodes in the drawing.</param>
		public static void Add(Point3d position, NodeType nodeType, IEnumerable<Point3d> existentNodes = null)
		{
            // Get the list of nodes
            var ndList = (existentNodes ?? NodePositions(NodeType.All)).ToList();

            // Check if a node already exists at the position. If not, its created
            if (ndList.Contains(position))
				return;

			// Add to the list
			ndList.Add(position);

			// Create the node and set the layer
			var dbPoint = new DBPoint(position)
			{
				Layer = $"{GetLayer(nodeType)}"
			};

			// Add the new object
			dbPoint.Add();
		}

        /// <summary>
        /// Add nodes to drawing in these <paramref name="positions"/>.
        /// </summary>
        /// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
        /// <param name="nodeType">The <see cref="NodeType"/>.</param>
        /// <param name="existentNodes">The collection containing the position of existent nodes in the drawing.</param>
        public static void Add(IEnumerable<Point3d> positions, NodeType nodeType, IEnumerable<Point3d> existentNodes = null)
		{
            // Get the list of nodes
            var ndList = (existentNodes ?? NodePositions(NodeType.All)).ToList();

            foreach (var position in positions)
				Add(position, nodeType, ndList);
		}

        /// <summary>
        /// Enumerate all the nodes in the model and return the collection of nodes.
        /// </summary>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        public static ObjectIdCollection Update(LengthUnit geometryUnit)
		{
			// Get all the nodes as points
			var ndObjs = AllNodes();
			var nds = ndObjs.ToDBObjectCollection();

			// Get the list of nodes ordered
			var ndList = NodePositions(NodeType.All).ToList();

			// Get the Xdata size
			int size = Enum.GetNames(typeof(NodeIndex)).Length;

			// Access the nodes on the document
			foreach (DBPoint nd in nds)
			{
				// Get the node number on the list
				double ndNum = ndList.IndexOf(nd.Position) + 1;

				// Initialize the array of typed values for XData
				var data = nd.XData?.AsArray();
				data = data?.Length == size ? data : NewXData();

				// Set the updated number
				data[(int)NodeIndex.Number] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNum);

				// Add the new XData
				nd.XData = new ResultBuffer(data);
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
			var nds = new ObjectIdCollection();

			// Select the node type
			if (nodeType == NodeType.All)
				nds = AllNodes();

			if (nodeType == NodeType.Internal)
				nds = Model.GetObjectsOnLayer(Layer.IntNode);

			if (nodeType == NodeType.External)
				nds = Model.GetObjectsOnLayer(Layer.ExtNode);

			// Create a point collection
			var pts = new List<Point3d>();

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
            using (nds)
	            pts.AddRange(from ObjectId ndObj in nds select (DBPoint) trans.GetObject(ndObj, OpenMode.ForRead) into nd select nd.Position);
			
			// Return the node list ordered
			return
				pts.Order();
		}

        /// <summary>
        /// Get the collection of all of the nodes in the drawing.
        /// </summary>
        public static ObjectIdCollection AllNodes()
		{
			// Create a unique collection for all the nodes
			var nds = new ObjectIdCollection();

            // Create the nodes collection and initialize getting the elements on node layer
            using (var extNds = Model.GetObjectsOnLayer(Layer.ExtNode))
            using (var intNds = Model.GetObjectsOnLayer(Layer.IntNode))
            {
	            foreach (ObjectId ndObj in extNds)
		            nds.Add(ndObj);

	            foreach (ObjectId ndObj in intNds)
		            nds.Add(ndObj);
            }

            return nds;
		}

        /// <summary>
        /// Read <see cref="Node"/> objects from an <see cref="ObjectIdCollection"/>.
        /// </summary>
        /// <param name="nodeObjectsIds">The <see cref="ObjectIdCollection"/> containing the nodes of drawing.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static IEnumerable<Node> Read(ObjectIdCollection nodeObjectsIds, Units units) => (from ObjectId ndObj in nodeObjectsIds select Read(ndObj, units)).OrderBy(node => node.Number);

        /// <summary>
        /// Read a <see cref="Node"/> in the drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of the node.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static Node Read(ObjectId objectId, Units units)
        {
	        // Read the object as a point
	        var ndPt = (DBPoint)objectId.ToDBObject();

	        // Read the XData and get the necessary data
	        var data = ndPt.ReadXData();

	        // Get the node number
	        var number = data[(int)NodeIndex.Number].ToInt();

	        return
		        new Node(objectId, number, ndPt.Position, GetNodeType(ndPt), units.Geometry, units.Displacements);
        }

        /// <summary>
        /// Get <see cref="NodeType"/>.
        /// </summary>
        /// <param name="nodePoint">The <see cref="Entity"/> object.</param>
        private static NodeType GetNodeType(Entity nodePoint) => nodePoint.Layer == Layer.ExtNode.ToString() ? NodeType.External : NodeType.Internal;

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
        /// <param name="nodes"></param>
        public static int GetNumber(Point3d position, ObjectIdCollection nodes = null)
		{
			var collection = (nodes ?? AllNodes()).ToDBObjectCollection();

			// Compare to the nodes collection
			return (from DBPoint nd in collection where position.Approx(nd.Position) select nd.ReadXData() into data select Convert.ToInt32(data[(int) NodeIndex.Number].Value)).FirstOrDefault();
		}

        /// <summary>
        /// Create node XData.
        /// </summary>
        /// <returns></returns>
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
	}
}