using System;
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
                trans.Dispose();
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
                trans.Dispose();
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
                trans.Dispose();
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
                trans.Dispose();
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

        // This method returns an array of points enumerated (num, xCoord, yCoord)
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
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
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
                trans.Dispose();
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
                            ResultBuffer ndRb = nd.GetXDataForApplication(Global.appName);
                            TypedValue[] dataNd = ndRb.AsArray();

                            // Get the node number (line 2)
                            strStNd = Convert.ToDouble(dataNd[2].Value);
                        }

                        // Compare the end node
                        if (strEnPos == ndPos)
                        {
                            // Get the node number
                            // Access the XData as an array
                            ResultBuffer ndRb = nd.GetXDataForApplication(Global.appName);
                            TypedValue[] dataNd = ndRb.AsArray();

                            // Get the node number (line 2)
                            strEnNd = Convert.ToDouble(dataNd[2].Value);
                        }
                    }

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
                trans.Dispose();
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
                                ResultBuffer ndRb = nd.GetXDataForApplication(Global.appName);
                                TypedValue[] dataNd = ndRb.AsArray();

                                // Get the node number (line 2) and assign it to the node array
                                ndNums[i] = Convert.ToInt32(dataNd[2].Value);
                                i++;
                            }
                        }
                    }


                    //// Order the nodes array in ascending
                    //var ndOrd = ndNums.OrderBy(x => x);
                    //ndNums = ndOrd.ToArray();

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

                        // Append the extended data to the object
                        pnl.XData = rb;
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

                    //for (int k = 3; k <= 6; k++)
                    //{
                    //    data[k] = new TypedValue((int)DxfCode.ExtendedDataReal, ndNums[k-3]);
                    //}

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
