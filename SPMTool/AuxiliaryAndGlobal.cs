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

        // Get the direction cosines of a vector
        public static (double l, double m) DirectionCosines(double angle)
        {
            double l, m;
            // Calculate the cosine, return 0 if 90 or 270 degrees
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
