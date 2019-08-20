using System;
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

            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Node data";

            // Loop for creating infinite nodes (until user exits the command)
            for (; ; )
            {
                // Start a transaction
                using (Transaction trans = curDb.TransactionManager.StartTransaction())
                {
                    // Open the Layer table for read
                    LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                    string nodeLayer = "Node";

                    // Check if the layer Node already exists in the drawing. If it doesn't, then it's created:

                    if (lyrTbl.Has(nodeLayer) == false)
                    {
                        using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                        {
                            // Assign the layer the ACI color 1 (red) and a name
                            lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                            lyrTblRec.Name = nodeLayer;

                            // Upgrade the Layer table for write
                            trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                            // Append the new layer to the Layer table and the transaction
                            lyrTbl.Add(lyrTblRec);
                            trans.AddNewlyCreatedDBObject(lyrTblRec, true);
                        }
                    }

                    // Open the Registered Applications table for read
                    RegAppTable acRegAppTbl;
                    acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                    // Check to see if the Registered Applications table record for the custom app exists
                    if (acRegAppTbl.Has(appName) == false)
                    {
                        using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                        {
                            acRegAppTblRec.Name = appName;
                            trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                            acRegAppTbl.Add(acRegAppTblRec);
                            trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                        }
                    }

                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create the node in Model space
                    // Tell user to insert the point:
                    PromptPointOptions pickPoint = new PromptPointOptions("\nPick point or enter coordinates: ");
                    PromptPointResult pointResult = ed.GetPoint(pickPoint);

                    // Exit if the user presses ESC or cancels the command
                    if (pointResult.Status == PromptStatus.OK)
                    {

                        // Create the node and set its layer to Node:

                        DBPoint newNode = new DBPoint(pointResult.Value);
                        newNode.Layer = nodeLayer;

                        // Add the new object to the block table record and the transaction
                        blkTblRec.AppendEntity(newNode);
                        trans.AddNewlyCreatedDBObject(newNode, true);

                        // Inicialization of node conditions
                        int nodeNumber = 0;                     // Node number (to be set later)
                        double xPosition = pointResult.Value.X; // X position
                        double yPosition = pointResult.Value.Y; // Y position
                        string support = "Free";                // Support condition
                        double xForce = 0;                      // Force on X direction
                        double yForce = 0;                      // Force on Y direction

                        // Define the Xdata to add to the node
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));   // 0
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr)); // 1
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, nodeNumber)); // 2
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xPosition));       // 3
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yPosition));       // 4
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, support));  // 5
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xForce));          // 6
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yForce));          // 7

                            // Open the node for write
                            Entity ent = trans.GetObject(newNode.ObjectId, OpenMode.ForWrite) as Entity;

                            // Append the extended data to each object
                            ent.XData = rb;
                        }

                        // Set the style for all point objects in the drawing
                        curDb.Pdmode = 32;
                        curDb.Pdsize = 50;

                        // Save the new object to the database and dispose the transaction
                        trans.Commit();
                        trans.Dispose();
                    }
                    else
                    {
                        // Exit the command
                        trans.Dispose();
                        break;
                    }
                }
            }
        }


        [CommandMethod("AddStringer")]
        public static void AddStringer()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Stringer data";

            // Prompt for the start point of stringer
            PromptPointOptions strStartOp = new PromptPointOptions("\nPick the start node: ");
            PromptPointResult strStartRes = ed.GetPoint(strStartOp);

            // Exit if the user presses ESC or cancels the command
            if (strStartRes.Status == PromptStatus.Cancel) return;

            // Loop for creating infinite stringers (until user exits the command)
            for (; ; )
            {
                // Start a transaction
                using (Transaction trans = curDb.TransactionManager.StartTransaction())
                {
                    // Get the stringer start point
                    Point3d strStart = strStartRes.Value;

                    // Open the Layer table for read
                    LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                    string stringerLayer = "Stringer";

                    // Check if the layer Stringer already exists in the drawing. If it doesn't, then it's created:
                    if (lyrTbl.Has(stringerLayer) == false)
                    {
                        using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                        {
                            // Assign the layer the ACI color 1 (cyan) and a name
                            lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                            lyrTblRec.Name = stringerLayer;

                            // Upgrade the Layer table for write
                            trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                            // Append the new layer to the Layer table and the transaction
                            lyrTbl.Add(lyrTblRec);
                            trans.AddNewlyCreatedDBObject(lyrTblRec, true);
                        }
                    }

                    // Open the Registered Applications table for read
                    RegAppTable acRegAppTbl;
                    acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                    // Check to see if the Registered Applications table record for the custom app exists
                    if (acRegAppTbl.Has(appName) == false)
                    {
                        using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                        {
                            acRegAppTblRec.Name = appName;
                            trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                            acRegAppTbl.Add(acRegAppTblRec);
                            trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                        }
                    }

                    // Prompt for the end point
                    PromptPointOptions strEndOp = new PromptPointOptions("\nPick the end node: ");
                    strEndOp.UseBasePoint = true;
                    strEndOp.BasePoint = strStart;
                    PromptPointResult strEndRes = ed.GetPoint(strEndOp);
                    Point3d strEnd = strEndRes.Value;

                    if (strEndRes.Status == PromptStatus.OK)
                    {

                        // Open the Block table for read
                        BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                        // Open the Block table record Model space for write
                        BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        // Create the line in Model space

                        using (Line newStringer = new Line(strStart, strEnd))
                        {
                            // Set the layer to stringer
                            newStringer.Layer = stringerLayer;

                            // Add the line to the drawing
                            blkTblRec.AppendEntity(newStringer);
                            trans.AddNewlyCreatedDBObject(newStringer, true);

                            // Inicialization of stringer conditions
                            double strStXPos = strStartRes.Value.X; // Stringer start point (X)
                            double strStYPos = strStartRes.Value.Y; // Stringer start point (Y)
                            double strEnXPos = strEndRes.Value.X;   // Stringer end point (X)
                            double strEnYPos = strEndRes.Value.Y;   // Stringer end point (Y)
                            double strW = 1;                        // Width
                            double strH = 1;                        // Height
                            double As = 0;                          // Reinforcement Area

                            // Define the Xdata to add to the node
                            using (ResultBuffer rb = new ResultBuffer())
                            {
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));   // 0
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr)); // 1
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strStXPos));       // 2
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strStYPos));       // 3
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strEnXPos));       // 4
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strEnYPos));       // 5
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strW));            // 6
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strH));            // 7
                                rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, As));              // 8

                                // Open the stringer for write
                                Entity ent = trans.GetObject(newStringer.ObjectId, OpenMode.ForWrite) as Entity;

                                // Append the extended data to each object
                                ent.XData = rb;
                            }
                        }
                        // Save the new object to the database
                        trans.Commit();

                        // Dispose the transaction 
                        trans.Dispose();

                        // Set the start point of the new stringer
                        strStartRes = strEndRes;
                    }
                    else
                    {
                        // Exit the command
                        trans.Dispose();
                        break;
                    }
                }
            }
        }

        [CommandMethod("AddPanel")]
        public static void AddPanel()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Panel data";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                string panelLayer = "Panel";

                // Check if the layer Stringer already exists in the drawing. If it doesn't, then it's created:
                if (lyrTbl.Has(panelLayer) == false)
                {
                    using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color 254 (grey) and a name
                        lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 254);

                        // Assign a layer transparency
                        byte alpha = (byte)(255 * (100 - 70) / 100);
                        Transparency transp = new Transparency(alpha);

                        // Assign the name to the layer
                        lyrTblRec.Name = panelLayer;

                        // Upgrade the Layer table for write
                        trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                        // Append the new layer to the Layer table and the transaction
                        lyrTbl.Add(lyrTblRec);
                        trans.AddNewlyCreatedDBObject(lyrTblRec, true);

                        // Assign teh transparency
                        lyrTblRec.Transparency = transp;
                    }
                }

                // Open the Registered Applications table for read
                RegAppTable acRegAppTbl;
                acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                // Check to see if the Registered Applications table record for the custom app exists
                if (acRegAppTbl.Has(appName) == false)
                {
                    using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                    {
                        acRegAppTblRec.Name = appName;
                        trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                        acRegAppTbl.Add(acRegAppTblRec);
                        trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                    }
                }

                // Initialize the vertices of the panel
                Point3d[] panVerts =
                {
                    new Point3d(),
                    new Point3d(),
                    new Point3d(),
                    new Point3d()
                };

                // Initialize the panel parameters
                double panW = 1; // width
                double psx = 0;  // reinforcement ratio (X)
                double psy = 0;  // reinforcement ratio (Y)

                // Initialize a Result Buffer to add to the panel
                ResultBuffer rb = new ResultBuffer();
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));   // 0
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr)); // 1

                // Prompt for user enter four vertices of the panel
                for (int i = 0; i < 4; i++)
                {
                    // Prompt each vertice
                    PromptPointOptions panNodeOp = new PromptPointOptions("\nSelect nodes performing a loop");
                    PromptPointResult panNodeOpRes = ed.GetPoint(panNodeOp);

                    // Add to the vertices array
                    panVerts[i] = panNodeOpRes.Value;

                    // Add the node position to the Result Buffer
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, panNodeOpRes.Value.X));  // 2, 4, 6, 8
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, panNodeOpRes.Value.Y));  // 3, 5, 7, 9
                }

                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create the panel as a solid with 4 segments (4 points)
                using (Solid newPanel = new Solid(panVerts[0], panVerts[1], panVerts[3], panVerts[2]))
                {
                    // Set the layer to Panel
                    newPanel.Layer = panelLayer;

                    // Add the line to the drawing
                    blkTblRec.AppendEntity(newPanel);
                    trans.AddNewlyCreatedDBObject(newPanel, true);

                    // Add the final data to the Result Buffer
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, panW)); // 10
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psx));  // 11
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psy));  // 12

                    // Open the selected object for write
                    Entity ent = trans.GetObject(newPanel.ObjectId, OpenMode.ForWrite) as Entity;

                    // Append the extended data to each object
                    ent.XData = rb;
                }

                // Save the new object to the database
                trans.Commit();

                // Dispose the transaction 
                trans.Dispose();
            }
        }

        [CommandMethod("EnumerateNodes")]
        public void EnumerateNodes()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

        }

        [CommandMethod("SetStringerParameters")]
        public void SetStringerParameters()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                ed.WriteMessage("Select the stringers to assign properties (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
                PromptSelectionResult selRes = ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    SelectionSet set = selRes.Value;

                    // Open the Registered Applications table for read
                    RegAppTable acRegAppTbl;
                    acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                    // Check to see if the Registered Applications table record for the custom app exists
                    if (acRegAppTbl.Has(appName) == false)
                    {
                        using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                        {
                            acRegAppTblRec.Name = appName;
                            trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                            acRegAppTbl.Add(acRegAppTblRec);
                            trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                        }
                    }

                    // Ask the user to input the stringer width
                    PromptDoubleOptions strWOp = new PromptDoubleOptions("\nInput the width (in mm) for the selected stringers:");

                    // Restrict input to positive and non-negative values
                    strWOp.AllowZero = false;
                    strWOp.AllowNegative = false;

                    // Get the result
                    PromptDoubleResult strWRes = ed.GetDouble(strWOp);
                    double strW = strWRes.Value;

                    // Ask the user to input the stringer height
                    PromptDoubleOptions strHOp = new PromptDoubleOptions("\nInput the height (in mm) for the selected stringers:");

                    // Restrict input to positive and non-negative values
                    strHOp.AllowZero = false;
                    strHOp.AllowNegative = false;

                    // Get the result
                    PromptDoubleResult strHRes = ed.GetDouble(strHOp);
                    double strH = strHRes.Value;

                    // Ask the user to input the reinforcement area
                    PromptDoubleOptions AsOp = new PromptDoubleOptions("\nInput the reinforcement area for the selected stringers (only needed in non-linear analysis):");

                    // Restrict input to positive and non-negative values
                    AsOp.AllowNegative = false;

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

                            // Set the new geometry and reinforcement (line 6, 7 and 8 of the array)
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

                // Dispose the transaction
                trans.Dispose();
            }
        }


        [CommandMethod("SetPanelParameters")]
        public void SetPanelParameters()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

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
                    SelectionSet set = selRes.Value;

                    // Open the Registered Applications table for read
                    RegAppTable acRegAppTbl;
                    acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                    // Check to see if the Registered Applications table record for the custom app exists
                    if (acRegAppTbl.Has(appName) == false)
                    {
                        using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                        {
                            acRegAppTblRec.Name = appName;
                            trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                            acRegAppTbl.Add(acRegAppTblRec);
                            trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                        }
                    }

                    // Ask the user to input the panel width
                    PromptDoubleOptions panWOp = new PromptDoubleOptions("\nInput the width (in mm) for the selected panels:");

                    // Restrict input to positive and non-negative values
                    panWOp.AllowZero = false;
                    panWOp.AllowNegative = false;

                    // Get the result
                    PromptDoubleResult panWRes = ed.GetDouble(panWOp);
                    double panW = panWRes.Value;

                    // Ask the user to input the reinforcement ratio in x direction
                    PromptDoubleOptions psxOp = new PromptDoubleOptions("\nInput the reinforcement ratio in x direction for selected panels (only needed in non-linear analysis):");

                    // Restrict input to positive and non-negative values
                    psxOp.AllowNegative = false;

                    // Get the result
                    PromptDoubleResult psxRes = ed.GetDouble(psxOp);
                    double psx = psxRes.Value;

                    // Ask the user to input the reinforcement ratio in x direction
                    PromptDoubleOptions psyOp = new PromptDoubleOptions("\nInput the reinforcement ratio in y direction for selected panels (only needed in non-linear analysis):");

                    // Restrict input to positive and non-negative values
                    psyOp.AllowNegative = false;

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

                            // Set the new geometry and reinforcement (line 10, 11 and 12 of the array)
                            data[10] = new TypedValue((int)DxfCode.ExtendedDataReal, panW);
                            data[11] = new TypedValue((int)DxfCode.ExtendedDataReal, psx);
                            data[12] = new TypedValue((int)DxfCode.ExtendedDataReal, psy);

                            // Add the new XData
                            ResultBuffer newRb = new ResultBuffer(data);
                            ent.XData = newRb;
                        }
                    }
                }

                // Save the new object to the database
                trans.Commit();

                // Dispose the transaction
                trans.Dispose();
            }
        }

        [CommandMethod("ViewElementData")]
        public void ViewElementData()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string msgstr = "";
            string dataType = "";

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

                                // Get the data type
                                dataType = data[1].Value.ToString();

                                // Get the parameters
                                msgstr = "\nNode number: " + data[2].Value.ToString() +
                                         "\nNode position: (" + data[3].Value.ToString() + ", " + data[4].Value.ToString() + ")" +
                                         "\nSupport conditions: " + data[5].Value.ToString() +
                                         "\nForce in X direction = " + data[6].Value.ToString() + " N" +
                                         "\nForce in Y direction = " + data[7].Value.ToString() + " N";
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

                                // Get the data type
                                dataType = data[1].Value.ToString();

                                // Get the parameters
                                msgstr = "\nStart node: (" + data[2].Value.ToString() + ", " + data[3].Value.ToString() + ")" +
                                         "\nEnd node: (" + data[4].Value.ToString() + ", " + data[5].Value.ToString() + ")" +
                                         "\nWidth = " + data[6].Value.ToString() + " mm" +
                                         "\nHeight = " + data[7].Value.ToString() + " mm" +
                                         "\nReinforcement = " + data[8].Value.ToString() + " mm2";
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

                                // Get the data type
                                dataType = data[1].Value.ToString();

                                // Get the parameters
                                msgstr = "\nVertices: (" + data[2].Value.ToString() + ", " + data[3].Value.ToString() + "), ("
                                                                        + data[4].Value.ToString() + ", " + data[5].Value.ToString() + "), ("
                                                                        + data[6].Value.ToString() + ", " + data[7].Value.ToString() + "), ("
                                                                        + data[8].Value.ToString() + ", " + data[9].Value.ToString() + ")" +
                                         "\nWidth = " + data[10].Value.ToString() + " mm" +
                                         "\nReinforcement ratio (x) = " + data[11].Value.ToString() +
                                         "\nReinforcement ratio (y) = " + data[12].Value.ToString();
                            }

                            else
                            {
                                msgstr = "NONE";
                            }
                        }

                        // Display the values returned
                        Application.ShowAlertDialog(appName + "\n\n" + dataType + "\n" + msgstr);
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
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Concrete data";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Ask the user to input the concrete compressive strength
                PromptDoubleOptions fcOp = new PromptDoubleOptions("Input the concrete compressive strength (fc) in MPa:");

                // Restrict input to positive and non-negative values
                fcOp.AllowZero = false;
                fcOp.AllowNegative = false;

                // Get the result
                PromptDoubleResult fcRes = ed.GetDouble(fcOp);
                double fc = fcRes.Value;

                // Ask the user to input the concrete Elastic Module
                PromptDoubleOptions EcOp = new PromptDoubleOptions("Input the concrete Elastic Module (Ec) in MPa:");

                // Restrict input to positive and non-negative values
                EcOp.AllowZero = false;
                EcOp.AllowNegative = false;

                // Get the result
                PromptDoubleResult EcRes = ed.GetDouble(EcOp);
                double Ec = EcRes.Value;

                // Open the Registered Applications table for read
                RegAppTable acRegAppTbl;
                acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                // Check to see if the Registered Applications table record for the custom app exists
                if (acRegAppTbl.Has(appName) == false)
                {
                    using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                    {
                        acRegAppTblRec.Name = appName;
                        trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                        acRegAppTbl.Add(acRegAppTblRec);
                        trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                    }
                }

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

                // Dispose the transaction
                trans.Dispose();
            }
        }

        [CommandMethod("SetSteelParameters")]
        public static void SetSteelParameters()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Steel data";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Ask the user to input the steel tensile strength
                PromptDoubleOptions fyOp = new PromptDoubleOptions("Input the steel tensile strength (fy) in MPa:");

                // Restrict input to positive and non-negative values
                fyOp.AllowZero = false;
                fyOp.AllowNegative = false;

                // Get the result
                PromptDoubleResult fyRes = ed.GetDouble(fyOp);
                double fy = fyRes.Value;

                // Ask the user to input the steel Elastic Module
                PromptDoubleOptions EsOp = new PromptDoubleOptions("Input the steel Elastic Module (Es) in MPa:");

                // Restrict input to positive and non-negative values
                EsOp.AllowZero = false;
                EsOp.AllowNegative = false;

                // Get the result
                PromptDoubleResult EsRes = ed.GetDouble(EsOp);
                double Es = EsRes.Value;

                // Open the Registered Applications table for read
                RegAppTable acRegAppTbl;
                acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                // Check to see if the Registered Applications table record for the custom app exists
                if (acRegAppTbl.Has(appName) == false)
                {
                    using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                    {
                        acRegAppTblRec.Name = appName;
                        trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                        acRegAppTbl.Add(acRegAppTblRec);
                        trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                    }
                }

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

                // Dispose the transaction
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
                              "\nfc = " + data[2].Value.ToString() + " MPa" +
                              "\nEc = " + data[3].Value.ToString() + " MPa";

                    // Read the Steel Xrecord
                    Xrecord steelXrec = (Xrecord)trans.GetObject(steelPar, OpenMode.ForRead);
                    ResultBuffer rb2 = steelXrec.Data;
                    TypedValue[] data2 = rb.AsArray();

                    // Get the parameters
                    steelmsg = "\nSteel Parameters" +
                               "\nfy = " + data2[2].Value.ToString() + " MPa" +
                               "\nEs = " + data2[3].Value.ToString() + " MPa";
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




    // Support related commands
    public class SupportsAndForces
    {
        [CommandMethod("AddSupport")]
        public void AddSupport()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize variables
                PromptSelectionResult selRes;
                SelectionSet set;

                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                // Check if the layer Stringer already exists in the drawing. If it doesn't, then it's created:
                string layer = "Support";
                if (lyrTbl.Has(layer) == false)
                {
                    using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color 1 (red) and a name
                        lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                        lyrTblRec.Name = layer;

                        // Upgrade the Layer table for write
                        trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                        // Append the new layer to the Layer table and the transaction
                        lyrTbl.Add(lyrTblRec);
                        trans.AddNewlyCreatedDBObject(lyrTblRec, true);
                    }
                }

                // Open the Registered Applications table for read
                RegAppTable acRegAppTbl;
                acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                // Check to see if the Registered Applications table record for the custom app exists
                if (acRegAppTbl.Has(appName) == false)
                {
                    using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                    {
                        acRegAppTblRec.Name = appName;
                        trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                        acRegAppTbl.Add(acRegAppTblRec);
                        trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                    }
                }

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
                            // 1st line
                            Line line1 = new Line()
                            {
                                StartPoint = origin,
                                EndPoint = new Point3d(-200, 115, 0)
                            };
                            // Add to the collection
                            lines.Add(line1);

                            // 2nd line
                            Line line2 = new Line
                            {
                                StartPoint = origin,
                                EndPoint = new Point3d(-200, -115, 0)
                            };
                            // Add to the collection
                            lines.Add(line2);

                            // 3rd line
                            Line line3 = new Line
                            {
                                StartPoint = new Point3d(-200, 150, 0),
                                EndPoint = new Point3d(-200, -150, 0)
                            };
                            // Add to the collection
                            lines.Add(line3);

                            // 4th line
                            Line line4 = new Line
                            {
                                StartPoint = new Point3d(-250, 150, 0),
                                EndPoint = new Point3d(-250, -150, 0)
                            };
                            // Add to the collection
                            lines.Add(line4);

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
                            // 1st line
                            Line line1 = new Line()
                            {
                                StartPoint = origin,
                                EndPoint = new Point3d(-115, -200, 0)
                            };
                            // Add to the collection
                            lines.Add(line1);

                            // 2nd line
                            Line line2 = new Line
                            {
                                StartPoint = origin,
                                EndPoint = new Point3d(115, -200, 0)
                            };
                            // Add to the collection
                            lines.Add(line2);

                            // 3rd line
                            Line line3 = new Line
                            {
                                StartPoint = new Point3d(-150, -200, 0),
                                EndPoint = new Point3d(150, -200, 0)
                            };
                            // Add to the collection
                            lines.Add(line3);

                            // 4th line
                            Line line4 = new Line
                            {
                                StartPoint = new Point3d(-150, -250, 0),
                                EndPoint = new Point3d(+150, -250, 0)
                            };
                            // Add to the collection
                            lines.Add(line4);

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
                            // 1st line
                            Line line1 = new Line()
                            {
                                StartPoint = origin,
                                EndPoint = new Point3d(-115, -200, 0)
                            };
                            // Add to the collection
                            lines.Add(line1);

                            // 2nd line
                            Line line2 = new Line()
                            {
                                StartPoint = origin,
                                EndPoint = new Point3d(115, -200, 0)
                            };
                            // Add to the collection
                            lines.Add(line2);

                            // 3rd line
                            Line line3 = new Line()
                            {
                                StartPoint = new Point3d(-150, -200, 0),
                                EndPoint = new Point3d(150, -200, 0)
                            };
                            // Add to the collection
                            lines.Add(line3);

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
                else
                {
                    // The blocks already exist
                    xBlock = blkTbl["SupportX"];
                    yBlock = blkTbl["SupportY"];
                    xyBlock = blkTbl["SupportXY"];
                }

                // Enter a loop
                for (; ; )
                {
                    // Request objects to be selected in the drawing area
                    ed.WriteMessage("Select a node to add support conditions.");
                    selRes = ed.GetSelection();

                    // If the prompt status is OK, objects were selected
                    if (selRes.Status == PromptStatus.OK)
                    {
                        // Get the objects selected
                        set = selRes.Value;

                        // If user selected more than one node 
                        if (set.Count > 1)
                        {
                            Application.ShowAlertDialog("Please select one node at a time.");
                        }

                        // If user selected only one node, continue the command
                        else break;
                    }
                    else return;
                }

                // Ask the user set the support conditions:
                PromptKeywordOptions supOp = new PromptKeywordOptions("");
                supOp.Message = "\nAdd support in which direction?";
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

                        // Access the XData as an array
                        ResultBuffer rb = ent.GetXDataForApplication(appName);
                        TypedValue[] data = rb.AsArray();

                        // Get the node coordinates on the XData
                        double xPos = Convert.ToDouble(data[3].Value);
                        double yPos = Convert.ToDouble(data[4].Value);

                        // Set the new support conditions (line 5 of the array)
                        data[5] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, support);

                        // Add the new XData
                        ResultBuffer newRb = new ResultBuffer(data);
                        ent.XData = newRb;

                        // Add the block to selected node at
                        Point3d insPt = new Point3d(xPos, yPos, 0);

                        // Insert the block into the current space
                        if (support == "X" && xBlock != ObjectId.Null)
                        {
                            using (BlockReference blkRef = new BlockReference(insPt, xBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = layer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);
                            }
                        }

                        if (support == "Y" && yBlock != ObjectId.Null)
                        {
                            using (BlockReference blkRef = new BlockReference(insPt, yBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = layer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);
                            }
                        }

                        if (support == "XY" && xyBlock != ObjectId.Null)
                        {
                            using (BlockReference blkRef = new BlockReference(insPt, xyBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = layer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);
                            }
                        }
                    }

                    // Save the new object to the database
                    trans.Commit();

                    // Dispose the transaction
                    trans.Dispose();
                }
            }
        }

        [CommandMethod("AddForce")]
        public void AddForce()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Get the coordinate system for transformations
            Matrix3d curUCSMatrix = curDoc.Editor.CurrentUserCoordinateSystem;
            CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize variables
                PromptSelectionResult selRes;
                SelectionSet set;

                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                // Check if the layer Stringer already exists in the drawing. If it doesn't, then it's created:
                string layer = "Force";
                if (lyrTbl.Has(layer) == false)
                {
                    using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color 2 (yellow) and a name
                        lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);
                        lyrTblRec.Name = layer;

                        // Upgrade the Layer table for write
                        trans.GetObject(curDb.LayerTableId, OpenMode.ForWrite);

                        // Append the new layer to the Layer table and the transaction
                        lyrTbl.Add(lyrTblRec);
                        trans.AddNewlyCreatedDBObject(lyrTblRec, true);
                    }
                }

                // Open the Registered Applications table for read
                RegAppTable acRegAppTbl;
                acRegAppTbl = trans.GetObject(curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                // Check to see if the Registered Applications table record for the custom app exists
                if (acRegAppTbl.Has(appName) == false)
                {
                    using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                    {
                        acRegAppTblRec.Name = appName;
                        trans.GetObject(curDb.RegAppTableId, OpenMode.ForWrite);
                        acRegAppTbl.Add(acRegAppTblRec);
                        trans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                    }
                }

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
                                StartPoint = new Point3d(0, 150, 0),
                                EndPoint = new Point3d(0, 500, 0)
                            };
                            // Add to the collection
                            arrow.Add(line);

                            // Create the solid and add to the collection
                            Solid solid = new Solid(origin, new Point3d(-100, 150, 0), new Point3d(100, 150, 0));
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
                else
                {
                    // The blocks already exist
                    ForceBlock = blkTbl["ForceBlock"];
                }

                // Enter a loop
                for ( ; ; )
                {
                    // Request objects to be selected in the drawing area
                    ed.WriteMessage("\nSelect a node to add load:");
                    selRes = ed.GetSelection();

                    // If the prompt status is OK, objects were selected
                    if (selRes.Status == PromptStatus.OK)
                    {
                        // Get the objects selected
                        set = selRes.Value;

                        // If user selected more than one node 
                        if (set.Count > 1)
                        {
                            Application.ShowAlertDialog("Please select one node at a time.");
                        }

                        // If user selected only one node, continue the command
                        else break;
                    }
                    else return;
                }

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

                        // Access the XData as an array
                        ResultBuffer rb = ent.GetXDataForApplication(appName);
                        TypedValue[] data = rb.AsArray();

                        // Get the node coordinates on the XData
                        double xPos = Convert.ToDouble(data[3].Value);
                        double yPos = Convert.ToDouble(data[4].Value);

                        // Set the new forces (line 6 and 7 of the array)
                        data[6] = new TypedValue((int)DxfCode.ExtendedDataReal, xForce);
                        data[7] = new TypedValue((int)DxfCode.ExtendedDataReal, yForce);

                        // Add the new XData
                        ResultBuffer newRb = new ResultBuffer(data);
                        ent.XData = newRb;

                        // Add the block to selected node at
                        Point3d insPt = new Point3d(xPos, yPos, 0);

                        // Insert the block into the current space
                        if (xForce > 0) // positive force in x
                        {
                            using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = layer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);

                                // Rotate 90 degress counterclockwise
                                blkRef.TransformBy(Matrix3d.Rotation(1.570796, curUCS.Zaxis, insPt));

                                // Insert the force value as text and add to the block table
                                DBText text = new DBText()
                                {
                                    Position = new Point3d (xPos - 800, yPos + 50, 0),
                                    Height = 100,
                                    TextString = xForce.ToString() + " N",
                                    Layer = layer
                                };

                                blkTblRec.AppendEntity(text);
                                trans.AddNewlyCreatedDBObject(text, true);
                            }
                        }
                        if (xForce < 0) // negative force in x
                        {
                            using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = layer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);

                                // Rotate 90 degress clockwise
                                blkRef.TransformBy(Matrix3d.Rotation(-1.570796, curUCS.Zaxis, insPt));

                                // Insert the force value as text and add to the block table
                                DBText text = new DBText()
                                {
                                    Position = new Point3d(xPos + 300, yPos + 50, 0),
                                    Height = 100,
                                    TextString = xForce.ToString() + " N",
                                    Layer = layer
                                };

                                blkTblRec.AppendEntity(text);
                                trans.AddNewlyCreatedDBObject(text, true);
                            }
                        }

                        if (yForce > 0) // positive force in y
                        {
                            using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = layer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);

                                // Rotate 90 degress counterclockwise
                                blkRef.TransformBy(Matrix3d.Rotation(3.14159265, curUCS.Zaxis, insPt));

                                // Insert the force value as text and add to the block table
                                DBText text = new DBText()
                                {
                                    Position = new Point3d(xPos + 50, yPos - 500, 0),
                                    Height = 100,
                                    TextString = yForce.ToString() + " N",
                                    Layer = layer
                                };

                                blkTblRec.AppendEntity(text);
                                trans.AddNewlyCreatedDBObject(text, true);
                            }
                        }

                        if (yForce < 0) // negative force in y
                        {
                            using (BlockReference blkRef = new BlockReference(insPt, ForceBlock))
                            {
                                BlockTableRecord blkTblRec = trans.GetObject(curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                blkTblRec.AppendEntity(blkRef);
                                blkRef.Layer = layer;
                                trans.AddNewlyCreatedDBObject(blkRef, true);

                                // No rotation needed

                                // Insert the force value as text and add to the block table
                                DBText text = new DBText()
                                {
                                    Position = new Point3d(xPos + 50, yPos + 400, 0),
                                    Height = 100,
                                    TextString = yForce.ToString() + " N",
                                    Layer = layer
                                };

                                blkTblRec.AppendEntity(text);
                                trans.AddNewlyCreatedDBObject(text, true);
                            }
                        }
                        // If x or y forces are 0, the block is not added
                    }

                    // Save the new object to the database
                    trans.Commit();

                    // Dispose the transaction
                    trans.Dispose();
                }
            }
        }
    }
}
