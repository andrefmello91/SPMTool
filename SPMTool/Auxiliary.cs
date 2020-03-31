﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
    // Auxiliary Methods
    public static class Auxiliary
    {
        // Add the app to the Registered Applications Record
        public static void RegisterApp()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Registered Applications table for read
                RegAppTable regAppTbl = trans.GetObject(AutoCAD.curDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                if (!regAppTbl.Has(AutoCAD.appName))
                {
                    using (RegAppTableRecord regAppTblRec = new RegAppTableRecord())
                    {
                        regAppTblRec.Name = AutoCAD.appName;
                        trans.GetObject(AutoCAD.curDb.RegAppTableId, OpenMode.ForWrite);
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
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(AutoCAD.curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lyrTbl.Has(layerName))
                {
                    lyrTbl.UpgradeOpen();
                    using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color and a name
                        lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, layerColor);

                        // Upgrade the Layer table for write
                        trans.GetObject(AutoCAD.curDb.LayerTableId, OpenMode.ForWrite);

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

        // Method to toogle view of a layer (on and off)
        public static void ToogleLayer(string layerName)
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(AutoCAD.curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (lyrTbl.Has(layerName))
                {
                    using (LayerTableRecord lyrTblRec = trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
                    {
                        // Verify the state
                        if (!lyrTblRec.IsOff)
                        {
                            lyrTblRec.IsOff = true;   // Turn it off
                        }
                        else
                        {
                            lyrTblRec.IsOff = false;  // Turn it on
                        }
                    }

                    // Commit and dispose the transaction
                    trans.Commit();
                }
            }
        }

        // Method to turn a layer Off
        public static void LayerOff(string layerName)
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(AutoCAD.curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (lyrTbl.Has(layerName))
                {
                    using (LayerTableRecord lyrTblRec = trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
                    {
                        // Verify the state
                        if (!lyrTblRec.IsOff)
                        {
                            lyrTblRec.IsOff = true;   // Turn it off
                        }
                    }

                    // Commit and dispose the transaction
                    trans.Commit();
                }
            }
        }

        // Method to turn a layer On
        public static void LayerOn(string layerName)
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable lyrTbl = trans.GetObject(AutoCAD.curDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (lyrTbl.Has(layerName))
                {
                    using (LayerTableRecord lyrTblRec = trans.GetObject(lyrTbl[layerName], OpenMode.ForWrite) as LayerTableRecord)
                    {
                        // Verify the state
                        if (lyrTblRec.IsOff)
                        {
                            lyrTblRec.IsOff = false;   // Turn it on
                        }
                    }

                    // Commit and dispose the transaction
                    trans.Commit();
                }
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
            PromptSelectionResult selRes = AutoCAD.edtr.SelectAll(selFt);

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
        public static List<Point3d> OrderPoints(List<Point3d> points)
        {
            // Order the point list
            points = points.OrderBy(pt => pt.Y).ThenBy(pt => pt.X).ToList();

            // Return the point list
            return points;
        }

        // Add objects to drawing
        public static void AddObject(Entity entity)
        {
            if (entity != null)
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Add the object to the drawing
                    blkTblRec.AppendEntity(entity);
                    trans.AddNewlyCreatedDBObject(entity, true);

                    // Commit changes
                    trans.Commit();
                }
            }
        }

        // Erase the objects in a collection
        public static void EraseObjects(ObjectIdCollection objects)
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
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

		// Get global indexes of a node
		public static int[] GlobalIndexes(int nodeNumber)
		{
			return
				new []
				{
					2 * nodeNumber - 2, 2 * nodeNumber - 1
				};
		}

		// Get global indexes of an element's grips
		public static int[] GlobalIndexes(int[] gripNumbers)
		{
			// Initialize the array
			int[] ind = new int[2 * gripNumbers.Length];

			// Get the indexes
			for (int i = 0; i < gripNumbers.Length; i++)
			{
				int j = 2 * i;

				ind[j]     = 2 * gripNumbers[i] - 2;
				ind[j + 1] = 2 * gripNumbers[i] - 1;
			}

			return ind;
		}

        // Get the direction cosines of a vector
        public static (double cos, double sin) DirectionCosines(double angle)
        {
            double 
                cos = Trig.Cos(angle).CoerceZero(1E-6), 
                sin = Trig.Sin(angle).CoerceZero(1E-6);

            return (cos, sin);
        }

        public static double Tangent(double angle)
        {
	        double tan;

	        // Calculate the tangent, return 0 if 90 or 270 degrees
	        if (angle == Constants.PiOver2 || angle == Constants.Pi3Over2)
		        tan = 1.633e16;

	        else
		        tan = Trig.Cos(angle).CoerceZero(1E-6);

	        return tan;
        }

        // Function to verify if a number is not zero
        public static Func<double, bool> NotZero = delegate (double num)
        {
            if (num != 0) 
                return true;
            else 
                return false;
        };

    }
}
