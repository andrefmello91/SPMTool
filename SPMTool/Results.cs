﻿using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

[assembly: CommandClass(typeof(SPMTool.Results))]

namespace SPMTool
{
    public class Results
    {
        // Create the block for panel shear stress
        public static void CreatePanelShearBlock()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Initialize the block Id
                ObjectId shearBlock = ObjectId.Null;

                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(Blocks.shearBlock))
                {
                    // Create the X block
                    using (BlockTableRecord blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = Blocks.shearBlock;

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

        // Draw the panel shear blocks
        public static void DrawPanelForces(ObjectIdCollection panels, Matrix<double> panelForces)
        {
            // Check if the layer already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.panelForce, (short)AutoCAD.Colors.Green, 0);

            // Check if the shear blocks already exist. If not, create the blocks
            CreatePanelShearBlock();

            // Erase all the panel forces in the drawing
            ObjectIdCollection pnlFs = Auxiliary.GetEntitiesOnLayer(Layers.panelForce);
            if (pnlFs.Count > 0) Auxiliary.EraseObjects(pnlFs);

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Read the object Ids of the support blocks
                ObjectId shearBlock = blkTbl[Blocks.shearBlock];

                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in panels)
                {
                    // Read the object as a solid
                    Solid pnl = trans.GetObject(obj, OpenMode.ForWrite) as Solid;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the approximate coordinates of the center point of the panel
                    Point3d cntrPt = Auxiliary.MidPoint(pnlVerts[0], pnlVerts[3]);

                    // Get the maximum lenght of the panel
                    List<double> ls = new List<double>();
                    ls.Add(Math.Abs(pnlVerts[0].DistanceTo(pnlVerts[1])));
                    ls.Add(Math.Abs(pnlVerts[1].DistanceTo(pnlVerts[3])));
                    ls.Add(Math.Abs(pnlVerts[3].DistanceTo(pnlVerts[2])));
                    ls.Add(Math.Abs(pnlVerts[2].DistanceTo(pnlVerts[0])));
                    double lMax = ls.Max();

                    // Read the XData and get the panel number and width
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] pnlData = pnlRb.AsArray();
                    int pnlNum = Convert.ToInt32(pnlData[(int)XData.Panel.Number].Value);
                    double wd = Convert.ToDouble(pnlData[(int)XData.Panel.Width].Value);

                    // Get the forces in the matrix
                    var f = panelForces.Row(pnlNum - 1);

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
                        blkRef.Layer = Layers.panelForce;
                        Auxiliary.AddObject(blkRef);

                        // Set the scale of the block
                        blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

                        // If the shear is negative, mirror the block
                        if (tauAvg < 0)
                        {
                            blkRef.TransformBy(Matrix3d.Rotation(Constants.pi, AutoCAD.curUCS.Yaxis, cntrPt));
                        }
                    }

                    // Create the texts
                    using (DBText tauTxt = new DBText())
                    {
                        // Set the alignment point
                        Point3d algnPt = new Point3d(cntrPt.X, cntrPt.Y, 0);

                        // Set the parameters
                        tauTxt.Layer = Layers.panelForce;
                        tauTxt.Height = 30 * scFctr;
                        tauTxt.TextString = Math.Abs(tauAvg).ToString();
                        tauTxt.Position = algnPt;
                        tauTxt.HorizontalMode = TextHorizontalMode.TextCenter;
                        tauTxt.AlignmentPoint = algnPt;

                        // Add the text to the drawing
                        Auxiliary.AddObject(tauTxt);
                    }
                }

                // Save the new objects to the database
                trans.Commit();
            }

            // Turn the layer on
            Auxiliary.LayerOn(Layers.panelForce);
        }

        // Draw the stringer forces diagrams
        public static void DrawStringerForces(ObjectIdCollection stringers, Matrix<double> stringerForces)
        {
            // Check if the layer already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layers.stringerForce, (short)AutoCAD.Colors.Grey, 0);

            // Erase all the stringer forces in the drawing
            ObjectIdCollection strFs = Auxiliary.GetEntitiesOnLayer(Layers.stringerForce);
            if (strFs.Count > 0) Auxiliary.EraseObjects(strFs);

            // Verify the maximum stringer force in the model to draw in an uniform scale
            double fMax = stringerForces.Enumerate().MaximumAbsolute();

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in stringers)
                {
                    // Read the object as a line
                    Line str = trans.GetObject(obj, OpenMode.ForWrite) as Line;

                    // Get the parameters of the stringer
                    double l = str.Length,
                           ang = str.Angle;

                    // Read the XData and get the stringer number
                    ResultBuffer strRb = str.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] strData = strRb.AsArray();
                    int strNum = Convert.ToInt32(strData[(int)XData.Stringer.Number].Value);

                    // Get the forces in the list
                    var f = stringerForces.Row(strNum - 1);
                    double f1 =  Math.Round(f[0], 2),
                           f3 = -Math.Round(f[2], 2);

                    // Check if at least one force is not zero
                    if (f1 != 0 || f3 != 0)
                    {
                        // Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
                        double h1 = 150 * f1 / fMax,
                               h3 = 150 * f3 / fMax;

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
                                dgrm.Layer = Layers.stringerForce;
                                dgrm.Transparency = Auxiliary.Transparency(80);

                                // Set the color (blue to compression and red to tension)
                                if (Math.Max(f1, f3) > 0) dgrm.ColorIndex = (short)AutoCAD.Colors.Blue1;
                                else dgrm.ColorIndex = (short)AutoCAD.Colors.Red;

                                // Add the diagram to the drawing
                                Auxiliary.AddObject(dgrm);

                                // Rotate the diagram
                                dgrm.TransformBy(Matrix3d.Rotation(ang, AutoCAD.curUCS.Zaxis, str.StartPoint));
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
                                dgrm1.Layer = Layers.stringerForce;
                                dgrm1.Transparency = Auxiliary.Transparency(80);

                                // Set the color (blue to compression and red to tension)
                                if (f1 > 0) dgrm1.ColorIndex = (short)AutoCAD.Colors.Blue1;
                                else dgrm1.ColorIndex = (short)AutoCAD.Colors.Red;

                                // Add the diagram to the drawing
                                Auxiliary.AddObject(dgrm1);

                                // Rotate the diagram
                                dgrm1.TransformBy(Matrix3d.Rotation(ang, AutoCAD.curUCS.Zaxis, str.StartPoint));
                            }

                            using (Solid dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2]))
                            {
                                // Set the layer and transparency
                                dgrm3.Layer = Layers.stringerForce;
                                dgrm3.Transparency = Auxiliary.Transparency(80);

                                // Set the color (blue to compression and red to tension)
                                if (f3 > 0) dgrm3.ColorIndex = (short)AutoCAD.Colors.Blue1;
                                else dgrm3.ColorIndex = (short)AutoCAD.Colors.Red;

                                // Add the diagram to the drawing
                                Auxiliary.AddObject(dgrm3);

                                // Rotate the diagram
                                dgrm3.TransformBy(Matrix3d.Rotation(ang, AutoCAD.curUCS.Zaxis, str.StartPoint));
                            }
                        }

                        // Create the texts if forces are not zero
                        if (f1 != 0)
                        {
                            using (DBText txt1 = new DBText())
                            {
                                // Set the parameters
                                txt1.Layer = Layers.stringerForce;
                                txt1.Height = 30;
                                txt1.TextString = Math.Abs(f1).ToString();

                                // Set the color (blue to compression and red to tension) and position
                                if (f1 > 0)
                                {
                                    txt1.ColorIndex = (short)AutoCAD.Colors.Blue1;
                                    txt1.Position = new Point3d(str.StartPoint.X + 10, str.StartPoint.Y + h1 + 20, 0);
                                }
                                else
                                {
                                    txt1.ColorIndex = (short)AutoCAD.Colors.Red;
                                    txt1.Position = new Point3d(str.StartPoint.X + 10, str.StartPoint.Y + h1 - 50, 0);
                                }

                                // Add the text to the drawing
                                Auxiliary.AddObject(txt1);

                                // Rotate the text
                                txt1.TransformBy(Matrix3d.Rotation(ang, AutoCAD.curUCS.Zaxis, str.StartPoint));
                            }
                        }

                        if (f3 != 0)
                        {
                            using (DBText txt3 = new DBText())
                            {
                                // Set the parameters
                                txt3.Layer = Layers.stringerForce;
                                txt3.Height = 30;
                                txt3.TextString = Math.Abs(f3).ToString();

                                // Set the color (blue to compression and red to tension) and position
                                if (f3 > 0)
                                {
                                    txt3.ColorIndex = (short)AutoCAD.Colors.Blue1;
                                    txt3.Position = new Point3d(str.StartPoint.X + l - 10, str.StartPoint.Y + h3 + 20, 0);
                                }
                                else
                                {
                                    txt3.ColorIndex = (short)AutoCAD.Colors.Red;
                                    txt3.Position = new Point3d(str.StartPoint.X + l - 10, str.StartPoint.Y + h3 - 50, 0);
                                }

                                // Adjust the alignment
                                txt3.HorizontalMode = TextHorizontalMode.TextRight;
                                txt3.AlignmentPoint = txt3.Position;

                                // Add the text to the drawing
                                Auxiliary.AddObject(txt3);

                                // Rotate the text
                                txt3.TransformBy(Matrix3d.Rotation(ang, AutoCAD.curUCS.Zaxis, str.StartPoint));
                            }
                        }
                    }
                }

                // Save the new objects to the database
                trans.Commit();
            }

            // Turn the layer on
            Auxiliary.LayerOn(Layers.stringerForce);
        }

        // Draw the displaced model
        public static void DrawDisplacements(ObjectIdCollection stringers, List<Tuple<Point3d, double, double>> ndDisp)
        {
            // Create the layer
            Auxiliary.CreateLayer(Layers.displacements, (short)AutoCAD.Colors.Yellow1, 0);

            // Erase all the displaced objects in the drawing
            ObjectIdCollection dispObjs = Auxiliary.GetEntitiesOnLayer(Layers.displacements);
            if (dispObjs.Count > 0) Auxiliary.EraseObjects(dispObjs);

            // Set a scale factor for displacements
            int scFctr = 100;

            // Create lists of points for adding the nodes later
            List<Point3d> dispNds = new List<Point3d>();

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in stringers)
                {
                    // Read the object as a line
                    Line str = trans.GetObject(obj, OpenMode.ForRead) as Line;

                    // Initialize the displacements of the initial and end nodes
                    double ux1 = 0,
                           uy1 = 0,
                           ux3 = 0,
                           uy3 = 0;

                    // Get the displacements on the list
                    foreach (var disp in ndDisp) // Initial node
                    {
                        if (str.StartPoint == disp.Item1)
                        {
                            ux1 = disp.Item2 * scFctr;
                            uy1 = disp.Item3 * scFctr;
                            break;
                        }
                    }

                    foreach (var disp in ndDisp) // End node
                    {
                        if (str.EndPoint == disp.Item1)
                        {
                            ux3 = disp.Item2 * scFctr;
                            uy3 = disp.Item3 * scFctr;
                            break;
                        }
                    }

                    // Calculate the displaced nodes
                    Point3d stPt = new Point3d(str.StartPoint.X + ux1, str.StartPoint.Y + uy1, 0),
                            enPt = new Point3d(str.EndPoint.X + ux3, str.EndPoint.Y + uy3, 0),
                            midPt = Auxiliary.MidPoint(stPt, enPt);

                    // Draw the displaced stringer
                    using (Line newStr = new Line(stPt, enPt))
                    {
                        // Set the layer to stringer
                        newStr.Layer = Layers.displacements;

                        // Add the line to the drawing
                        Auxiliary.AddObject(newStr);
                    }

                    // Add the position of the nodes to the list
                    if (!dispNds.Contains(stPt))
                        dispNds.Add(stPt);

                    if (!dispNds.Contains(enPt))
                        dispNds.Add(enPt);

                    if (!dispNds.Contains(midPt))
                        dispNds.Add(midPt);
                }

                // Commit changes
                trans.Commit();
            }

            // Add the nodes
            Geometry.Node.NewNode(dispNds, Layers.displacements);

            // Turn the layer off
            Auxiliary.LayerOff(Layers.displacements);
        }

        // Get the nodal displacements and save to XData
        public static void NodalDisplacements(ObjectIdCollection nodes, ObjectIdCollection stringers, List<Point3d> nodeList, Vector<double> u)
        {
            // Initialize a tuple list to store the node position and the displacements
            List<Tuple<Point3d, double, double>> ndDisp = new List<Tuple<Point3d, double, double>>();

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in nodes)
                {
                    // Read the object as a point
                    DBPoint nd = trans.GetObject(obj, OpenMode.ForWrite) as DBPoint;

                    // Get the index of the node on the list
                    int i = 2 * nodeList.IndexOf(nd.Position);

                    // Get the displacements
                    double ux = Math.Round(u[i], 6),
                           uy = Math.Round(u[i + 1], 6);

                    // Get the result buffer as an array
                    ResultBuffer rb = nd.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] data = rb.AsArray();

                    // Save the displacements on the XData
                    data[(int)XData.Node.Ux] = new TypedValue((int)DxfCode.ExtendedDataReal, ux);
                    data[(int)XData.Node.Uy] = new TypedValue((int)DxfCode.ExtendedDataReal, uy);

                    // Add the new XData
                    nd.XData = new ResultBuffer(data);

                    // Save only external nodes to the list
                    if (nd.Layer == Layers.extNode)
                        ndDisp.Add(Tuple.Create(nd.Position, ux, uy));
                }

                // Commit changes
                trans.Commit();
            }

            // Draw the displacements
            DrawDisplacements(stringers, ndDisp);
        }

        // Calculate panel forces
        public static void PanelForces(ObjectIdCollection panels, Analysis.Panel[] pnls, Vector<double> u)
        {
            // Create a list to store the panel forces
            var pnlForces = Matrix<double>.Build.Dense(panels.Count, 4);

            // Create empty elements to the list
            //for (int i = 0; i < panels.Count; i++)
            //    pnlForces.Add(Vector<double>.Build.Dense(2));

            for (int i = 0; i < panels.Count; i++)
            {
                // Get the parameters
                int[] ind = pnls[i].Index;
                var   Kl  = pnls[i].LocalStiffness;
                var   T   = pnls[i].TransMatrix;

                // Get the displacements
                var uStr = Vector<double>.Build.DenseOfArray(new double[]
                {
                    u[ind[0]] , u[ind[0] + 1], u[ind[1]], u[ind[1] + 1], u[ind[2]] , u[ind[2] + 1], u[ind[3]] , u[ind[3] + 1]
                });

                // Get the displacements in the direction of the stringer
                var ul = T * uStr;

                // Calculate the vector of forces
                var fl = Kl * ul;

                // Save to the list of stringer forces
                pnlForces.SetRow(i, fl);
            }

            // Draw the panel shear blocks
            DrawPanelForces(panels, pnlForces);
        }

        // Calculate stringer forces
        public static void StringerForces(ObjectIdCollection stringers, Analysis.Stringer[] strs, Vector<double> u)
        {
            // Create a list to store the stringer forces
            var strForces = Matrix<double>.Build.Dense(stringers.Count,3);

            // Create empty elements to the list
            //for (int i = 0; i < stringers.Count; i++)
            //    strForces.Add(Vector<double>.Build.Dense(2));

            for (int i = 0; i < stringers.Count; i++)
            {
                // Get the parameters
                int[] ind = strs[i].Index;
                var   Kl  = strs[i].LocalStiffness;
                var   T   = strs[i].TransMatrix;

                // Get the displacements
                var uStr = Vector<double>.Build.DenseOfArray(new double[]
                {
                    u[ind[0]] , u[ind[0] + 1], u[ind[1]], u[ind[1] + 1], u[ind[2]] , u[ind[2] + 1]
                });

                // Get the displacements in the direction of the stringer
                var ul = T * uStr;

                // Calculate the vector of normal forces (in kN)
                var fl = 0.001 * Kl * ul;

                // Aproximate small values to zero
                fl.CoerceZero(0.000001);

                // Save to the matrix of stringer forces
                strForces.SetRow(i, fl);

                //Global.ed.WriteMessage("\nStringer " + strNum.ToString() + ":\n" + fl.ToString());
            }

            // Draw the stringer forces diagrams
            DrawStringerForces(stringers, strForces);
        }

        [CommandMethod("ViewElementData")]
        public void ViewElementData()
        {
            // Initialize a message to display
            string msgstr = "";

            // Start a loop for viewing continuous elements
            for ( ; ; )
            {
                // Request the object to be selected in the drawing area
                PromptEntityOptions entOp = new PromptEntityOptions("\nSelect an element to view data:");
                PromptEntityResult entRes = AutoCAD.edtr.GetEntity(entOp);

                // If the prompt status is OK, objects were selected
                if (entRes.Status == PromptStatus.OK)
                {
                    // Start a transaction
                    using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                    {
                        // Get the entity for read
                        Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

                        // Get the extended data attached to each object for SPMTool
                        ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);

                        // Make sure the Xdata is not empty
                        if (rb != null)
                        {
                            // Get the XData as an array
                            TypedValue[] data = rb.AsArray();

                            // If it's a node
                            if (ent.Layer == Layers.extNode || ent.Layer == Layers.intNode)
                            {
                                // Read as a DBpoint
                                DBPoint nd = ent as DBPoint;

                                // Get the position
                                double xPos = Math.Round(nd.Position.X, 2),
                                       yPos = Math.Round(nd.Position.Y, 2);

                                // Get the parameters
                                string ndNum = data[(int)XData.Node.Number].Value.ToString(),
                                       sup   = data[(int)XData.Node.Support].Value.ToString(),
                                       fX    = data[(int)XData.Node.Fx].Value.ToString(),
                                       fY    = data[(int)XData.Node.Fy].Value.ToString(),
                                       ux    = data[(int)XData.Node.Ux].Value.ToString(),
                                       uy    = data[(int)XData.Node.Uy].Value.ToString();

                                msgstr = "Node " + ndNum + "\n\n" +
                                         "Node position: (" + xPos + ", " + yPos + ")" + "\n" +
                                         "Support conditions: " + sup + "\n" +
                                         "Fx = " + fX + " kN" + "\n" +
                                         "Fy = " + fY + " kN" + "\n" +
                                         "ux = " + ux + " mm" + "\n" +
                                         "uy = " + uy + " mm";
                            }

                            // If it's a stringer
                            if (ent.Layer == Layers.stringer)
                            {
                                // Read as a line
                                Line str = ent as Line;

                                // Get the lenght
                                double lgt = Math.Round(str.Length, 2);

                                // Get the parameters
                                string grip1 = data[(int)XData.Stringer.Number].Value.ToString(),
                                       grip2 = data[(int)XData.Stringer.Grip1].Value.ToString(),
                                       grip3 = data[(int)XData.Stringer.Grip2].Value.ToString(),
                                       endNd = data[(int)XData.Stringer.Grip3].Value.ToString(),
                                       wdt   = data[(int)XData.Stringer.Width].Value.ToString(),
                                       hgt   = data[(int)XData.Stringer.Height].Value.ToString();

                                // Get the reinforcement
                                double nBars = Convert.ToDouble(data[(int)XData.Stringer.NumOfBars].Value),
                                       phi   = Convert.ToDouble(data[(int)XData.Stringer.BarDiam].Value);

                                // Calculate the reinforcement area
                                double As = Math.Round(Reinforcement.StringerReinforcement(nBars, phi), 2);

                                msgstr = "Stringer " + grip1 + "\n\n" +
                                         "Grips: (" + grip2 + " - " + grip3 + " - " + endNd + ")" + "\n" +
                                         "Lenght = " + lgt + " mm" + "\n" +
                                         "Width = " + wdt + " mm" + "\n" +
                                         "Height = " + hgt + " mm" + "\n" +
                                         "Reinforcement = " + nBars + " Ø " + phi + " mm (" + As + " mm2)";
                            }

                            // If it's a panel
                            if (ent.Layer == Layers.panel)
                            {
                                // Get the parameters
                                string pnlNum = data[(int)XData.Panel.Number].Value.ToString();

                                string[] pnlGps =
                                    {
                                        data[(int)XData.Panel.Grip1].Value.ToString(),
                                        data[(int)XData.Panel.Grip2].Value.ToString(),
                                        data[(int)XData.Panel.Grip3].Value.ToString(),
                                        data[(int)XData.Panel.Grip4].Value.ToString()
                                    };

                                double w = Convert.ToDouble(data[(int)XData.Panel.Width].Value);

                                double[] phi = new double[]
                                {
                                    Convert.ToDouble(data[(int)XData.Panel.XDiam].Value),
                                    Convert.ToDouble(data[(int)XData.Panel.YDiam].Value)
                                };

                                double[] s = new double[]
                                {
                                    Convert.ToDouble(data[(int)XData.Panel.Sx].Value),
                                    Convert.ToDouble(data[(int)XData.Panel.Sy].Value)
                                };

                                // Calculate the reinforcement ratio
                                double[] ps = Reinforcement.PanelReinforcement(phi, s, w);
                                double psx = Math.Round(ps[0], 5),
                                       psy = Math.Round(ps[1], 5);

                                msgstr = "Panel " + pnlNum + "\n\n" +
                                         "Grips: (" + pnlGps[0] + " - " + pnlGps[1] + " - " + pnlGps[2] + " - " + pnlGps[3] + ")" + "\n" +
                                         "Width = " + w + " mm" + "\n" +
                                         "Reinforcement (x) = Ø " + phi[0] + " mm, s = " + s[0] + " mm (ρsx = " + psx + ")\n" +
                                         "Reinforcement (y) = Ø " + phi[1] + " mm, s = " + s[1] + " mm (ρsy = " + psy + ")";
                            }

                            // If it's a force text
                            if (ent.Layer == Layers.forceText)
                            {
                                // Get the parameters
                                string posX = data[2].Value.ToString(),
                                       posY = data[3].Value.ToString();

                                msgstr = "Force at position  (" + posX + ", " + posY + ")";
                            }

                        }
                        else
                        {
                            msgstr = "NONE";
                        }
                        
                        // Display the values returned
                        Application.ShowAlertDialog(AutoCAD.appName + "\n\n" + msgstr);
                    }
                }
                else break;
            }
        }

        // Toggle view for stringer forces
        [CommandMethod("ToogleStringerForces")]
        public void ToogleStringerForces()
        {
            Auxiliary.ToogleLayer(Layers.stringerForce);
        }

        // Toggle view for panel forces
        [CommandMethod("TooglePanelForces")]
        public void TooglePanelForces()
        {
            Auxiliary.ToogleLayer(Layers.panelForce);
        }

        // Toggle view for displacements
        [CommandMethod("ToogleDisplacements")]
        public void ToogleDisplacements()
        {
            Auxiliary.ToogleLayer(Layers.displacements);
        }
    }
}
