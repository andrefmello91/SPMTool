using System;
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
    public class Results
    {
        public static void DrawStringerForces(ObjectIdCollection stringers, List<Tuple<int, Vector<double>>> stringerForces)
        {
            // Check if the layer already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(Global.strFLyr, Global.grey, 0);

            // Erase all the stringer forces in the drawing
            ObjectIdCollection strFs = AuxMethods.GetEntitiesOnLayer(Global.strFLyr);
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
                                dgrm.Layer = Global.strFLyr;
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
                                dgrm1.Layer = Global.strFLyr;
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
                                dgrm3.Layer = Global.strFLyr;
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
                                txt1.Layer = Global.strFLyr;
                                txt1.Height = 30;
                                txt1.TextString = Math.Abs(f1).ToString();

                                // Set the color (blue to compression and red to tension) and position
                                if (f1 > 0)
                                {
                                    txt1.ColorIndex = Global.blue1;
                                    txt1.Position = new Point3d(str.StartPoint.X + 10, str.StartPoint.Y + h1 + 20, 0);
                                }
                                else
                                {
                                    txt1.ColorIndex = Global.red;
                                    txt1.Position = new Point3d(str.StartPoint.X + 10, str.StartPoint.Y + h1 - 50, 0);
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
                                txt3.Layer = Global.strFLyr;
                                txt3.Height = 30;
                                txt3.TextString = Math.Abs(f3).ToString();

                                // Set the color (blue to compression and red to tension) and position
                                if (f3 > 0)
                                {
                                    txt3.ColorIndex = Global.blue1;
                                    txt3.Position = new Point3d(str.StartPoint.X + l - 10, str.StartPoint.Y + h3 + 20, 0);
                                }
                                else
                                {
                                    txt3.ColorIndex = Global.red;
                                    txt3.Position = new Point3d(str.StartPoint.X + l - 10, str.StartPoint.Y + h3 - 50, 0);
                                }

                                // Adjust the alignment
                                txt3.HorizontalMode = TextHorizontalMode.TextRight;
                                txt3.AlignmentPoint = txt3.Position;

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

        public static void DrawPanelForces(ObjectIdCollection panels, List<Tuple<int, Vector<double>>> panelForces)
        {
            // Check if the layer already exists in the drawing. If it doesn't, then it's created:
            AuxMethods.CreateLayer(Global.pnlFLyr, Global.green, 0);

            // Check if the shear blocks already exist. If not, create the blocks
            CreatePanelShearBlock();

            // Erase all the panel forces in the drawing
            ObjectIdCollection pnlFs = AuxMethods.GetEntitiesOnLayer(Global.pnlFLyr);
            if (pnlFs.Count > 0) AuxMethods.EraseObjects(pnlFs);

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord blkTblRec = trans.GetObject(Global.curDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                // Read the object Ids of the support blocks
                ObjectId shearBlock = blkTbl[Global.shearBlock];

                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in panels)
                {
                    // Read the object as a solid
                    Solid pnl = trans.GetObject(obj, OpenMode.ForWrite) as Solid;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the approximate coordinates of the center point of the panel
                    Point3d cntrPt = AuxMethods.MidPoint(pnlVerts[0], pnlVerts[3]);

                    // Get the maximum lenght of the panel
                    List<double> ls = new List<double>();
                    ls.Add(Math.Abs(pnlVerts[0].DistanceTo(pnlVerts[1])));
                    ls.Add(Math.Abs(pnlVerts[1].DistanceTo(pnlVerts[3])));
                    ls.Add(Math.Abs(pnlVerts[3].DistanceTo(pnlVerts[2])));
                    ls.Add(Math.Abs(pnlVerts[2].DistanceTo(pnlVerts[0])));
                    double lMax = ls.Max();

                    // Read the XData and get the panel number and width
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(Global.appName);
                    TypedValue[] pnlData = pnlRb.AsArray();
                    int pnlNum = Convert.ToInt32(pnlData[2].Value);
                    double wd = Convert.ToDouble(pnlData[7].Value);

                    // Get the forces in the list
                    var f = panelForces[pnlNum - 1].Item2;

                    // Get the dimensions as a vector
                    var lsV = Vector<double>.Build.DenseOfEnumerable(ls.AsEnumerable());

                    // Calculate the shear stresses
                    var tau = f / (lsV * wd);

                    // Calculate the average stress
                    double tauAvg = Math.Round((-tau[0] + tau[1] - tau[2] + tau[3]) / 4, 2);

                    // Calculate the scale factor for the block and text
                    double scFctr = lMax / 1000;

                    // Insert the block into the current space
                    using (BlockReference blkRef = new BlockReference(cntrPt, shearBlock))
                    {
                        blkTblRec.AppendEntity(blkRef);
                        blkRef.Layer = Global.pnlFLyr;
                        trans.AddNewlyCreatedDBObject(blkRef, true);

                        // Set the scale of the block
                        blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

                        // If the shear is negative, mirror the block
                        if (tauAvg < 0)
                        {
                            blkRef.TransformBy(Matrix3d.Rotation(Global.pi, Global.curUCS.Yaxis, cntrPt));
                        }
                    }

                    // Create the texts
                    using (DBText tauTxt = new DBText())
                    {
                        // Set the alignment point
                        Point3d algnPt = new Point3d(cntrPt.X, cntrPt.Y + 10 * scFctr, 0);

                        // Set the parameters
                        tauTxt.Layer = Global.pnlFLyr;
                        tauTxt.Height = 30 * scFctr;
                        tauTxt.TextString = Math.Abs(tauAvg).ToString();
                        tauTxt.Position = algnPt;
                        tauTxt.HorizontalMode = TextHorizontalMode.TextCenter;
                        tauTxt.AlignmentPoint = algnPt;

                        // Add the text to the drawing
                        blkTblRec.AppendEntity(tauTxt);
                        trans.AddNewlyCreatedDBObject(tauTxt, true);
                    }

                    using (DBText mpaTxt = new DBText())
                    {
                        // Set the alignment point
                        Point3d algnPt = new Point3d(cntrPt.X, cntrPt.Y - 30 * scFctr, 0);

                        // Set the parameters
                        mpaTxt.Layer = Global.pnlFLyr;
                        mpaTxt.Height = 20 * scFctr;
                        mpaTxt.TextString = "MPa";
                        mpaTxt.Position = algnPt;
                        mpaTxt.HorizontalMode = TextHorizontalMode.TextCenter;
                        mpaTxt.AlignmentPoint = algnPt;

                        // Add the text to the drawing
                        blkTblRec.AppendEntity(mpaTxt);
                        trans.AddNewlyCreatedDBObject(mpaTxt, true);
                    }
                }

                // Save the new objects to the database
                trans.Commit();
            }
        }

        // Create the block for panel shear stress
        public static void CreatePanelShearBlock()
        {
            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Id
                ObjectId shearBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(Global.shearBlock))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = Global.shearBlock;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the name
                        shearBlock = blkTblRec.Id;

                        // Set the insertion point for the block
                        Point3d origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a object collection and add the lines
                        using (DBObjectCollection lines = new DBObjectCollection())
                        {
                            // Define the points to add the lines
                            Point3d[] blkPts =
                            {
                                new Point3d(-140, -230, 0),
                                new Point3d(-175, -200, 0),
                                new Point3d( 175, -200, 0),
                                new Point3d(-230, -140, 0),
                                new Point3d(-200, -175, 0),
                                new Point3d(-200,  175, 0),
                                new Point3d( 140,  230, 0),
                                new Point3d( 175,  200, 0),
                                new Point3d(-175,  200, 0),
                                new Point3d( 230,  140, 0),
                                new Point3d( 200,  175, 0),
                                new Point3d( 200, -175, 0),
                            };

                            // Define the lines and add to the collection
                            for (int i = 0; i < 4; i++)
                            {
                                Line line1 = new Line()
                                {
                                    StartPoint = blkPts[3 * i],
                                    EndPoint = blkPts[3 * i + 1]
                                };
                                lines.Add(line1);

                                Line line2 = new Line()
                                {
                                    StartPoint = blkPts[3 * i + 1],
                                    EndPoint = blkPts[3 * i + 2]
                                };
                                lines.Add(line2);
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }

                    // Commit and dispose the transaction
                    trans.Commit();
                }
            }
        }
    }
}
