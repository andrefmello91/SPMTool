using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Elements;

[assembly: CommandClass(typeof(SPMTool.ACAD.Geometry.Node))]

namespace SPMTool.ACAD
{
	// Geometry related commands
	public partial class Geometry
	{
		// Node methods
		public class Node
		{
			// Properties
			public DBPoint PointObject { get; }

			public Point3d Position
				=> PointObject.Position;

			public int Type { get; }

			public string Layer
			{
				get
				{
					if (Type == (int)Elements.Node.NodeType.External)
						return
							Layers.extNode;
					if (Type == (int)Elements.Node.NodeType.Internal)
						return
							Layers.intNode;

					return
						Layers.displacements;
				}
			}

			// Constructor
			public Node(Point3d position, int nodeType)
			{
				// Get the list of nodes
				var ndList = NodePositions((int)Elements.Node.NodeType.All);

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

			public Node(List<Point3d> positions, int nodeType)
			{
				// Get the list of nodes
				var ndList = NodePositions((int) Elements.Node.NodeType.All);

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
			public static ObjectIdCollection UpdateNodes()
			{
				// Definition for the Extended Data
				string xdataStr = "Node Data";

				// Get all the nodes in the model
				ObjectIdCollection nds = AllNodes();

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Get the list of nodes ordered
					var ndList = NodePositions((int) Elements.Node.NodeType.All);

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
						int size = Enum.GetNames(typeof(XData.Node)).Length;

						// If the Extended data does not exist, create it
						if (nd.XData == null)
						{
							data = nodeXData();
						}

						else // Xdata exists
						{
							// Get the result buffer as an array
							ResultBuffer rb = nd.GetXDataForApplication(Current.appName);
							data = rb.AsArray();

							// Check length
							if (data.Length != size)
								data = nodeXData();
						}

						// Set the updated number
						data[(int) XData.Node.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, ndNum);

						// Add the new XData
						nd.XData = new ResultBuffer(data);
					}

					// Set the style for all point objects in the drawing
					Current.db.Pdmode = 32;
					Current.db.Pdsize = 40;

					// Commit and dispose the transaction
					trans.Commit();
				}

				// Create node XData
				TypedValue[] nodeXData()
				{
					// Get the Xdata size
					int size = Enum.GetNames(typeof(XData.Node)).Length;

					// Initialize the array of typed values for XData
					var nData = new TypedValue[size];

					// Set the initial parameters
					nData[(int) XData.Node.AppName] =
						new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName);
					nData[(int) XData.Node.XDataStr] =
						new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
					nData[(int) XData.Node.Ux] = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
					nData[(int) XData.Node.Uy] = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

					return
						nData;
				}

				// Return the collection of nodes
				return nds;
			}

			// Get the list of node positions ordered
			public static List<Point3d> NodePositions(int nodeType)
			{
				// Initialize an object collection
				ObjectIdCollection nds = new ObjectIdCollection();

				// Select the node type
				if (nodeType == (int) Elements.Node.NodeType.All)
					nds = AllNodes();

				if (nodeType == (int) Elements.Node.NodeType.Internal)
					nds = Auxiliary.GetEntitiesOnLayer(Layers.intNode);

				if (nodeType == (int) Elements.Node.NodeType.External)
					nds = Auxiliary.GetEntitiesOnLayer(Layers.extNode);

				// Create a point collection
				var pts = new List<Point3d>();

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
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
					Auxiliary.OrderPoints(pts);
			}

			// Get the collection of all of the nodes
			public static ObjectIdCollection AllNodes()
			{
				// Create the nodes collection and initialize getting the elements on node layer
				ObjectIdCollection extNds = Auxiliary.GetEntitiesOnLayer(Layers.extNode);
				ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNode);

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
				if (nodes == null)
					nodes = AllNodes();

				// Initiate the node number
				int ndNum = 0;

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
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
							ResultBuffer ndRb = nd.GetXDataForApplication(Current.appName);
							TypedValue[] dataNd = ndRb.AsArray();

							// Get the node number (line 2)
							ndNum = Convert.ToInt32(dataNd[(int) XData.Node.Number].Value);
						}
					}
				}

				return ndNum;
			}
		}
	}
}