using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.Data.Text;

[assembly: CommandClass(typeof(SPMTool.LinearAnalysis))]

namespace SPMTool
{
    class LinearAnalysis
    {
        [CommandMethod("DoLinearAnalysis")]
        public void DoLinearAnalysis()
        {
            // Get the concrete parameters
            var concParams = Material.ConcreteParams();

            // Verify if concrete parameters were set
            if (concParams != null)
            {
                // Get the elastic modulus
                double Ec = concParams[2];

                // Calculate the aproximated shear modulus (elastic material)
                double Gc = Ec / 2.4;

                // Update and get the elements collection
                ObjectIdCollection nds = Geometry.UpdateNodes(),
                                   strs = Geometry.UpdateStringers(),
                                   pnls = Geometry.UpdatePanels();

                // Get the list of node positions
                List<Point3d> ndList = Geometry.ListOfNodes("All");

                // Initialize the global stiffness matrix
                var Kg = Matrix<double>.Build.Dense(2 * nds.Count, 2 * nds.Count);
                
                // Calculate the stifness of each stringer and panel, add to the global stiffness and get the matrices of the stiffness of elements
                var strParams = StringersLinearStifness(strs, ndList, Ec, Kg);
                var pnlParams = PanelsLinearStifness(pnls, ndList, Gc, Kg);

                // Get the force vector and the constraints vector
                var f = Forces.ForceVector();
                var cons = Constraints.ConstraintList();

                // Simplify the stifness matrix
                SimplifyStiffnessMatrix(Kg, f, ndList, cons);

                // Solve the sistem
                var u = Kg.Solve(f);

                // Calculate the stringer, panel forces and nodal displacements
                Results.StringerForces(strs, strParams, u);
                Results.PanelForces(pnls, pnlParams, u);
                Results.NodalDisplacements(nds, strs, ndList, u);

                // If all went OK, notify the user
                //DelimitedWriter.Write("D:/SPMTooldataU.csv", u.ToColumnMatrix(), ";");
                //DelimitedWriter.Write("D:/SPMTooldataK.csv", Kg, ";");
            }
            else
            {
                Application.ShowAlertDialog("Please set the material parameters.");
            }
        }

        // Calculate the stifness matrix stringers, save to XData and add to global stifness matrix, returns the all the matrices in an numbered list
        public List<Tuple<int, int[], Matrix<double>, Matrix<double>>> StringersLinearStifness(ObjectIdCollection stringers, List<Point3d> nodeList, double Ec, Matrix<double> Kg)
        {
            // Initialize a tuple list to store the matrices of stringers
            List<Tuple<int, int[], Matrix<double>, Matrix<double>>> strMats = new List<Tuple<int, int[], Matrix<double>, Matrix<double>>>();

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
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
                    Point3d strMidPt = Auxiliary.MidPoint(str.StartPoint, str.EndPoint);

                    // Read the XData and get the necessary data
                    ResultBuffer strRb = str.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] strData = strRb.AsArray();
                    int strNum = Convert.ToInt32(strData[StringerXDataIndex.num].Value);
                    double wd  = Convert.ToDouble(strData[StringerXDataIndex.w].Value),
                           h   = Convert.ToDouble(strData[StringerXDataIndex.h].Value);

                    // Calculate the cross sectional area
                    double A = wd * h;

                    // Get the direction cosines
                    var (l, m) = Auxiliary.DirectionCosines(alpha);

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
                        // Check if the row is composed of zeroes
                        if (K.Row(o).Exists(Auxiliary.NotZero))
                        {
                            Kg[n, i] = Kg[n, i] + K[o, 0];              Kg[n, i + 1] = Kg[n, i + 1] + K[o, 1];
                            Kg[n, j] = Kg[n, j] + K[o, 2];              Kg[n, j + 1] = Kg[n, j + 1] + K[o, 3];
                            Kg[n, k] = Kg[n, k] + K[o, 4];              Kg[n, k + 1] = Kg[n, k + 1] + K[o, 5];
                        }

                        // Increment the line index
                        o++;

                        // Line o + 1
                        // Check if the row is composed of zeroes
                        if (K.Row(o).Exists(Auxiliary.NotZero))
                        {
                            Kg[n + 1, i] = Kg[n + 1, i] + K[o, 0];      Kg[n + 1, i + 1] = Kg[n + 1, i + 1] + K[o, 1];
                            Kg[n + 1, j] = Kg[n + 1, j] + K[o, 2];      Kg[n + 1, j + 1] = Kg[n + 1, j + 1] + K[o, 3];
                            Kg[n + 1, k] = Kg[n + 1, k] + K[o, 4];      Kg[n + 1, k + 1] = Kg[n + 1, k + 1] + K[o, 5];
                        }

                        // Increment the line index
                        o++;
                    }

                    // Save to the list of stringer parameters
                    strMats.Add(Tuple.Create(strNum, ind, Kl, T));
                    //DelimitedWriter.Write("D:/SPMTooldataS" + strNum + ".csv", K, ";");
                }
            }

            // Order and return the list
            strMats = strMats.OrderBy(tuple => tuple.Item1).ToList();
            return strMats;
        }

        // Calculate the stifness matrix of a panel, get the dofs and save to XData, returns the all the matrices in an ordered list
        public List<Tuple<int, int[], Matrix<double>, Matrix<double>>> PanelsLinearStifness(ObjectIdCollection panels, List<Point3d> nodeList, double Gc, Matrix<double> Kg)
        {
            // Initialize a tuple list to store the matrices of stringers
            List<Tuple<int, int[], Matrix<double>, Matrix<double>>> pnlMats = new List<Tuple<int, int[], Matrix<double>, Matrix<double>>>(panels.Count);

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Get the stringers stifness matrix and add to the global stifness matrix
                foreach (ObjectId obj in panels)
                {
                    // Read as a solid
                    Solid pnl = trans.GetObject(obj, OpenMode.ForWrite) as Solid;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the vertices in the order needed for calculations
                    Point3d nd1 = pnlVerts[0],
                            nd2 = pnlVerts[1],
                            nd3 = pnlVerts[3],
                            nd4 = pnlVerts[2];

                    // Get the dofs
                    Point3d grip1 = Auxiliary.MidPoint(nd1, nd2),
                            grip2 = Auxiliary.MidPoint(nd2, nd3),
                            grip3 = Auxiliary.MidPoint(nd3, nd4),
                            grip4 = Auxiliary.MidPoint(nd4, nd1);

                    // Read the XData and get the necessary data
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] pnlData = pnlRb.AsArray();

                    // Get the panel number and width
                    int pnlNum = Convert.ToInt32(pnlData[PanelXDataIndex.num].Value);
                    double t   = Convert.ToDouble(pnlData[PanelXDataIndex.w].Value);

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
                    if (ang2 == Constants.piOver2 && ang4 == Constants.piOver2)
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
                    var (m1, n1) = Auxiliary.DirectionCosines(ln1.Angle);
                    var (m2, n2) = Auxiliary.DirectionCosines(ln2.Angle);
                    var (m3, n3) = Auxiliary.DirectionCosines(ln3.Angle);
                    var (m4, n4) = Auxiliary.DirectionCosines(ln4.Angle);

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

                    // Get the positions in the global matrix
                    int i = 2 * nodeList.IndexOf(grip1),
                        j = 2 * nodeList.IndexOf(grip2),
                        k = 2 * nodeList.IndexOf(grip3),
                        l = 2 * nodeList.IndexOf(grip4);

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
                        // Check if the row is composed of zeroes
                        if (K.Row(o).Exists(Auxiliary.NotZero))
                        {
                            Kg[n, i] = Kg[n, i] + K[o, 0];              Kg[n, i + 1] = Kg[n, i + 1] + K[o, 1];
                            Kg[n, j] = Kg[n, j] + K[o, 2];              Kg[n, j + 1] = Kg[n, j + 1] + K[o, 3];
                            Kg[n, k] = Kg[n, k] + K[o, 4];              Kg[n, k + 1] = Kg[n, k + 1] + K[o, 5];
                            Kg[n, l] = Kg[n, l] + K[o, 6];              Kg[n, l + 1] = Kg[n, l + 1] + K[o, 7];
                        }

                        // Increment the line index
                        o++;

                        // Line o + 1
                        // Check if the row is composed of zeroes
                        if (K.Row(o).Exists(Auxiliary.NotZero))
                        {
                            Kg[n + 1, i] = Kg[n + 1, i] + K[o, 0];      Kg[n + 1, i + 1] = Kg[n + 1, i + 1] + K[o, 1];
                            Kg[n + 1, j] = Kg[n + 1, j] + K[o, 2];      Kg[n + 1, j + 1] = Kg[n + 1, j + 1] + K[o, 3];
                            Kg[n + 1, k] = Kg[n + 1, k] + K[o, 4];      Kg[n + 1, k + 1] = Kg[n + 1, k + 1] + K[o, 5];
                            Kg[n + 1, l] = Kg[n + 1, l] + K[o, 6];      Kg[n + 1, l + 1] = Kg[n + 1, l + 1] + K[o, 7];
                        }

                        // Increment the line index
                        o++;
                    }

                    // Save to the list of panel parameters
                    pnlMats.Add(Tuple.Create(pnlNum, ind, Kl, T));
                    //DelimitedWriter.Write("D:/SPMTooldataP" + pnlNum + ".csv", K, ";");
                }
            }

            // Order and return the list
            pnlMats.OrderBy(tuple => tuple.Item1);
            return pnlMats;
        }

        // Simplify the stiffness matrix
        public void SimplifyStiffnessMatrix(Matrix<double> Kg, Vector<double> f, List<Point3d> allNds, IEnumerable<Tuple<int, double>> constraints)
        {
            // Get the list of internal nodes
            List<Point3d> intNds = Geometry.ListOfNodes("Int");

            // Simplify the matrices removing the rows that have constraints
            foreach (Tuple<int, double> con in constraints)
            {
                // Simplification by the constraints
                if (con.Item2 == 0) // There is a support in this direction
                {
                    // Get the index of the row
                    int i = con.Item1;

                    // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
                    Kg.ClearRow(i);
                    Kg.ClearColumn(i);

                    // Set the diagonal element to 1
                    Kg[i, i] = 1;

                    // Clear the row in the force vector
                    f[i] = 0;
                    
                    // So ui = 0
                }
            }

            // Simplification for internal nodes (There is only a displacement at the stringer direction, the perpendicular one will be zero)
            foreach (Point3d intNd in intNds)
            {
                // Get the index of the global matrix
                int i = 2 * allNds.IndexOf(intNd);

                // Verify what line of the matrix is composed of zeroes
                if (!Kg.Row(i).Exists(Auxiliary.NotZero))
                {
                    // The row is composed of only zeroes, so the displacement must be zero
                    // Set the diagonal element to 1
                    Kg[i, i] = 1;

                    // Clear the row in the force vector
                    f[i] = 0;
                }

                if (!Kg.Row(i + 1).Exists(Auxiliary.NotZero))
                {
                    // The row is composed of only zeroes, so the displacement must be zero
                    // Set the diagonal element to 1
                    Kg[i + 1, i + 1] = 1;

                    // Clear the row in the force vector
                    f[i + 1] = 0;
                }
                // Else nothing is done
            }
        }

        //[CommandMethod("ViewElasticStifness")]
        //public void ViewElasticStifness()
        //{
        //    // Initialize the message to display
        //    string msgstr = "";

        //    // Request the object to be selected in the drawing area
        //    PromptEntityOptions entOp = new PromptEntityOptions("\nSelect an element to print the stiffness matrix:");
        //    PromptEntityResult entRes = AutoCAD.edtr.GetEntity(entOp);

        //    // If the prompt status is OK, objects were selected
        //    if (entRes.Status == PromptStatus.OK)
        //    {
        //        // Start a transaction
        //        using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
        //        {
        //            // Open the selected object for read
        //            Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

        //            // If it's a stringer
        //            if (ent.Layer == Layers.strLyr)
        //            {
        //                // Get the extended data attached to each object for MY_APP
        //                ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);

        //                // Make sure the Xdata is not empty
        //                if (rb != null)
        //                {
        //                    // Get the XData as an array
        //                    TypedValue[] data = rb.AsArray();

        //                    // Get the parameters
        //                    string strNum = data[StringerXDataIndex.strNum].Value.ToString(),
        //                           kl     = data[10].Value.ToString(),
        //                           k      = data[11].Value.ToString();

        //                    msgstr = "Stringer " + strNum + "\n\n" +
        //                             "Local Stifness Matrix: \n" +
        //                             kl + "\n\n" +
        //                             "Transformated Stifness Matrix: \n" +
        //                             k;
        //                }

        //                else msgstr = "NONE";
        //            }

        //            // If it's a panel
        //            if (ent.Layer == Layers.pnlLyr)
        //            {
        //                // Get the extended data attached to each object for MY_APP
        //                ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);

        //                // Make sure the Xdata is not empty
        //                if (rb != null)
        //                {
        //                    // Get the XData as an array
        //                    TypedValue[] data = rb.AsArray();

        //                    // Get the parameters
        //                    string pnlNum = data[2].Value.ToString(),
        //                           Kl     = data[10].Value.ToString(),
        //                           K      = data[11].Value.ToString();

        //                    msgstr = "Panel " + pnlNum + "\n\n" +
        //                             "Local Stifness Matrix:\n" + Kl + "\n" +
        //                             "Global Stifness Matrix:\n" + K;

        //                }

        //                else msgstr = "NONE";
        //            }

        //            //else msgstr = "Object is not a stringer or panel.";

        //            // Display the values returned
        //            AutoCAD.edtr.WriteMessage("\n" + msgstr);
        //        }
        //    }
        //}
    }
}
