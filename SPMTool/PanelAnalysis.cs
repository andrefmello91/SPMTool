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

[assembly: CommandClass(typeof(SPMTool.Analysis.Panel))]

namespace SPMTool
{
    public partial class Analysis
    {
        public class Panel
        {
            // Read the parameters of a panel
            public static Tuple<int, Point3d[], int[], double[], double[], double, double[]> Parameters(ObjectId panel)
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Read as a solid
                    Solid pnl = trans.GetObject(panel, OpenMode.ForWrite) as Solid;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Get the vertices in the order needed for calculations
                    Point3d nd1 = pnlVerts[0],
                        nd2 = pnlVerts[1],
                        nd3 = pnlVerts[3],
                        nd4 = pnlVerts[2];

                    // Read the XData and get the necessary data
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(AutoCAD.appName);
                    TypedValue[] pnlData = pnlRb.AsArray();

                    // Get the panel number and width
                    int num  = Convert.ToInt32(pnlData[PanelXDataIndex.num].Value);
                    double t = Convert.ToDouble(pnlData[PanelXDataIndex.w].Value);

                    // Get the reinforcement
                    double phiX = Convert.ToDouble(pnlData[PanelXDataIndex.phiX].Value),
                        sx   = Convert.ToDouble(pnlData[PanelXDataIndex.sx].Value),
                        phiY = Convert.ToDouble(pnlData[PanelXDataIndex.phiY].Value),
                        sy   = Convert.ToDouble(pnlData[PanelXDataIndex.sy].Value);

                    // Create the list of grips
                    int[] grips =
                    {
                        Convert.ToInt32(pnlData[PanelXDataIndex.grip1].Value),
                        Convert.ToInt32(pnlData[PanelXDataIndex.grip2].Value),
                        Convert.ToInt32(pnlData[PanelXDataIndex.grip3].Value),
                        Convert.ToInt32(pnlData[PanelXDataIndex.grip4].Value)
                    };

                    // Create lines to measure the angles between the edges and dimensions
                    Line ln1 = new Line(nd1, nd2),
                        ln2 = new Line(nd2, nd3),
                        ln3 = new Line(nd3, nd4),
                        ln4 = new Line(nd4, nd1);

                    // Create the list of vertices
                    Point3d[] verts =
                    {
                        nd1, nd2, nd3, nd4
                    };

                    // Create the list of dimensions
                    double[] dims =
                    {
                        ln1.Length,
                        ln2.Length,
                        ln3.Length,
                        ln4.Length,
                    };

                    // Create the list of angles
                    double[] angs =
                    {
                        ln1.Angle,
                        ln2.Angle,
                        ln3.Angle,
                        ln4.Angle,
                    };

                    // Calculate the reinforcement ratio and add to a list
                    double[] ps = Reinforcement.PanelReinforcement(phiX, sx, phiY, sy, t);

                    // Add to the list of stringer parameters in the index
                    // Num || vertices || grips || dimensions || angles || t || ps ||
                    return Tuple.Create(num, verts, grips, dims, angs, t, ps);
                }
            }

            // Add the panel stiffness to the global matrix
            public static void GlobalStiffness(int[] index, Matrix<double> K, Matrix<double> Kg)
            {
                // Get the positions in the global matrix
                int i = index[0],
                    j = index[1],
                    k = index[2],
                    l = index[3];

                // Initialize an index for lines of the local matrix
                int o = 0;

                // Add the local matrix to the global at the DoFs positions
                // i = index of the node in global matrix
                // o = index of the line in the local matrix
                foreach (int n in index)
                {
                    // Line o
                    // Check if the row is composed of zeroes
                    if (K.Row(o).Exists(Auxiliary.NotZero))
                    {
                        Kg[n, i] += K[o, 0]; Kg[n, i + 1] += K[o, 1];
                        Kg[n, j] += K[o, 2]; Kg[n, j + 1] += K[o, 3];
                        Kg[n, k] += K[o, 4]; Kg[n, k + 1] += K[o, 5];
                        Kg[n, l] += K[o, 6]; Kg[n, l + 1] += K[o, 7];
                    }

                    // Increment the line index
                    o++;

                    // Line o + 1
                    // Check if the row is composed of zeroes
                    if (K.Row(o).Exists(Auxiliary.NotZero))
                    {
                        Kg[n + 1, i] += K[o, 0]; Kg[n + 1, i + 1] += K[o, 1];
                        Kg[n + 1, j] += K[o, 2]; Kg[n + 1, j + 1] += K[o, 3];
                        Kg[n + 1, k] += K[o, 4]; Kg[n + 1, k + 1] += K[o, 5];
                        Kg[n + 1, l] += K[o, 6]; Kg[n + 1, l + 1] += K[o, 7];
                    }

                    // Increment the line index
                    o++;
                }
            }

            // Calculate the necessary dimensions of a panel
            public static double[] Dimensions(Point3d[] verts)
            {
                double a = (verts[1].X + verts[2].X) / 2 - (verts[0].X + verts[3].X) / 2,
                       b = (verts[2].Y + verts[3].Y) / 2 - (verts[0].Y + verts[1].Y) / 2,
                       c = (verts[2].X + verts[3].X) / 2 - (verts[0].X + verts[1].X) / 2,
                       d = (verts[1].Y + verts[2].Y) / 2 - (verts[0].Y + verts[3].Y) / 2;

                return new double[] {a, b, c, d};
            }

            public class Linear
            {
                // Calculate the stiffness matrix of a panel, get the dofs and save to XData, returns the all the matrices in an ordered list
                public static Tuple<int[], Matrix<double>, Matrix<double>>[] PanelsStiffness(ObjectIdCollection panels, double Gc, Matrix<double> Kg)
                {
                    // Initialize a tuple list to store the matrices of stringers
                    var pnlMats = new Tuple<int[], Matrix<double>, Matrix<double>>[panels.Count];

                    // Get the stringers stifness matrix and add to the global stifness matrix
                    foreach (ObjectId obj in panels)
                    {
                        // Read the parameters
                        var pnlPrms = Panel.Parameters(obj);
                        int num = pnlPrms.Item1;
                        var verts = pnlPrms.Item2;
                        var grips = pnlPrms.Item3;
                        var L = pnlPrms.Item4;
                        var alpha = pnlPrms.Item5;
                        double t = pnlPrms.Item6;

                        // Calculate the angles between the edges
                        double ang2 = alpha[1] - alpha[0];
                        double ang4 = alpha[3] - alpha[2];

                        // Initialize the stifness matrix
                        var Kl = Matrix<double>.Build.Dense(4, 4);

                        // If the panel is rectangular (ang2 and ang4 will be equal to 90 degrees)
                        if (ang2 == Constants.piOver2 && ang4 == Constants.piOver2)
                        {
                            // Get the dimensions
                            double a = L[0],
                                b = L[1];

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
                            // Get the vertices
                            Point3d nd1 = verts[0],
                                nd2 = verts[1],
                                nd3 = verts[2],
                                nd4 = verts[3];

                            // Get the dimensions
                            double l1 = L[0],
                                   l2 = L[1],
                                   l3 = L[2],
                                   l4 = L[3];

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
                        double[] dirCos1 = Auxiliary.DirectionCosines(alpha[0]),
                            dirCos2 = Auxiliary.DirectionCosines(alpha[1]),
                            dirCos3 = Auxiliary.DirectionCosines(alpha[2]),
                            dirCos4 = Auxiliary.DirectionCosines(alpha[3]);

                        double m1 = dirCos1[0], n1 = dirCos1[1],
                            m2 = dirCos2[0], n2 = dirCos2[1],
                            m3 = dirCos3[0], n3 = dirCos3[1],
                            m4 = dirCos4[0], n4 = dirCos4[1];

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

                        // Get the indexes as an array
                        int[] ind = GlobalIndexes(grips);

                        // Add to the global matrix
                        Panel.GlobalStiffness(ind, K, Kg);

                        // Save to the list of panel parameters
                        pnlMats[num - 1] = Tuple.Create(ind, Kl, T);

                        //DelimitedWriter.Write("D:/SPMTooldataP" + num + ".csv", K, ";");
                    }

                    // Return the list
                    return pnlMats;
                }
            }

            public class NonLinear
            {
                // Calculate the generalized strains
                public static Matrix<double> MatrixA(double[] dimensions)
                {
                    // Get the dimensions
                    double a = dimensions[0],
                           b = dimensions[1],
                           c = dimensions[2],
                           d = dimensions[3];

                    // Calculate t1, t2 and t3
                    double t1 = a * b - c * d,
                           t2 = 0.5 * (a * a - c * c) + b * b - d * d,
                           t3 = 0.5 * (b * b - d * d) + a * a - c * c;

                    // Calculate the components of the matrix
                    double  at1 = a / t1,
                            bt1 = b / t1,
                            ct1 = c / t1,
                            dt1 = d / t1,
                            at2 = a / t2,
                            bt3 = b / t3,
                            a2t1 = at1 / 2,
                            b2t1 = bt1 / 2,
                            c2t1 = ct1 / 2,
                            d2t1 = dt1 / 2;

                    // Create the matrix
                    return Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        {  dt1,    0,   bt1,    0, -dt1,     0, -bt1,     0 },
                        {    0, -at1,     0, -ct1,    0,   at1,    0,   ct1 },
                        {-a2t1, d2t1, -c2t1, b2t1, a2t1, -d2t1, c2t1, -b2t1 },
                        { -at2,    0,   at2,    0, -at2,     0,  at2,     0 },
                        {    0,  bt3,     0, -bt3,    0,   bt3,    0,  -bt3 }
                    });
                }
            }
        }
    }
}