using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPM.Elements;
using SPMTool.AutoCAD;
using SPMTool.Database;
using SPMTool.Model;
using NodeData = SPMTool.XData.Node;

[assembly: CommandClass(typeof(Geometry.Node))]

namespace SPMTool.Model
{
	// Geometry related commands
	public partial class Geometry
	{
		// Node methods
		public class Node
		{
			// Properties
			public DBPoint PointObject { get; }

			public Point3d Position => PointObject.Position;

			public NodeType Type { get; }

			public string Layer
			{
				get
				{
					string layerName;

					if (Type == NodeType.External)
						layerName = ExtNodeLayer;

					else if (Type == NodeType.Internal)
						layerName = IntNodeLayer;

					else
						layerName = DispNodeLayer;

					return layerName;
				}
			}

			// Layer names
			public static readonly string
				ExtNodeLayer  = Layers.ExtNode.ToString(),
				IntNodeLayer  = Layers.IntNode.ToString(),
				DispNodeLayer = Layers.Displacements.ToString();

			// Constructor
			public Node(Point3d position, NodeType nodeType)
			{
				// Get the list of nodes
				var ndList = NodePositions(NodeType.All);

				// Check if a node already exists at the position. If not, its created
				if (!ndList.Contains(position))
				{
					// Get the type of node
					Type = nodeType;

					// Add to the list
					ndList.Add(position);

					// Create the node and set the layer
					PointObject = new DBPoint(position)
					{
						Layer = Layer
					};

					// Add the new object
					Auxiliary.AddObject(PointObject);
				}
			}

			public Node(List<Point3d> positions, NodeType nodeType)
			{
				// Get the list of nodes
				var ndList = NodePositions(NodeType.All);

				foreach (var position in positions)
				{
					// Check if a node already exists at the position. If not, its created
					if (!ndList.Contains(position))
					{
						// Get the type of node
						Type = nodeType;

						// Add to the list
						ndList.Add(position);

						// Create the node and set the layer
						PointObject = new DBPoint(position)
						{
							Layer = Layer
						};

						// Add the new object
						Auxiliary.AddObject(PointObject);
					}
				}
			}

			// Enumerate all the nodes in the model and return the collection of nodes
			public static ObjectIdCollection UpdateNodes(Units units)
			{
				// Get all the nodes in the model
				ObjectIdCollection nds = AllNodes();

				// Start a transaction
				using (Transaction trans = DataBase.StartTransaction())
				{
					// Get the list of nodes ordered
					var ndList = NodePositions(NodeType.All);

					// Access the nodes on the document
					foreach (ObjectId ndObj in nds)
					{
						// Read the object as a point
						DBPoint nd = trans.GetObject(ndObj, OpenMode.ForWrite) as DBPoint;

						// Get the node number on the list
						double ndNum = ndList.IndexOf(nd.Position) + 1;

						// Initialize the array of typed values for XData
						TypedValue[] data;

						// Get the Xdata size
						int size = Enum.GetNames(typeof(NodeData)).Length;

						// If the Extended data does not exist, create it
						if (nd.XData == null)
						{
							data = NewNodeXData();
						}

						else // Xdata exists
						{
							// Get the result buffer as an array
							data = Auxiliary.ReadXData(nd);

							// Check length
							if (data.Length != size)
								data = NewNodeXData();
						}

						// Set the updated number
						data[(int) NodeData.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, ndNum);

						// Add the new XData
						nd.XData = new ResultBuffer(data);
					}

					// Set the style for all point objects in the drawing
					DataBase.Database.Pdmode = 32;
					DataBase.Database.Pdsize = 40 * GlobalAuxiliary.ScaleFactor(units.Geometry);

					// Commit and dispose the transaction
					trans.Commit();
				}

				// Return the collection of nodes
				return nds;
			}

			// Get the list of node positions ordered
			public static List<Point3d> NodePositions(NodeType nodeType)
			{
				// Initialize an object collection
				ObjectIdCollection nds = new ObjectIdCollection();

				// Select the node type
				if (nodeType == NodeType.All)
					nds = AllNodes();

				if (nodeType == NodeType.Internal)
					nds = Auxiliary.GetObjectsOnLayer(Layers.IntNode);

				if (nodeType == NodeType.External)
					nds = Auxiliary.GetObjectsOnLayer(Layers.ExtNode);

				// Create a point collection
				var pts = new List<Point3d>();

				// Start a transaction
				using (Transaction trans = DataBase.StartTransaction())
				{
					foreach (ObjectId ndObj in nds)
					{
						// Read as a point and add to the collection
						DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;
						pts.Add(nd.Position);
					}
				}

				// Return the node list ordered
				return
					GlobalAuxiliary.OrderPoints(pts);
			}

			// Get the collection of all of the nodes
			public static ObjectIdCollection AllNodes()
			{
				// Create the nodes collection and initialize getting the elements on node layer
				ObjectIdCollection extNds = Auxiliary.GetObjectsOnLayer(Layers.ExtNode);
				ObjectIdCollection intNds = Auxiliary.GetObjectsOnLayer(Layers.IntNode);

				// Create a unique collection for all the nodes
				ObjectIdCollection nds = new ObjectIdCollection();
				foreach (ObjectId ndObj in extNds)
					nds.Add(ndObj);
				foreach (ObjectId ndObj in intNds)
					nds.Add(ndObj);

				return nds;
			}

			// Get the node number at the position
			public static int GetNodeNumber(Point3d position, ObjectIdCollection nodes = null)
			{
				nodes = nodes ?? AllNodes();

				// Initiate the node number
				int ndNum = 0;

				// Start a transaction
				using (Transaction trans = DataBase.StartTransaction())
				{
					// Compare to the nodes collection
					foreach (ObjectId ndObj in nodes)
					{
						// Open the selected object as a point for read
						DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

						// Compare the positions
						if (position == nd.Position)
						{
							// Get the node number
							// Access the XData as an array
							ResultBuffer ndRb = nd.GetXDataForApplication(DataBase.AppName);
							TypedValue[] dataNd = ndRb.AsArray();

							// Get the node number (line 2)
							ndNum = Convert.ToInt32(dataNd[(int) NodeData.Number].Value);
						}
					}
				}

				return ndNum;
			}

			// Create node XData
			private static TypedValue[] NewNodeXData()
			{
				// Definition for the Extended Data
				string xdataStr = "Node Data";

                // Get the Xdata size
                int size = Enum.GetNames(typeof(NodeData)).Length;

				// Initialize the array of typed values for XData
				var data = new TypedValue[size];

				// Set the initial parameters
				data[(int)NodeData.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
				data[(int)NodeData.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
				data[(int)NodeData.Ux]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
				data[(int)NodeData.Uy]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

				return data;
			}

            // Read a node in the drawing
            public static DBPoint ReadNode(ObjectId objectId, OpenMode openMode = OpenMode.ForRead)
			{
				// Start a transaction
				using (Transaction trans = DataBase.StartTransaction())
				{
					// Read the object as a point
					return
						trans.GetObject(objectId, openMode) as DBPoint;
				}
			}
		}
	}
}