using System;
using System.Collections.Generic;
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
        [CommandMethod("AddNode")]
        public static void AddNode()
        {
            // Define the layer parameters
            string ndLayer = "Node";
            short red = 1;

            // Set the style for all point objects in the drawing
            Global.curDb.Pdmode = 32;
            Global.curDb.Pdsize = 50;

            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(ndLayer, red, 0);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            AuxMethods.RegisterApp();

            // Loop for creating infinite nodes (until user exits the command)
            for ( ; ; )
            {
                // Start a transaction
                using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create the node in Model space
                    // Tell user to insert the point:
                    PromptPointOptions ptOp = new PromptPointOptions("\nPick point or enter coordinates: ");
                    PromptPointResult ptRes = Global.ed.GetPoint(ptOp);

                    // Exit if the user presses ESC or cancels the command
                    if (ptRes.Status == PromptStatus.OK)
                    {
                        // Create the node and set its layer to Node:
                        DBPoint newNd = new DBPoint(ptRes.Value);
                        newNd.Layer = ndLayer;

                        // Add the new object to the block table record and the transaction
                        blkTblRec.AppendEntity(newNd);
                        trans.AddNewlyCreatedDBObject(newNd, true);
                    }
                    else
                    {
                        // Finish the command
                        break;
                    }

                    // Save the new object to the database and dispose the transaction
                    trans.Commit();
                }
            }

            // Enumerate the nodes
            AuxMethods.UpdateNodes();
        }

        [CommandMethod("AddStringer")]
        public static void AddStringer()
        {
            // Define the layer parameters
            string strLayer = "Stringer";
            short cyan = 4;

            // Check if the layer stringer already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(strLayer, cyan, 0);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            AuxMethods.RegisterApp();

            // Enumerate the nodes
            AuxMethods.UpdateNodes();

            // Get the current Object Snap Setting
            Object curOsmode = Application.GetSystemVariable("osmode");

            // Set the Object Snap Setting to node (8)
            Application.SetSystemVariable("OSMODE", 8);

            // Prompt for the start point of stringer
            PromptPointOptions strStOp = new PromptPointOptions("\nPick the start node: ");
            PromptPointResult strStRes = Global.ed.GetPoint(strStOp);

            // Exit if the user presses ESC or cancels the command
            if (strStRes.Status == PromptStatus.OK)
            {
                // Loop for creating infinite stringers (until user exits the command)
                for ( ; ; )
                {
                    // Start a transaction
                    using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
                    {
                        // Create a point3d collection and add the stringer start point
                        Point3dCollection nds = new Point3dCollection();
                        nds.Add(strStRes.Value);

                        // Prompt for the end point and add to the collection
                        PromptPointOptions strEndOp = new PromptPointOptions("\nPick the end node: ")
                        {
                            UseBasePoint = true,
                            BasePoint = strStRes.Value
                        };
                        PromptPointResult strEndRes = Global.ed.GetPoint(strEndOp);
                        nds.Add(strEndRes.Value);

                        if (strEndRes.Status == PromptStatus.OK)
                        {
                            // Get the points ordered in ascending Y and ascending X:
                            List<Point3d> extNds = AuxMethods.OrderPoints(nds);
                            Point3d strSt = extNds[0];
                            Point3d strEnd = extNds[1];

                            // Open the Block table for read
                            BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                            // Open the Block table record Model space for write
                            BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                            // Create the line in Model space
                            using (Line newStr = new Line(strSt, strEnd))
                            {
                                // Set the layer to stringer
                                newStr.Layer = strLayer;

                                // Add the line to the drawing
                                blkTblRec.AppendEntity(newStr);
                                trans.AddNewlyCreatedDBObject(newStr, true);
                            }

                            // Set the start point of the new stringer
                            strStRes = strEndRes;
                        }
                        else
                        {
                            // Finish the command
                            break;
                        }

                        // Save the new object to the database
                        trans.Commit();
                    }
                }
            }

            // Update the stringers
            AuxMethods.UpdateStringers();

            // Set the Object Snap Setting to the initial
            Application.SetSystemVariable("OSMODE", curOsmode);
        }

        [CommandMethod("AddPanel")]
        public static void AddPanel()
        {
            // Define the layer parameters
            string pnlLayer = "Panel";
            short grey = 254;

            // Get the current Object Snap Setting
            Object curOsmode = Application.GetSystemVariable("osmode");

            // Set the Object Snap Setting to node (8)
            Application.SetSystemVariable("OSMODE", 8);

            // Check if the layer panel already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(pnlLayer, grey, 80);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            AuxMethods.RegisterApp();

            // Enumerate the nodes
            AuxMethods.UpdateNodes();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Create a point3d collection
                Point3dCollection nds = new Point3dCollection();

                // Prompt for user enter the vertices of the panel
                for (int i = 0; i < 4; i++)
                {
                    // Prompt each vertice (using the base point)
                    PromptPointOptions pnlNdOp = new PromptPointOptions("\nSelect nodes performing a loop");

                    // If the first point were already selected, use the previous point as a basepoint
                    if (i >= 1)
                    {
                        pnlNdOp.UseBasePoint = true;
                        pnlNdOp.BasePoint = nds[i - 1];
                    }

                    // Get the result and add to the collection
                    PromptPointResult pnlNdRes = Global.ed.GetPoint(pnlNdOp);
                    nds.Add(pnlNdRes.Value);
                }

                // Order the vertices in ascending Y and ascending X
                List<Point3d> vrts = AuxMethods.OrderPoints(nds);

                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create the panel as a solid with 4 segments (4 points)
                using (Solid newPnl = new Solid(vrts[0], vrts[1], vrts[2], vrts[3]))
                {
                    // Set the layer to Panel
                    newPnl.Layer = pnlLayer;

                    // Add the panel to the drawing
                    blkTblRec.AppendEntity(newPnl);
                    trans.AddNewlyCreatedDBObject(newPnl, true);
                }

                // Save the new object to the database
                trans.Commit();
            }

            // Update the panels
            AuxMethods.UpdatePanels();

            // Set the Object Snap Setting to the initial
            Application.SetSystemVariable("OSMODE", curOsmode);
        }

        [CommandMethod("DivideStringer")]
        public static void DivideStringer()
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Prompt for select stringers
                Global.ed.WriteMessage("\nSelect stringers to divide:");
                PromptSelectionResult selRes = Global.ed.GetSelection();

                if (selRes.Status == PromptStatus.OK)
                {
                    // Prompt for the number of segments
                    PromptIntegerOptions strNumOp = new PromptIntegerOptions("\nEnter the number of stringers:")
                    {
                        AllowNegative = false,
                        AllowZero = false
                    };

                    // Get the number
                    PromptIntegerResult strNumRes = Global.ed.GetInteger(strNumOp);
                    int strNum = strNumRes.Value;

                    // Get the selection set and analyse the elements
                    SelectionSet set = selRes.Value;
                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Stringer"))
                        {
                            // Read as a line
                            Line str = ent as Line;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(Global.appName);

                            // Get the coordinates of the initial and end points
                            Point3d strSt = str.StartPoint;
                            Point3d strEnd = str.EndPoint;

                            // Calculate the distance of the points in X and Y
                            double distX = (strEnd.X - strSt.X) / strNum;
                            double distY = (strEnd.Y - strSt.Y) / strNum;

                            // Initialize the start point
                            Point3d stPt = strSt;

                            // Create the new stringers
                            for (int i = 0; i <= strNum; i++)
                            {
                                // Get the coordinates of the other points
                                double xCrd = str.StartPoint.X + i * distX;
                                double yCrd = str.StartPoint.Y + i * distY;
                                Point3d endPt = new Point3d(xCrd, yCrd, 0);

                                // Create the stringer
                                Line newStr = new Line()
                                {
                                    StartPoint = stPt,
                                    EndPoint = endPt,
                                    Layer = "Stringer"
                                };

                                // Add the line to the drawing
                                blkTblRec.AppendEntity(newStr);
                                trans.AddNewlyCreatedDBObject(newStr, true);

                                // Append the XData of the original stringer
                                newStr.XData = rb;

                                // Set the start point of the next stringer
                                stPt = endPt;
                            }

                            // Create the new nodes (initial and end node already exist)
                            for (int j = 1; j < strNum; j++)
                            {
                                // Get the coordinates
                                double xCrd = str.StartPoint.X + j * distX;
                                double yCrd = str.StartPoint.Y + j * distY;
                                Point3d ndPt = new Point3d(xCrd, yCrd, 0);

                                // Create the node and set its layer to Node:
                                DBPoint newNd = new DBPoint()
                                {
                                    Position = ndPt,
                                    Layer = "Node",
                                };

                                // Add the new object to the block table record and the transaction
                                blkTblRec.AppendEntity(newNd);
                                trans.AddNewlyCreatedDBObject(newNd, true);
                            }

                            // Erase the original stringer
                            ent.UpgradeOpen();
                            ent.Erase();
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }

            // Update nodes and stringers
            AuxMethods.UpdateNodes();
            AuxMethods.UpdateStringers();
        }

        [CommandMethod("DividePanel")]
        public static void DividePanel()
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Prompt for select panels
                Global.ed.WriteMessage("\nSelect panels to divide (panels must be rectangular):");
                PromptSelectionResult selRes = Global.ed.GetSelection();

                if (selRes.Status == PromptStatus.OK)
                {
                    // Prompt for the number of rows
                    PromptIntegerOptions rowOp = new PromptIntegerOptions("\nEnter the number of rows for adding panels:")
                    {
                        AllowNegative = false,
                        AllowZero = false
                    };

                    // Get the number
                    PromptIntegerResult rowRes = Global.ed.GetInteger(rowOp);
                    int row = rowRes.Value;

                    // Prompt for the number of columns
                    PromptIntegerOptions clmnOp = new PromptIntegerOptions("\nEnter the number of columns for adding panels:")
                    {
                        AllowNegative = false,
                        AllowZero = false
                    };

                    // Get the number
                    PromptIntegerResult clmnRes = Global.ed.GetInteger(clmnOp);
                    int clmn = rowRes.Value;

                    // Get the selection set and analyse the elements
                    SelectionSet set = selRes.Value;
                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Panel"))
                        {
                            // Read as a solid
                            Solid pnl = ent as Solid;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(Global.appName);

                            // Get the coordinates of the grip points
                            Point3dCollection grpPts = new Point3dCollection();
                            pnl.GetGripPoints(grpPts, new IntegerCollection(), new IntegerCollection());

                            // Calculate the distance of the points in X and Y
                            double distX = (grpPts[1].X - grpPts[0].X) / clmn;
                            double distY = (grpPts[2].Y - grpPts[0].Y) / row;
                            
                            // Initialize the start point
                            Point3d stPt = grpPts[0];
                            Point3d[] verts = new Point3d[4];

                            // Create the new panels
                            for (int i = 0; i < row; i++)
                            {
                                for (int j = 0; j < clmn; j++)
                                {
                                   // Get the vertices of the panel
                                    verts[0] = new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0);
                                    verts[1] = new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0);
                                    verts[2] = new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0);
                                    verts[3] = new Point3d(stPt.X + (j + 1) * distX, stPt.Y + (i + 1) * distY, 0);

                                    // Create the panel
                                    Solid newPnl = new Solid(verts[0], verts[1], verts[2], verts[3])
                                    {
                                        Layer = "Panel"
                                    };

                                    // Add the line to the drawing
                                    blkTblRec.AppendEntity(newPnl);
                                    trans.AddNewlyCreatedDBObject(newPnl, true);

                                    // Append the XData of the original panel
                                    newPnl.XData = rb;

                                    // Create the internal nodes
                                    if (i > 0 && j > 0)
                                    {
                                        DBPoint nd = new DBPoint(new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0))
                                        {
                                            Layer = "Node"
                                        };

                                        // Add the node to the drawing
                                        blkTblRec.AppendEntity(nd);
                                        trans.AddNewlyCreatedDBObject(nd, true);
                                    }

                                    // Create the internal horizontal stringers
                                    if (i > 0)
                                    {
                                        Line strX = new Line()
                                        {
                                            StartPoint = new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
                                            EndPoint = new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0),
                                            Layer = "Stringer"
                                        };

                                        // Add the line to the drawing
                                        blkTblRec.AppendEntity(strX);
                                        trans.AddNewlyCreatedDBObject(strX, true);
                                    }

                                    // Create the internal vertical stringers
                                    if (j > 0)
                                    {
                                        Line strY = new Line()
                                        {
                                            StartPoint = new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
                                            EndPoint = new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0),
                                            Layer = "Stringer"
                                        };

                                        // Add the line to the drawing
                                        blkTblRec.AppendEntity(strY);
                                        trans.AddNewlyCreatedDBObject(strY, true);
                                    }

                                }

                            }

                            // Erase the original panel
                            ent.UpgradeOpen();
                            ent.Erase();
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }

            // Update the elements
            AuxMethods.UpdateNodes();
            AuxMethods.UpdateStringers();
            AuxMethods.UpdatePanels();
        }

        [CommandMethod("UpdateElements")]
        public void UpdateElements()
        {
            // Enumerate and get the number of nodes
            int numNds = AuxMethods.UpdateNodes();

            // Update and get the number of stringers
            int numStrs = AuxMethods.UpdateStringers();

            // Update and get the number of panels
            int numPnls = AuxMethods.UpdatePanels();

            // Display the number of updated elements
            Global.ed.WriteMessage("\n" + numNds.ToString() + " nodes, " + numStrs.ToString() + " stringers and " + numPnls.ToString() + " panels updated.");
        }

        [CommandMethod("SetStringerParameters")]
        public void SetStringerParameters()
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                Global.ed.WriteMessage("\nSelect the stringers to assign properties (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
                PromptSelectionResult selRes = Global.ed.GetSelection();

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
                    PromptDoubleResult strWRes = Global.ed.GetDouble(strWOp);
                    double strW = strWRes.Value;

                    // Ask the user to input the stringer height
                    PromptDoubleOptions strHOp = new PromptDoubleOptions("\nInput the height (in mm) for the selected stringers:")
                    {
                        DefaultValue = 1,
                        AllowZero = false,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult strHRes = Global.ed.GetDouble(strHOp);
                    double strH = strHRes.Value;

                    // Ask the user to input the reinforcement area
                    PromptDoubleOptions AsOp = new PromptDoubleOptions("\nInput the reinforcement area for the selected stringers (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult AsRes = Global.ed.GetDouble(AsOp);
                    double As = AsRes.Value;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Stringer"))
                        {
                            // Upgrade the OpenMode
                            ent.UpgradeOpen();

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(Global.appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new geometry and reinforcement (line 6 to 8 of the array)
                            data[6] = new TypedValue((int)DxfCode.ExtendedDataReal, strW);
                            data[7] = new TypedValue((int)DxfCode.ExtendedDataReal, strH);
                            data[8] = new TypedValue((int)DxfCode.ExtendedDataReal, As);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }
        }

        [CommandMethod("SetPanelParameters")]
        public void SetPanelParameters()
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                Global.ed.WriteMessage("\nSelect the panels to assign properties (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
                PromptSelectionResult selRes = Global.ed.GetSelection();

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
                    PromptDoubleResult pnlWRes = Global.ed.GetDouble(pnlWOp);
                    double pnlW = pnlWRes.Value;

                    // Ask the user to input the reinforcement ratio in x direction
                    PromptDoubleOptions psxOp = new PromptDoubleOptions("\nInput the reinforcement ratio in x direction for selected panels (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult psxRes = Global.ed.GetDouble(psxOp);
                    double psx = psxRes.Value;

                    // Ask the user to input the reinforcement ratio in y direction
                    PromptDoubleOptions psyOp = new PromptDoubleOptions("\nInput the reinforcement ratio in y direction for selected panels (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult psyRes = Global.ed.GetDouble(psyOp);
                    double psy = psyRes.Value;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Panel"))
                        {
                            // Upgrade the OpenMode
                            ent.UpgradeOpen();

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(Global.appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new geometry and reinforcement (line 7 to 9 of the array)
                            data[7] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlW);
                            data[8] = new TypedValue((int)DxfCode.ExtendedDataReal, psx);
                            data[9] = new TypedValue((int)DxfCode.ExtendedDataReal, psy);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }
        }

        [CommandMethod("ViewElementData")]
        public void ViewElementData()
        {
            // Initialize a message to display
            string msgstr = "";

            // Start a loop for viewing continuous elements
            for ( ; ; )
            { 
                // Request the object to be selected in the drawing area
                PromptEntityOptions entOp = new PromptEntityOptions("\nSelect an element to view data:");
                PromptEntityResult entRes = Global.ed.GetEntity(entOp);

                // If the prompt status is OK, objects were selected
                if (entRes.Status == PromptStatus.OK)
                {
                    // Start a transaction
                    using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
                    {

                        // Get the entity for read
                        Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

                        // Get the extended data attached to each object for SPMTool
                        ResultBuffer rb = ent.GetXDataForApplication(Global.appName);

                        // Make sure the Xdata is not empty
                        if (rb != null)
                        {
                            // Get the XData as an array
                            TypedValue[] data = rb.AsArray();

                            // If it's a node
                            if (ent.Layer == "Node")
                            {
                                // Get the parameters
                                string ndNum = data[2].Value.ToString(), posX = data[3].Value.ToString(), posY = data[4].Value.ToString();
                                string sup = data[5].Value.ToString(), forX = data[6].Value.ToString(), forY = data[7].Value.ToString();

                                msgstr = "Node " + ndNum + "\n\n" +
                                         "Node position: (" + posX + ", " + posY + ")" + "\n" +
                                         "Support conditions: " + sup + "\n" +
                                         "Force in X direction = " + forX + " N" + "\n" +
                                         "Force in Y direction = " + forY + " N";
                            }

                            // If it's a stringer
                            if (ent.Layer == "Stringer")
                            {
                                // Get the parameters
                                string strNum = data[2].Value.ToString(), strtNd = data[3].Value.ToString(), endNd = data[4].Value.ToString();
                                string lgt = data[5].Value.ToString(), wdt = data[6].Value.ToString(), hgt = data[7].Value.ToString();
                                string As = data[8].Value.ToString();

                                msgstr = "Stringer " + strNum + "\n\n" +
                                         "Nodes: (" + strtNd + " - " + endNd + ")" + "\n" +
                                         "Lenght = " + lgt + " mm" + "\n" +
                                         "Width = " + wdt + " mm" + "\n" +
                                         "Height = " + hgt + " mm" + "\n" +
                                         "Reinforcement = " + As + " mm2";

                            }

                            // If it's a panel
                            if (ent.Layer == "Panel")
                            {

                                // Get the parameters
                                string pnlNum = data[2].Value.ToString();
                                string[] pnlNds = { data[3].Value.ToString(),
                                                    data[4].Value.ToString(),
                                                    data[5].Value.ToString(),
                                                    data[6].Value.ToString() };
                                string pnlW = data[7].Value.ToString(), psx = data[8].Value.ToString(), psy = data[9].Value.ToString();

                                msgstr = "Panel " + pnlNum + "\n\n" +
                                         "Nodes: (" + pnlNds[0] + " - " + pnlNds[1] + " - " + pnlNds[2] + " - " + pnlNds[3] + ")" + "\n" +
                                         "Width = " + pnlW + " mm" + "\n" +
                                         "Reinforcement ratio (x) = " + psx + "\n" +
                                         "Reinforcement ratio (y) = " + psy;
                            }

                            // If it's a force text
                            if (ent.Layer == "ForceText")
                            {
                                // Get the parameters
                                string posX = data[2].Value.ToString(), posY = data[3].Value.ToString();

                                msgstr = "Force at position  (" + posX + ", " + posY + ")";
                            }

                        }
                        else
                        {
                            msgstr = "NONE";
                        }

                        // Display the values returned
                        Application.ShowAlertDialog(Global.appName + "\n\n" + msgstr);
                    }
                }
                else break;
            }
        }
    }
}