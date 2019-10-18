﻿using System;
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

        // Get the coordinate system for transformations
        public static Matrix3d curUCSMatrix = Global.ed.CurrentUserCoordinateSystem;
        public static CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

        // Define the appName
        public static string appName = "SPMTool";

        // Layer names
        public static string extNdLyr = "ExtNode",
                             intNdLyr = "IntNode",
                             strLyr = "Stringer",
                             pnlLyr = "Panel",
                             supLyr = "Support",
                             fLyr = "Force",
                             fTxtLyr = "ForceText",
                             strFLyr = "StringerForces",
                             pnlFLyr = "PanelShear";

        // Block names
        public static string supportX = "SupportX",
                             supportY = "SupportY",
                             supportXY = "SupportXY",
                             forceBlock = "ForceBlock",
                             shearBlock = "ShearBlock";

        // Color codes
        public static short red    = 1,
                            yellow = 2,
                            cyan   = 4,
                            blue1  = 5,
                            blue   = 150,
                            green  = 92,
                            grey   = 254;

        // Constants
        public static double pi = MathNet.Numerics.Constants.Pi,
                             piOver2 = MathNet.Numerics.Constants.PiOver2,
                             pi3Over2 = MathNet.Numerics.Constants.Pi3Over2;
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

        // Method to assign transparency to an object
        public static Transparency Transparency(int transparency)
        {
            byte alpha = (byte)(255 * (100 - transparency) / 100);
            Transparency transp = new Transparency(alpha);
            return transp;
        }

        // Method to create a layer given a name, a color and transparency
        public static void CreateLayer(string layerName, short layerColor, int transparency)
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

                        // Upgrade the Layer table for write
                        trans.GetObject(Global.curDb.LayerTableId, OpenMode.ForWrite);

                        // Append the new layer to the Layer table and the transaction
                        lyrTbl.Add(lyrTblRec);
                        trans.AddNewlyCreatedDBObject(lyrTblRec, true);

                        // Assign the name and transparency to the layer
                        lyrTblRec.Name = layerName;
                        lyrTblRec.Transparency = Transparency(transparency);
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

        // Get the list of node positions ordered
        public static List<Point3d> ListOfNodes(string nodeType)
        {
            // Initialize an object collection
            ObjectIdCollection nds = new ObjectIdCollection();

            // Select the node type
            if (nodeType == "All") nds = AllNodes();
            if (nodeType == "Int") nds = GetEntitiesOnLayer(Global.intNdLyr);
            if (nodeType == "Ext") nds = GetEntitiesOnLayer(Global.extNdLyr);

            // Create a point collection
            Point3dCollection ndPos = new Point3dCollection();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ndObj in nds)
                {
                    // Read as a point and add to the collection
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;
                    ndPos.Add(nd.Position);
                }
            }

            // Return the node list ordered
            return OrderPoints(ndPos);
        }

        // Enumerate all the nodes in the model and return the collection of nodes
        public static ObjectIdCollection UpdateNodes()
        {
            // Definition for the Extended Data
            string xdataStr = "Node Data";

            // Get all the nodes in the model
            ObjectIdCollection nds = AllNodes();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Get the list of nodes ordered
                List<Point3d> ndList = ListOfNodes("All");

                // Access the nodes on the document
                foreach (ObjectId ndObj in nds)
                {
                    // Read the object as a point
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForWrite) as DBPoint;

                    // Initialize the node conditions
                    double ndNum = 0;                                // Node number (to be set later)
                    double xPosition = nd.Position.X;                  // X position
                    double yPosition = nd.Position.Y;                  // Y position
                    string support = "Free";                           // Support condition
                    double xForce = 0;                                 // Force on X direction
                    double yForce = 0;                                 // Force on Y direction

                    // If the Extended data does not exist, create it
                    if (nd.XData == null)
                    {
                        // Define the Xdata to add to the node
                        using (ResultBuffer defRb = new ResultBuffer())
                        {
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));        // 1
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, ndNum));                  // 2
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xPosition));              // 3
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yPosition));              // 4
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, support));         // 5
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, xForce));                 // 6
                            defRb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, yForce));                 // 7

                            // Append the extended data to each object
                            nd.XData = defRb;
                        }
                    }

                    // Get the node number on the list
                    ndNum = ndList.IndexOf(nd.Position) + 1;

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
                Global.curDb.Pdsize = 40;

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
            ObjectIdCollection strs = GetEntitiesOnLayer(Global.strLyr);

            // Get all the nodes in the model
            ObjectIdCollection nds = AllNodes();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Create a point collection
                Point3dCollection midPts = new Point3dCollection();

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
                    // Open the selected object as a line for write
                    Line str = trans.GetObject(strObj, OpenMode.ForWrite) as Line;

                    // Initialize the variables
                    int strStNd = 0,                             // Start node
                        strMidNd = 0,                            // Mid node
                        strEnNd = 0;                             // End node

                    // Inicialization of stringer conditions
                    double strNum = 0;                           // Stringer number (initially unassigned)
                    double strLgt = str.Length;                  // Stringer lenght
                    double strW = 1;                             // Width
                    double strH = 1;                             // Height
                    double As = 0;                               // Reinforcement Area

                    // If XData does not exist, create it
                    if (str.XData == null)
                    {

                        // Define the Xdata to add to the node
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
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
                    Point3d midPt = MidPoint(str.StartPoint, str.EndPoint);

                    // Get the stringer number
                    strNum = midPtsList.IndexOf(midPt) + 1;

                    // Get the start, mid and end nodes
                    strStNd = GetNodeNumber(str.StartPoint, nds);
                    strMidNd = GetNodeNumber(midPt, nds);
                    strEnNd = GetNodeNumber(str.EndPoint, nds);

                    // Access the XData as an array
                    ResultBuffer strRb = str.GetXDataForApplication(Global.appName);
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
            ObjectIdCollection intNds = GetEntitiesOnLayer(Global.intNdLyr);

            // Create the panels collection and initialize getting the elements on node layer
            ObjectIdCollection pnls = GetEntitiesOnLayer(Global.pnlLyr);

            // Create a point collection
            Point3dCollection cntrPts = new Point3dCollection();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));   // 0
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
                    Point3d cntrPt = MidPoint(pnlVerts[0], pnlVerts[3]);

                    // Get the coordinates of the panel DoFs in the necessary order
                    Point3dCollection pnlDofs = new Point3dCollection();
                    pnlDofs.Add(MidPoint(pnlVerts[0], pnlVerts[1]));
                    pnlDofs.Add(MidPoint(pnlVerts[1], pnlVerts[3]));
                    pnlDofs.Add(MidPoint(pnlVerts[3], pnlVerts[2]));
                    pnlDofs.Add(MidPoint(pnlVerts[2], pnlVerts[0]));

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
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(Global.appName);
                    TypedValue[] data = pnlRb.AsArray();

                    // Set the updated panel number (line 2)
                    data[2] = new TypedValue((int)DxfCode.ExtendedDataReal, pnlNum);

                    // Set the updated node numbers in the necessary order (line 3 to 6 of the array)
                    for (int i = 3; i <= 6; i++)
                    {
                        data[i] = new TypedValue((int)DxfCode.ExtendedDataReal, dofs[i-3]);
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

        // Get the collection of all of the nodes
        public static ObjectIdCollection AllNodes()
        {
            // Create the nodes collection and initialize getting the elements on node layer
            ObjectIdCollection extNds = GetEntitiesOnLayer(Global.extNdLyr);
            ObjectIdCollection intNds = GetEntitiesOnLayer(Global.intNdLyr);

            // Create a unique collection for all the nodes
            ObjectIdCollection nds = new ObjectIdCollection();
            foreach (ObjectId ndObj in extNds) nds.Add(ndObj);
            foreach (ObjectId ndObj in intNds) nds.Add(ndObj);

            return nds;
        }

        // Erase the objects in a collection
        public static void EraseObjects(ObjectIdCollection objects)
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                foreach (ObjectId obj in objects)
                {
                    // Read as entity
                    Entity ent = trans.GetObject(obj, OpenMode.ForWrite) as Entity;

                    // Erase the object
                    ent.Erase();
                }

                // Commit changes
                trans.Commit();
            }
        }

        // Get the node number at the position
        public static int GetNodeNumber(Point3d position, ObjectIdCollection nodes)
        {
            // Initiate the node number
            int ndNum = 0;

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
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
                        ResultBuffer ndRb = nd.GetXDataForApplication(Global.appName);
                        TypedValue[] dataNd = ndRb.AsArray();

                        // Get the node number (line 2)
                        ndNum = Convert.ToInt32(dataNd[2].Value);
                    }
                }
            }

            return ndNum;
        }

        // Get the direction cosines of a vector
        public static (double l, double m) DirectionCosines(double angle)
        {
            double l, m;
            // Calculate the cossine, return 0 if 90 or 270 degrees
            if (angle == Global.piOver2 || angle == Global.pi3Over2) l = 0;
            else l = MathNet.Numerics.Trig.Cos(angle);

            // Calculate the sine, return 0 if 0 or 180 degrees
            if (angle == 0 || angle == Global.pi) m = 0;
            else m = MathNet.Numerics.Trig.Sin(angle);

            return (l, m);
        }

        // Function to verify if a number is not zero
        public static Func<double, bool> NotZero = delegate (double num)
        {
            if (num != 0) return true;
            else return false;
        };

        // In case a support or force is erased
        //public static void BlockErased(object sender, ObjectEventArgs eventArgs)
        //{
        //    if (eventArgs.DBObject.IsErased)
        //    {
        //        // Read as a block reference
        //        BlockReference blkRef = eventArgs.DBObject as BlockReference;

        //        // Check if it's a support
        //        if (blkRef.Layer == Global.supLyr)
        //        {
        //            // Get the nodes collection
        //            ObjectIdCollection nds = AuxMethods.GetEntitiesOnLayer("Node");

        //            // Start a transaction
        //            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
        //            {
        //                // Update the support condition in the node XData
        //                foreach (ObjectId obj in nds)
        //                {
        //                    // Read as a point
        //                    DBPoint nd = trans.GetObject(obj, OpenMode.ForRead) as DBPoint;

        //                    if (nd.Position == blkRef.Position)
        //                    {
        //                        // Get the result buffer as an array
        //                        ResultBuffer rb = nd.GetXDataForApplication(Global.appName);
        //                        TypedValue[] data = rb.AsArray();

        //                        // Set the updated support condition (in case of a support block was erased)
        //                        string support = "Free";
        //                        data[5] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, support);

        //                        // Add the new XData
        //                        nd.UpgradeOpen();
        //                        ResultBuffer newRb = new ResultBuffer(data);
        //                        nd.XData = newRb;
        //                        break;
        //                    }
        //                }

        //                // Commit
        //                trans.Commit();
        //            }
        //        }
        //    }
        //}
    }
}
