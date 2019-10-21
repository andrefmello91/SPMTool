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
        [CommandMethod("AddStringer")]
        public static void AddStringer()
        {
            // Check if the layers already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.extNdLyr, Colors.red, 0);
            Auxiliary.CreateLayer(Layers.intNdLyr, Colors.blue, 0);
            Auxiliary.CreateLayer(Layers.strLyr, Colors.cyan, 0);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            Auxiliary.RegisterApp();

            // Enumerate the nodes
            UpdateNodes();

            // Prompt for the start point of stringer
            PromptPointOptions strStOp = new PromptPointOptions("\nEnter the start point: ");
            PromptPointResult strStRes = AutoCAD.edtr.GetPoint(strStOp);

            // Exit if the user presses ESC or cancels the command
            if (strStRes.Status == PromptStatus.OK)
            {
                // Loop for creating infinite stringers (until user exits the command)
                for ( ; ; )
                {
                    // Start a transaction
                    using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                    {
                        // Create a point3d collection and add the stringer start point
                        Point3dCollection nds = new Point3dCollection();
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
                            Point3d strSt = extNds[0];
                            Point3d strEnd = extNds[1];

                            // Open the Block table for read
                            BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                            // Open the Block table record Model space for write
                            BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                            // Create the line in Model space
                            using (Line newStr = new Line(strSt, strEnd))
                            {
                                // Set the layer to stringer
                                newStr.Layer = Layers.strLyr;

                                // Add the line to the drawing
                                blkTblRec.AppendEntity(newStr);
                                trans.AddNewlyCreatedDBObject(newStr, true);
                            }

                            // Create the external nodes
                            AddNode(strSt,  Layers.extNdLyr);
                            AddNode(strEnd, Layers.extNdLyr);

                            // Get the midpoint and add the internal node
                            Point3d midPt = Auxiliary.MidPoint(strSt, strEnd);
                            AddNode(midPt, Layers.intNdLyr);

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

            // Update the nodes and stringers
            UpdateNodes();
            UpdateStringers();
        }

        [CommandMethod("AddPanel")]
        public static void AddPanel()
        {
            // Check if the layer panel already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.pnlLyr, Colors.grey, 80);

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            Auxiliary.RegisterApp();

            // Create a loop for creating infinite panels
            for ( ; ; )
            {
                // Prompt for user select 4 vertices of the panel
                AutoCAD.edtr.WriteMessage("\nSelect four nodes to be the vertices of the panel:");
                PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                if (selRes.Status == PromptStatus.OK)
                {
                    SelectionSet set = selRes.Value;

                    // Create a collection
                    ObjectIdCollection verts = new ObjectIdCollection();

                    // Create a point3d collection
                    Point3dCollection nds = new Point3dCollection();

                    // Start a transaction
                    using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                    {
                        // Get the objects in the selection and add to the collection only the external nodes
                        foreach (SelectedObject obj in set)
                        {
                            // Read as entity
                            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                            // Check if it is a external node
                            if (ent.Layer == Layers.extNdLyr) verts.Add(obj.ObjectId);
                        }

                        // Check if there are four objects
                        if (verts.Count == 4)
                        {
                            // Get the position of the points and add to the collection
                            foreach (ObjectId obj in verts)
                            {
                                DBPoint nd = trans.GetObject(obj, OpenMode.ForRead) as DBPoint;
                                nds.Add(nd.Position);
                            }

                            // Order the vertices in ascending Y and ascending X
                            List<Point3d> vrts = Auxiliary.OrderPoints(nds);

                            // Open the Block table for read
                            BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                            // Open the Block table record Model space for write
                            BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                            // Create the panel as a solid with 4 segments (4 points)
                            using (Solid newPnl = new Solid(vrts[0], vrts[1], vrts[2], vrts[3]))
                            {
                                // Set the layer to Panel
                                newPnl.Layer = Layers.pnlLyr;

                                // Add the panel to the drawing
                                blkTblRec.AppendEntity(newPnl);
                                trans.AddNewlyCreatedDBObject(newPnl, true);
                            }

                            // Save the new object to the database
                            trans.Commit();
                        }

                        else
                        {
                            Application.ShowAlertDialog("Please select four external nodes.");
                        }
                    }
                }

                else
                {
                    // Finish the command
                    break;
                }
            }

            // Update nodes and panels
            UpdateNodes();
            UpdatePanels();
        }

        [CommandMethod("DivideStringer")]
        public static void DivideStringer()
        {
            //// Start a transaction
            //using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            //{
            //    // Open the Block table for read
            //    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            //    // Open the Block table record Model space for write
            //    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

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
                if (strNumRes.Status == PromptStatus.Cancel) return;
                int strNum = strNumRes.Value;

                // Get the selection set and analyse the elements
                SelectionSet set = selRes.Value;

                // Add to an object collection
                ObjectIdCollection strs = new ObjectIdCollection();
                foreach (SelectedObject obj in set) strs.Add(obj.ObjectId);

                // Divide the stringers
                StringerDivide(strs, strNum);

                //{
                //    strs.Add(obj.ObjectId);

                //    // Open the selected object for read
                //    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                //    // Check if the selected object is a stringer
                //    if (ent.Layer == Layers.strLyr)
                //    {
                //        // Read as a line
                //        Line str = ent as Line;

                //        // Access the XData as an array
                //        ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);

                //        // Get the coordinates of the initial and end points
                //        Point3d strSt = str.StartPoint;
                //        Point3d strEnd = str.EndPoint;

                //        // Calculate the distance of the points in X and Y
                //        double distX = (strEnd.X - strSt.X) / strNum;
                //        double distY = (strEnd.Y - strSt.Y) / strNum;

                //        // Initialize the start point
                //        Point3d stPt = strSt;

                //        // Get the midpoint
                //        Point3d midPt = Auxiliary.MidPoint(strSt, strEnd);

                //        // Read the internal nodes
                //        foreach (ObjectId intNd in intNds)
                //        {
                //            // Read as point
                //            DBPoint nd = trans.GetObject(intNd, OpenMode.ForRead) as DBPoint;

                //            // Erase the internal node
                //            if (nd.Position == midPt)
                //            {
                //                nd.UpgradeOpen();
                //                nd.Erase();
                //                break;
                //            }
                //        }

                //        // Create the new stringers
                //        for (int i = 1; i <= strNum; i++)
                //        {
                //            // Get the coordinates of the other points
                //            double xCrd = str.StartPoint.X + i * distX;
                //            double yCrd = str.StartPoint.Y + i * distY;
                //            Point3d endPt = new Point3d(xCrd, yCrd, 0);

                //            // Create the stringer
                //            Line newStr = new Line()
                //            {
                //                StartPoint = stPt,
                //                EndPoint = endPt,
                //                Layer = Layers.strLyr
                //            };

                //            // Add the line to the drawing
                //            blkTblRec.AppendEntity(newStr);
                //            trans.AddNewlyCreatedDBObject(newStr, true);

                //            // Append the XData of the original stringer
                //            newStr.XData = rb;

                //            // Create the external nodes
                //            AddNode(stPt,  Layers.extNdLyr);
                //            AddNode(endPt, Layers.extNdLyr);

                //            // Get the mid point and add the internal node
                //            midPt = Auxiliary.MidPoint(stPt, endPt);
                //            AddNode(midPt, Layers.intNdLyr);

                //            // Set the start point of the next stringer
                //            stPt = endPt;
                //        }

                //        // Erase the original stringer
                //        ent.UpgradeOpen();
                //        ent.Erase();
                //    }
                //}
                //}

                //    // Save the new object to the database
                //    trans.Commit();
            }

            // Update nodes and stringers
            UpdateNodes();
            UpdateStringers();
        }

        // Method to divide stringers in a collection
        public static void StringerDivide(ObjectIdCollection stringers, int divNumber)
        {
            if (stringers.Count > 0)
            {
                // Access the internal nodes in the model
                ObjectIdCollection intNds = Auxiliary.GetEntitiesOnLayer(Layers.intNdLyr);

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    foreach (ObjectId obj in stringers)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj, OpenMode.ForRead) as Entity;

                        // Check if the selected object is a stringer
                        if (ent.Layer == Layers.strLyr)
                        {
                            // Read as a line
                            Line str = ent as Line;

                            // Access the XData as an array
                            ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);

                            // Get the coordinates of the initial and end points
                            Point3d strSt = str.StartPoint;
                            Point3d strEnd = str.EndPoint;

                            // Calculate the distance of the points in X and Y
                            double distX = (strEnd.X - strSt.X) / divNumber;
                            double distY = (strEnd.Y - strSt.Y) / divNumber;

                            // Initialize the start point
                            Point3d stPt = strSt;

                            // Get the midpoint
                            Point3d midPt = Auxiliary.MidPoint(strSt, strEnd);

                            // Read the internal nodes
                            foreach (ObjectId intNd in intNds)
                            {
                                // Read as point
                                DBPoint nd = trans.GetObject(intNd, OpenMode.ForRead) as DBPoint;

                                // Erase the internal node
                                if (nd.Position == midPt)
                                {
                                    nd.UpgradeOpen();
                                    nd.Erase();
                                    break;
                                }
                            }

                            // Create the new stringers
                            for (int i = 1; i <= divNumber; i++)
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
                                    Layer = Layers.strLyr
                                };

                                // Add the line to the drawing
                                blkTblRec.AppendEntity(newStr);
                                trans.AddNewlyCreatedDBObject(newStr, true);

                                // Append the XData of the original stringer
                                newStr.XData = rb;

                                // Create the external nodes
                                AddNode(stPt, Layers.extNdLyr);
                                AddNode(endPt, Layers.extNdLyr);

                                // Get the mid point and add the internal node
                                midPt = Auxiliary.MidPoint(stPt, endPt);
                                AddNode(midPt, Layers.intNdLyr);

                                // Set the start point of the next stringer
                                stPt = endPt;
                            }

                            // Erase the original stringer
                            ent.UpgradeOpen();
                            ent.Erase();
                        }
                    }

                    // Save the new object to the database
                    trans.Commit();
                }
            }
        }

        // Method to divide a panel and adjacent stringers
        [CommandMethod("DividePanel")]
        public static void DividePanel()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

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
                    if (rowRes.Status == PromptStatus.Cancel) return;
                    int row = rowRes.Value;

                    // Prompt for the number of columns
                    PromptIntegerOptions clmnOp = new PromptIntegerOptions("\nEnter the number of columns for adding panels:")
                    {
                        AllowNegative = false,
                        AllowZero = false
                    };

                    // Get the number
                    PromptIntegerResult clmnRes = AutoCAD.edtr.GetInteger(clmnOp);
                    if (clmnRes.Status == PromptStatus.Cancel) return;
                    int clmn = rowRes.Value;

                    // Access the stringers in the model
                    ObjectIdCollection strs = Auxiliary.GetEntitiesOnLayer(Layers.strLyr);

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
                            ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);

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
                                        Layer = Layers.pnlLyr
                                    };

                                    // Add the line to the drawing
                                    blkTblRec.AppendEntity(newPnl);
                                    trans.AddNewlyCreatedDBObject(newPnl, true);

                                    // Append the XData of the original panel
                                    newPnl.XData = rb;

                                    // Create the internal nodes of the panel (external for stringers)
                                    if (i > 0 && j > 0)
                                    {
                                        // Position
                                        Point3d pt = new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0);
                                        AddNode(pt, Layers.extNdLyr);
                                    }

                                    // Create the internal horizontal stringers
                                    if (i > 0)
                                    {
                                        Line strX = new Line()
                                        {
                                            StartPoint = new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
                                            EndPoint = new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0),
                                            Layer = Layers.strLyr
                                        };

                                        // Add the line to the drawing
                                        blkTblRec.AppendEntity(strX);
                                        trans.AddNewlyCreatedDBObject(strX, true);

                                        // Get the midpoint and add the internal node
                                        Point3d midPt = Auxiliary.MidPoint(strX.StartPoint, strX.EndPoint);
                                        AddNode(midPt, Layers.intNdLyr);
                                    }

                                    // Create the internal vertical stringers
                                    if (j > 0)
                                    {
                                        Line strY = new Line()
                                        {
                                            StartPoint = new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
                                            EndPoint = new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0),
                                            Layer = Layers.strLyr
                                        };

                                        // Add the line to the drawing
                                        blkTblRec.AppendEntity(strY);
                                        trans.AddNewlyCreatedDBObject(strY, true);

                                        // Get the midpoint and add the internal node
                                        Point3d midPt = Auxiliary.MidPoint(strY.StartPoint, strY.EndPoint);
                                        AddNode(midPt, Layers.intNdLyr);
                                    }
                                }
                            }

                            // Create object collections to adjacent stringers
                            ObjectIdCollection xStrs = new ObjectIdCollection(),
                                               yStrs = new ObjectIdCollection();

                            // Divide the adjacent stringers
                            foreach (ObjectId strObj in strs)
                            {
                                // Read as a line
                                Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;

                                // Verify if the stringer starts and ends in a panel vertex
                                if (grpPts.Contains(str.StartPoint) && grpPts.Contains(str.EndPoint))
                                {
                                    // Verify the angle of the stringer to add to the collection
                                    if (str.Angle == 0 || str.Angle == Constants.pi)
                                    {
                                        xStrs.Add(strObj);
                                    }

                                    if (str.Angle == Constants.piOver2 || str.Angle == Constants.pi3Over2)
                                    {
                                        yStrs.Add(strObj);
                                    }
                                }
                            }

                            // Divide the stringers
                            StringerDivide(xStrs, clmn);
                            StringerDivide(yStrs, row);

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
            UpdateNodes();
            UpdateStringers();
            UpdatePanels();
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

        [CommandMethod("SetStringerParameters")]
        public void SetStringerParameters()
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
                    double strH = strHRes.Value;

                    // Ask the user to input the reinforcement area
                    PromptDoubleOptions AsOp = new PromptDoubleOptions("\nInput the reinforcement area for the selected stringers (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult AsRes = AutoCAD.edtr.GetDouble(AsOp);
                    double As = AsRes.Value;

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
                            data[7] = new TypedValue((int)DxfCode.ExtendedDataReal, strW);
                            data[8] = new TypedValue((int)DxfCode.ExtendedDataReal, strH);
                            data[9] = new TypedValue((int)DxfCode.ExtendedDataReal, As);

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
                    double pnlW = pnlWRes.Value;

                    // Ask the user to input the reinforcement ratio in x direction
                    PromptDoubleOptions psxOp = new PromptDoubleOptions("\nInput the reinforcement ratio in x direction for selected panels (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult psxRes = AutoCAD.edtr.GetDouble(psxOp);
                    double psx = psxRes.Value;

                    // Ask the user to input the reinforcement ratio in y direction
                    PromptDoubleOptions psyOp = new PromptDoubleOptions("\nInput the reinforcement ratio in y direction for selected panels (only needed in non-linear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult psyRes = AutoCAD.edtr.GetDouble(psyOp);
                    double psy = psyRes.Value;

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

        // Method to add a node given a point and a layer name
        public static void AddNode(Point3d position, string layerName)
        {
            // Access the nodes in the model
            ObjectIdCollection nds = AllNodes();

            // Create a point collection and add the position of the nodes
            Point3dCollection ndPos = new Point3dCollection();

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ndObj in nds)
                {
                    // Read as a point and add to the collection
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;
                    ndPos.Add(nd.Position);
                }

                // Check if a node already exists at the position. If not, its created
                if (!ndPos.Contains(position))
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create the node in Model space
                    // Create the node and set its layer to Node:
                    DBPoint newNd = new DBPoint(position);
                    newNd.Layer = layerName;

                    // Add the new object to the block table record and the transaction
                    blkTblRec.AppendEntity(newNd);
                    trans.AddNewlyCreatedDBObject(newNd, true);

                    // Save the new object to the database and dispose the transaction
                    trans.Commit();
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

                    // Initialize the node conditions
                    double ndNum = 0,                                  // Node number (to be set later)
                           xPosition = Math.Round(nd.Position.X, 2),   // X position
                           yPosition = Math.Round(nd.Position.Y, 2);   // Y position
                    string support = "Free";                           // Support condition
                    double xForce = 0,                                 // Force on X direction
                           yForce = 0,                                 // Force on Y direction
                           ux = 0,                                     // Displacment on X direction
                           uy = 0;                                     // Displacment on X direction

                    // If the Extended data does not exist, create it
                    if (nd.XData == null)
                    {
                        // Define the Xdata to add to the node
                        using (ResultBuffer defRb = new ResultBuffer())
                        {
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));  // 0
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ndNum));                  // 2
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xPosition));              // 3
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yPosition));              // 4
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, support));         // 5
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xForce));                 // 6
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yForce));                 // 7
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ux));                     // 8
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, uy));                     // 9

                            // Append the extended data to each object
                            nd.XData = defRb;
                        }
                    }

                    // Get the node number on the list
                    ndNum = ndList.IndexOf(nd.Position) + 1;

                    // Get the result buffer as an array
                    ResultBuffer rb = nd.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] data = rb.AsArray();

                    // Set the new node number (line 2)
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNum);

                    // Set the updated coordinates (in case of a node copy)
                    data[3] = new TypedValue((int)DxfCode.ExtendedDataReal, xPosition);
                    data[4] = new TypedValue((int)DxfCode.ExtendedDataReal, yPosition);

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    nd.XData = newRb;
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
                Point3dCollection midPts = new Point3dCollection();

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

                // Access the nodes on the document
                foreach (ObjectId strObj in strs)
                {
                    // Open the selected object as a line for write
                    Line str = trans.GetObject(strObj, OpenMode.ForWrite) as Line;

                    // Initialize the variables
                    int strStNd = 0,                             // Start node
                        strMidNd = 0,                            // Mid node
                        strEnNd = 0;                             // End node

                    // Inicialization of stringer conditions
                    double strNum = 0,                           // Stringer number (initially unassigned)
                           strLgt = Math.Round(str.Length, 2),   // Stringer lenght
                           strW = 1,                             // Width
                           strH = 1,                             // Height
                           As = 0;                               // Reinforcement Area

                    // If XData does not exist, create it
                    if (str.XData == null)
                    {

                        // Define the Xdata to add to the node
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));   // 0
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strNum));                 // 2
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strStNd));                // 3
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strMidNd));               // 4
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strEnNd));                // 5
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strLgt));                 // 6
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strW));                   // 7
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strH));                   // 8
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, As));                     // 9

                            // Open the stringer for write
                            Entity ent = trans.GetObject(str.ObjectId, OpenMode.ForWrite) as Entity;

                            // Append the extended data to each object
                            ent.XData = rb;
                        }
                    }

                    // Get the coordinates of the midpoint of the stringer
                    Point3d midPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);

                    // Get the stringer number
                    strNum = midPtsList.IndexOf(midPt) + 1;

                    // Get the start, mid and end nodes
                    strStNd = GetNodeNumber(str.StartPoint, nds);
                    strMidNd = GetNodeNumber(midPt, nds);
                    strEnNd = GetNodeNumber(str.EndPoint, nds);

                    // Access the XData as an array
                    ResultBuffer strRb = str.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] data = strRb.AsArray();

                    // Set the updated number and nodes in ascending number and length (line 2 to 6)
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, strNum);
                    data[3] = new TypedValue((int)DxfCode.ExtendedDataReal, strStNd);
                    data[4] = new TypedValue((int)DxfCode.ExtendedDataReal, strMidNd);
                    data[5] = new TypedValue((int)DxfCode.ExtendedDataReal, strEnNd);
                    data[6] = new TypedValue((int)DxfCode.ExtendedDataReal, str.Length);

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    str.XData = newRb;
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
            Point3dCollection cntrPts = new Point3dCollection();

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

                    // Initialize the panel parameters
                    double pnlNum = 0;                            // Panel number (initially unassigned)
                    int[] dofs = { 0, 0, 0, 0 };                  // Panel DoFs (initially unassigned)
                    double pnlW = 1;                              // width
                    double psx = 0;                               // reinforcement ratio (X)
                    double psy = 0;                               // reinforcement ratio (Y)

                    // Check if the XData already exist. If not, create it
                    if (pnl.XData == null)
                    {
                        // Initialize a Result Buffer to add to the panel
                        ResultBuffer rb = new ResultBuffer();
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));   // 0
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum));                 // 2
                        for (int i = 0; i < 4; i++)
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, dofs[i]));            // 3, 4, 5, 6
                        }
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlW));                   // 7
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psx));                    // 8
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psy));                    // 9

                        // Append the extended data to the object
                        pnl.XData = rb;
                    }

                    // Read it as a block and get the draw order table
                    BlockTableRecord blck = trans.GetObject(pnl.BlockId, OpenMode.ForRead) as BlockTableRecord;
                    DrawOrderTable drawOrder = trans.GetObject(blck.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the approximate coordinates of the center point of the panel
                    Point3d cntrPt = Auxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);

                    // Get the coordinates of the panel DoFs in the necessary order
                    Point3dCollection pnlDofs = new Point3dCollection();
                    pnlDofs.Add(Auxiliary.MidPoint(pnlVerts[0], pnlVerts[1]));
                    pnlDofs.Add(Auxiliary.MidPoint(pnlVerts[1], pnlVerts[3]));
                    pnlDofs.Add(Auxiliary.MidPoint(pnlVerts[3], pnlVerts[2]));
                    pnlDofs.Add(Auxiliary.MidPoint(pnlVerts[2], pnlVerts[0]));

                    // Get the panel number
                    pnlNum = cntrPtsList.IndexOf(cntrPt) + 1;

                    // Compare the node position to the panel vertices
                    foreach (Point3d dof in pnlDofs)
                    {
                        // Get the position of the vertex in the array
                        int i = pnlDofs.IndexOf(dof);

                        // Get the node number
                        dofs[i] = GetNodeNumber(dof, intNds);
                    }

                    // Access the XData as an array
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] data = pnlRb.AsArray();

                    // Set the updated panel number (line 2)
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum);

                    // Set the updated node numbers in the necessary order (line 3 to 6 of the array)
                    for (int i = 3; i <= 6; i++)
                    {
                        data[i] = new TypedValue((int)DxfCode.ExtendedDataReal, dofs[i - 3]);
                    }

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    pnl.XData = newRb;

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
            if (nodeType == "All") nds = AllNodes();
            if (nodeType == "Int") nds = Auxiliary.GetEntitiesOnLayer(Layers.intNdLyr);
            if (nodeType == "Ext") nds = Auxiliary.GetEntitiesOnLayer(Layers.extNdLyr);

            // Create a point collection
            Point3dCollection ndPos = new Point3dCollection();

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
                        ndNum = Convert.ToInt32(dataNd[2].Value);
                    }
                }
            }

            return ndNum;
        }
    }
}