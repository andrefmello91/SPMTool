using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

namespace SPMTool
{
    // Global variables
    public static class Global
    {
        // Get the current document, database and editor
        public static Document curDoc = Application.DocumentManager.MdiActiveDocument;
        public static Database curDb = curDoc.Database;
        public static Editor ed = curDoc.Editor;

        // Define the appName
        public static string appName = "SPMTool";

        // Constants
        public static double pi = MathNet.Numerics.Constants.Pi;
        public static double piOver2 = MathNet.Numerics.Constants.PiOver2;
        public static double pi3Over2 = MathNet.Numerics.Constants.Pi3Over2;
    }

    // Auxiliary Methods
    public class AuxMethods
    {
        // Add the app to the Registered Applications Record
        public static void RegisterApp()
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Registered Applications table for read
                RegAppTable regAppTbl = trans.GetObject(Global.curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                if (!regAppTbl.Has(Global.appName))
                {
                    using (RegAppTableRecord regAppTblRec = new RegAppTableRecord())
                    {
                        regAppTblRec.Name = Global.appName;
                        trans.GetObject(Global.curDb.RegAppTableId, OpenMode.ForWrite);
                        regAppTbl.Add(regAppTblRec);
                        trans.AddNewlyCreatedDBObject(regAppTblRec, true);
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
            }
        }

        // Method to create a layer given a name, a color and transparency
        public static void CreateLayer(string layerName, short layerColor, int layerTransp)
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(Global.curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

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
                        trans.GetObject(Global.curDb.LayerTableId, OpenMode.ForWrite);

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
            }
        }

        // Method to create the support blocks
        public static void CreateSupportBlocks()
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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
            }
        }

        // Method to create the force block
        public static void CreateForceBlock()
        {
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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
            }
        }

        // This method select all objects on a determined layer
        public static ObjectIdCollection GetEntitiesOnLayer(string layerName)
        {
            // Build a filter list so that only entities on the specified layer are selected
            TypedValue[] tvs = new TypedValue[1]
            {
                new TypedValue((int)DxfCode.LayerName, layerName)
            };

            SelectionFilter selFt = new SelectionFilter(tvs);

            // Get the entities on the layername
            PromptSelectionResult selRes = Global.ed.SelectAll(selFt);

            if (selRes.Status == PromptStatus.OK)
            {
                return new ObjectIdCollection(selRes.Value.GetObjectIds());
            }
            else
            {
                return new ObjectIdCollection();
            }
        }

        // This method calculates the midpoint between two points
        public static Point3d MidPoint(Point3d point1, Point3d point2)
        {
            // Get the coordinates of the Midpoint
            double x = (point1.X + point2.X) / 2;
            double y = (point1.Y + point2.Y) / 2;
            double z = (point1.Z + point2.Z) / 2;

            // Create the point
            Point3d midPoint = new Point3d(x, y, z);
            return midPoint;
        }

        // This method order the elements in a collection in ascending yCoord, then ascending xCoord, returns the array of points ordered
        public static List<Point3d> OrderPoints(Point3dCollection points)
        {
            // Initialize the point list
            List<Point3d> ptList = new List<Point3d>();

            // Add the point collection to the list
            foreach (Point3d pt in points) ptList.Add(pt);

            // Order the point list
            ptList = ptList.OrderBy(pt => pt.Y).ThenBy(pt => pt.X).ToList();

            // Return the point list
            return ptList;
        }

        // Enumerate all the nodes in the model and return the number of nodes
        public static int UpdateNodes()
        {
            // Definition for the Extended Data
            string xdataStr = "Node Data";

            // Initialize the number of nodes
            int numNds = 0;

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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
                List<Point3d> ndList = OrderPoints(pts);

                // Access the nodes on the document
                foreach (ObjectId obj in nds)
                {
                    // Read the object as a point
                    DBPoint nd = trans.GetObject(obj, OpenMode.ForWrite) as DBPoint;

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
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, nodeNum));                // 2
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xPosition));              // 3
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yPosition));              // 4
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, support));         // 5
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xForce));                 // 6
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yForce));                 // 7

                            // Open the node for write
                            Entity ent = trans.GetObject(nd.ObjectId, OpenMode.ForWrite) as Entity;

                            // Append the extended data to each object
                            ent.XData = defRb;
                        }
                    }

                    // Get the node number on the list
                    double ndNum = ndList.IndexOf(nd.Position) + 1;

                    // Get the result buffer as an array
                    ResultBuffer rb = nd.GetXDataForApplication(Global.appName);
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
                Global.curDb.Pdmode = 32;
                Global.curDb.Pdsize = 50;

                // Commit and dispose the transaction
                trans.Commit();
            }

            // Return the number of nodes
            return numNds;
        }

        // Update the node numbers on the XData of each stringer in the model and return the number of stringers
        public static int UpdateStringers()
        {
            // Definition for the Extended Data
            string xdataStr = "Stringer Data";

            // Initialize the number of stringers
            int numStrs = 0;

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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

                    // Get the midpoint and add to the collection
                    Point3d midPt = MidPoint(str.StartPoint, str.EndPoint);
                    midPts.Add(midPt);
                }

                // Get the array of midpoints ordered
                List<Point3d> midPtsList = OrderPoints(midPts);

                // Access the nodes on the document
                foreach (ObjectId strObj in strs)
                {
                    // Initialize the variables
                    double strStNd = 0;
                    double strEnNd = 0;

                    // Open the selected object as a line for write
                    Line str = trans.GetObject(strObj, OpenMode.ForWrite) as Line;

                    // If XData does not exist, create it
                    if (str.XData == null)
                    {
                        // Inicialization of stringer conditions
                        double strNumb = 0;                       // Stringer number (initially unassigned)
                        double strSrtNd = 0;                      // Stringer start node (initially unassigned)
                        double strEndNd = 0;                      // Stringer end node (initially unassigned)
                        double strLgt = str.Length;               // Stringer lenght
                        double strW = 1;                          // Width
                        double strH = 1;                          // Height
                        double As = 0;                            // Reinforcement Area
                        double kl = 0;                            // Local stiffness matrix
                        double k = 0;                             // Transformated stiffness matrix

                        // Define the Xdata to add to the node
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strNumb));                // 2
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strSrtNd));               // 3
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strEndNd));               // 4
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strLgt));                 // 5
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strW));                   // 6
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, strH));                   // 7
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, As));                     // 8
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, kl));                     // 9
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, k));                      // 10 

                            // Open the stringer for write
                            Entity ent = trans.GetObject(str.ObjectId, OpenMode.ForWrite) as Entity;

                            // Append the extended data to each object
                            ent.XData = rb;
                        }
                    }

                    // Get the coordinates of the midpoint of the stringer
                    Point3d midPt = MidPoint(str.StartPoint, str.EndPoint);

                    // Get the stringer number
                    double strNum = midPtsList.IndexOf(midPt) + 1;

                    // Compare to the nodes collection
                    foreach (ObjectId ndObj in nds)
                    {
                        // Open the selected object as a point for read
                        DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                        // Get the start and end nodes
                        // Compare the start node
                        if (str.StartPoint == nd.Position)
                        {
                            // Get the node number
                            // Access the XData as an array
                            ResultBuffer ndRb = nd.GetXDataForApplication(Global.appName);
                            TypedValue[] dataNd = ndRb.AsArray();

                            // Get the node number (line 2)
                            strStNd = Convert.ToDouble(dataNd[2].Value);
                        }

                        // Compare the end node
                        if (str.EndPoint == nd.Position)
                        {
                            // Get the node number
                            // Access the XData as an array
                            ResultBuffer ndRb = nd.GetXDataForApplication(Global.appName);
                            TypedValue[] dataNd = ndRb.AsArray();

                            // Get the node number (line 2)
                            strEnNd = Convert.ToDouble(dataNd[2].Value);
                        }
                    }

                    // Access the XData as an array
                    ResultBuffer strRb = str.GetXDataForApplication(Global.appName);
                    TypedValue[] data = strRb.AsArray();

                    // Set the updated number and nodes in ascending number (line 2 and 3 of the array) and length
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, strNum);
                    data[3] = new TypedValue((int)DxfCode.ExtendedDataReal, strStNd);
                    data[4] = new TypedValue((int)DxfCode.ExtendedDataReal, strEnNd);
                    data[5] = new TypedValue((int)DxfCode.ExtendedDataReal, str.Length);

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    str.XData = newRb;
                }

                // Commit and dispose the transaction
                trans.Commit();
            }

            // Return the number of stringers
            return numStrs;
        }

        // Update the node numbers on the XData of each panel in the model and return the number of panels
        public static int UpdatePanels()
        {
            // Definition for the Extended Data
            string xdataStr = "Panel Data";

            // Initialize the number of panels
            int numPnls = 0;

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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

                    // Get the approximate coordinates of the center point of the panel
                    Point3d cntrPt = MidPoint(pnlVerts[0], pnlVerts[3]);
                    cntrPts.Add(cntrPt);
                }

                // Get the list of center points ordered
                List<Point3d> cntrPtsList = OrderPoints(cntrPts);

                // Access the panels on the document
                foreach (ObjectId pnlObj in pnls)
                {
                    // Open the selected object as a solid for write
                    Solid pnl = trans.GetObject(pnlObj, OpenMode.ForWrite) as Solid;

                    // Check if the XData already exist. If not, create it
                    if (pnl.XData == null)
                    {
                        // Initialize the panel parameters
                        double pnlN = 0;                              // Panel number (initially unassigned)
                        double[] verts = { 0, 0, 0, 0 };              // Panel vertices (initially unassigned)
                        double pnlW = 1;                              // width
                        double psx = 0;                               // reinforcement ratio (X)
                        double psy = 0;                               // reinforcement ratio (Y)
                        string pnlK = "";                             // stifness matrix

                        // Initialize a Result Buffer to add to the panel
                        ResultBuffer rb = new ResultBuffer();
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr)); // 1
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlN));            // 2
                        for (int j = 0; j < 4; j++)
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, verts[j]));    // 3, 4, 5, 6
                        }
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, pnlW));            // 7
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psx));             // 8
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, psy));             // 9
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, pnlK));     // 10

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
                    Point3d cntrPt = MidPoint(pnlVerts[0], pnlVerts[3]);

                    // Get the panel number
                    double pnlNum = cntrPtsList.IndexOf(cntrPt) + 1;

                    // Initialize the array of node numbers
                    int[] ndNums = { 0, 0, 0, 0 };

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
                                ResultBuffer ndRb = nd.GetXDataForApplication(Global.appName);
                                TypedValue[] dataNd = ndRb.AsArray();

                                // Get the position of the vertex in the array
                                int i = pnlVerts.IndexOf(vert);

                                // Get the node number (line 2) and assign it to the node array in the position of the vertex
                                ndNums[i] = Convert.ToInt32(dataNd[2].Value);
                            }
                        }
                    }

                    // Access the XData as an array
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(Global.appName);
                    TypedValue[] data = pnlRb.AsArray();

                    // Set the updated panel number (line 2)
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum);

                    // Set the updated node numbers in the necessary order (line 3 to 6 of the array)
                    data[3] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNums[0]);
                    data[4] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNums[1]);
                    data[5] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNums[3]);
                    data[6] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNums[2]);

                    // Add the new XData
                    ResultBuffer newRb = new ResultBuffer(data);
                    pnl.XData = newRb;

                    // Move the panels to bottom
                    drawOrder.MoveToBottom(pnls);
                }

                // Commit and dispose the transaction
                trans.Commit();
            }

            // Return the number of panels
            return numPnls;
        }

        // Generate the degrees of freedom of the model
        //public void GenerateDoFs()
        //{
        //    // Create the layer
        //    CreateLayer("DoF", 5, 0);

        //    // Get the nodes and stringers collections
        //    ObjectIdCollection nds = GetEntitiesOnLayer("Node");
        //    ObjectIdCollection strs = GetEntitiesOnLayer("Stringer");
        //    ObjectIdCollection dofs = GetEntitiesOnLayer("DoF");

        //    // Create a point3D collection for the position of DoFs
        //    Point3dCollection dofPts = new Point3dCollection();

        //    // Start a transaction
        //    using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
        //    {
        //        // Open the Block table for read
        //        BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

        //        // Open the Block table record Model space for write
        //        BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

        //        // Erase the DoFs created previously
        //        if (dofs.Count > 0)
        //        {
        //            foreach (ObjectId dof in dofs)
        //            {
        //                // Get the entity and erase it
        //                Entity ent = trans.GetObject(dof, OpenMode.ForWrite) as Entity;
        //                ent.Erase();
        //            }
        //        }

        //        // Get the nodes positions
        //        foreach (ObjectId ndObj in nds)
        //        {
        //            // Read as a point and add to the collection
        //            DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;
        //            dofPts.Add(nd.Position);
        //        }

        //        // Get the stringers midpoints
        //        foreach (ObjectId strObj in strs)
        //        {
        //            // Read as a line and get the midpoint
        //            Line str = trans.GetObject(strObj, OpenMode.ForRead) as Line;
        //            Point3d midPt = MidPoint(str.StartPoint, str.EndPoint);
        //            dofPts.Add(midPt);
        //        }

        //        // Create the DoFs
        //        foreach (Point3d dofPt in dofPts)
        //        {
        //            DBPoint dof = new DBPoint(dofPt);

        //            // Set the layer
        //            dof.Layer = "DoF";

        //            // Add the new object to the block table record and the transaction
        //            blkTblRec.AppendEntity(dof);
        //            trans.AddNewlyCreatedDBObject(dof, true);
        //        }

        //        // Save the new object to the database and dispose the transaction
        //        trans.Commit();
        //    }
        //}

        // Enumerate all the nodes in the model and return the number of nodes
        //public static int UpdateDoFs()
        //{
        //    // Definition for the Extended Data
        //    string xdataStr = "DoF Data";

        //    // Initialize the number of DoFs
        //    int numDofs = 0;

        //    // Start a transaction
        //    using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
        //    {
        //        // Open the Block table for read
        //        BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

        //        // Create the nodes collection and initialize getting the elements on node layer
        //        ObjectIdCollection dofs = GetEntitiesOnLayer("DoF");

        //        // Create a point collection
        //        Point3dCollection pts = new Point3dCollection();

        //        // Get the number of Dofs
        //        numDofs = dofs.Count;

        //        // Add each point to the collection
        //        foreach (ObjectId obj in dofs)
        //        {
        //            // Read the object as a point
        //            DBPoint dof = trans.GetObject(obj, OpenMode.ForRead) as DBPoint;
        //            pts.Add(dof.Position);
        //        }

        //        // Get the array of points ordered
        //        List<Point3d> dofList = OrderPoints(pts);

        //        // Access the dofs on the document
        //        foreach (ObjectId obj in dofs)
        //        {
        //            // Read the object as a point
        //            DBPoint dof = trans.GetObject(obj, OpenMode.ForWrite) as DBPoint;

        //            // If the Extended data does not exist, create it
        //            if (dof.XData == null)
        //            {
        //                // Inicialization of node conditions
        //                double dofNum = 0;                                  // Dof number (to be set later)
        //                double xPosition = dof.Position.X;                  // X position
        //                double yPosition = dof.Position.Y;                  // Y position
        //                double xDisp = 1;                                   // Displacement in X (0 if there is a support)
        //                double yDisp = 1;                                   // Displacement in X (0 if there is a support)

        //                // Define the Xdata to add to the node
        //                using (ResultBuffer defRb = new ResultBuffer())
        //                {
        //                    defRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
        //                    defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
        //                    defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, dofNum));                 // 2
        //                    defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xPosition));              // 3
        //                    defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yPosition));              // 4
        //                    defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xDisp));                  // 5
        //                    defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yDisp));                  // 6
                            
        //                    // Append the extended data to each object
        //                    dof.XData = defRb;
        //                }
        //            }

        //            // Get the Dof number on the list
        //            double dofNum = dofList.IndexOf(dof.Position) + 1;

        //            // Get the result buffer as an array
        //            ResultBuffer rb = dof.GetXDataForApplication(Global.appName);
        //            TypedValue[] data = rb.AsArray();

        //            // Set the new DoF number (line 2)
        //            data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, dofNum);

        //            // Set the updated coordinates
        //            data[3] = new TypedValue((int)DxfCode.ExtendedDataReal, dof.Position.X);
        //            data[4] = new TypedValue((int)DxfCode.ExtendedDataReal, dof.Position.Y);

        //            // Add the new XData
        //            ResultBuffer newRb = new ResultBuffer(data);
        //            dof.XData = newRb;
        //        }

        //        // Set the style for all point objects in the drawing
        //        Global.curDb.Pdmode = 32;
        //        Global.curDb.Pdsize = 50;

        //        // Commit and dispose the transaction
        //        trans.Commit();
        //    }

        //    // Return the number of nodes
        //    return numDofs;
        //}

    }
}
