﻿using System;
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
        public void AddNode()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Define the layer parameters
            string ndLayer = "Node";
            short red = 1;

            // Set the style for all point objects in the drawing
            curDb.Pdmode = 32;
            curDb.Pdsize = 50;

            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(ndLayer, red, 0);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            AuxMethods.RegisterApp();

            // Loop for creating infinite nodes (until user exits the command)
            for (; ; )
            {
                // Start a transaction
                using (Transaction trans = curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create the node in Model space
                    // Tell user to insert the point:
                    PromptPointOptions ptOp = new PromptPointOptions("\nPick point or enter coordinates: ");
                    PromptPointResult ptRes = ed.GetPoint(ptOp);

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
                    trans.Dispose();
                }
            }

            // Enumerate the nodes
            AuxMethods.UpdateNodes();
        }

        [CommandMethod("AddStringer")]
        public static void AddStringer()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

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
            PromptPointResult strStRes = ed.GetPoint(strStOp);

            // Exit if the user presses ESC or cancels the command
            if (strStRes.Status == PromptStatus.OK)
            {
                // Loop for creating infinite stringers (until user exits the command)
                for (; ; )
                {
                    // Start a transaction
                    using (Transaction trans = curDb.TransactionManager.StartTransaction())
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
                        PromptPointResult strEndRes = ed.GetPoint(strEndOp);
                        nds.Add(strEndRes.Value);

                        if (strEndRes.Status == PromptStatus.OK)
                        {
                            // Get the points ordered in ascending Y and ascending X:
                            double[][] extNds = AuxMethods.OrderElements(2, nds);
                            Point3d strSt = new Point3d(extNds[0][1], extNds[0][2], 0);
                            Point3d strEnd = new Point3d(extNds[1][1], extNds[1][2], 0);

                            // Open the Block table for read
                            BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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
                        trans.Dispose();
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
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

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
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
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
                    PromptPointResult pnlNdRes = ed.GetPoint(pnlNdOp);
                    nds.Add(pnlNdRes.Value);
                }

                // Order the vertices in ascending Y and ascending X
                double[][] vrts = AuxMethods.OrderElements(4, nds);

                // Initialize the array of vertices of the panel
                Point3d[] pnlVrts = new Point3d[4];

                // Add the vertices ordered
                for (int i = 0; i < 4; i++)
                {
                    pnlVrts[i] = new Point3d(vrts[i][1], vrts[i][2], 0);
                }

                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create the panel as a solid with 4 segments (4 points)
                using (Solid newPnl = new Solid(pnlVrts[0], pnlVrts[1], pnlVrts[2], pnlVrts[3]))
                {
                    // Set the layer to Panel
                    newPnl.Layer = pnlLayer;

                    // Add the panel to the drawing
                    blkTblRec.AppendEntity(newPnl);
                    trans.AddNewlyCreatedDBObject(newPnl, true);
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }

            // Update the panels
            AuxMethods.UpdatePanels();

            // Set the Object Snap Setting to the initial
            Application.SetSystemVariable("OSMODE", curOsmode);
        }

        [CommandMethod("DivideStringer")]
        public static void DivideStringer()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Prompt for select stringers
                ed.WriteMessage("\nSelect stringers to divide:");
                PromptSelectionResult selRes = ed.GetSelection();

                if (selRes.Status == PromptStatus.OK)
                {
                    // Prompt for the number of segments
                    PromptIntegerOptions strNumOp = new PromptIntegerOptions("\nEnter the number of stringers:")
                    {
                        AllowNegative = false,
                        AllowZero = false
                    };

                    // Get the number
                    PromptIntegerResult strNumRes = ed.GetInteger(strNumOp);
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
                            ResultBuffer rb = ent.GetXDataForApplication(appName);

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

                                // Open the stringer for write
                                //Entity newStrEnt = trans.GetObject(newStr.ObjectId, OpenMode.ForWrite) as Entity;

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
                trans.Dispose();
            }

            // Update nodes and stringers
            AuxMethods.UpdateNodes();
            AuxMethods.UpdateStringers();
        }

        [CommandMethod("UpdateElements")]
        public void UpdateElements()
        {
            // Get the editor
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Enumerate and get the number of nodes
            int numNds = AuxMethods.UpdateNodes();

            // Update and get the number of stringers
            int numStrs = AuxMethods.UpdateStringers();

            // Update and get the number of panels
            int numPnls = AuxMethods.UpdatePanels();

            // Display the number of updated elements
            ed.WriteMessage(numNds.ToString() + " nodes, " + numStrs.ToString() + " stringers and " + numPnls.ToString() + " panels updated.");
        }

        [CommandMethod("SetStringerParameters")]
        public void SetStringerParameters()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                ed.WriteMessage("\nSelect the stringers to assign properties (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
                PromptSelectionResult selRes = ed.GetSelection();

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
                    PromptDoubleResult strWRes = ed.GetDouble(strWOp);
                    double strW = strWRes.Value;

                    // Ask the user to input the stringer height
                    PromptDoubleOptions strHOp = new PromptDoubleOptions("\nInput the height (in mm) for the selected stringers:")
                    {
                        DefaultValue = 1,
                        AllowZero = false,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult strHRes = ed.GetDouble(strHOp);
                    double strH = strHRes.Value;

                    // Ask the user to input the reinforcement area
                    PromptDoubleOptions AsOp = new PromptDoubleOptions("\nInput the reinforcement area for the selected stringers (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult AsRes = ed.GetDouble(AsOp);
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
                            ResultBuffer rb = ent.GetXDataForApplication(appName);
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
                trans.Dispose();
            }
        }

        [CommandMethod("SetPanelParameters")]
        public void SetPanelParameters()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                ed.WriteMessage("\nSelect the panels to assign properties (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
                PromptSelectionResult selRes = ed.GetSelection();

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
                    PromptDoubleResult pnlWRes = ed.GetDouble(pnlWOp);
                    double pnlW = pnlWRes.Value;

                    // Ask the user to input the reinforcement ratio in x direction
                    PromptDoubleOptions psxOp = new PromptDoubleOptions("\nInput the reinforcement ratio in x direction for selected panels (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult psxRes = ed.GetDouble(psxOp);
                    double psx = psxRes.Value;

                    // Ask the user to input the reinforcement ratio in y direction
                    PromptDoubleOptions psyOp = new PromptDoubleOptions("\nInput the reinforcement ratio in y direction for selected panels (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult psyRes = ed.GetDouble(psyOp);
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
                            ResultBuffer rb = ent.GetXDataForApplication(appName);
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
                trans.Dispose();
            }
        }

        [CommandMethod("ViewElementData")]
        [Obsolete]
        public void ViewElementData()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string msgstr = "";

            // Start a loop for viewing continuous elements
            for ( ; ; )
            { 
                // Request the object to be selected in the drawing area
                PromptEntityOptions entOp = new PromptEntityOptions("\nSelect an element to view data:");
                PromptEntityResult entRes = ed.GetEntity(entOp);

                // If the prompt status is OK, objects were selected
                if (entRes.Status == PromptStatus.OK)
                {
                    // Start a transaction
                    using (Transaction trans = curDb.TransactionManager.StartTransaction())
                    {

                        // Get the entity for read
                        Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

                        // Get the extended data attached to each object for SPMTool
                        ResultBuffer rb = ent.GetXDataForApplication(appName);

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
                        }
                        else
                        {
                            msgstr = "NONE";
                        }

                        // Display the values returned
                        Application.ShowAlertDialog(appName + "\n\n" + msgstr);

                        // Dispose the transaction
                        trans.Dispose();
                    }
                }
                else break;
            }
        }
    }
}