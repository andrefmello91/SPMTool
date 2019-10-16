﻿using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
    public class Drawing
    {
        public static void DrawStringerForces(ObjectIdCollection stringers, List<Tuple<int, Vector<double>>> stringerForces)
        {
            // Check if the layer already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(Global.strF, Global.grey, 0);

            // Erase all the stringer forces in the drawing
            ObjectIdCollection strFs = AuxMethods.GetEntitiesOnLayer(Global.strF);
            if (strFs.Count > 0) AuxMethods.EraseObjects(strFs);

            // Verify the maximum stringer force in the model to draw in an uniform scale
            List<double> maxForces = new List<double>();
            foreach (var strF in stringerForces) maxForces.Add(strF.Item2.AbsoluteMaximum());
            double fMax = maxForces.Max();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in stringers)
                {
                    // Read the object as a line
                    Line str = trans.GetObject(obj, OpenMode.ForWrite) as Line;

                    // Get the parameters of the stringer
                    double l   = str.Length,
                           ang = str.Angle;

                    // Read the XData and get the stringer number
                    ResultBuffer strRb = str.GetXDataForApplication(Global.appName);
                    TypedValue[] strData = strRb.AsArray();
                    int strNum = Convert.ToInt32(strData[2].Value);

                    // Get the forces in the list
                    var f = stringerForces[strNum - 1].Item2;
                    double f1 =   Math.Round(f[0], 2),
                           f3 = - Math.Round(f[2], 2);

                    // Check if at least one force is not zero
                    if (f1 != 0 || f3 != 0)
                    {
                        // Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
                        double h1 = 150 * f1 / fMax,
                               h3 = 150 * f3 / fMax;

                        // Open the Block table for read
                        BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                        // Open the Block table record Model space for write
                        BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        // Check if the forces are in the same direction
                        if (f1 * f3 >= 0) // same direction
                        {
                            // Calculate the points (the solid will be rotated later)
                            Point3d[] vrts = new Point3d[]
                            {
                            str.StartPoint,
                            new Point3d(str.StartPoint.X + l, str.StartPoint.Y,      0),
                            new Point3d(str.StartPoint.X,     str.StartPoint.Y + h1, 0),
                            new Point3d(str.StartPoint.X + l, str.StartPoint.Y + h3, 0),
                            };

                            // Create the diagram as a solid with 4 segments (4 points)
                            using (Solid dgrm = new Solid(vrts[0], vrts[1], vrts[2], vrts[3]))
                            {
                                // Set the layer and transparency
                                dgrm.Layer = Global.strF;
                                dgrm.Transparency = AuxMethods.Transparency(80);

                                // Set the color (blue to compression and red to tension)
                                if (Math.Max(f1, f3) > 0) dgrm.ColorIndex = Global.blue1;
                                else dgrm.ColorIndex = Global.red;

                                // Add the diagram to the drawing
                                blkTblRec.AppendEntity(dgrm);
                                trans.AddNewlyCreatedDBObject(dgrm, true);

                                // Rotate the diagram
                                dgrm.TransformBy(Matrix3d.Rotation(ang, Global.curUCS.Zaxis, str.StartPoint));
                            }
                        }

                        else // forces are in diferent directions
                        {
                            // Calculate the point where the stringer force will be zero
                            double x = Math.Abs(h1) * l / (Math.Abs(h1) + Math.Abs(h3));
                            Point3d invPt = new Point3d(str.StartPoint.X + x, str.StartPoint.Y, 0);

                            // Calculate the points (the solid will be rotated later)
                            Point3d[] vrts1 = new Point3d[]
                            {
                                str.StartPoint,
                                invPt,
                                new Point3d(str.StartPoint.X,     str.StartPoint.Y + h1, 0),
                            };

                            Point3d[] vrts3 = new Point3d[]
                            {
                                invPt,
                                new Point3d(str.StartPoint.X + l, str.StartPoint.Y,      0),
                                new Point3d(str.StartPoint.X + l, str.StartPoint.Y + h3, 0),
                            };

                            // Create the diagrams as solids with 3 segments (3 points)
                            using (Solid dgrm1 = new Solid(vrts1[0], vrts1[1], vrts1[2]))
                            {
                                // Set the layer and transparency
                                dgrm1.Layer = Global.strF;
                                dgrm1.Transparency = AuxMethods.Transparency(80);

                                // Set the color (blue to compression and red to tension)
                                if (f1 > 0) dgrm1.ColorIndex = Global.blue1;
                                else dgrm1.ColorIndex = Global.red;

                                // Add the diagram to the drawing
                                blkTblRec.AppendEntity(dgrm1);
                                trans.AddNewlyCreatedDBObject(dgrm1, true);

                                // Rotate the diagram
                                dgrm1.TransformBy(Matrix3d.Rotation(ang, Global.curUCS.Zaxis, str.StartPoint));
                            }

                            using (Solid dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2]))
                            {
                                // Set the layer and transparency
                                dgrm3.Layer = Global.strF;
                                dgrm3.Transparency = AuxMethods.Transparency(80);

                                // Set the color (blue to compression and red to tension)
                                if (f3 > 0) dgrm3.ColorIndex = Global.blue1;
                                else dgrm3.ColorIndex = Global.red;

                                // Add the diagram to the drawing
                                blkTblRec.AppendEntity(dgrm3);
                                trans.AddNewlyCreatedDBObject(dgrm3, true);

                                // Rotate the diagram
                                dgrm3.TransformBy(Matrix3d.Rotation(ang, Global.curUCS.Zaxis, str.StartPoint));
                            }
                        }

                        // Create the texts if forces are not zero
                        if (f1 != 0)
                        {
                            using (DBText txt1 = new DBText())
                            {
                                // Set the parameters
                                txt1.Layer = Global.strF;
                                txt1.Height = 30;
                                txt1.TextString = Math.Abs(f1).ToString();

                                // Set the color (blue to compression and red to tension) and position
                                if (f1 > 0)
                                {
                                    txt1.ColorIndex = Global.blue1;
                                    txt1.Position = new Point3d(str.StartPoint.X + 20, str.StartPoint.Y + h1 + 20, 0);
                                }
                                else
                                {
                                    txt1.ColorIndex = Global.red;
                                    txt1.Position = new Point3d(str.StartPoint.X + 20, str.StartPoint.Y + h1 - 50, 0);
                                }

                                // Add the text to the drawing
                                blkTblRec.AppendEntity(txt1);
                                trans.AddNewlyCreatedDBObject(txt1, true);

                                // Rotate the text
                                txt1.TransformBy(Matrix3d.Rotation(ang, Global.curUCS.Zaxis, str.StartPoint));
                            }
                        }

                        if (f3 != 0)
                        {
                            using (DBText txt3 = new DBText())
                            {
                                // Set the parameters
                                txt3.Layer = Global.strF;
                                txt3.Height = 30;
                                txt3.TextString = Math.Abs(f3).ToString();

                                // Set the color (blue to compression and red to tension) and position
                                if (f3 > 0)
                                {
                                    txt3.ColorIndex = Global.blue1;
                                    txt3.Position = new Point3d(str.StartPoint.X + l - 100, str.StartPoint.Y + h3 + 20, 0);
                                }
                                else
                                {
                                    txt3.ColorIndex = Global.red;
                                    txt3.Position = new Point3d(str.StartPoint.X + l - 100, str.StartPoint.Y + h3 - 50, 0);
                                }

                                // Add the text to the drawing
                                blkTblRec.AppendEntity(txt3);
                                trans.AddNewlyCreatedDBObject(txt3, true);

                                // Rotate the text
                                txt3.TransformBy(Matrix3d.Rotation(ang, Global.curUCS.Zaxis, str.StartPoint));
                            }
                        }
                    }
                }

                // Save the new objects to the database
                trans.Commit();
            }
        }
    }
}
