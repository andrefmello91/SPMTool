using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SPMTool.Geometry))]
[assembly: CommandClass(typeof(SPMTool.Material))]
[assembly: CommandClass(typeof(SPMTool.Supports))]

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
                        bool xSupport = false;
                        bool ySupport = false;
                        double xForce = 0;
                        double yForce = 0;

                        // Define the Xdata to add to the node
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, xSupport));
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, ySupport));
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, xForce));
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, yForce));

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

            // Loop for creating infinite stringers (until user exits the command)
            for (; ; )
            {
                // Start a transaction
                using (Transaction trans = curDb.TransactionManager.StartTransaction())
                {
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

                    // Prompt for the start point of stringer
                    PromptPointOptions strStartOp = new PromptPointOptions("\nPick the start node: ");
                    PromptPointResult strStartRes = ed.GetPoint(strStartOp);
                    Point3d strStart = strStartRes.Value;

                    // Exit if the user presses ESC or cancels the command
                    if (strStartRes.Status == PromptStatus.Cancel) return;

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
                        }

                        // Save the new object to the database
                        trans.Commit();

                        // Dispose the transaction 
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

        [CommandMethod("AddPanel")]
        public static void AddPanel()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

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

                // Initialize the vertices of the panel
                Point3d pan1Node = new Point3d();
                Point3d pan2Node = new Point3d();
                Point3d pan3Node = new Point3d();
                Point3d pan4Node = new Point3d();

                // Prompt for user enter four vertices of the panel
                for (int i = 1; i < 5; i++)
                {
                    // Prompt each vertice
                    PromptPointOptions panNodeOp = new PromptPointOptions("\nSelect nodes performing a loop");
                    PromptPointResult panNodeOpRes = ed.GetPoint(panNodeOp);
                    if (panNodeOpRes.Status == PromptStatus.OK)
                    {
                        if (i == 1) { pan1Node = panNodeOpRes.Value; }
                        if (i == 2) { pan2Node = panNodeOpRes.Value; }
                        if (i == 3) { pan3Node = panNodeOpRes.Value; }
                        if (i == 4) { pan4Node = panNodeOpRes.Value; }
                    }
                }

                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create the panel as a solid with 4 segments (4 points)
                using (Solid newPanel = new Solid(pan1Node, pan2Node, pan4Node, pan3Node))
                {
                    // Set the layer to Panel
                    newPanel.Layer = panelLayer;

                    // Add the line to the drawing
                    blkTblRec.AppendEntity(newPanel);
                    trans.AddNewlyCreatedDBObject(newPanel, true);
                }

                // Save the new object to the database
                trans.Commit();

                // Dispose the transaction 
                trans.Dispose();
            }
        }
        


        [CommandMethod("SetStringerGeometry")]
        public void SetStringerGeometry()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string xdataStr = "Stringer data";

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
                    PromptDoubleOptions strWOp = new PromptDoubleOptions("Input the width (in mm) for the selected stringers");

                    // Restrict input to positive and non-negative values
                    strWOp.AllowZero = false;
                    strWOp.AllowNegative = false;

                    // Get the result
                    PromptDoubleResult strWRes = ed.GetDouble(strWOp);
                    double strW = strWRes.Value;

                    // Ask the user to input the stringer height
                    PromptDoubleOptions strHOp = new PromptDoubleOptions("Input the height (in mm) for the selected stringers");

                    // Restrict input to positive and non-negative values
                    strHOp.AllowZero = false;
                    strHOp.AllowNegative = false;

                    // Get the result
                    PromptDoubleResult strHRes = ed.GetDouble(strHOp);
                    double strH = strHRes.Value;

                    // Calculate the cross-section area
                    double strArea = strW * strH;

                    // Define the Xdata to add to each selected object
                    using (ResultBuffer rb = new ResultBuffer())
                    {
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, strW));
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, strH));
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, strArea));

                        foreach (SelectedObject obj in set)
                        {

                            // Open the selected object for read
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                            // Check if it's a stringer (if the stringer layer is active)
                            if (ent.Layer.Equals("Stringer"))
                            {
                                // Upgrade the OpenMode
                                ent.UpgradeOpen();

                                // Append the extended data to each object
                                ent.XData = rb;
                            }
                        }

                    }
                }

                // Save the new object to the database
                trans.Commit();

                // Dispose the transaction
                trans.Dispose();
            }
        }


        [CommandMethod("SetPanelWidth")]
        public void SetPanelWidth()
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
                // Request objects to be selected in the drawing area
                ed.WriteMessage("Select the panels to assign properties (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
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
                    PromptDoubleOptions panWOp = new PromptDoubleOptions("Input the width (in mm) for the selected panels");

                    // Restrict input to positive and non-negative values
                    panWOp.AllowZero = false;
                    panWOp.AllowNegative = false;

                    // Get the result
                    PromptDoubleResult panWRes = ed.GetDouble(panWOp);
                    double panW = panWRes.Value;

                    // Define the Xdata to add to each selected object
                    using (ResultBuffer rb = new ResultBuffer())
                    {
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, panW));

                        SelectionSet objSet = selRes.Value;

                        // Step through the objects in the selection set
                        foreach (SelectedObject obj in objSet)
                        {
                            // Open the selected object for read
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                            // Check if it's a panel (if the panel layer is active)
                            if (ent.Layer.Equals("Panel"))
                            {
                                // Upgrade the OpenMode
                                ent.UpgradeOpen();

                                // Append the extended data to each object
                                ent.XData = rb;
                            }
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

                        // Get the extended data attached to each object for MY_APP
                        ResultBuffer rb = ent.GetXDataForApplication(appName);

                        // Make sure the Xdata is not empty
                        if (rb != null)
                        {
                            // Get the values in the xdata
                            foreach (TypedValue typeVal in rb)
                            {
                                msgstr = msgstr + "\n" + typeVal.TypeCode.ToString() + ":" + typeVal.Value;
                            }
                        }
                        else
                        {
                            msgstr = "NONE";
                        }

                        // Display the values returned
                        Application.ShowAlertDialog(appName + " xdata on " + ent.GetType().ToString() + ":\n" + msgstr);

                        msgstr = "";
                    }
                }

                // Ends the transaction and ensures any changes made are ignored
                trans.Abort();

                // Dispose of the transaction
                trans.Dispose();
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

                // Save the variables on the Xrecord
                using (ResultBuffer rb = new ResultBuffer())
                {
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, fc));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, Ec));

                    // Create and add data to an Xrecord
                    Xrecord concXrec = new Xrecord();
                    concXrec.Data = rb;
                }
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

                // Save the variables on the Xrecord
                using (ResultBuffer rb = new ResultBuffer())
                {
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, fy));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, Es));

                    // Create and add data to an Xrecord
                    Xrecord steelXrec = new Xrecord();
                    steelXrec.Data = rb;
                }

            }
        }
    }

    // Support related commands
    public class Supports
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
            string xdataStr = "Node data";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                ed.WriteMessage("Select the nodes to add support conditions.");
                PromptSelectionResult selRes = ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    SelectionSet set = selRes.Value;

                    // Check if the selected objects are stringers
                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        if (ent.Layer.Equals("Node") == false)
                        {
                            Application.ShowAlertDialog("You selected objects other than nodes. Also, make sure that all the nodes have the layer 'Node' activated. Please select the node(s) again.");

                            // Abort the transaction
                            trans.Abort();
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

                    // Ask the user set the support conditions in the x direction:
                    PromptKeywordOptions xSupOp = new PromptKeywordOptions("");
                    xSupOp.Message = "\nAdd support in the x direction?";
                    xSupOp.Keywords.Add("Yes");
                    xSupOp.Keywords.Add("No");
                    xSupOp.Keywords.Default = "Yes";
                    xSupOp.AllowNone = true;

                    // Get the result
                    PromptResult xSupRes = ed.GetKeywords(xSupOp);

                    if (xSupRes.Status == PromptStatus.OK)
                    {
                        switch (xSupRes.StringResult)
                        {
                            case "Yes":
                                {
                                    bool xSupport = true;
                                }
                                break;

                            case "No":
                                {
                                    bool xSupport = false;
                                }
                                break;
                        }
                    }

                    // Ask the user set the support conditions in the y direction:
                    PromptKeywordOptions ySupOp = new PromptKeywordOptions("");
                    ySupOp.Message = "\nAdd support in the y direction?";
                    ySupOp.Keywords.Add("Yes");
                    ySupOp.Keywords.Add("No");
                    ySupOp.Keywords.Default = "Yes";
                    ySupOp.AllowNone = true;

                    // Get the result
                    PromptResult ySupRes = ed.GetKeywords(ySupOp);

                    if (ySupRes.Status == PromptStatus.OK)
                    {
                        switch (ySupRes.StringResult)
                        {
                            case "Yes":
                                {
                                    bool ySupport = true;
                                }
                                break;

                            case "No":
                                {
                                    bool ySupport = false;
                                }
                                break;
                        }
                    }

                }
            }
        }
    }
}
