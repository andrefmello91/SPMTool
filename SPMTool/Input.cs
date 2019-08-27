﻿using System;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SPMTool.Geometry))]
[assembly: CommandClass(typeof(SPMTool.Material))]
[assembly: CommandClass(typeof(SPMTool.SupportsAndForces))]

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
            for ( ; ; )
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
            if (strStRes.Status == PromptStatus.Cancel) return;

            // Loop for creating infinite stringers (until user exits the command)
            for ( ; ; )
            {
                // Start a transaction
                using (Transaction trans = curDb.TransactionManager.StartTransaction())
                {
                    // Get the stringer start point
                    Point3d strSt = strStRes.Value;

                    // Prompt for the end point
                    PromptPointOptions strEndOp = new PromptPointOptions("\nPick the end node: ")
                    {
                        UseBasePoint = true,
                        BasePoint = strSt
                    };
                    PromptPointResult strEndRes = ed.GetPoint(strEndOp);
                    Point3d strEnd = strEndRes.Value;

                    if (strEndRes.Status == PromptStatus.OK)
                    {
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
                // Initialize the array of vertices of the panel
                Point3d[] pnlVerts = new Point3d[4];

                // Prompt for user enter four vertices of the panel
                for (int i = 0; i < 4; i++)
                {
                    // Prompt each vertice
                    PromptPointOptions pnlNdOp = new PromptPointOptions("\nSelect nodes performing a loop");
                    PromptPointResult pnlNdRes = ed.GetPoint(pnlNdOp);

                    // Add to the vertices array
                    pnlVerts[i] = pnlNdRes.Value;
                }

                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create the panel as a solid with 4 segments (4 points)
                using (Solid newPanel = new Solid(pnlVerts[0], pnlVerts[1], pnlVerts[3], pnlVerts[2]))
                {
                    // Set the layer to Panel
                    newPanel.Layer = pnlLayer;

                    // Add the panel to the drawing
                    blkTblRec.AppendEntity(newPanel);
                    trans.AddNewlyCreatedDBObject(newPanel, true);
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

            // Get the number of elements
            //ObjectIdCollection nds = AuxMethods.GetEntitiesOnLayer("Node");
            //int numNds = nds.Count;

            //ObjectIdCollection strs = AuxMethods.GetEntitiesOnLayer("Stringer");
            //int numStrs = strs.Count;

            //ObjectIdCollection pnls = AuxMethods.GetEntitiesOnLayer("Panel");
            //int numPnls = pnls.Count;

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
        public void ViewElementData()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string msgstr = "";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                PromptSelectionOptions selOps = new PromptSelectionOptions();
                PromptSelectionResult selRes = ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    SelectionSet set = selRes.Value;

                    // Step through the objects in the selection set
                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // If it's a node
                        if (ent.Layer == "Node")
                        {
                            // Get the extended data attached to each object for MY_APP
                            ResultBuffer rb = ent.GetXDataForApplication(appName);

                            // Make sure the Xdata is not empty
                            if (rb != null)
                            {
                                // Get the XData as an array
                                TypedValue[] data = rb.AsArray();

                                // Get the parameters
                                string ndNum = data[2].Value.ToString(), posX = data[3].Value.ToString(), posY = data[4].Value.ToString();
                                string sup = data[5].Value.ToString(), forX = data[6].Value.ToString(), forY = data[7].Value.ToString();

                                msgstr = "Node "                   + ndNum + "\n\n" +
                                         "Node position: ("        + posX  + ", "   + posY + ")" + "\n" +
                                         "Support conditions: "    + sup   + "\n"   +
                                         "Force in X direction = " + forX  + " N"   + "\n" +
                                         "Force in Y direction = " + forY  + " N";
                            }

                            else
                            {
                                msgstr = "NONE";
                            }
                        }

                        // If it's a stringer
                        if (ent.Layer == "Stringer")
                        {
                            // Get the extended data attached to each object for MY_APP
                            ResultBuffer rb = ent.GetXDataForApplication(appName);

                            // Make sure the Xdata is not empty
                            if (rb != null)
                            {
                                // Get the XData as an array
                                TypedValue[] data = rb.AsArray();

                                // Get the parameters
                                string strNum = data[2].Value.ToString(), strtNd = data[3].Value.ToString(), endNd = data[4].Value.ToString();
                                string lgt = data[5].Value.ToString(), wdt = data[6].Value.ToString(), hgt = data[7].Value.ToString();
                                string As = data[8].Value.ToString();

                                msgstr = "Stringer "        + strNum                        + "\n\n" +
                                         "Nodes: ("         + strtNd + " - "  + endNd + ")" + "\n"   +
                                         "Lenght = "        + lgt    + " mm"                + "\n"   +
                                         "Width = "         + wdt    + " mm"                + "\n"   +
                                         "Height = "        + hgt    + " mm"                + "\n"   +
                                         "Reinforcement = " + As     + " mm2";
                            }
                            else
                            {
                                msgstr = "NONE";
                            }
                        }

                        // If it's a panel
                        if (ent.Layer == "Panel")
                        {
                            // Get the extended data attached to each object for MY_APP
                            ResultBuffer rb = ent.GetXDataForApplication(appName);

                            // Make sure the Xdata is not empty
                            if (rb != null)
                            {
                                // Get the XData as an array
                                TypedValue[] data = rb.AsArray();

                                // Get the parameters
                                string pnlNum = data[2].Value.ToString();
                                string[] pnlNds = { data[3].Value.ToString(),
                                                    data[4].Value.ToString(),
                                                    data[5].Value.ToString(),
                                                    data[6].Value.ToString() };
                                string pnlW = data[7].Value.ToString(), psx = data[8].Value.ToString(), psy = data[9].Value.ToString();

                                msgstr = "Panel "                     + pnlNum    + "\n\n" +
                                         "Nodes: ("                   + pnlNds[0] + " - " + pnlNds[1] + " - " + pnlNds[2] + " - " + pnlNds[3] + ")" + "\n" +
                                         "Width = "                   + pnlW      + " mm" + "\n" +
                                         "Reinforcement ratio (x) = " + psx       + "\n" +
                                         "Reinforcement ratio (y) = " + psy;
                            }

                            else
                            {
                                msgstr = "NONE";
                            }
                        }

                        // Display the values returned
                        Application.ShowAlertDialog(appName + "\n\n" + msgstr);
                    }
                }
            }
        }
    }


    // Material related commands:
    public class Material
    {
        [CommandMethod("SetConcreteParameters")]
        public static void SetConcreteParameters()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Concrete data";

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            AuxMethods.RegisterApp();

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Ask the user to input the concrete compressive strength
                PromptDoubleOptions fcOp = new PromptDoubleOptions("\nInput the concrete compressive strength (fc) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult fcRes = ed.GetDouble(fcOp);
                double fc = fcRes.Value;

                // Ask the user to input the concrete Elastic Module
                PromptDoubleOptions EcOp = new PromptDoubleOptions("\nInput the concrete Elastic Module (Ec) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult EcRes = ed.GetDouble(EcOp);
                double Ec = EcRes.Value;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                // Save the variables on the Xrecord
                using (ResultBuffer rb = new ResultBuffer())
                {
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fc));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, Ec));

                    // Create and add data to an Xrecord
                    Xrecord xRec = new Xrecord();
                    xRec.Data = rb;

                    // Create the entry in the NOD and add to the transaction
                    nod.SetAt("ConcreteParams", xRec);
                    trans.AddNewlyCreatedDBObject(xRec, true);
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }
        }

        [CommandMethod("SetSteelParameters")]
        public static void SetSteelParameters()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Steel data";

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            AuxMethods.RegisterApp();

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Ask the user to input the steel tensile strength
                PromptDoubleOptions fyOp = new PromptDoubleOptions("\nInput the steel tensile strength (fy) in MPa:")
                {                
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult fyRes = ed.GetDouble(fyOp);
                double fy = fyRes.Value;

                // Ask the user to input the steel Elastic Module
                PromptDoubleOptions EsOp = new PromptDoubleOptions("\nInput the steel Elastic Module (Es) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult EsRes = ed.GetDouble(EsOp);
                double Es = EsRes.Value;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                // Save the variables on the Xrecord
                using (ResultBuffer rb = new ResultBuffer())
                {
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fy));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, Es));

                    // Create and add data to an Xrecord
                    Xrecord xRec = new Xrecord();
                    xRec.Data = rb;

                    // Create the entry in the NOD and add to the transaction
                    nod.SetAt("SteelParams", xRec);
                    trans.AddNewlyCreatedDBObject(xRec, true);
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }
        }

        [CommandMethod("ViewMaterialParameters")]
        public void ViewMaterialParameters()
        {
            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the XData
            string appName = "SPMTool";
            string xData = "Material Parameters";
            string concmsg;
            string steelmsg;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

                // Read the materials Xrecords
                ObjectId concPar = nod.GetAt("ConcreteParams");
                ObjectId steelPar = nod.GetAt("SteelParams");

                if (concPar != null && steelPar != null)
                {
                    // Read the Concrete Xrecord
                    Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
                    ResultBuffer rb = concXrec.Data;
                    TypedValue[] data = rb.AsArray();

                    // Get the parameters
                    concmsg = "\nConcrete Parameters" +
                              "\nfc = "               + data[2].Value.ToString() + " MPa" +
                              "\nEc = "               + data[3].Value.ToString() + " MPa";

                    // Read the Steel Xrecord
                    Xrecord steelXrec = (Xrecord)trans.GetObject(steelPar, OpenMode.ForRead);
                    ResultBuffer rb2 = steelXrec.Data;
                    TypedValue[] data2 = rb.AsArray();

                    // Get the parameters
                    steelmsg = "\nSteel Parameters" +
                               "\nfy = "            + data2[2].Value.ToString() + " MPa" +
                               "\nEs = "            + data2[3].Value.ToString() + " MPa";
                }
                else
                {
                    concmsg = "\nMaterial Parameters NOT SET";
                    steelmsg = "";
                }

                // Display the values returned
                Application.ShowAlertDialog(appName + "\n\n" + xData + "\n" + concmsg + "\n" + steelmsg);

                // Dispose the transaction
                trans.Dispose();
            }
        }
    }

    // Support and forces related commands
    public class SupportsAndForces
    {
        [CommandMethod("AddSupport")]
        public void AddSupport()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Define the layer parameters
            string supLayer = "Support";
            short red = 1;

            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(supLayer, red, 0);

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the support blocks already exist. If not, create the blocks
            AuxMethods.CreateSupportBlocks();

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the object Ids of the support blocks
                ObjectId xBlock = blkTbl["SupportX"];
                ObjectId yBlock = blkTbl["SupportY"];
                ObjectId xyBlock = blkTbl["SupportXY"];

                // Request objects to be selected in the drawing area
                ed.WriteMessage("\nSelect nodes to add support conditions:");
                selRes = ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    // Get the objects selected
                    set = selRes.Value;

                    // Ask the user set the support conditions:
                    PromptKeywordOptions supOp = new PromptKeywordOptions("\nAdd support in which direction?");
                    supOp.Keywords.Add("Free");
                    supOp.Keywords.Add("X");
                    supOp.Keywords.Add("Y");
                    supOp.Keywords.Add("XY");
                    supOp.Keywords.Default = "Free";
                    supOp.AllowNone = true;

                    // Get the result
                    PromptResult supRes = ed.GetKeywords(supOp);

                    // Set the support
                    string support = supRes.StringResult;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Node"))
                        {
                            // Upgrade the OpenMode
                            ent.UpgradeOpen();

                            // Read as a point and get the position
                            DBPoint nd = ent as DBPoint;
                            Point3d ndPos = nd.Position;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new support conditions (line 5 of the array)
                            data[5] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, support);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;

                            // Add the block to selected node at
                            Point3d insPt = ndPos;

                            // Choose the block to insert
                            ObjectId supBlock = ObjectId.Null;
                            if (support == "X" && xBlock != ObjectId.Null)   supBlock = xBlock;
                            if (support == "Y" && yBlock != ObjectId.Null)   supBlock = yBlock;
                            if (support == "XY" && xyBlock != ObjectId.Null) supBlock = xyBlock;

                            // Insert the block into the current space
                            using (BlockReference blkRef = new BlockReference(insPt, supBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = supLayer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);
                            }
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }
        }
        

        [CommandMethod("AddForce")]
        public void AddForce()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Get the coordinate system for transformations
            Matrix3d curUCSMatrix = curDoc.Editor.CurrentUserCoordinateSystem;
            CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Define the layer parameters
            string fLayer = "Force";
            short yellow = 2;

            // Initialize variables
            PromptSelectionResult selRes;
            SelectionSet set;

            // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(fLayer, yellow, 0);

            // Check if the force block already exist. If not, create the blocks
            AuxMethods.CreateForceBlock();

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the force block
                ObjectId ForceBlock = blkTbl["ForceBlock"];

                // Request objects to be selected in the drawing area
                ed.WriteMessage("\nSelect a node to add load:");
                selRes = ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    // Get the objects selected
                    set = selRes.Value;

                    // Ask the user set the load value in x direction:
                    PromptDoubleOptions xForceOp = new PromptDoubleOptions("\nEnter force (in N) in X direction(positive following axis direction)?")
                    {
                        DefaultValue = 0
                    };

                    // Get the result
                    PromptDoubleResult xForceRes = ed.GetDouble(xForceOp);
                    Double xForce = xForceRes.Value;

                    // Ask the user set the load value in y direction:
                    PromptDoubleOptions yForceOp = new PromptDoubleOptions("\nEnter force (in N) in Y direction(positive following axis direction)?")
                    {
                        DefaultValue = 0
                    };

                    // Get the result
                    PromptDoubleResult yForceRes = ed.GetDouble(yForceOp);
                    Double yForce = yForceRes.Value;

                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a node
                        if (ent.Layer.Equals("Node"))
                        {
                            // Upgrade the OpenMode
                            ent.UpgradeOpen();

                            // Read as a point and get the position
                            DBPoint nd = ent as DBPoint;
                            Point3d ndPos = nd.Position;

                            // Get the node coordinates
                            double xPos = ndPos.X;
                            double yPos = ndPos.Y;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new forces (line 6 and 7 of the array)
                            data[6] = new TypedValue((int)DxfCode.ExtendedDataReal, xForce);
                            data[7] = new TypedValue((int)DxfCode.ExtendedDataReal, yForce);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;

                            // Add the block to selected node at
                            Point3d insPt = ndPos;

                            // Insert the block into the current space
                            // For forces in x
                            if (xForce != 0)
                            {
                                using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                                {
                                    // Append the block to drawing
                                    BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    blkTblRec.AppendEntity(blkRef);
                                    blkRef.Layer = fLayer;
                                    trans.AddNewlyCreatedDBObject(blkRef, true);

                                    // Get the force absolute value
                                    double xForceAbs = Math.Abs(xForce);

                                    // Initialize the rotation angle and the text position
                                    double rotAng = 0;
                                    Point3d txtPos = new Point3d();

                                    if (xForce > 0) // positive force in x
                                    {
                                        // Rotate 90 degress counterclockwise
                                        rotAng = 1.570796;

                                        // Set the text position
                                        txtPos = new Point3d(xPos - 400, yPos + 25, 0);
                                    }

                                    if (xForce < 0) // negative force in x
                                    {
                                        // Rotate 90 degress clockwise
                                        rotAng = -1.570796;

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 150, yPos + 25, 0);
                                    }

                                    // Rotate the block
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, curUCS.Zaxis, insPt));

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = xForceAbs.ToString() + " N",
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = fLayer
                                    };

                                    // Append the text to drawing
                                    blkTblRec.AppendEntity(text);
                                    trans.AddNewlyCreatedDBObject(text, true);
                                }
                            }

                            // For forces in y
                            if (yForce != 0)
                            {
                                using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                                {
                                    // Append the block to drawing
                                    BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                    blkTblRec.AppendEntity(blkRef);
                                    blkRef.Layer = fLayer;
                                    trans.AddNewlyCreatedDBObject(blkRef, true);

                                    // Get the force absolute value
                                    double yForceAbs = Math.Abs(yForce);

                                    // Initialize the rotation angle and the text position
                                    double rotAng = 0;
                                    Point3d txtPos = new Point3d();

                                    if (yForce > 0) // positive force in y
                                    {
                                        // Rotate 90 degress counterclockwise
                                        rotAng = 3.14159265;

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 25, yPos - 250, 0);
                                    }

                                    if (yForce < 0) // negative force in y
                                    {
                                        // No rotation needed

                                        // Set the text position
                                        txtPos = new Point3d(xPos + 25, yPos + 200, 0);
                                    }

                                    // Rotate the block
                                    blkRef.TransformBy(Matrix3d.Rotation(rotAng, curUCS.Zaxis, insPt));

                                    // Define the force text
                                    DBText text = new DBText()
                                    {
                                        TextString = yForceAbs.ToString() + " N",
                                        Position = txtPos,
                                        Height = 50,
                                        Layer = fLayer
                                    };

                                    // Append the text to drawing
                                    blkTblRec.AppendEntity(text);
                                    trans.AddNewlyCreatedDBObject(text, true);
                                }
                            }
                        }
                        // If x or y forces are 0, the block is not added
                    }
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }
        }
    }

    public class AuxMethods
    {
        // Add the app to the Registered Applications Record
        public static void RegisterApp()
        {
            // Define the appName
            string appName = "SPMTool";

            // Get the current document, database and editor
            Database curDb = Application.DocumentManager.MdiActiveDocument.Database;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Registered Applications table for read
                RegAppTable regAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                if (!regAppTbl.Has(appName))
                {
                    using (RegAppTableRecord regAppTblRec = new RegAppTableRecord())
                    {
                        regAppTblRec.Name = appName;
                        trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                        regAppTbl.Add(regAppTblRec);
                        trans.AddNewlyCreatedDBObject(regAppTblRec, true);
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }
        }

        // Method to create a layer given a name, a color and transparency
        public static void CreateLayer(string layerName, short layerColor, int layerTransp)
        {
            // Get the current document and database
            Database curDb = Application.DocumentManager.MdiActiveDocument.Database;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lyrTbl.Has(layerName))
                {
                    lyrTbl.UpgradeOpen();
                    using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color and a name
                        lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, layerColor);

                        // Assign a layer transparency
                        byte alpha = (byte)(255 * (100 - layerTransp) / 100);
                        Transparency transp = new Transparency(alpha);

                        // Upgrade the Layer table for write
                        trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                        // Append the new layer to the Layer table and the transaction
                        lyrTbl.Add(lyrTblRec);
                        trans.AddNewlyCreatedDBObject(lyrTblRec, true);

                        // Assign the name and transparency to the layer
                        lyrTblRec.Name = layerName;
                        lyrTblRec.Transparency = transp;
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }
        }

        // Method to create the support blocks
        public static void CreateSupportBlocks()
        {
            // Get the current document and database
            Database curDb = Application.DocumentManager.MdiActiveDocument.Database;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId xBlock = ObjectId.Null;
                ObjectId yBlock = ObjectId.Null;
                ObjectId xyBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has("SupportX"))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = "SupportX";

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        xBlock = blkTblRec.Id;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a object collection and add the lines
                        using (DBObjectCollection lines = new DBObjectCollection())
                        {
                            // Define the points to add the lines
                            Point3d[] blkPts =
                            {
                                origin,
                                new Point3d(-200, 115, 0),
                                origin,
                                new Point3d(-200, -115, 0),
                                new Point3d(-200, 150, 0),
                                new Point3d(-200, -150, 0),
                                new Point3d(-250, 150, 0),
                                new Point3d(-250, -150, 0)
                            };

                            // Define the lines and add to the collection
                            for (int i = 0; i < 4; i++)
                            {
                                Line line = new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                };
                                lines.Add(line);
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }

                    // Create the Y block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = "SupportY";

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        yBlock = blkTblRec.Id;

                        // Create a object collection and add the lines
                        using (DBObjectCollection lines = new DBObjectCollection())
                        {
                            // Define the points to add the lines
                            Point3d[] blkPts =
                            {
                                origin,
                                new Point3d(-115, -200, 0),
                                origin,
                                new Point3d(115, -200, 0),
                                new Point3d(-150, -200, 0),
                                new Point3d(150, -200, 0),
                                new Point3d(-150, -250, 0),
                                new Point3d(+150, -250, 0)
                            };

                            // Define the lines and add to the collection
                            for (int i = 0; i < 4; i++)
                            {
                                Line line = new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                };
                                lines.Add(line);
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }

                    // Create the XY block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = "SupportXY";

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        xyBlock = blkTblRec.Id;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a object collection and add the lines
                        using (DBObjectCollection lines = new DBObjectCollection())
                        {
                            // Define the points to add the lines
                            Point3d[] blkPts =
                            {
                                origin,
                                new Point3d(-115, -200, 0),
                                origin,
                                new Point3d(115, -200, 0),
                                new Point3d(-150, -200, 0),
                                new Point3d(150, -200, 0)
                            };

                            // Define the lines and add to the collection
                            for (int i = 0; i < 3; i++)
                            {
                                Line line = new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                };
                                lines.Add(line);
                            }

                            // Create the diagonal lines
                            for (int i = 0; i < 6; i++)
                            {
                                int xInc = 46 * i; // distance between the lines

                                Line diagLine = new Line()
                                {
                                    StartPoint = new Point3d(-115 + xInc, -200, 0),
                                    EndPoint = new Point3d(-140 + xInc, -245, 0)
                                };

                                // Add to the collection
                                lines.Add(diagLine);
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }
        }

        // Method to create the force block
        public static void CreateForceBlock()
        {
            // Get the current document and database
            Database curDb = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Ids
                ObjectId ForceBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has("ForceBlock"))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = "ForceBlock";

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        ForceBlock = blkTblRec.Id;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a collection
                        using (DBObjectCollection arrow = new DBObjectCollection())
                        {
                            // Create the arrow line and solid)
                            Line line = new Line()
                            {
                                StartPoint = new Point3d(0, 75, 0),
                                EndPoint = new Point3d(0, 250, 0)
                            };
                            // Add to the collection
                            arrow.Add(line);

                            // Create the solid and add to the collection
                            Solid solid = new Solid(origin, new Point3d(-50, 75, 0), new Point3d(50, 75, 0));
                            arrow.Add(solid);

                            // Add the lines to the block table record
                            foreach (Entity ent in arrow)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }
        }

        // This method select all objects on a determined layer
        public static ObjectIdCollection GetEntitiesOnLayer(string layerName)
        {
            // Get the current document and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = curDoc.Editor;

            // Build a filter list so that only entities on the specified layer are selected
            TypedValue[] tvs = new TypedValue[1]
            {
                new TypedValue((int)DxfCode.LayerName, layerName)
            };

            SelectionFilter selFt = new SelectionFilter(tvs);

            // Get the entities on the layername
            PromptSelectionResult selRes = ed.SelectAll(selFt);

            if (selRes.Status == PromptStatus.OK)

                return

                  new ObjectIdCollection(selRes.Value.GetObjectIds());

            else

                return new ObjectIdCollection();
        }

        // This method order the elements in a collection in ascending yCoord, then ascending xCoord, returns the array of points ordered
        public static double[][] OrderElements(int numElements, Point3dCollection points)
        {
            // Initialize the point array with numNodes lines and 3 columns (nodeNumber, xCoord, yCoord)
            double[][] elmntsArray = new double[numElements][];

            // Access the nodes on the document
            int i = 0; // array position
            foreach (Point3d pt in points)
            {
                // Get the coordinates
                double xCoord = pt.X;
                double yCoord = pt.Y;

                // Add to the array with the number initially unassigned
                elmntsArray[i] = new double[] { 0, xCoord, yCoord };

                // Increment the array position
                i++;
            }

            // Order the array
            var elmntsArrayOrd = elmntsArray.OrderBy(y => y[2]).ThenBy(x => x[1]);
            elmntsArray = elmntsArrayOrd.ToArray();

            // Set the node numbers in the array
            for (int elmntNum = 1; elmntNum <= numElements; elmntNum++)
            {
                // array position
                i = elmntNum - 1;
                elmntsArray[i][0] = elmntNum;
            }

            // Return the array ordered
            return elmntsArray;
        }

        public static double SetElementNumber(Point3d point, int numElements, double[][] elmntsArray)
        {
            // Initialize the element number
            double elmntNum = 0;
            // Assign the node number from the array
            for (int i = 0; i < numElements; i++)
            {
                // Check what line of the array corresponds the point position
                if (point.X == elmntsArray[i][1] && point.Y == elmntsArray[i][2])
                {
                    // Get the element number from the array
                    elmntNum = elmntsArray[i][0];
                }
            }

            // Return the element number
            return elmntNum;
        }

        // Enumerate all the nodes in the model and return the number of nodes
        public static int UpdateNodes()
        {
            // Get the current database
            Database curDb = Application.DocumentManager.MdiActiveDocument.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Node Data";

            // Initialize the number of nodes
            int numNds = 0;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Create the nodes collection and initialize getting the elements on node layer
                ObjectIdCollection nds = GetEntitiesOnLayer("Node");

                // Create a point collection
                Point3dCollection pts = new Point3dCollection();

                // Get the number of nodes
                numNds = nds.Count;

                // Add each point to the collection
                foreach (ObjectId obj in nds)
                {
                    // Read the object as a point
                    DBPoint nd = trans.GetObject(obj, OpenMode.ForRead) as DBPoint;
                    pts.Add(nd.Position);
                }

                // Get the array of points ordered
                double[][] ptsArray = OrderElements(numNds, pts);

                // Access the nodes on the document
                foreach (ObjectId obj in nds)
                {
                    // Read the object as a point
                    DBPoint nd = trans.GetObject(obj, OpenMode.ForWrite) as DBPoint;

                    // Get the node number
                    double ndNum = SetElementNumber(nd.Position, numNds, ptsArray);

                    // If the Extended data does not exist, create it
                    if (nd.XData == null)
                    {
                        // Inicialization of node conditions
                        double nodeNum = 0;                                // Node number (to be set later)
                        double xPosition = nd.Position.X;                  // X position
                        double yPosition = nd.Position.Y;                  // Y position
                        string support = "Free";                           // Support condition
                        double xForce = 0;                                 // Force on X direction
                        double yForce = 0;                                 // Force on Y direction

                        // Define the Xdata to add to the node
                        using (ResultBuffer defRb = new ResultBuffer())
                        {
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));   // 0
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr)); // 1
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, nodeNum));         // 2
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xPosition));       // 3
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yPosition));       // 4
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, support));  // 5
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xForce));          // 6
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yForce));          // 7

                            // Open the node for write
                            Entity ent = trans.GetObject(nd.ObjectId, OpenMode.ForWrite) as Entity;

                            // Append the extended data to each object
                            ent.XData = defRb;
                        }
                    }

                    // Get the result buffer as an array
                    ResultBuffer rb = nd.GetXDataForApplication(appName);
                    TypedValue[] data = rb.AsArray();

                    // Set the new node number (line 2)
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNum);

                    // Set the updated coordinates (in case of a node copy)
                    data[3] = new TypedValue((int)DxfCode.ExtendedDataReal, nd.Position.X);
                    data[4] = new TypedValue((int)DxfCode.ExtendedDataReal, nd.Position.Y);

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    nd.XData = newRb;
                }

                // Set the style for all point objects in the drawing
                curDb.Pdmode = 32;
                curDb.Pdsize = 50;

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }

            // Return the number of nodes
            return numNds;
        }

        // Update the node numbers on the XData of each stringer in the model and return the number of stringers
        public static int UpdateStringers()
        {
            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Stringer Data";

            // Initialize the number of stringers
            int numStrs = 0;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Create the nodes collection and initialize getting the elements on node layer
                ObjectIdCollection nds = GetEntitiesOnLayer("Node");

                // Create the stringer collection and initialize getting the elements on node layer
                ObjectIdCollection strs = GetEntitiesOnLayer("Stringer");

                // Create a point collection
                Point3dCollection midPts = new Point3dCollection();

                // Get the number of stringers
                numStrs = strs.Count;

                // Add the midpoint of each stringer to the collection
                foreach (ObjectId strObj in strs)
                {
                    // Read the object as a line
                    Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;

                    // Get the coordinates of the midpoint of the stringer
                    double midPtX = (str.StartPoint.X + str.EndPoint.X) / 2;
                    double midPtY = (str.StartPoint.Y + str.EndPoint.Y) / 2;

                    // Set the midpoint and add to the collection
                    Point3d midPt = new Point3d(midPtX, midPtY, 0);
                    midPts.Add(midPt);
                }

                // Get the array of midpoints ordered
                double[][] midPtsArray = OrderElements(numStrs, midPts);

                // Access the nodes on the document
                foreach (ObjectId strObj in strs)
                {
                    // Initialize the variables
                    double strStNd = 0;
                    double strEnNd = 0;
                    double strNum = 0;

                    // Open the selected object as a line for write
                    Line str = trans.GetObject(strObj, OpenMode.ForWrite) as Line;
                    
                    // Get the points of the line
                    Point3d strStPos = str.StartPoint;
                    Point3d strEnPos = str.EndPoint;

                    // Get the coordinates of the midpoint of the stringer
                    double midPtX = (str.StartPoint.X + str.EndPoint.X) / 2;
                    double midPtY = (str.StartPoint.Y + str.EndPoint.Y) / 2;
                    Point3d midPt = new Point3d(midPtX, midPtY, 0);

                    // Get the stringer number
                    strNum = SetElementNumber(midPt, numStrs, midPtsArray);

                    // Compare to the nodes collection
                    foreach (ObjectId ndObj in nds)
                    {
                        // Open the selected object as a point for read
                        DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                        // Read the entity as a point and get the position
                        Point3d ndPos = nd.Position;

                        // Get the start and end nodes
                        // Compare the start node
                        if (strStPos == ndPos)
                        {
                            // Get the node number
                            // Access the XData as an array
                            ResultBuffer ndRb = nd.GetXDataForApplication(appName);
                            TypedValue[] dataNd = ndRb.AsArray();

                            // Get the node number (line 2)
                            strStNd = Convert.ToDouble(dataNd[2].Value);
                        }

                        // Compare the end node
                        if (strEnPos == ndPos)
                        {
                            // Get the node number
                            // Access the XData as an array
                            ResultBuffer ndRb = nd.GetXDataForApplication(appName);
                            TypedValue[] dataNd = ndRb.AsArray();

                            // Get the node number (line 2)
                            strEnNd = Convert.ToDouble(dataNd[2].Value);
                        }
                    }

                    // If XData does not exist, create it
                    if (str.XData == null)
                    {
                        // Inicialization of stringer conditions
                        double strNumb = 0;                // Stringer number (initially unassigned)
                        double strSrtNd = 0;               // Stringer start node (initially unassigned)
                        double strEndNd = 0;               // Stringer end node (initially unassigned)
                        double strLgt = str.Length;        // Stringer lenght
                        double strW = 1;                   // Width
                        double strH = 1;                   // Height
                        double As = 0;                     // Reinforcement Area

                        // Define the Xdata to add to the node
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));   // 0
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr)); // 1
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strNumb));         // 2
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strSrtNd));        // 3
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strEndNd));        // 4
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strLgt));          // 5
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strW));            // 6
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strH));            // 7
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, As));              // 8

                            // Open the stringer for write
                            Entity ent = trans.GetObject(str.ObjectId, OpenMode.ForWrite) as Entity;

                            // Append the extended data to each object
                            ent.XData = rb;
                        }
                    }

                    // Access the XData as an array
                    ResultBuffer strRb = str.GetXDataForApplication(appName);
                    TypedValue[] data = strRb.AsArray();

                    // Set the updated number and nodes in ascending number (line 2 and 3 of the array) and length
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, strNum);
                    data[3] = new TypedValue((int)DxfCode.ExtendedDataReal, Math.Min(strStNd, strEnNd));
                    data[4] = new TypedValue((int)DxfCode.ExtendedDataReal, Math.Max(strStNd, strEnNd));
                    data[5] = new TypedValue((int)DxfCode.ExtendedDataReal, str.Length);

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    str.XData = newRb;
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }

            // Return the number of stringers
            return numStrs;
        }

        // Update the node numbers on the XData of each panel in the model and return the number of panels
        public static int UpdatePanels()
        {
            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Panel Data";

            // Initialize the number of panels
            int numPnls = 0;

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Create the nodes collection and initialize getting the elements on node layer
                ObjectIdCollection nds = GetEntitiesOnLayer("Node");

                // Create the panels collection and initialize getting the elements on node layer
                ObjectIdCollection pnls = GetEntitiesOnLayer("Panel");

                // Create a point collection
                Point3dCollection cntrPts = new Point3dCollection();

                // Get the number of panels
                numPnls = pnls.Count;

                // Add the centerpoint of each panel to the collection
                foreach (ObjectId pnlObj in pnls)
                {
                    // Read the object as a solid
                    Solid pnl = trans.GetObject(pnlObj, OpenMode.ForRead) as Solid;
                    
                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the summation of the coordinates of the vertices
                    double xCrdSum = 0, yCrdSum = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        xCrdSum = xCrdSum + pnlVerts[i].X;
                        yCrdSum = yCrdSum + pnlVerts[i].Y;
                    }

                    // Get the approximate coordinates of the center point of the panel
                    double cntrPtX = xCrdSum / 4;
                    double cntrPtY = yCrdSum / 4;

                    // Set the center and add to the collection
                    Point3d cntrPt = new Point3d(cntrPtX, cntrPtY, 0);
                    cntrPts.Add(cntrPt);
                }

                // Get the array of center points ordered
                double[][] cntrPtsArray = OrderElements(numPnls, cntrPts);

                // Access the nodes on the document
                foreach (ObjectId pnlObj in pnls)
                {
                    // Initialize the array of node numbers and the position on the array
                    int[] ndNums = { 0, 0, 0, 0 };
                    int i = 0;
                    
                    // Open the selected object as a solid for write
                    Solid pnl = trans.GetObject(pnlObj, OpenMode.ForWrite) as Solid;

                    // Read it as a block and get the draw order table
                    BlockTableRecord blck = trans.GetObject(pnl.BlockId, OpenMode.ForRead) as BlockTableRecord;
                    DrawOrderTable drawOrder = trans.GetObject(blck.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the summation of the coordinates of the vertices
                    double xCrdSum = 0, yCrdSum = 0;
                    for (int j = 0; j < 4; j++)
                    {
                        xCrdSum = xCrdSum + pnlVerts[j].X;
                        yCrdSum = yCrdSum + pnlVerts[j].Y;
                    }

                    // Get the approximate coordinates of the center point of the panel
                    double cntrPtX = xCrdSum / 4;
                    double cntrPtY = yCrdSum / 4;
                    Point3d cntrPt = new Point3d(cntrPtX, cntrPtY, 0);

                    // Get the panel number
                    double pnlNum = SetElementNumber(cntrPt, numPnls, cntrPtsArray);

                    // Compare the node position to the panel vertices
                    foreach (Point3d vert in pnlVerts)
                    {
                        // Get the nodes in the collection
                        foreach (ObjectId ndObj in nds)
                        {
                            // Open the selected object as a point for read
                            DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                            // Compare the position
                            if (vert == nd.Position)
                            {
                                // Access the XData as an array
                                ResultBuffer ndRb = nd.GetXDataForApplication(appName);
                                TypedValue[] dataNd = ndRb.AsArray();

                                // Get the node number (line 2) and assign it to the node array
                                ndNums[i] = Convert.ToInt32(dataNd[2].Value);
                                i++;
                            }
                        }
                    }


                    // Order the nodes array in ascending
                    var ndOrd = ndNums.OrderBy(x => x);
                    ndNums = ndOrd.ToArray();

                    // Check if the XData already exist. If not, create it
                    if (pnl.XData == null)
                    {
                        // Initialize the panel parameters
                        double pnlN = 0;                              // Panel number (initially unassigned)
                        double[] verts = { 0, 0, 0, 0 };              // Panel vertices (initially unassigned)
                        double pnlW = 1;                              // width
                        double psx = 0;                               // reinforcement ratio (X)
                        double psy = 0;                               // reinforcement ratio (Y)

                        // Initialize a Result Buffer to add to the panel
                        ResultBuffer rb = new ResultBuffer();
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));   // 0
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr)); // 1
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlN));            // 2
                        for (int j = 0; j < 4; j++)
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, verts[j]));    // 3, 4, 5, 6
                        }
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlW));            // 7
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psx));             // 8
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psy));             // 9

                        // Append the extended data to the object
                        pnl.XData = rb;
                    }

                    // Access the XData as an array
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(appName);
                    TypedValue[] data = pnlRb.AsArray();

                    // Set the updated panel number (line 2)
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum);

                    // Set the updated node numbers (line 3 to 6 of the array)
                    for (int k = 3; k <= 6; k++)
                    {
                        data[k] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNums[k-3]);
                    }

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    pnl.XData = newRb;

                    // Move the panels to bottom
                    drawOrder.MoveToBottom(pnls);
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }

            // Return the number of panels
            return numPnls;
        }
    }
}
