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

                // Initialize the global stiffness matrix
                var Kg = Matrix<double>.Build.Dense(2 * nds.Count, 2 * nds.Count);

                // Calculate the stifness of each stringer and panel, add to the global stiffness and get the matrices of the stiffness of elements
                var (StrsK, StrsT) = StringersLinearStifness(strs, ndList, Ec, Kg);
                var (PnlsK, PnlsT) = PanelsLinearStifness(pnls, ndList, Gc, Kg);

                // Get the force vector and the constraints vector
                var f = ForceVector();
                var cons = ConstraintVector();

                // Get cons as a enumerated list (row, column, value)
                var consEnum = cons.EnumerateIndexed();
                
                // Simplify the matrices removing the lines that have supports
                foreach (Tuple<int, int, double> con in consEnum)
                {
                    if (con.Item3 == 0) // There is a support in this direction
                    {
                        // Get the index of the row
                        int i = con.Item1;

                        // Remove the row and column [i] in the stiffness matrix
                        Kg = Kg.RemoveRow(i);
                        Kg = Kg.RemoveColumn(i);

                        // Remove the row in the force vector and the constraint vector
                        f = f.RemoveRow(i);
                    }
                }

                // If all went OK, notify the user
                Global.ed.WriteMessage(Kg.ToString() + "\n" + f.ToString());
            }
            else
            {
                Application.ShowAlertDialog("Please set the material parameters.");
            }
        }

        // Calculate the stifness matrix stringers, save to XData and add to global stifness matrix
        public (Matrix<double> StrsK, Matrix<double> StrsT) StringersLinearStifness(ObjectIdCollection stringers, List<Point3d> nodeList, double Ec, Matrix<double> Kg)
        {
            // Initialize matrices to store the stiffness and tranformation matrices of each stringer
            var StrsK = Matrix<double>.Build.Dense(6 * stringers.Count, 6);
            var StrsT = Matrix<double>.Build.Dense(3 * stringers.Count, 6);

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in stringers)
                {
                    // Read the object as a line
                    Line str = trans.GetObject(obj, OpenMode.ForWrite) as Line;

                    // Get the length and angles
                    double lngt = str.Length,
                    alpha = str.Angle;                          // angle with x coordinate

                    // Get the midpoint
                    Point3d strMidPt = AuxMethods.MidPoint(str.StartPoint, str.EndPoint);

                    // Read the XData and get the necessary data
                    ResultBuffer strRb = str.GetXDataForApplication(Global.appName);
                    TypedValue[] strData = strRb.AsArray();
                    int strNum = Convert.ToInt32(strData[2].Value);
                    double wd  = Convert.ToDouble(strData[7].Value),
                           h   = Convert.ToDouble(strData[8].Value);

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
                    str.XData = strRb;

                    // Copy the matrix to the collection of stringer matrices at
                    int idxK = 6 * strNum - 6,
                        idxT = 3 * strNum - 3;
                    StrsK.SetSubMatrix(idxK, 0, K);
                    StrsT.SetSubMatrix(idxT, 0, T);

                    // Get the positions in the global matrix
                    int i = 2 * nodeList.IndexOf(str.StartPoint),               // DoF 1
                        j = 2 * nodeList.IndexOf(strMidPt),                     // DoF 2
                        k = 2 * nodeList.IndexOf(str.EndPoint);                 // DoF 3

                    // Get the indexes as an array
                    int[] ind = { i, j, k };

                    // Initialize an index for lines of the local matrix
                    int o = 0;

                    // Add the local matrix to the global at the DoFs positions
                    // n = index of the node in global matrix
                    // o = index of the line in the local matrix
                    foreach (int n in ind)
                    {
                        // Line o
                        Kg[n, i] = Kg[n, i] + K[o, 0];                  Kg[n, i + 1] = Kg[n, i + 1] + K[o, 1];
                        Kg[n, j] = Kg[n, j] + K[o, 2];                  Kg[n, j + 1] = Kg[n, j + 1] + K[o, 3];
                        Kg[n, k] = Kg[n, k] + K[o, 4];                  Kg[n, k + 1] = Kg[n, k + 1] + K[o, 5];

                        // Increment the line index
                        o++;

                        // Line o + 1
                        Kg[n + 1, i] = Kg[n + 1, i] + K[o, 0];          Kg[n + 1, i + 1] = Kg[n + 1, i + 1] + K[o, 1];
                        Kg[n + 1, j] = Kg[n + 1, j] + K[o, 2];          Kg[n + 1, j + 1] = Kg[n + 1, j + 1] + K[o, 3];
                        Kg[n + 1, k] = Kg[n + 1, k] + K[o, 4];          Kg[n + 1, k + 1] = Kg[n + 1, k + 1] + K[o, 5];

                        // Increment the line index
                        o++;
                    }

                    //// 1st line              
                    //Kg[i, i] = Kg[i, i] + K[0, 0];              Kg[i, i + 1] = Kg[i, i + 1] + K[0, 1];
                    //Kg[i, j] = Kg[i, j] + K[0, 2];              Kg[i, j + 1] = Kg[i, j + 1] + K[0, 3];
                    //Kg[i, k] = Kg[i, k] + K[0, 4];              Kg[i, k + 1] = Kg[i, k + 1] + K[0, 5];

                    //// 2nd line
                    //Kg[i + 1, i] = Kg[i + 1, i] + K[1, 0];      Kg[i + 1, i + 1] = Kg[i + 1, i + 1] + K[1, 1];
                    //Kg[i + 1, j] = Kg[i + 1, j] + K[1, 2];      Kg[i + 1, j + 1] = Kg[i + 1, j + 1] + K[1, 3];
                    //Kg[i + 1, k] = Kg[i + 1, k] + K[1, 4];      Kg[i + 1, k + 1] = Kg[i + 1, k + 1] + K[1, 5];

                    //// 3rd line
                    //Kg[j, i] = Kg[j, i] + K[2, 0];              Kg[j, i + 1] = Kg[j, i + 1] + K[2, 1];
                    //Kg[j, j] = Kg[j, j] + K[2, 2];              Kg[j, j + 1] = Kg[j, j + 1] + K[2, 3];
                    //Kg[j, k] = Kg[j, k] + K[2, 4];              Kg[j, k + 1] = Kg[j, k + 1] + K[2, 5];

                    //// 4th line
                    //Kg[j + 1, i] = Kg[j + 1, i] + K[3, 0];      Kg[j + 1, i + 1] = Kg[j + 1, i + 1] + K[3, 1];
                    //Kg[j + 1, j] = Kg[j + 1, j] + K[3, 2];      Kg[j + 1, j + 1] = Kg[j + 1, j + 1] + K[3, 3];
                    //Kg[j + 1, k] = Kg[j + 1, k] + K[3, 4];      Kg[j + 1, k + 1] = Kg[j + 1, k + 1] + K[3, 5];

                    //// 5th line
                    //Kg[k, i] = Kg[k, i] + K[4, 0];              Kg[k, i + 1] = Kg[k, i + 1] + K[4, 1];
                    //Kg[k, j] = Kg[k, j] + K[4, 2];              Kg[k, j + 1] = Kg[k, j + 1] + K[4, 3];
                    //Kg[k, k] = Kg[k, k] + K[4, 4];              Kg[k, k + 1] = Kg[k, k + 1] + K[4, 5];

                    //// 6th line
                    //Kg[k + 1, i] = Kg[k + 1, i] + K[5, 0];      Kg[k + 1, i + 1] = Kg[k + 1, i + 1] + K[5, 1];
                    //Kg[k + 1, j] = Kg[k + 1, j] + K[5, 2];      Kg[k + 1, j + 1] = Kg[k + 1, j + 1] + K[5, 3];
                    //Kg[k + 1, k] = Kg[k + 1, k] + K[5, 4];      Kg[k + 1, k + 1] = Kg[k + 1, k + 1] + K[5, 5];
                }
            }
            return (StrsK, StrsT);
        }

        // Calculate the stifness matrix of a panel, get the dofs and save to XData
        public (Matrix<double> PnlsK, Matrix<double> PnlsT) PanelsLinearStifness(ObjectIdCollection panels, List<Point3d> nodeList, double Gc, Matrix<double> Kg)
        {
            // Initialize matrices to store the stiffness and tranformation matrices of each panel
            var PnlsK = Matrix<double>.Build.Dense(8 * panels.Count, 8);
            var PnlsT = Matrix<double>.Build.Dense(4 * panels.Count, 8);

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in panels)
                {
                    // Read as a solid
                    Solid panel = trans.GetObject(obj, OpenMode.ForWrite) as Solid;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    panel.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the vertices in the order needed for calculations
                    Point3d nd1 = pnlVerts[0],
                            nd2 = pnlVerts[1],
                            nd3 = pnlVerts[3],
                            nd4 = pnlVerts[2];

                    // Get the dofs
                    Point3d dof1 = AuxMethods.MidPoint(nd1, nd2),
                            dof2 = AuxMethods.MidPoint(nd2, nd3),
                            dof3 = AuxMethods.MidPoint(nd3, nd4),
                            dof4 = AuxMethods.MidPoint(nd4, nd1);

                    // Read the XData and get the necessary data
                    ResultBuffer pnlRb = panel.GetXDataForApplication(Global.appName);
                    TypedValue[] pnlData = pnlRb.AsArray();

                    // Get the panel number and width
                    int pnlNum = Convert.ToInt32(pnlData[2].Value);
                    double t   = Convert.ToDouble(pnlData[7].Value);

                    // Create lines to measure the angles between the edges
                    Line ln1 = new Line(nd1, nd2),
                         ln2 = new Line(nd2, nd3),
                         ln3 = new Line(nd3, nd4),
                         ln4 = new Line(nd4, nd1);

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

                    // Copy the matrix to the collection of stringer matrices at
                    int idxK = 8 * pnlNum - 8,
                        idxT = 4 * pnlNum - 4;
                    PnlsK.SetSubMatrix(idxK, 0, K);
                    PnlsT.SetSubMatrix(idxT, 0, T);

                    // Get the positions in the global matrix
                    int i = 2 * nodeList.IndexOf(dof1),
                        j = 2 * nodeList.IndexOf(dof2),
                        k = 2 * nodeList.IndexOf(dof3),
                        l = 2 * nodeList.IndexOf(dof4);

                    // Get the indexes as an array
                    int[] ind = { i, j, k, l };

                    // Initialize an index for lines of the local matrix
                    int o = 0;

                    // Add the local matrix to the global at the DoFs positions
                    // i = index of the node in global matrix
                    // o = index of the line in the local matrix
                    foreach (int n in ind)
                    {
                        // Line o
                        Kg[n, i] = Kg[n, i] + K[o, 0];                  Kg[n, i + 1] = Kg[n, i + 1] + K[o, 1];
                        Kg[n, j] = Kg[n, j] + K[o, 2];                  Kg[n, j + 1] = Kg[n, j + 1] + K[o, 3];
                        Kg[n, k] = Kg[n, k] + K[o, 4];                  Kg[n, k + 1] = Kg[n, k + 1] + K[o, 5];
                        Kg[n, l] = Kg[n, l] + K[o, 6];                  Kg[n, l + 1] = Kg[n, l + 1] + K[o, 7];

                        // Increment the line index
                        o++;

                        // Line o + 1
                        Kg[n + 1, i] = Kg[n + 1, i] + K[o, 0];          Kg[n + 1, i + 1] = Kg[n + 1, i + 1] + K[o, 1];
                        Kg[n + 1, j] = Kg[n + 1, j] + K[o, 2];          Kg[n + 1, j + 1] = Kg[n + 1, j + 1] + K[o, 3];
                        Kg[n + 1, k] = Kg[n + 1, k] + K[o, 4];          Kg[n + 1, k + 1] = Kg[n + 1, k + 1] + K[o, 5];
                        Kg[n + 1, l] = Kg[n + 1, l] + K[o, 6];          Kg[n + 1, l + 1] = Kg[n + 1, l + 1] + K[o, 7];

                        // Increment the line index
                        o++;
                    }

                    //// Add the local matrix to the global at the DoFs positions
                    //// 1st line
                    //Kg[i, i] = Kg[i, i] + K[0, 0];              Kg[i, i + 1] = Kg[i, i + 1] + K[0, 1];
                    //Kg[i, j] = Kg[i, j] + K[0, 2];              Kg[i, j + 1] = Kg[i, j + 1] + K[0, 3];
                    //Kg[i, k] = Kg[i, k] + K[0, 4];              Kg[i, k + 1] = Kg[i, k + 1] + K[0, 5];
                    //Kg[i, l] = Kg[i, l] + K[0, 6];              Kg[i, l + 1] = Kg[i, l + 1] + K[0, 7];

                    //// 2nd line
                    //Kg[i + 1, i] = Kg[i + 1, i] + K[1, 0];      Kg[i + 1, i + 1] = Kg[i + 1, i + 1] + K[1, 1];
                    //Kg[i + 1, j] = Kg[i + 1, j] + K[1, 2];      Kg[i + 1, j + 1] = Kg[i + 1, j + 1] + K[1, 3];
                    //Kg[i + 1, k] = Kg[i + 1, k] + K[1, 4];      Kg[i + 1, k + 1] = Kg[i + 1, k + 1] + K[1, 5];
                    //Kg[i + 1, l] = Kg[i + 1, l] + K[1, 6];      Kg[i + 1, l + 1] = Kg[i + 1, l + 1] + K[1, 7];

                    //// 3rd line
                    //Kg[j, i] = Kg[j, i] + K[2, 0];              Kg[j, i + 1] = Kg[j, i + 1] + K[2, 1];
                    //Kg[j, j] = Kg[j, j] + K[2, 2];              Kg[j, j + 1] = Kg[j, j + 1] + K[2, 3];
                    //Kg[j, k] = Kg[j, k] + K[2, 4];              Kg[j, k + 1] = Kg[j, k + 1] + K[2, 5];
                    //Kg[j, l] = Kg[j, l] + K[2, 6];              Kg[j, l + 1] = Kg[j, l + 1] + K[2, 7];

                    //// 4th line
                    //Kg[j + 1, i] = Kg[j + 1, i] + K[3, 0];      Kg[j + 1, i + 1] = Kg[j + 1, i + 1] + K[3, 1];
                    //Kg[j + 1, j] = Kg[j + 1, j] + K[3, 2];      Kg[j + 1, j + 1] = Kg[j + 1, j + 1] + K[3, 3];
                    //Kg[j + 1, k] = Kg[j + 1, k] + K[3, 4];      Kg[j + 1, k + 1] = Kg[j + 1, k + 1] + K[3, 5];
                    //Kg[j + 1, l] = Kg[j + 1, l] + K[3, 6];      Kg[j + 1, l + 1] = Kg[j + 1, l + 1] + K[3, 7];

                    //// 5th line
                    //Kg[k, i] = Kg[k, i] + K[4, 0];              Kg[k, i + 1] = Kg[k, i + 1] + K[4, 1];
                    //Kg[k, j] = Kg[k, j] + K[4, 2];              Kg[k, j + 1] = Kg[k, j + 1] + K[4, 3];
                    //Kg[k, k] = Kg[k, k] + K[4, 4];              Kg[k, k + 1] = Kg[k, k + 1] + K[4, 5];
                    //Kg[k, l] = Kg[k, l] + K[4, 6];              Kg[k, l + 1] = Kg[k, l + 1] + K[4, 7];

                    //// 6th line
                    //Kg[k + 1, i] = Kg[k + 1, i] + K[5, 0];      Kg[k + 1, i + 1] = Kg[k + 1, i + 1] + K[5, 1];
                    //Kg[k + 1, j] = Kg[k + 1, j] + K[5, 2];      Kg[k + 1, j + 1] = Kg[k + 1, j + 1] + K[5, 3];
                    //Kg[k + 1, k] = Kg[k + 1, k] + K[5, 4];      Kg[k + 1, k + 1] = Kg[k + 1, k + 1] + K[5, 5];
                    //Kg[k + 1, l] = Kg[k + 1, l] + K[5, 6];      Kg[k + 1, l + 1] = Kg[k + 1, l + 1] + K[5, 7];

                    //// 7th line
                    //Kg[l, i] = Kg[l, i] + K[6, 0];              Kg[l, i + 1] = Kg[l, i + 1] + K[6, 1];
                    //Kg[l, j] = Kg[l, j] + K[6, 2];              Kg[l, j + 1] = Kg[l, j + 1] + K[6, 3];
                    //Kg[l, k] = Kg[l, k] + K[6, 4];              Kg[l, k + 1] = Kg[l, k + 1] + K[6, 5];
                    //Kg[l, l] = Kg[l, l] + K[6, 6];              Kg[l, l + 1] = Kg[l, l + 1] + K[6, 7];

                    //// 8th line
                    //Kg[l + 1, i] = Kg[l + 1, i] + K[7, 0];      Kg[l + 1, i + 1] = Kg[l + 1, i + 1] + K[7, 1];
                    //Kg[l + 1, j] = Kg[l + 1, j] + K[7, 2];      Kg[l + 1, j + 1] = Kg[l + 1, j + 1] + K[7, 3];
                    //Kg[l + 1, k] = Kg[l + 1, k] + K[7, 4];      Kg[l + 1, k + 1] = Kg[l + 1, k + 1] + K[7, 5];
                    //Kg[l + 1, l] = Kg[l + 1, l] + K[7, 6];      Kg[l + 1, l + 1] = Kg[l + 1, l + 1] + K[7, 7];
                }
            }
            return (PnlsK, PnlsT);
        }

        // Get the force vector
        public Matrix<double> ForceVector()
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
            //Global.ed.WriteMessage("\nVector of forces:\n" + f.ToString());
            return f.ToColumnMatrix();
        }
        
        // Get the constraint vector to get the support conditions
        public Matrix<double> ConstraintVector()
        {
            // Access the nodes in the model
            ObjectIdCollection nds = AuxMethods.AllNodes();

            // Get the number of DoFs
            int numDofs = nds.Count;

            // Initialize the constraint list with size 2x number of nodes (displacements in x and y)
            // Assign 1 (free node) initially to each value
            var cons = Vector<double>.Build.Dense(2 * numDofs, 1);

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
                    if (sup.Contains("X")) cons.At(i, 0);
                    if (sup.Contains("Y")) cons.At(i + 1, 0);
                }
            }

            // Write the values
            //Global.ed.WriteMessage("\nVector of displacements:\n" + u.ToString());
            return cons.ToColumnMatrix();
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
