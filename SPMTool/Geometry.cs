using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(SPMTool.Geometry))]
[assembly: CommandClass(typeof(SPMTool.Geometry.Node))]
[assembly: CommandClass(typeof(SPMTool.Geometry.Stringer))]
[assembly: CommandClass(typeof(SPMTool.Geometry.Panel))]

namespace SPMTool
{
    // Geometry related commands
    public class Geometry
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
					if (Type == (int)SPMTool.Node.NodeType.External)
						return
							Layers.extNode;
					if (Type == (int)SPMTool.Node.NodeType.Internal)
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
				var ndList = NodePositions((int)SPMTool.Node.NodeType.All);

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
				var ndList = NodePositions((int)SPMTool.Node.NodeType.All);

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
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Get the list of nodes ordered
                    var ndList = NodePositions((int)SPMTool.Node.NodeType.All);

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
                            ResultBuffer rb = nd.GetXDataForApplication(AutoCAD.appName);
                            data = rb.AsArray();

                            // Check length
                            if (data.Length != size)
                                data = nodeXData();
                        }

                        // Set the updated number
                        data[(int)XData.Node.Number] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNum);

                        // Add the new XData
                        nd.XData = new ResultBuffer(data);
                    }

                    // Set the style for all point objects in the drawing
                    AutoCAD.curDb.Pdmode = 32;
                    AutoCAD.curDb.Pdsize = 40;

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
                    nData[(int)XData.Node.AppName] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName);
                    nData[(int)XData.Node.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
                    nData[(int)XData.Node.Ux] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                    nData[(int)XData.Node.Uy] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

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
                if (nodeType == (int)SPMTool.Node.NodeType.All)
                    nds = AllNodes();

                if (nodeType == (int)SPMTool.Node.NodeType.Internal)
                    nds = Auxiliary.GetEntitiesOnLayer(Layers.intNode);

                if (nodeType == (int)SPMTool.Node.NodeType.External)
                    nds = Auxiliary.GetEntitiesOnLayer(Layers.extNode);

                // Create a point collection
               var pts = new List<Point3d>();

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
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
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
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
                            ResultBuffer ndRb = nd.GetXDataForApplication(AutoCAD.appName);
                            TypedValue[] dataNd = ndRb.AsArray();

                            // Get the node number (line 2)
                            ndNum = Convert.ToInt32(dataNd[(int)XData.Node.Number].Value);
                        }
                    }
                }

                return ndNum;
            }
        }

        // Stringer methods
        public class Stringer
        {
			// Properties
			public Line    LineObject { get; }
			public Point3d StartPoint
				=> LineObject.StartPoint;
			public Point3d EndPoint
				=> LineObject.EndPoint;
			public string  Layer = Layers.stringer;

			// Constructor
			public Stringer(Point3d startPoint, Point3d endPoint, List<(Point3d start, Point3d end)> stringerList = null)
			{
				// Get the list of stringers if it's not imposed
				if (stringerList == null)
					stringerList = ListOfStringerPoints();

				// Check if a stringer already exist on that position. If not, create it
				if (!stringerList.Contains((startPoint, endPoint)))
				{
					// Add to the list
					stringerList.Add((startPoint, endPoint));

					// Create the line in Model space
					LineObject = new Line(startPoint, endPoint)
					{
						Layer = Layer
					};

					// Add the object
					Auxiliary.AddObject(LineObject);
				}
			}

            [CommandMethod("AddStringer")]
            public static void AddStringer()
            {
                // Check if the layers already exists in the drawing. If it doesn't, then it's created:
                Auxiliary.CreateLayer(Layers.extNode, (short)AutoCAD.Colors.Red);
                Auxiliary.CreateLayer(Layers.intNode, (short)AutoCAD.Colors.Blue);
                Auxiliary.CreateLayer(Layers.stringer, (short)AutoCAD.Colors.Cyan);

                // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
                Auxiliary.RegisterApp();

                // Get the list of start and endpoints
                var strList = ListOfStringerPoints();

                // Create lists of points for adding the nodes later
                List<Point3d> 
                    newIntNds = new List<Point3d>(),
                    newExtNds = new List<Point3d>();

                // Prompt for the start point of stringer
                PromptPointOptions strStOp = new PromptPointOptions("\nEnter the start point: ");
                PromptPointResult strStRes = AutoCAD.edtr.GetPoint(strStOp);

                // Exit if the user presses ESC or cancels the command
                if (strStRes.Status == PromptStatus.OK)
                {
                    // Loop for creating infinite stringers (until user exits the command)
                    for ( ; ; )
                    {
                        // Create a point3d collection and add the stringer start point
                        List<Point3d> nds = new List<Point3d>();
                        nds.Add(strStRes.Value);

                        // Prompt for the end point and add to the collection
                        PromptPointOptions strEndOp = new PromptPointOptions("\nEnter the end point: ")
                        {
                            UseBasePoint = true,
                            BasePoint = strStRes.Value
                        };
                        PromptPointResult strEndRes = AutoCAD.edtr.GetPoint(strEndOp);

                        if (strEndRes.Status == PromptStatus.OK)
                        {
                            nds.Add(strEndRes.Value);

                            // Get the points ordered in ascending Y and ascending X:
                            List<Point3d> extNds = Auxiliary.OrderPoints(nds);

                            // Create the stringer and add to drawing
                            new Stringer(extNds[0], extNds[1], strList);

                            // Get the midpoint
                            Point3d midPt = Auxiliary.MidPoint(extNds[0], extNds[1]);

                            // Add the position of the nodes to the list
                            if (!newExtNds.Contains(extNds[0]))
                                newExtNds.Add(extNds[0]);

                            if (!newExtNds.Contains(extNds[1]))
                                newExtNds.Add(extNds[1]);

                            if (!newIntNds.Contains(midPt))
                                newIntNds.Add(midPt);

                            // Set the start point of the new stringer
                            strStRes = strEndRes;
                        }

                        else
                        {
                            // Finish the command
                            break;
                        }
                    }
                }

                // Create the nodes
                new Node(newExtNds, (int)SPMTool.Node.NodeType.External);
                new Node(newIntNds, (int)SPMTool.Node.NodeType.Internal);

                // Update the nodes and stringers
                Node.UpdateNodes();
                UpdateStringers();
            }

            [CommandMethod("DivideStringer")]
            public static void DivideStringer()
            {
                // Prompt for select stringers
                AutoCAD.edtr.WriteMessage("\nSelect stringers to divide:");
                PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                if (selRes.Status == PromptStatus.OK)
                {
                    // Prompt for the number of segments
                    PromptIntegerOptions strNumOp = new PromptIntegerOptions("\nEnter the number of stringers:")
                    {
                        AllowNegative = false,
                        AllowZero = false
                    };

                    // Get the number
                    PromptIntegerResult strNumRes = AutoCAD.edtr.GetInteger(strNumOp);
                    if (strNumRes.Status == PromptStatus.OK)
                    {
                        int strNum = strNumRes.Value;

                        // Get the list of start and endpoints
                        var strList = ListOfStringerPoints();

                        // Get the selection set and analyse the elements
                        SelectionSet set = selRes.Value;

                        // Divide the stringers
                        if (set.Count > 0)
                        {
                            // Create lists of points for adding the nodes later
                            List<Point3d> newIntNds = new List<Point3d>(),
                                newExtNds = new List<Point3d>();

                            // Access the internal nodes in the model
                            ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNode);

                            // Start a transaction
                            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                            {
                                foreach (SelectedObject obj in set)
                                {
                                    // Open the selected object for read
                                    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                    // Check if the selected object is a stringer
                                    if (ent.Layer == Layers.stringer)
                                    {
                                        // Read as a line
                                        Line str = ent as Line;

                                        // Access the XData as an array
                                        ResultBuffer rb = str.GetXDataForApplication(AutoCAD.appName);

                                        // Get the coordinates of the initial and end points
                                        Point3d strSt = str.StartPoint,
                                            strEnd = str.EndPoint;

                                        // Calculate the distance of the points in X and Y
                                        double distX = (strEnd.X - strSt.X) / strNum,
                                            distY = (strEnd.Y - strSt.Y) / strNum;

                                        // Initialize the start point
                                        Point3d stPt = strSt;

                                        // Get the midpoint
                                        Point3d midPt = Auxiliary.MidPoint(strSt, strEnd);

                                        // Read the internal nodes
                                        foreach (ObjectId intNd in intNds)
                                        {
                                            // Read as point
                                            DBPoint nd = trans.GetObject(intNd, OpenMode.ForRead) as DBPoint;

                                            // Erase the internal node and remove from the list
                                            if (nd.Position == midPt)
                                            {
                                                nd.UpgradeOpen();
                                                nd.Erase();
                                                break;
                                            }
                                        }

                                        // Create the new stringers
                                        for (int i = 1; i <= strNum; i++)
                                        {
                                            // Get the coordinates of the other points
                                            double xCrd = str.StartPoint.X + i * distX;
                                            double yCrd = str.StartPoint.Y + i * distY;
                                            Point3d endPt = new Point3d(xCrd, yCrd, 0);

                                            // Create the stringer
                                            var newStr = new Stringer(stPt, endPt, strList);

											// Get the line
											var strLine = newStr.LineObject;

                                            // Append the XData of the original stringer
											if (strLine != null)
												strLine.XData = rb;

                                            // Get the mid point
                                            midPt = Auxiliary.MidPoint(stPt, endPt);

                                            // Add the position of the nodes to the list
                                            if (!newExtNds.Contains(stPt))
                                                newExtNds.Add(stPt);

                                            if (!newExtNds.Contains(endPt))
                                                newExtNds.Add(endPt);

                                            if (!newIntNds.Contains(midPt))
                                                newIntNds.Add(midPt);

                                            // Set the start point of the next stringer
                                            stPt = endPt;
                                        }

                                        // Erase the original stringer
                                        str.UpgradeOpen();
                                        str.Erase();

                                        // Remove from the list
                                        strList.Remove((strSt, strEnd));
                                    }
                                }

                                // Commit changes
                                trans.Commit();
                            }

                            // Create the nodes
                            new Node(newExtNds, (int)SPMTool.Node.NodeType.External);
                            new Node(newIntNds, (int)SPMTool.Node.NodeType.Internal);
                        }
                    }
                }

                // Update nodes and stringers
                Node.UpdateNodes();
                UpdateStringers();
            }

            // Update the stringer numbers on the XData of each stringer in the model and return the collection of stringers
            public static ObjectIdCollection UpdateStringers()
            {
                // Definition for the Extended Data
                string xdataStr = "Stringer Data";

                // Create the stringer collection and initialize getting the elements on layer
                ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.stringer);

                // Get all the nodes in the model
                ObjectIdCollection nds = Node.AllNodes();

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Create a point collection
                    List<Point3d> midPts = new List<Point3d>();

                    // Add the midpoint of each stringer to the collection
                    foreach (ObjectId strObj in strs)
                    {
                        // Read the object as a line
                        Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;

                        // Get the midpoint and add to the collection
                        Point3d midPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);
                        midPts.Add(midPt);
                    }

                    // Get the array of midpoints ordered
                    List<Point3d> midPtsList = Auxiliary.OrderPoints(midPts);

                    // Bool to alert the user
                    bool userAlert = false;

                    // Access the stringers on the document
                    foreach (ObjectId strObj in strs)
                    {
                        // Open the selected object as a line for write
                        Line str = trans.GetObject(strObj, OpenMode.ForWrite) as Line;

                        // Initialize the array of typed values for XData
                        TypedValue[] data;

                        // Get the Xdata size
                        int size = Enum.GetNames(typeof(XData.Stringer)).Length;

                        // If XData does not exist, create it
                        if (str.XData == null)
	                        data = NewStringerXData();

                        else // Xdata exists
                        {
                            // Get the result buffer as an array
                            ResultBuffer rb = str.GetXDataForApplication(AutoCAD.appName);
                            data = rb.AsArray();

							// Verify the size of XData
							if (data.Length != size)
							{
								data = NewStringerXData();

								// Alert the user
								userAlert = true;
							}
                        }

						// Method to create XData
						TypedValue[] NewStringerXData()
						{
							var newData = new TypedValue[size];

                            // Set the initial parameters
                            newData[(int)XData.Stringer.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName);
                            newData[(int)XData.Stringer.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
                            newData[(int)XData.Stringer.Width]     = new TypedValue((int)DxfCode.ExtendedDataReal, 1);
                            newData[(int)XData.Stringer.Height]    = new TypedValue((int)DxfCode.ExtendedDataReal, 1);
                            newData[(int)XData.Stringer.NumOfBars] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Stringer.BarDiam]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Stringer.Steelfy]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Stringer.SteelEs]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

                            return newData;
						}

                        // Get the coordinates of the midpoint of the stringer
                        Point3d midPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);

                        // Get the stringer number
                        int strNum = midPtsList.IndexOf(midPt) + 1;

                        // Get the start, mid and end nodes
                        int strStNd  = Node.GetNodeNumber(str.StartPoint, nds),
                            strMidNd = Node.GetNodeNumber(midPt, nds),
                            strEnNd  = Node.GetNodeNumber(str.EndPoint, nds);

                        // Set the updated number and nodes in ascending number and length (line 2 to 6)
                        data[(int)XData.Stringer.Number] = new TypedValue((int)DxfCode.ExtendedDataReal, strNum);
                        data[(int)XData.Stringer.Grip1]  = new TypedValue((int)DxfCode.ExtendedDataReal, strStNd);
                        data[(int)XData.Stringer.Grip2]  = new TypedValue((int)DxfCode.ExtendedDataReal, strMidNd);
                        data[(int)XData.Stringer.Grip3]  = new TypedValue((int)DxfCode.ExtendedDataReal, strEnNd);

                        // Add the new XData
                        str.XData = new ResultBuffer(data);
                    }

					// Alert the user
					if (userAlert)
						Application.ShowAlertDialog("Please set stringer geometry and reinforcement again");

                    // Commit and dispose the transaction
                    trans.Commit();
                }

                // Return the collection of stringers
                return strs;
            }

            // List of stringers (start and end points)
            public static List<(Point3d start, Point3d end)> ListOfStringerPoints()
            {
                // Get the stringers in the model
                ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.stringer);

                // Initialize a list
                var strList = new List<(Point3d startPoint, Point3d endPoint)>();

                if (strs.Count > 0)
                {
                    // Start a transaction
                    using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId obj in strs)
                        {
                            // Read as a line and add the start and end points to the collection
                            Line str = trans.GetObject(obj, OpenMode.ForRead) as Line;

                            // Add to the list
                            strList.Add((str.StartPoint, str.EndPoint));
                        }
                    }
                }

                return strList;
            }

            [CommandMethod("SetStringerGeometry")]
            public static void SetStringerGeometry()
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Request objects to be selected in the drawing area
                    AutoCAD.edtr.WriteMessage("\nSelect the stringers to assign properties (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
                    PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                    // If the prompt status is OK, objects were selected
                    if (selRes.Status == PromptStatus.OK)
                    {
                        SelectionSet set = selRes.Value;

                        // Ask the user to input the stringer width
                        PromptDoubleOptions strWOp = new PromptDoubleOptions("\nInput the width (in mm) for the selected stringers:")
                        {
                            DefaultValue = 1,
                            AllowZero = false,
                            AllowNegative = false
                        };

                        // Get the result
                        PromptDoubleResult strWRes = AutoCAD.edtr.GetDouble(strWOp);

                        if (strWRes.Status == PromptStatus.OK)
                        {
                            double strW = strWRes.Value;

                            // Ask the user to input the stringer height
                            PromptDoubleOptions strHOp = new PromptDoubleOptions("\nInput the height (in mm) for the selected stringers:")
                            {
                                DefaultValue = 1,
                                AllowZero = false,
                                AllowNegative = false
                            };

                            // Get the result
                            PromptDoubleResult strHRes = AutoCAD.edtr.GetDouble(strHOp);

                            if (strHRes.Status == PromptStatus.OK)
                            {
                                double strH = strHRes.Value;

                                foreach (SelectedObject obj in set)
                                {
                                    // Open the selected object for read
                                    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                    // Check if the selected object is a node
                                    if (ent.Layer == Layers.stringer)
                                    {
                                        // Upgrade the OpenMode
                                        ent.UpgradeOpen();

                                        // Access the XData as an array
                                        ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                        TypedValue[] data = rb.AsArray();

                                        // Set the new geometry and reinforcement (line 7 to 9 of the array)
                                        data[(int)XData.Stringer.Width] = new TypedValue((int)DxfCode.ExtendedDataReal, strW);
                                        data[(int)XData.Stringer.Height] = new TypedValue((int)DxfCode.ExtendedDataReal, strH);

                                        // Add the new XData
                                        ent.XData = new ResultBuffer(data);
                                    }
                                }

                                // Save the new object to the database
                                trans.Commit();
                            }
                        }
                    }
                }
            }
        }

        // Panel methods
        public class Panel
        {
			// Properties
			public Solid  SolidObject { get; }
			public string Layer = Layers.panel;

			// Constructor
			public Panel((Point3d, Point3d, Point3d, Point3d) vertices, List<(Point3d, Point3d, Point3d, Point3d)> panelList = null)
			{
				// Check if list of panels is null
				if (panelList == null)
					panelList = ListOfPanelVertices();

				// Check if a panel already exist on that position. If not, create it
				if (!panelList.Contains(vertices))
				{
					// Add to the list
					panelList.Add(vertices);

					// Create the panel as a solid with 4 segments (4 points)
					SolidObject = new Solid(vertices.Item1, vertices.Item2, vertices.Item3, vertices.Item4)
					{
						// Set the layer to Panel
						Layer = Layer
					};

					// Add the object
					Auxiliary.AddObject(SolidObject);
				}
            }

            [CommandMethod("AddPanel")]
            public static void AddPanel()
            {
                // Check if the layer panel already exists in the drawing. If it doesn't, then it's created:
                Auxiliary.CreateLayer(Layers.panel, (short)AutoCAD.Colors.Grey, 80);

                // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
                Auxiliary.RegisterApp();

                // Get the list of panel vertices
                var pnlList = ListOfPanelVertices();

                // Create a loop for creating infinite panels
                for ( ; ; )
                {
                    // Prompt for user select 4 vertices of the panel
                    AutoCAD.edtr.WriteMessage("\nSelect four nodes to be the vertices of the panel:");
                    PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                    if (selRes.Status == PromptStatus.OK)
                    {
                        SelectionSet set = selRes.Value;

                        // Create a point3d collection
                        List<Point3d> nds = new List<Point3d>();

                        // Start a transaction
                        using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                        {
                            // Get the objects in the selection and add to the collection only the external nodes
                            foreach (SelectedObject obj in set)
                            {
                                // Read as entity
                                Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                // Check if it is a external node
                                if (ent.Layer == Layers.extNode)
                                {
                                    // Read as a DBPoint and add to the collection
                                    DBPoint nd = ent as DBPoint;
                                    nds.Add(nd.Position);
                                }
                            }
                        }

                        // Check if there are four points
                        if (nds.Count == 4)
                        {
                            // Order the vertices in ascending Y and ascending X
                            List<Point3d> vrts = Auxiliary.OrderPoints(nds);

                            // Create the panel if it doesn't exist
                            var pnlPts = (vrts[0], vrts[1], vrts[2], vrts[3]);
                            new Panel(pnlPts, pnlList);
                        }

                        else
                            Application.ShowAlertDialog("Please select four external nodes.");
                    }

                    else 
                        // Finish the command
                        break;
                }

                // Update nodes and panels
                Node.UpdateNodes();
                UpdatePanels();
            }

            [CommandMethod("DividePanel")]
            public static void DividePanel()
            {
                // Prompt for select panels
                AutoCAD.edtr.WriteMessage("\nSelect panels to divide (panels must be rectangular):");
                PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                if (selRes.Status == PromptStatus.OK)
                {
                    // Prompt for the number of rows
                    PromptIntegerOptions rowOp = new PromptIntegerOptions("\nEnter the number of rows for adding panels:")
                    {
                        AllowNegative = false,
                        AllowZero = false
                    };

                    // Get the number
                    PromptIntegerResult rowRes = AutoCAD.edtr.GetInteger(rowOp);
                    if (rowRes.Status == PromptStatus.OK)
                    {
                        int row = rowRes.Value;

                        // Prompt for the number of columns
                        PromptIntegerOptions clmnOp = new PromptIntegerOptions("\nEnter the number of columns for adding panels:")
                        {
                            AllowNegative = false,
                            AllowZero = false
                        };

                        // Get the number
                        PromptIntegerResult clmnRes = AutoCAD.edtr.GetInteger(clmnOp);
                        if (clmnRes.Status == PromptStatus.OK)
                        {
                            int clmn = clmnRes.Value;

                            // Get the list of start and endpoints
                            var strList = Stringer.ListOfStringerPoints();

                            // Get the list of panels
                            var pnlList = ListOfPanelVertices();

                            // Create lists of points for adding the nodes later
                            List<Point3d> newIntNds = new List<Point3d>(),
                                newExtNds = new List<Point3d>();

                            // Create a list of start and end points for adding the stringers later
                            var newStrList = new List<(Point3d start, Point3d end)>();

                            // Access the stringers in the model
                            ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.stringer);

                            // Access the internal nodes in the model
                            ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNode);

                            // Start a transaction
                            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                            {
                                // Open the Block table for read
                                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                                // Open the Block table record Model space for write
                                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                                // Get the selection set and analyse the elements
                                SelectionSet set = selRes.Value;
                                foreach (SelectedObject obj in set)
                                {
                                    // Open the selected object for read
                                    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                    // Check if the selected object is a node
                                    if (ent.Layer == Layers.panel)
                                    {
                                        // Read as a solid
                                        Solid pnl = ent as Solid;

                                        // Access the XData as an array
                                        ResultBuffer rb = pnl.GetXDataForApplication(AutoCAD.appName);
                                        TypedValue[] data = rb.AsArray();

                                        // Get the panel number
                                        int pnlNum = Convert.ToInt32(data[(int)XData.Panel.Number].Value);

                                        // Get the coordinates of the grip points
                                        Point3dCollection grpPts = new Point3dCollection();
                                        pnl.GetGripPoints(grpPts, new IntegerCollection(), new IntegerCollection());

                                        // Create lines to measure the angles between the edges
                                        Line 
                                            ln1 = new Line(grpPts[0], grpPts[1]),
                                            ln2 = new Line(grpPts[0], grpPts[2]),
                                            ln3 = new Line(grpPts[2], grpPts[3]),
                                            ln4 = new Line(grpPts[1], grpPts[3]);

                                        // Get the angles
                                        double ang1 = ln2.Angle - ln1.Angle;
                                        double ang4 = ln4.Angle - ln3.Angle;

                                        // Verify if the panel is rectangular
                                        if (ang1 == Constants.PiOver2 && ang4 == Constants.PiOver2) // panel is rectangular
                                        {
                                            // Get the surrounding stringers to erase
                                            foreach (ObjectId strObj in strs)
                                            {
                                                // Read as a line
                                                Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;

                                                // Verify if the stringer starts and ends in a panel vertex
                                                if (grpPts.Contains(str.StartPoint) && grpPts.Contains(str.EndPoint))
                                                {
                                                    // Get the midpoint
                                                    Point3d midPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);

                                                    // Read the internal nodes
                                                    foreach (ObjectId intNd in intNds)
                                                    {
                                                        // Read as point
                                                        DBPoint nd = trans.GetObject(intNd, OpenMode.ForRead) as DBPoint;

                                                        // Erase the internal node and remove from the list
                                                        if (nd.Position == midPt)
                                                        {
                                                            nd.UpgradeOpen();
                                                            nd.Erase();
                                                            break;
                                                        }
                                                    }

                                                    // Erase and remove from the list
                                                    strList.Remove((str.StartPoint, str.EndPoint));
                                                    str.UpgradeOpen();
                                                    str.Erase();
                                                }
                                            }

                                            // Calculate the distance of the points in X and Y
                                            double distX = (grpPts[1].X - grpPts[0].X) / clmn;
                                            double distY = (grpPts[2].Y - grpPts[0].Y) / row;

                                            // Initialize the start point
                                            Point3d stPt = grpPts[0];

                                            // Create the new panels
                                            for (int i = 0; i < row; i++)
                                            {
                                                for (int j = 0; j < clmn; j++)
                                                {
                                                    // Get the vertices of the panel and add to a list
                                                    var verts = new List<Point3d>();
                                                    verts.Add(new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0));
                                                    verts.Add(new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0));
                                                    verts.Add(new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0));
                                                    verts.Add(new Point3d(stPt.X + (j + 1) * distX, stPt.Y + (i + 1) * distY, 0));

                                                    // Create the panel
                                                    var pnlPts = (verts[0], verts[1], verts[2], verts[3]);
                                                    var newPnl = new Panel(pnlPts, pnlList);

													// Get the solid object
													var pnlSolid = newPnl.SolidObject;

                                                    // Append the XData of the original panel
													if (pnlSolid != null)
														pnlSolid.XData = rb;

                                                    // Add the vertices to the list for creating external nodes
                                                    foreach (Point3d pt in verts)
                                                    {
                                                        if (!newExtNds.Contains(pt))
                                                            newExtNds.Add(pt);
                                                    }

                                                    // Create tuples to adding the stringers later
                                                    var strsToAdd = new []
                                                    {
                                                        (verts[0], verts[1]),
                                                        (verts[0], verts[2]),
                                                        (verts[2], verts[3]),
                                                        (verts[1], verts[3])
                                                    };

                                                    // Add to the list of new stringers
                                                    foreach (var pts in strsToAdd)
                                                    {
                                                        if (!newStrList.Contains(pts))
                                                            newStrList.Add(pts);
                                                    }
                                                }
                                            }

                                            // Erase the original panel
                                            pnl.UpgradeOpen();
                                            pnl.Erase();

                                            // Remove from the list
                                            pnlList.Remove((grpPts[0], grpPts[1], grpPts[2], grpPts[3]));
                                        }

                                        else // panel is not rectangular
                                            AutoCAD.edtr.WriteMessage("\nPanel " + pnlNum + " is not rectangular");
                                    }
                                }

                                // Save the new object to the database
                                trans.Commit();
                            }

                            // Create the stringers
                            foreach (var pts in newStrList)
                            {
                                new Stringer(pts.start, pts.end, strList);

                                // Get the midpoint to add the external node
                                Point3d midPt = Auxiliary.MidPoint(pts.Item1, pts.Item2);
                                if (!newIntNds.Contains(midPt))
                                    newIntNds.Add(midPt);
                            }

                            // Create the nodes
                            new Node(newExtNds, (int)SPMTool.Node.NodeType.External);
                            new Node(newIntNds, (int)SPMTool.Node.NodeType.Internal);

                            // Update the elements
                            Node.UpdateNodes();
                            Stringer.UpdateStringers();
                            UpdatePanels();

                            // Show an alert for editing stringers
                            Application.ShowAlertDialog("Alert: stringers parameters must be set again.");
                        }
                    }
                }
            }

            [CommandMethod("SetPanelGeometry")]
            public static void SetPanelGeometry()
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Request objects to be selected in the drawing area
                    AutoCAD.edtr.WriteMessage("\nSelect the panels to assign properties (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
                    PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                    // If the prompt status is OK, objects were selected
                    if (selRes.Status == PromptStatus.OK)
                    {
                        // Get the selection
                        SelectionSet set = selRes.Value;

                        // Ask the user to input the panel width
                        PromptDoubleOptions pnlWOp = new PromptDoubleOptions("\nInput the width (in mm) for the selected panels:")
                        {
                            DefaultValue = 1,
                            AllowZero = false,
                            AllowNegative = false
                        };

                        // Get the result
                        PromptDoubleResult pnlWRes = AutoCAD.edtr.GetDouble(pnlWOp);

                        if (pnlWRes.Status == PromptStatus.OK)
                        {
                            double pnlW = pnlWRes.Value;

                            foreach (SelectedObject obj in set)
                            {
                                // Open the selected object for read
                                Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                // Check if the selected object is a node
                                if (ent.Layer == Layers.panel)
                                {
                                    // Upgrade the OpenMode
                                    ent.UpgradeOpen();

                                    // Access the XData as an array
                                    ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                    TypedValue[] data = rb.AsArray();

                                    // Set the new geometry and reinforcement (line 7 to 9 of the array)
                                    data[(int)XData.Panel.Width] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlW);

                                    // Add the new XData
                                    ent.XData = new ResultBuffer(data);
                                }
                            }

                            // Save the new object to the database
                            trans.Commit();
                        }
                    }
                }
            }

            // Update the panel numbers on the XData of each panel in the model and return the collection of panels
            public static ObjectIdCollection UpdatePanels()
            {
                // Definition for the Extended Data
                string xdataStr = "Panel Data";

                // Get the internal nodes of the model
                ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNode);

                // Create the panels collection and initialize getting the elements on node layer
                ObjectIdCollection pnls = Auxiliary.GetEntitiesOnLayer(Layers.panel);

                // Create a point collection
                List<Point3d> cntrPts = new List<Point3d>();

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Add the centerpoint of each panel to the collection
                    foreach (ObjectId pnlObj in pnls)
                    {
                        // Read the object as a solid
                        Solid pnl = trans.GetObject(pnlObj, OpenMode.ForRead) as Solid;

                        // Get the vertices
                        Point3dCollection pnlVerts = new Point3dCollection();
                        pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                        // Get the approximate coordinates of the center point of the panel
                        Point3d cntrPt = Auxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);
                        cntrPts.Add(cntrPt);
                    }

                    // Get the list of center points ordered
                    List<Point3d> cntrPtsList = Auxiliary.OrderPoints(cntrPts);

					// Bool to alert the user
					bool userAlert = false;

                    // Access the panels on the document
                    foreach (ObjectId pnlObj in pnls)
                    {
                        // Open the selected object as a solid for write
                        Solid pnl = trans.GetObject(pnlObj, OpenMode.ForWrite) as Solid;

                        // Initialize the array of typed values for XData
                        TypedValue[] data;

                        // Get the Xdata size
                        int size = Enum.GetNames(typeof(XData.Panel)).Length;

                        // Check if the XData already exist. If not, create it
                        if (pnl.XData == null)
	                        data = NewPanelXData();

                        else // Xdata exists
                        {
                            // Get the result buffer as an array
                            ResultBuffer rb = pnl.GetXDataForApplication(AutoCAD.appName);
                            data = rb.AsArray();

                            // Verify the size of XData
                            if (data.Length != size)
                            {
	                            data = NewPanelXData();

								// Alert user
								userAlert = true;
                            }
                        }

						// Method to set panel data
                        TypedValue[] NewPanelXData()
                        {
	                        TypedValue[] newData = new TypedValue[size];

                            // Set the initial parameters
                            newData[(int)XData.Panel.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName);
                            newData[(int)XData.Panel.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
                            newData[(int)XData.Panel.Width]    = new TypedValue((int)DxfCode.ExtendedDataReal, 1);
                            newData[(int)XData.Panel.XDiam]    = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Panel.Sx]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Panel.fyx]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Panel.Esx]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Panel.YDiam]    = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Panel.Sy]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Panel.fyy]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                            newData[(int)XData.Panel.Esy]      = new TypedValue((int)DxfCode.ExtendedDataReal, 0);

                            return newData;
                        }

                        // Get the vertices
                        Point3dCollection pnlVerts = new Point3dCollection();
                        pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                        // Get the approximate coordinates of the center point of the panel
                        Point3d cntrPt = Auxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);

                        // Get the coordinates of the panel DoFs in the necessary order
                        Point3dCollection pnlGrips = new Point3dCollection();
                        pnlGrips.Add(Auxiliary.MidPoint(pnlVerts[0], pnlVerts[1]));
                        pnlGrips.Add(Auxiliary.MidPoint(pnlVerts[1], pnlVerts[3]));
                        pnlGrips.Add(Auxiliary.MidPoint(pnlVerts[3], pnlVerts[2]));
                        pnlGrips.Add(Auxiliary.MidPoint(pnlVerts[2], pnlVerts[0]));

                        // Get the panel number
                        int pnlNum = cntrPtsList.IndexOf(cntrPt) + 1;

                        // Initialize an int array of grip numbers
                        int[] grips = new int[4];

                        // Compare the node position to the panel vertices
                        foreach (Point3d grip in pnlGrips)
                        {
                            // Get the position of the vertex in the array
                            int i = pnlGrips.IndexOf(grip);

                            // Get the node number
                            grips[i] = Node.GetNodeNumber(grip, intNds);
                        }

                        // Set the updated panel number
                        data[(int)XData.Panel.Number] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum);

                        // Set the updated node numbers in the necessary order
                        data[(int)XData.Panel.Grip1] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[0]);
                        data[(int)XData.Panel.Grip2] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[1]);
                        data[(int)XData.Panel.Grip3] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[2]);
                        data[(int)XData.Panel.Grip4] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[3]);

                        // Add the new XData
                        pnl.XData = new ResultBuffer(data);

                        // Read it as a block and get the draw order table
                        BlockTableRecord blck = trans.GetObject(pnl.BlockId, OpenMode.ForRead) as BlockTableRecord;
                        DrawOrderTable drawOrder = trans.GetObject(blck.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

                        // Move the panels to bottom
                        drawOrder.MoveToBottom(pnls);
                    }

                    // Alert user
					if (userAlert)
						Application.ShowAlertDialog("Please set panel geometry and reinforcement again");

                    // Commit and dispose the transaction
                    trans.Commit();
                }

                // Return the collection of panels
                return pnls;
            }

            // List of panels (collection of vertices)
            public static List<(Point3d, Point3d, Point3d, Point3d)> ListOfPanelVertices()
            {
                // Get the stringers in the model
                ObjectIdCollection pnls = Auxiliary.GetEntitiesOnLayer(Layers.panel);

                // Initialize a list
                var pnlList = new List<(Point3d, Point3d, Point3d, Point3d)>();

                if (pnls.Count > 0)
                {
                    // Start a transaction
                    using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId obj in pnls)
                        {
                            // Read the object as a solid
                            Solid pnl = trans.GetObject(obj, OpenMode.ForRead) as Solid;

                            // Get the vertices
                            Point3dCollection pnlVerts = new Point3dCollection();
                            pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                            // Add to the list
                            var pnlPts = (pnlVerts[0], pnlVerts[1], pnlVerts[2], pnlVerts[3]);
                            pnlList.Add(pnlPts);
                        }
                    }
                }

                return pnlList;
            }
        }

        [CommandMethod("UpdateElements")]
        public static void UpdateElements()
        {
            // Enumerate and get the number of nodes
            ObjectIdCollection nds = Node.UpdateNodes();
            int numNds = nds.Count;

            // Update and get the number of stringers
            ObjectIdCollection strs = Stringer.UpdateStringers();
            int numStrs = strs.Count;

            // Update and get the number of panels
            ObjectIdCollection pnls = Panel.UpdatePanels();
            int numPnls = pnls.Count;

            // Display the number of updated elements
            AutoCAD.edtr.WriteMessage("\n" + numNds + " nodes, " + numStrs + " stringers and " + numPnls + " panels updated.");
        }

        // Toggle view for nodes
        [CommandMethod("ToogleNodes")]
        public static void ToogleNodes()
        {
            Auxiliary.ToogleLayer(Layers.extNode);
            Auxiliary.ToogleLayer(Layers.intNode);
        }

        // Toggle view for stringers
        [CommandMethod("ToogleStringers")]
        public static void ToogleStringers()
        {
            Auxiliary.ToogleLayer(Layers.stringer);
        }

        // Toggle view for panels
        [CommandMethod("TooglePanels")]
        public static void TooglePanels()
        {
            Auxiliary.ToogleLayer(Layers.panel);
        }
    }
}