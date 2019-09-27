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
        [CommandMethod("StringerStiffness")]
        public void StringerStifness()
        {
            // Initialize the parameters needed
            double Ec = 0; // concrete elastic modulus

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(Global.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

                // Read the concrete Xrecord
                ObjectId concPar = nod.GetAt("ConcreteParams");
                if (concPar != null)
                {
                    // Read the Concrete Xrecord
                    Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
                    ResultBuffer concRb = concXrec.Data;
                    TypedValue[] concData = concRb.AsArray();

                    // Get the elastic modulus
                    Ec = Convert.ToDouble(concData[3].Value);
                }
                else
                {
                    Application.ShowAlertDialog("Please set the material parameters.");
                }

                // Get the stringer collection
                ObjectIdCollection strs = AuxMethods.GetEntitiesOnLayer("Stringer");

                foreach (ObjectId obj in strs)
                {
                    // Read each stringer as a line
                    Line str = trans.GetObject(obj, OpenMode.ForWrite) as Line;

                    // Get the length and angles
                    double lngt =  str.Length,
                           alpha = str.Angle;                          // angle with x coordinate

                    // Read the XData and get the necessary data
                    ResultBuffer strRb = str.GetXDataForApplication(Global.appName);
                    TypedValue[] strData = strRb.AsArray();
                    double strNum = Convert.ToDouble(strData[2].Value),
                           strNd  = Convert.ToDouble(strData[3].Value),
                           midNd  = Convert.ToDouble(strData[4].Value),
                           endNd  = Convert.ToDouble(strData[5].Value),
                           wd     = Convert.ToDouble(strData[7].Value),
                           h      = Convert.ToDouble(strData[8].Value);

                    // Calculate the cross sectional area
                    double A = wd * h;

                    // Get the direction cosines
                    double l;                                         // cosine with x

                    // If the angle is 90 or 270 degrees, the cosine is zero
                    if (alpha == Global.piOver2 || alpha == Global.pi3Over2) l = 0;
                    else l = MathNet.Numerics.Trig.Cos(alpha);

                    double m = MathNet.Numerics.Trig.Sin(alpha);      // cosine with y

                    // Obtain the transformation matrix
                    var T = Matrix<double>.Build.DenseOfArray(new double[,] {
                        {l, m, 0, 0, 0 },
                        {0, 0, 1, 0, 0 },
                        {0, 0, 0, l, m }
                    });

                    // Calculate the constant factor of stifness
                    double EcAOverL = Ec * A / lngt;

                    // Calculate the local stiffness matrix
                    var kl = EcAOverL * Matrix<double>.Build.DenseOfArray(new double[,] {
                        {  4, -6,  2 },
                        { -6, 12, -6 },
                        {  2, -6,  4 }
                    });

                    // Calculate the transformated stiffness matrix
                    var k = T.Transpose() * kl * T;

                    // Save to the XData
                    strData[10] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, kl.ToString());
                    strData[11] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, k.ToString());

                    // Save the new XData
                    strRb = new ResultBuffer(strData);
                    str.XData = strRb;
                }

                // Commit and dispose the transaction
                trans.Commit();
            }

            // If all went OK, notify the user
            Global.ed.WriteMessage("\nLinear stifness matrix of stringers obtained.");
        }

        [CommandMethod("PanelStiffness")]
        public void PanelStifness()
        {
            // Initialize the parameters needed
            double Ec = 0; // concrete elastic modulus

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(Global.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

                // Read the concrete Xrecord
                ObjectId concPar = nod.GetAt("ConcreteParams");
                if (concPar != null)
                {
                    // Read the Concrete Xrecord
                    Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
                    ResultBuffer concRb = concXrec.Data;
                    TypedValue[] concData = concRb.AsArray();

                    // Get the elastic modulus
                    Ec = Convert.ToDouble(concData[3].Value);
                }
                else
                {
                    Application.ShowAlertDialog("Please set the material parameters.");
                }

                // Calculate the aproximated shear modulus (elastic material)
                double Gc = Ec / 2.4;

                // Get the panel collection
                ObjectIdCollection pnls = AuxMethods.GetEntitiesOnLayer("Panel");

                foreach (ObjectId obj in pnls)
                {
                    // Read each panel as a solid
                    Solid pnl = trans.GetObject(obj, OpenMode.ForWrite) as Solid;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the vertices in the order needed for calculations
                    Point3d nd1 = pnlVerts[0],
                            nd2 = pnlVerts[1],
                            nd3 = pnlVerts[3],
                            nd4 = pnlVerts[2];

                    // Read the XData and get the necessary data
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(Global.appName);
                    TypedValue[] pnlData = pnlRb.AsArray();

                    // Get the number of the panel
                    double pnlNum = Convert.ToDouble(pnlData[2].Value);

                    // Get the panel width
                    double t = Convert.ToDouble(pnlData[7].Value);

                    // Get the number of the DoFs
                    DoubleCollection pnlDofs = new DoubleCollection();
                    for(int i = 3; i <= 6; i++)
                    {
                        pnlDofs.Add(Convert.ToDouble(pnlData[i].Value));
                    }

                    // Create lines to measure the angles between the edges
                    Line ln1 = new Line(nd1, nd2);
                    Line ln2 = new Line(nd2, nd3);
                    Line ln3 = new Line(nd3, nd4);
                    Line ln4 = new Line(nd4, nd1);

                    // Get the angles
                    double ang2 = ln2.Angle - ln1.Angle;
                    double ang4 = ln4.Angle - ln3.Angle;

                    // Initialize the stifness matrix
                    var K = Matrix<double>.Build.Dense(4, 4);

                    // If the panel is rectangular (ang1 and ang3 will be equal to 90 degrees)
                    if (ang2.Equals(Global.piOver2) || ang4.Equals(Global.piOver2))
                    {
                        // Get the dimensions
                        double a = ln1.Length,
                               b = ln2.Length;

                        // Calculate the parameters of the stifness matrix
                        double aOverb = a / b,
                               bOvera = b / a;

                        // Calculate the stiffness matrix
                        K = Gc * t * Matrix<double>.Build.DenseOfArray(new double[,]
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
                               r1 = nd1.X * nd2.Y - nd2.X * nd1.Y,     r2 = nd2.X * nd3.Y - nd3.X * nd2.Y,
                               r3 = nd3.X * nd4.Y - nd4.X * nd3.Y,     r4 = nd4.X * nd1.Y - nd1.X * nd4.Y;

                        // Kinematic parameters
                        double a = (c1 - c3) / 2,
                               b = (s2 - s4) / 2,
                               c = (c2 - c4) / 2,
                               d = (s1 - s3) / 2;

                        double t1 = -b * c1 - c * s1,
                               t2 =  a * s2 + d * c2,
                               t3 =  b * c3 + c * s3,
                               t4 = -a * s4 - d * c4;

                        // Matrices to calculate the determinants
                        var m1 = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            { c2, c3, c4 },
                            { s2, s3, s4 },
                            { r2, r3, r4 },
                        });

                        var m2 = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            { c1, c3, c4 },
                            { s1, s3, s4 },
                            { r1, r3, r4 },
                        });

                        var m3 = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            { c1, c2, c4 },
                            { s1, s2, s4 },
                            { r1, r2, r4 },
                        });

                        var m4 = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            { c1, c2, c3 },
                            { s1, s2, s3 },
                            { r1, r2, r3 },
                        });

                        // Calculate the determinants
                        double k1 = m1.Determinant(),
                               k2 = m2.Determinant(),
                               k3 = m3.Determinant(),
                               k4 = m4.Determinant();

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
                        K = B.ToColumnMatrix() * D * B.ToRowMatrix();
                    }

                    // Save to the XData
                    pnlData[10] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, K.ToString());

                    // Save the new XData
                    pnlRb = new ResultBuffer(pnlData);
                    pnl.XData = pnlRb;
                }

                // Commit and dispose the transaction
                trans.Commit();
            }

            // If all went OK, notify the user
            Global.ed.WriteMessage("\nStifness matrix of panels obtained.");
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
                                   K = data[10].Value.ToString();

                            msgstr = "Panel " + pnlNum + "\n\n" +
                                     "Stifness Matrix: \n" + K;
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
