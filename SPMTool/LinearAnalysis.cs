using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(SPMTool.LinearAnalysis))]

namespace SPMTool
{
    class LinearAnalysis
    {
        [CommandMethod("DoLinearAnalysis")]
        public void DoLinearAnalysis()
        {
            // Get the concrete parameters
            var (fc, Ec) = Material.ConcreteParams();

            // Verify if concrete parameters were set
            if (fc > 0 && Ec > 0)
            {
                // Calculate the aproximated shear modulus (elastic material)
                double Gc = Ec / 2.4;

                // Update and get the elements collection
                ObjectIdCollection nds = AuxMethods.UpdateNodes(),
                                   strs = AuxMethods.UpdateStringers(),
                                   pnls = AuxMethods.UpdatePanels();

                // Get the list of node positions
                List<Point3d> ndList = AuxMethods.ListOfNodes();

                // Start a transaction
                using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
                {
                    // Get the stringers stifness matrix and add to the global stifness matrix
                    foreach (ObjectId obj in strs)
                    {
                        // Read the object as a line
                        Line str = trans.GetObject(obj, OpenMode.ForWrite) as Line;

                        // Get the transformated stifness matrix and the dofs
                        var (K, dofs) = StringerStifness(str, Ec);
                    }

                    // Get the panels stifness matrix and add to the global stifness matrix
                    foreach (ObjectId obj in pnls)
                    {
                        // Read each panel as a solid
                        Solid pnl = trans.GetObject(obj, OpenMode.ForWrite) as Solid;

                        // Get the transformated stifness matrix and the dofs
                        var (K, dofs) = PanelStifness(pnl, Gc);
                    }

                }

                // If all went OK, notify the user
                Global.ed.WriteMessage("\nLinear stifness matrix of elements obtained.");
               
            }
            else
            {
                Application.ShowAlertDialog("Please set the material parameters.");
            }
        }

        // Calculate the stifness matrix of a stringer, get the dofs and save to XData
        public (Matrix<double> K, Point3dCollection dofs) StringerStifness(Line stringer, double Ec)
        {
            // Get the length and angles
            double lngt = stringer.Length,
                   alpha = stringer.Angle;                          // angle with x coordinate

            // Get the dofs collection
            Point3dCollection dofs = new Point3dCollection();
            dofs.Add(stringer.StartPoint);
            dofs.Add(AuxMethods.MidPoint(stringer.StartPoint, stringer.EndPoint));
            dofs.Add(stringer.EndPoint);

            // Read the XData and get the necessary data
            ResultBuffer strRb = stringer.GetXDataForApplication(Global.appName);
            TypedValue[] strData = strRb.AsArray();
            double wd = Convert.ToDouble(strData[7].Value),
                   h = Convert.ToDouble(strData[8].Value);

            // Calculate the cross sectional area
            double A = wd * h;

            // Get the direction cosines
            var (l, m) = AuxMethods.DirectionCosines(alpha);

            // Obtain the transformation matrix
            var T = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                        {l, m, 0, 0, 0, 0 },
                        {0, 0, l, m, 0, 0 },
                        {0, 0, 0, 0, l, m }
            });

            // Calculate the constant factor of stifness
            double EcAOverL = Ec * A / lngt;

            // Calculate the local stiffness matrix
            var Kl = EcAOverL * Matrix<double>.Build.DenseOfArray(new double[,]
            {
                        {  4, -6,  2 },
                        { -6, 12, -6 },
                        {  2, -6,  4 }
            });

            // Calculate the transformated stiffness matrix
            var K = T.Transpose() * Kl * T;

            // Save to the XData
            strData[10] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, Kl.ToString());
            strData[11] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, K.ToString());

            // Save the new XData
            strRb = new ResultBuffer(strData);
            stringer.XData = strRb;

            // Commit and dispose the transaction
            return (K, dofs);
        }

        // Calculate the stifness matrix of a panel, get the dofs and save to XData
        public (Matrix<double> K, Point3dCollection dofs) PanelStifness(Solid panel, double Gc)
        {
            // Get the vertices
            Point3dCollection pnlVerts = new Point3dCollection();
            panel.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

            // Get the vertices in the order needed for calculations
            Point3d nd1 = pnlVerts[0],
                    nd2 = pnlVerts[1],
                    nd3 = pnlVerts[3],
                    nd4 = pnlVerts[2];

            // Get the dofs collection
            Point3dCollection dofs = new Point3dCollection();
            dofs.Add(AuxMethods.MidPoint(nd1, nd2));
            dofs.Add(AuxMethods.MidPoint(nd2, nd3));
            dofs.Add(AuxMethods.MidPoint(nd3, nd4));
            dofs.Add(AuxMethods.MidPoint(nd4, nd1));

            // Read the XData and get the necessary data
            ResultBuffer pnlRb = panel.GetXDataForApplication(Global.appName);
            TypedValue[] pnlData = pnlRb.AsArray();

            // Get the panel width
            double t = Convert.ToDouble(pnlData[7].Value);

            // Create lines to measure the angles between the edges
            Line ln1 = new Line(nd1, nd2);
            Line ln2 = new Line(nd2, nd3);
            Line ln3 = new Line(nd3, nd4);
            Line ln4 = new Line(nd4, nd1);

            // Get the angles
            double ang2 = ln2.Angle - ln1.Angle;
            double ang4 = ln4.Angle - ln3.Angle;

            // Initialize the stifness matrix
            var Kl = Matrix<double>.Build.Dense(4, 4);

            // If the panel is rectangular (ang2 and ang4 will be equal to 90 degrees)
            if (ang2.Equals(Global.piOver2) && ang4.Equals(Global.piOver2))
            {
                // Get the dimensions
                double a = ln1.Length,
                       b = ln2.Length;

                // Calculate the parameters of the stifness matrix
                double aOverb = a / b,
                       bOvera = b / a;

                // Calculate the stiffness matrix
                Kl = Gc * t * Matrix<double>.Build.DenseOfArray(new double[,]
                {
                            {  aOverb,   -1  ,  aOverb,   -1   },
                            {    -1  , bOvera,    -1  , bOvera },
                            {  aOverb,   -1  ,  aOverb,   -1   },
                            {    -1  , bOvera,    -1  , bOvera }
                });
            }

            // If the panel is not rectangular
            else
            {
                // Get the dimensions
                double l1 = ln1.Length,
                       l2 = ln2.Length,
                       l3 = ln3.Length,
                       l4 = ln4.Length;

                // Equilibrium parameters
                double c1 = nd2.X - nd1.X, c2 = nd3.X - nd2.X, c3 = nd4.X - nd3.X, c4 = nd1.X - nd4.X,
                       s1 = nd2.Y - nd1.Y, s2 = nd3.Y - nd2.Y, s3 = nd4.Y - nd3.Y, s4 = nd1.Y - nd4.Y,
                       r1 = nd1.X * nd2.Y - nd2.X * nd1.Y, r2 = nd2.X * nd3.Y - nd3.X * nd2.Y,
                       r3 = nd3.X * nd4.Y - nd4.X * nd3.Y, r4 = nd4.X * nd1.Y - nd1.X * nd4.Y;

                // Kinematic parameters
                double a = (c1 - c3) / 2,
                       b = (s2 - s4) / 2,
                       c = (c2 - c4) / 2,
                       d = (s1 - s3) / 2;

                double t1 = -b * c1 - c * s1,
                       t2 = a * s2 + d * c2,
                       t3 = b * c3 + c * s3,
                       t4 = -a * s4 - d * c4;

                // Matrices to calculate the determinants
                var km1 = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                            { c2, c3, c4 },
                            { s2, s3, s4 },
                            { r2, r3, r4 },
                });

                var km2 = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                            { c1, c3, c4 },
                            { s1, s3, s4 },
                            { r1, r3, r4 },
                });

                var km3 = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                            { c1, c2, c4 },
                            { s1, s2, s4 },
                            { r1, r2, r4 },
                });

                var km4 = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                            { c1, c2, c3 },
                            { s1, s2, s3 },
                            { r1, r2, r3 },
                });

                // Calculate the determinants
                double k1 = km1.Determinant(),
                       k2 = km2.Determinant(),
                       k3 = km3.Determinant(),
                       k4 = km4.Determinant();

                // Calculate kf and ku
                double kf = k1 + k2 + k3 + k4,
                       ku = -t1 * k1 + t2 * k2 - t3 * k3 + t4 * k4;

                // Calculate D
                double D = 16 * Gc * t / (kf * ku);

                // Get the vector B
                var B = Vector<double>.Build.DenseOfArray(new double[]
                {
                    -k1 * l1, k2 * l2, -k3 * l3, k4 * l4
                });

                // Get the stifness matrix
                Kl = B.ToColumnMatrix() * D * B.ToRowMatrix();
            }

            // Get the transformation matrix
            // Direction cosines
            var (m1, n1) = AuxMethods.DirectionCosines(ln1.Angle);
            var (m2, n2) = AuxMethods.DirectionCosines(ln2.Angle);
            var (m3, n3) = AuxMethods.DirectionCosines(ln3.Angle);
            var (m4, n4) = AuxMethods.DirectionCosines(ln4.Angle);

            // T matrix
            var T = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                        { m1, n1,  0,  0,  0,  0,  0,  0 },
                        {  0,  0, m2, n2,  0,  0,  0,  0 },
                        {  0,  0,  0,  0, m3, n3,  0,  0 },
                        {  0,  0,  0,  0,  0,  0, m4, n4 },

            });

            // Global stifness matrix
            var K = T.Transpose() * Kl * T;

            // Save to the XData
            pnlData[10] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, Kl.ToString());
            pnlData[11] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, K.ToString());

            // Save the new XData
            pnlRb = new ResultBuffer(pnlData);
            panel.XData = pnlRb;

            return (K, dofs);
        }

        [CommandMethod("ForceVector")]
        public void ForceVector()
        {
            // Access the nodes in the model
            ObjectIdCollection nds = AuxMethods.AllNodes();

            // Get the number of DoFs
            int numDofs = nds.Count;

            // Initialize the force vector with size 2x number of DoFs (forces in x and y)
            var f = Vector<double>.Build.Dense(numDofs * 2);

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Read the nodes data
                foreach (ObjectId ndObj in nds)
                {
                    // Read as a DBPoint
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                    // Get the result buffer as an array
                    ResultBuffer rb = nd.GetXDataForApplication(Global.appName);
                    TypedValue[] data = rb.AsArray();

                    // Read the node number
                    int ndNum = Convert.ToInt32(data[2].Value);

                    // Read the forces in x and y
                    double Fx = Convert.ToDouble(data[6].Value),
                           Fy = Convert.ToDouble(data[7].Value);

                    // Get the position in the vector from the DoF list
                    int i = 2 * ndNum - 2;

                    // If force is not zero, assign the values in the force vector at position (i) and (i + 1)
                    if (Fx != 0) f.At(i, Fx);
                    if (Fy != 0) f.At(i + 1, Fy);
                }
            }

            // Write the values
            Global.ed.WriteMessage("\nVector of forces:\n" + f.ToString());
        }

        [CommandMethod("DisplacementVector")]
        public void DisplacementVector()
        {
            // Access the nodes in the model
            ObjectIdCollection nds = AuxMethods.AllNodes();

            // Get the number of DoFs
            int numDofs = nds.Count;

            // Initialize the displacement vector with size 2x number of nodes (displacements in x and y)
            // Assign 1 (free node) initially to each value
            var u = Vector<double>.Build.Dense(numDofs * 2, 1);

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Read the nodes data
                foreach (ObjectId ndObj in nds)
                {
                    // Read as a DBPoint
                    DBPoint nd = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                    // Get the result buffer as an array
                    ResultBuffer rb = nd.GetXDataForApplication(Global.appName);
                    TypedValue[] data = rb.AsArray();

                    // Read the node number
                    int ndNum = Convert.ToInt32(data[2].Value);

                    // Read the support condition
                    string sup = data[5].Value.ToString();

                    // Get the position in the vector
                    int i = 2 * ndNum - 2;

                    // If there is a support the value on the vector will be zero on that direction
                    // X (i) , Y (i + 1)
                    if (sup.Contains("X")) u.At(i, 0);
                    if (sup.Contains("Y")) u.At(i + 1, 0);
                }
            }

            // Write the values
            Global.ed.WriteMessage("\nVector of displacements:\n" + u.ToString());
        }

        [CommandMethod("ViewElasticStifness")]
        public void ViewElasticStifness()
        {
            // Initialize the message to display
            string msgstr = "";

            // Request the object to be selected in the drawing area
            PromptEntityOptions entOp = new PromptEntityOptions("\nSelect an element to print the stiffness matrix:");
            PromptEntityResult entRes = Global.ed.GetEntity(entOp);

            // If the prompt status is OK, objects were selected
            if (entRes.Status == PromptStatus.OK)
            {
                // Start a transaction
                using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
                {
                    // Open the selected object for read
                    Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

                    // If it's a stringer
                    if (ent.Layer == "Stringer")
                    {
                        // Get the extended data attached to each object for MY_APP
                        ResultBuffer rb = ent.GetXDataForApplication(Global.appName);

                        // Make sure the Xdata is not empty
                        if (rb != null)
                        {
                            // Get the XData as an array
                            TypedValue[] data = rb.AsArray();

                            // Get the parameters
                            string strNum = data[2 ].Value.ToString(),
                                   kl     = data[10].Value.ToString(),
                                   k      = data[11].Value.ToString();

                            msgstr = "Stringer " + strNum + "\n\n" +
                                     "Local Stifness Matrix: \n" +
                                     kl + "\n\n" +
                                     "Transformated Stifness Matrix: \n" +
                                     k;
                        }

                        else msgstr = "NONE";
                    }

                    // If it's a panel
                    if (ent.Layer == "Panel")
                    {
                        // Get the extended data attached to each object for MY_APP
                        ResultBuffer rb = ent.GetXDataForApplication(Global.appName);

                        // Make sure the Xdata is not empty
                        if (rb != null)
                        {
                            // Get the XData as an array
                            TypedValue[] data = rb.AsArray();

                            // Get the parameters
                            string pnlNum = data[2].Value.ToString(),
                                   Kl     = data[10].Value.ToString(),
                                   K      = data[11].Value.ToString();

                            msgstr = "Panel " + pnlNum + "\n\n" +
                                     "Local Stifness Matrix:\n" + Kl + "\n" +
                                     "Global Stifness Matrix:\n" + K;

                        }

                        else msgstr = "NONE";
                    }

                    //else msgstr = "Object is not a stringer or panel.";

                    // Display the values returned
                    Global.ed.WriteMessage("\n" + msgstr);
                }
            }
        }
    }
}
