using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(SPMTool.Geometry))]

namespace SPMTool
{
    // Geometry related commands
    public class Geometry
    {
        [CommandMethod("AddStringer")]
        public static void AddStringer()
        {
            // Check if the layers already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.extNdLyr, Colors.red, 0);
            Auxiliary.CreateLayer(Layers.intNdLyr, Colors.blue, 0);
            Auxiliary.CreateLayer(Layers.strLyr, Colors.cyan, 0);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            Auxiliary.RegisterApp();

            // Get the list of start and endpoints
            var strList = ListOfStringers();

            // Create lists of points for adding the nodes later
            List<Point3d> newIntNds = new List<Point3d>(),
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
                    nds.Add(strEndRes.Value);

                    if (strEndRes.Status == PromptStatus.OK)
                    {
                        // Get the points ordered in ascending Y and ascending X:
                        List<Point3d> extNds = Auxiliary.OrderPoints(nds);

                        // Create the stringer and add to drawing
                        Tuple<Point3d, Point3d> strPts = Tuple.Create(extNds[0], extNds[1]);
                        Line str = Stringer(strList, strPts);
                        Auxiliary.AddObject(str);

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
            Node(newExtNds, Layers.extNdLyr);
            Node(newIntNds, Layers.intNdLyr);

            // Update the nodes and stringers
            UpdateNodes();
            UpdateStringers();
        }

        // Create a stringer if it doesn't already exist
        public static Line Stringer(List<Tuple<Point3d, Point3d>> stringerList, Tuple<Point3d, Point3d> stringerPoints)
        {
            // Initialize a line
            Line str = new Line();

            // Check if a stringer already exist on that position. If not, create it
            if (!stringerList.Contains(stringerPoints))
            {
                // Add to the list
                stringerList.Add(stringerPoints);

                // Create the line in Model space
                str = new Line(stringerPoints.Item1, stringerPoints.Item2)
                {
                    Layer = Layers.strLyr
                };
            }

            return str;
        }

        [CommandMethod("AddPanel")]
        public static void AddPanel()
        {
            // Check if the layer panel already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.pnlLyr, Colors.grey, 80);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            Auxiliary.RegisterApp();

            // Get the list of panel vertices
            var pnlList = ListOfPanels();

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
                            if (ent.Layer == Layers.extNdLyr)
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
                        var pnlPts = Tuple.Create(vrts[0], vrts[1], vrts[2], vrts[3]);
                        Solid pnl = Panel(pnlList, pnlPts);
                        Auxiliary.AddObject(pnl);
                    }

                    else
                        Application.ShowAlertDialog("Please select four external nodes.");
                }

                else 
                    // Finish the command
                    break;
            }

            // Update nodes and panels
            UpdateNodes();
            UpdatePanels();
        }

        // Create a panel if it doesn't already exist
        public static Solid Panel(List<Tuple<Point3d, Point3d, Point3d, Point3d>> panelList, Tuple<Point3d, Point3d, Point3d, Point3d> vertices)
        {
            // Initialize a solid
            Solid pnl = new Solid();

            // Check if a panel already exist on that position. If not, create it
            if (!panelList.Contains(vertices))
            {
                // Add to the list
                panelList.Add(vertices);

                // Create the panel as a solid with 4 segments (4 points)
                pnl = new Solid(vertices.Item1, vertices.Item2, vertices.Item3, vertices.Item4)
                {
                    // Set the layer to Panel
                    Layer = Layers.pnlLyr
                };
            }

            return pnl;
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
                    var strList = ListOfStringers();

                    // Get the selection set and analyse the elements
                    SelectionSet set = selRes.Value;

                    // Divide the stringers
                    if (set.Count > 0)
                    {
                        // Create lists of points for adding the nodes later
                        List<Point3d> newIntNds = new List<Point3d>(),
                                      newExtNds = new List<Point3d>();

                        // Access the internal nodes in the model
                        ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNdLyr);

                        // Start a transaction
                        using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                        {
                            // Open the Block table for read
                            BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                            // Open the Block table record Model space for write
                            BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                            foreach (SelectedObject obj in set)
                            {
                                // Open the selected object for read
                                Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                // Check if the selected object is a stringer
                                if (ent.Layer == Layers.strLyr)
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
                                        Tuple<Point3d, Point3d> strPts = Tuple.Create(stPt, endPt);
                                        Line newStr = Stringer(strList, strPts);
                                        Auxiliary.AddObject(newStr);

                                        // Append the XData of the original stringer
                                        newStr.XData = rb;

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
                                    strList.Remove(Tuple.Create(strSt, strEnd));
                                }
                            }

                            // Commit changes
                            trans.Commit();
                        }

                        // Create the nodes
                        Node(newExtNds, Layers.extNdLyr);
                        Node(newIntNds, Layers.intNdLyr);
                    }
                }
            }

            // Update nodes and stringers
            UpdateNodes();
            UpdateStringers();
        }

        // Method to divide a panel and adjacent stringers
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
                        var strList = ListOfStringers();

                        // Get the list of panels
                        var pnlList = ListOfPanels();

                        // Create lists of points for adding the nodes later
                        List<Point3d> newIntNds = new List<Point3d>(),
                                      newExtNds = new List<Point3d>();

                        // Create a list of start and end points for adding the stringers later
                        var newStrList = new List<Tuple<Point3d, Point3d>>();

                        // Access the stringers in the model
                        ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.strLyr);

                        // Access the internal nodes in the model
                        ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNdLyr);

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
                                if (ent.Layer == Layers.pnlLyr)
                                {
                                    // Read as a solid
                                    Solid pnl = ent as Solid;

                                    // Access the XData as an array
                                    ResultBuffer rb = pnl.GetXDataForApplication(AutoCAD.appName);
                                    TypedValue[] data = rb.AsArray();

                                    // Get the panel number
                                    int pnlNum = Convert.ToInt32(data[PanelXDataIndex.num].Value);

                                    // Get the coordinates of the grip points
                                    Point3dCollection grpPts = new Point3dCollection();
                                    pnl.GetGripPoints(grpPts, new IntegerCollection(), new IntegerCollection());

                                    // Create lines to measure the angles between the edges
                                    Line ln1 = new Line(grpPts[0], grpPts[1]),
                                         ln2 = new Line(grpPts[0], grpPts[2]),
                                         ln3 = new Line(grpPts[2], grpPts[3]),
                                         ln4 = new Line(grpPts[1], grpPts[3]);

                                    // Get the angles
                                    double ang1 = ln2.Angle - ln1.Angle;
                                    double ang4 = ln4.Angle - ln3.Angle;

                                    // Verify if the panel is rectangular
                                    if (ang1 == Constants.piOver2 && ang4 == Constants.piOver2) // panel is rectangular
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
                                                strList.Remove(Tuple.Create(str.StartPoint, str.EndPoint));
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
                                                var pnlPts = Tuple.Create(verts[0], verts[1], verts[2], verts[3]);
                                                Solid newPnl = Panel(pnlList, pnlPts);
                                                Auxiliary.AddObject(newPnl);

                                                // Append the XData of the original panel
                                                newPnl.XData = rb;

                                                // Add the vertices to the list for creating external nodes
                                                foreach (Point3d pt in verts)
                                                {
                                                    if (!newExtNds.Contains(pt))
                                                        newExtNds.Add(pt);
                                                }

                                                // Create tuples to adding the stringers later
                                                Tuple<Point3d, Point3d>[] strsToAdd = new Tuple<Point3d, Point3d>[]
                                                {
                                                    Tuple.Create(verts[0], verts[1]),
                                                    Tuple.Create(verts[0], verts[2]),
                                                    Tuple.Create(verts[2], verts[3]),
                                                    Tuple.Create(verts[1], verts[3])
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
                                        pnlList.Remove(Tuple.Create(grpPts[0], grpPts[1], grpPts[2], grpPts[3]));
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
                            Line str = Stringer(strList, pts);
                            Auxiliary.AddObject(str);

                            // Get the midpoint to add the external node
                            Point3d midPt = Auxiliary.MidPoint(pts.Item1, pts.Item2);
                            if (!newIntNds.Contains(midPt))
                                newIntNds.Add(midPt);
                        }

                        // Create the nodes
                        Node(newExtNds, Layers.extNdLyr);
                        Node(newIntNds, Layers.intNdLyr);

                        // Update the elements
                        UpdateNodes();
                        UpdateStringers();
                        UpdatePanels();

                        // Show an alert for editing stringers
                        Application.ShowAlertDialog("Alert: stringers parameters must be set again.");
                    }
                }
            }
        }


        [CommandMethod("UpdateElements")]
        public void UpdateElements()
        {
            // Enumerate and get the number of nodes
            ObjectIdCollection nds = UpdateNodes();
            int numNds = nds.Count;

            // Update and get the number of stringers
            ObjectIdCollection strs = UpdateStringers();
            int numStrs = strs.Count;

            // Update and get the number of panels
            ObjectIdCollection pnls = UpdatePanels();
            int numPnls = pnls.Count;

            // Display the number of updated elements
            AutoCAD.edtr.WriteMessage("\n" + numNds.ToString() + " nodes, " + numStrs.ToString() + " stringers and " + numPnls.ToString() + " panels updated.");
        }

        [CommandMethod("SetStringerGeometry")]
        public void SetStringerGeometry()
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
                                if (ent.Layer == Layers.strLyr)
                                {
                                    // Upgrade the OpenMode
                                    ent.UpgradeOpen();

                                    // Access the XData as an array
                                    ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                    TypedValue[] data = rb.AsArray();

                                    // Set the new geometry and reinforcement (line 7 to 9 of the array)
                                    data[StringerXDataIndex.w] = new TypedValue((int)DxfCode.ExtendedDataReal, strW);
                                    data[StringerXDataIndex.h] = new TypedValue((int)DxfCode.ExtendedDataReal, strH);

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

        [CommandMethod("SetPanelGeometry")]
        public void SetPanelGeometry()
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
                            if (ent.Layer == Layers.pnlLyr)
                            {
                                // Upgrade the OpenMode
                                ent.UpgradeOpen();

                                // Access the XData as an array
                                ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                TypedValue[] data = rb.AsArray();

                                // Set the new geometry and reinforcement (line 7 to 9 of the array)
                                data[PanelXDataIndex.w] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlW);

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

        // Method to add nodes at a given position and a layer name
        public static void Node(Point3d position, string layerName)
        {
            // Get the list of nodes
            var ndList = ListOfNodes("All");

            // Check if a node already exists at the position. If not, its created
            if (!ndList.Contains(position))
            {
                // Add to the list
                ndList.Add(position);

                // Create the node and set the layer
                DBPoint nd = new DBPoint(position)
                {
                    Layer = layerName
                };

                // Add the new object
                Auxiliary.AddObject(nd);
            }
        }

        // Method to add nodes at a given list of positions and a layer name
        public static void Node(List<Point3d> positions, string layerName)
        {
            // Get the list of nodes
            var ndList = ListOfNodes("All");

            foreach (Point3d pt in positions)
            {
                // Check if a node already exists at the position. If not, its created
                if (!ndList.Contains(pt))
                {
                    // Add to the list
                    ndList.Add(pt);

                    // Create the node and set the layer
                    DBPoint nd = new DBPoint(pt)
                    {
                        Layer = layerName
                    };

                    // Add the new object
                    Auxiliary.AddObject(nd);
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
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Get the list of nodes ordered
                List<Point3d> ndList = ListOfNodes("All");

                // Access the nodes on the document
                foreach (ObjectId ndObj in nds)
                {
                    // Read the object as a point
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForWrite) as DBPoint;

                    // Get the node number on the list
                    double ndNum = ndList.IndexOf(nd.Position) + 1;

                    // Initialize the array of typed values for XData
                    TypedValue[] data;

                    // If the Extended data does not exist, create it
                    if (nd.XData == null)
                    {
                        // Get the Xdata size
                        data = new TypedValue[NodeXDataIndex.size];

                        // Set the initial parameters
                        data[NodeXDataIndex.appName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName);
                        data[NodeXDataIndex.xdataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
                        data[NodeXDataIndex.support]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Free");
                        data[NodeXDataIndex.Fx]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                        data[NodeXDataIndex.Fy]   = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                        data[NodeXDataIndex.ux]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                        data[NodeXDataIndex.uy]       = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                    }

                    else // Xdata exists
                    {
                        // Get the result buffer as an array
                        ResultBuffer rb = nd.GetXDataForApplication(AutoCAD.appName);
                        data = rb.AsArray();
                    }

                    //// Define the Xdata to add to the node
                    //using (ResultBuffer defRb = new ResultBuffer())
                    //    {
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));  // 0
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ndNum));                  // 2
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xPosition));              // 3
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yPosition));              // 4
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, support));         // 5
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xForce));                 // 6
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yForce));                 // 7
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ux));                     // 8
                    //        defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, uy));                     // 9

                    //        // Append the extended data to each object
                    //        nd.XData = defRb;
                    //    }

                    // Set the updated number
                    data[NodeXDataIndex.num] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNum);

                    // Add the new XData
                    nd.XData = new ResultBuffer(data);
                }

                // Set the style for all point objects in the drawing
                AutoCAD.curDb.Pdmode = 32;
                AutoCAD.curDb.Pdsize = 40;

                // Commit and dispose the transaction
                trans.Commit();
            }

            // Return the collection of nodes
            return nds;
        }

        // Update the node numbers on the XData of each stringer in the model and return the collection of stringers
        public static ObjectIdCollection UpdateStringers()
        {
            // Definition for the Extended Data
            string xdataStr = "Stringer Data";

            // Create the stringer collection and initialize getting the elements on layer
            ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.strLyr);

            // Get all the nodes in the model
            ObjectIdCollection nds = AllNodes();

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

                // Access the stringers on the document
                foreach (ObjectId strObj in strs)
                {
                    // Open the selected object as a line for write
                    Line str = trans.GetObject(strObj, OpenMode.ForWrite) as Line;

                    // Initialize the array of typed values for XData
                    TypedValue[] data;

                    // If XData does not exist, create it
                    if (str.XData == null)
                    {
                        // Get the Xdata size
                        data = new TypedValue[StringerXDataIndex.size];

                        // Set the initial parameters
                        data[StringerXDataIndex.appName] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName);
                        data[StringerXDataIndex.xdataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
                        data[StringerXDataIndex.w] = new TypedValue((int)DxfCode.ExtendedDataReal, 1);
                        data[StringerXDataIndex.h] = new TypedValue((int)DxfCode.ExtendedDataReal, 1);
                        data[StringerXDataIndex.nBars] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                        data[StringerXDataIndex.phi] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                    }

                    else // Xdata exists
                    {
                        // Get the result buffer as an array
                        ResultBuffer rb = str.GetXDataForApplication(AutoCAD.appName);
                        data = rb.AsArray();
                    }

                    //// Define the Xdata to add to the node
                    //using (ResultBuffer rb = new ResultBuffer())
                    //    {
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));   // 0
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strNum));                 // 2
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strStNd));                // 3
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strMidNd));               // 4
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strEnNd));                // 5
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strLgt));                 // 6
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strW));                   // 7
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strH));                   // 8
                    //        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, As));                     // 9

                    //        // Open the stringer for write
                    //        Entity ent = trans.GetObject(str.ObjectId, OpenMode.ForWrite) as Entity;

                    //        // Append the extended data to each object
                    //        ent.XData = rb;
                    //    }

                    // Get the coordinates of the midpoint of the stringer
                    Point3d midPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);

                    // Get the stringer number
                    int strNum = midPtsList.IndexOf(midPt) + 1;

                    // Get the start, mid and end nodes
                    int strStNd  = GetNodeNumber(str.StartPoint, nds),
                        strMidNd = GetNodeNumber(midPt, nds),
                        strEnNd  = GetNodeNumber(str.EndPoint, nds);

                    // Set the updated number and nodes in ascending number and length (line 2 to 6)
                    data[StringerXDataIndex.num]   = new TypedValue((int)DxfCode.ExtendedDataReal, strNum);
                    data[StringerXDataIndex.StNd]  = new TypedValue((int)DxfCode.ExtendedDataReal, strStNd);
                    data[StringerXDataIndex.MidNd] = new TypedValue((int)DxfCode.ExtendedDataReal, strMidNd);
                    data[StringerXDataIndex.EndNd]  = new TypedValue((int)DxfCode.ExtendedDataReal, strEnNd);

                    // Add the new XData
                    str.XData = new ResultBuffer(data);
                }

                // Commit and dispose the transaction
                trans.Commit();
            }

            // Return the collection of stringers
            return strs;
        }

        // Update the node numbers on the XData of each panel in the model and return the collection of panels
        public static ObjectIdCollection UpdatePanels()
        {
            // Definition for the Extended Data
            string xdataStr = "Panel Data";

            // Get the internal nodes of the model
            ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNdLyr);

            // Create the panels collection and initialize getting the elements on node layer
            ObjectIdCollection pnls = Auxiliary.GetEntitiesOnLayer(Layers.pnlLyr);

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

                // Access the panels on the document
                foreach (ObjectId pnlObj in pnls)
                {
                    // Open the selected object as a solid for write
                    Solid pnl = trans.GetObject(pnlObj, OpenMode.ForWrite) as Solid;

                    // Initialize the array of typed values for XData
                    TypedValue[] data;

                    // Check if the XData already exist. If not, create it
                    if (pnl.XData == null)
                    {
                        // Get the Xdata size
                        data = new TypedValue[PanelXDataIndex.size];

                        // Set the initial parameters
                        data[PanelXDataIndex.appName] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName);
                        data[PanelXDataIndex.xdataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
                        data[PanelXDataIndex.w] = new TypedValue((int)DxfCode.ExtendedDataReal, 1);
                        data[PanelXDataIndex.phiX] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                        data[PanelXDataIndex.sx] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                        data[PanelXDataIndex.phiY] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                        data[PanelXDataIndex.sy] = new TypedValue((int)DxfCode.ExtendedDataReal, 0);
                    }

                    else // Xdata exists
                    {
                        // Get the result buffer as an array
                        ResultBuffer rb = pnl.GetXDataForApplication(AutoCAD.appName);
                        data = rb.AsArray();
                    }

                    //// Initialize a Result Buffer to add to the panel
                    //ResultBuffer rb = new ResultBuffer();
                    //rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));  // 0
                    //rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                    //rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum));                 // 2
                    //for (int i = 0; i < 4; i++)
                    //{
                    //    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, grips[i]));           // 3, 4, 5, 6
                    //}
                    //rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlW));                   // 7
                    //rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psx));                    // 8
                    //rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psy));                    // 9

                    //// Append the extended data to the object
                    //pnl.XData = rb;

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
                        grips[i] = GetNodeNumber(grip, intNds);
                    }

                    // Set the updated panel number
                    data[PanelXDataIndex.num] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum);

                    // Set the updated node numbers in the necessary order
                    data[PanelXDataIndex.grip1] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[0]);
                    data[PanelXDataIndex.grip2] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[1]);
                    data[PanelXDataIndex.grip3] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[2]);
                    data[PanelXDataIndex.grip4] = new TypedValue((int)DxfCode.ExtendedDataReal, grips[3]);

                    // Add the new XData
                    pnl.XData = new ResultBuffer(data);

                    // Read it as a block and get the draw order table
                    BlockTableRecord blck = trans.GetObject(pnl.BlockId, OpenMode.ForRead) as BlockTableRecord;
                    DrawOrderTable drawOrder = trans.GetObject(blck.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

                    // Move the panels to bottom
                    drawOrder.MoveToBottom(pnls);
                }

                // Commit and dispose the transaction
                trans.Commit();
            }

            // Return the collection of panels
            return pnls;
        }

        // Get the list of node positions ordered
        public static List<Point3d> ListOfNodes(string nodeType)
        {
            // Initialize an object collection
            ObjectIdCollection nds = new ObjectIdCollection();

            // Select the node type
            if (nodeType == "All")
                nds = AllNodes();

            if (nodeType == "Int")
                nds = Auxiliary.GetEntitiesOnLayer(Layers.intNdLyr);

            if (nodeType == "Ext")
                nds = Auxiliary.GetEntitiesOnLayer(Layers.extNdLyr);

            // Create a point collection
            List<Point3d> ndPos = new List<Point3d>();

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ndObj in nds)
                {
                    // Read as a point and add to the collection
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;
                    ndPos.Add(nd.Position);
                }
            }

            // Return the node list ordered
            return Auxiliary.OrderPoints(ndPos);
        }

        // List of stringers (start and end points)
        public static List<Tuple<Point3d, Point3d>> ListOfStringers()
        {
            // Get the stringers in the model
            ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.strLyr);

            // Initialize a list
            List<Tuple<Point3d, Point3d>> strList = new List<Tuple<Point3d, Point3d>>();

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
                        strList.Add(Tuple.Create(str.StartPoint, str.EndPoint));
                    }
                }
            }

            return strList;
        }

        // List of panels (collection of vertices)
        public static List<Tuple<Point3d, Point3d, Point3d, Point3d>> ListOfPanels()
        {
            // Get the stringers in the model
            ObjectIdCollection pnls = Auxiliary.GetEntitiesOnLayer(Layers.pnlLyr);

            // Initialize a list
            var pnlList = new List<Tuple<Point3d, Point3d, Point3d, Point3d>>();

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
                        var pnlPts = Tuple.Create(pnlVerts[0], pnlVerts[1], pnlVerts[2], pnlVerts[3]);
                        pnlList.Add(pnlPts);
                    }
                }
            }

            return pnlList;
        }

        // Get the collection of all of the nodes
        public static ObjectIdCollection AllNodes()
        {
            // Create the nodes collection and initialize getting the elements on node layer
            ObjectIdCollection extNds = Auxiliary.GetEntitiesOnLayer(Layers.extNdLyr);
            ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNdLyr);

            // Create a unique collection for all the nodes
            ObjectIdCollection nds = new ObjectIdCollection();
            foreach (ObjectId ndObj in extNds) nds.Add(ndObj);
            foreach (ObjectId ndObj in intNds) nds.Add(ndObj);

            return nds;
        }

        // Get the node number at the position
        public static int GetNodeNumber(Point3d position, ObjectIdCollection nodes)
        {
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
                        ndNum = Convert.ToInt32(dataNd[NodeXDataIndex.num].Value);
                    }
                }
            }

            return ndNum;
        }

        // Toggle view for nodes
        [CommandMethod("ToogleNodes")]
        public void ToogleNodes()
        {
            Auxiliary.ToogleLayer(Layers.extNdLyr);
            Auxiliary.ToogleLayer(Layers.intNdLyr);
        }

        // Toggle view for stringers
        [CommandMethod("ToogleStringers")]
        public void ToogleStringers()
        {
            Auxiliary.ToogleLayer(Layers.strLyr);
        }

        // Toggle view for panels
        [CommandMethod("TooglePanels")]
        public void TooglePanels()
        {
            Auxiliary.ToogleLayer(Layers.pnlLyr);
        }
    }
}