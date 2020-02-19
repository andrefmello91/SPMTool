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
            public static Tuple<int, Point3d[], int[], double[], double[], double, double[,]> Parameters(ObjectId panel)
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
                    int num  = Convert.ToInt32(pnlData[(int)XData.Panel.Number].Value);
                    double t = Convert.ToDouble(pnlData[(int)XData.Panel.Width].Value);

                    // Get the reinforcement diameter and spacing
                    double[,] reinf = new double[,]
                    {
                        { Convert.ToDouble(pnlData[(int)XData.Panel.XDiam].Value), Convert.ToDouble(pnlData[(int)XData.Panel.Sx].Value) },
                        { Convert.ToDouble(pnlData[(int)XData.Panel.YDiam].Value), Convert.ToDouble(pnlData[(int)XData.Panel.Sy].Value) }
                    };

                    // Create the list of grips
                    int[] grips =
                    {
                        Convert.ToInt32(pnlData[(int)XData.Panel.Grip1].Value),
                        Convert.ToInt32(pnlData[(int)XData.Panel.Grip2].Value),
                        Convert.ToInt32(pnlData[(int)XData.Panel.Grip3].Value),
                        Convert.ToInt32(pnlData[(int)XData.Panel.Grip4].Value)
                    };

                    // Create the list of vertices
                    Point3d[] verts =
                    {
                        nd1, nd2, nd3, nd4
                    };

                    // Create lines to measure the angles between the edges and dimensions
                    Line ln1 = new Line(nd1, nd2),
                         ln2 = new Line(nd2, nd3),
                         ln3 = new Line(nd3, nd4),
                         ln4 = new Line(nd4, nd1);

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

                    // Add to the list of stringer parameters in the index
                    // Num || vertices || grips || dimensions || angles || t || phi || s ||
                    return Tuple.Create(num, verts, grips, dims, angs, t, reinf);
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
                // Calculate the stiffness matrix of a panel, get the dofs and save to XData, returns the all the matrices in an ordered list
                public static Tuple<int[], Matrix<double>, Matrix<double>>[] PanelsStiffness(ObjectIdCollection panels, double Gc, Matrix<double> Kg)
                {
                    // Initialize a tuple list to store the matrices of stringers
                    var pnlMats = new Tuple<int[], Matrix<double>, Matrix<double>>[panels.Count];

                    // Get the stringers stifness matrix and add to the global stiffness matrix
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

                        // Get X and Y coordinates of the vertices
                        double[] x = new double[4],
                                 y = new double[4];

                        for (int i = 0; i < 4; i++)
                        {
                            x[i] = verts[i].X;
                            y[i] = verts[i].Y;
                        }

                        // Calculate the necessary dimensions of the panel
                        double a = (x[1] + x[2]) / 2 - (x[0] + x[3]) / 2,
                               b = (y[2] + y[3]) / 2 - (y[0] + y[1]) / 2,
                               c = (x[2] + x[3]) / 2 - (x[0] + x[1]) / 2,
                               d = (y[1] + y[2]) / 2 - (y[0] + y[3]) / 2;

                        // Calculate t1, t2, t3 and t4
                        double t1 = a * b - c * d,
                               t2 = 0.5 * (a * a - c * c) + b * b - d * d,
                               t3 = 0.5 * (b * b - d * d) + a * a - c * c,
                               t4 = a * a + b * b;

                        // Calculate the components of A matrix
                        double aOvert1  = a / t1,
                               bOvert1  = b / t1,
                               cOvert1  = c / t1,
                               dOvert1  = d / t1,
                               aOvert2  = a / t2,
                               bOvert3  = b / t3,
                               aOver2t1 = aOvert1 / 2,
                               bOver2t1 = bOvert1 / 2,
                               cOver2t1 = cOvert1 / 2,
                               dOver2t1 = dOvert1 / 2;

                        // Create A matrix
                        var A = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            {  dOvert1,        0,   bOvert1,        0, -dOvert1,         0, -bOvert1,         0 },
                            {        0, -aOvert1,         0, -cOvert1,        0,   aOvert1,        0,   cOvert1 },
                            {-aOver2t1, dOver2t1, -cOver2t1, bOver2t1, aOver2t1, -dOver2t1, cOver2t1, -bOver2t1 },
                            { -aOvert2,        0,   aOvert2,        0, -aOvert2,         0,  aOvert2,         0 },
                            {        0,  bOvert3,         0, -bOvert3,        0,   bOvert3,        0,  -bOvert3 }
                        });

                        // Calculate the components of B matrix
                        double cOvera = c / a,
                               dOverb = d / b;

                        // Create B matrix
                        var B = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            { 1, 0, 0, -cOvera,       0 },
                            { 0, 1, 0,       0,      -1 },
                            { 0, 0, 2,       0,       0 },
                            { 1, 0, 0,       1,       0 },
                            { 0, 1, 0,       0,  dOverb },
                            { 0, 0, 2,       0,       0 },
                            { 1, 0, 0,  cOvera,       0 },
                            { 0, 1, 0,       0,       1 },
                            { 0, 0, 2,       0,       0 },
                            { 1, 0, 0,      -1,       0 },
                            { 0, 1, 0,       0, -dOverb },
                            { 0, 0, 2,       0,       0 }
                        });

                        // Calculate the components of Q matrix
                        double a2 = a * a,
                               bc = b * c,
                               bdMt4 = b * d - t4,
                               ab = a * b,
                               MbdMt4 = -b * d - t4,
                               Tt4 = 2 * t4,
                               acMt4 = a * c - t4,
                               ad = a * d,
                               b2 = b * b,
                               MacMt4 = -a * c - t4;

                        // Create Q matrix
                        var Q = 1 / Tt4 * Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            {  a2,     bc,  bdMt4, -ab, -a2,    -bc, MbdMt4,  ab },
                            {   0,    Tt4,      0,   0,   0,      0,      0,   0 },
                            {   0,      0,    Tt4,   0,   0,      0,      0,   0 },
                            { -ab,  acMt4,     ad,  b2,  ab, MacMt4,    -ad, -b2 },
                            { -a2,    -bc, MbdMt4,  ab,  a2,     bc,  bdMt4, -ab },
                            {   0,      0,      0,   0,   0,    Tt4,      0,   0 },
                            {   0,      0,      0,   0,   0,      0,    Tt4,   0 },
                            {  ab, MacMt4,    -ad, -b2, -ab,  acMt4,     ad,  b2 }
                        });

                        // Get the indexes as an array
                        int[] ind = GlobalIndexes(grips);

                        // Add to the global matrix
                        //Panel.GlobalStiffness(ind, K, Kg);

                        // Save to the list of panel parameters
                        //pnlMats[num - 1] = Tuple.Create(ind, Kl, T);

                        //DelimitedWriter.Write("D:/SPMTooldataP" + num + ".csv", K, ";");
                    }

                    // Return the list
                    return pnlMats;
                }

                // Calculate the stresses in concrete and steel by MCFT
                public static void MCFT(double[] concPars, double phiAg, double[] fy, double[] Es, double[] phi, double[] s, Vector<double> f, double t)
                {
                    // Get the concrete parameters
                    double fc = concPars[0],
                           ec = concPars[1],
                           Ec = concPars[2],
                           fcr = concPars[3],
                           ecr = concPars[4];

                    // Get the steel parameters
                    double fyx  = fy[0],
                           fyy  = fy[1],
                           Esxi = Es[0],
                           Esyi = Es[1];

                    // Get the reinforcement
                    double phiX = phi[0],
                           phiY = phi[1],
                           sx = s[0],
                           sy = s[1];

                    // Calculate the reinforcement ratio
                    double[] ps = Reinforcement.PanelReinforcement(phi, s, t);
                    double   psx = ps[0],
                             psy = ps[1];

                    // Calculate the crack spacings
                    double smx = 2 / 3 * phiX / (3.6 * psx),
                           smy = 2 / 3 * phiY / (3.6 * psy);

                    // Initialize the strain vector
                    var em = Vector<double>.Build.Dense(3);

                    // Initialize the principal stresses
                    double f1 = 0,
                           f2 = 0;

                    // Initialize the tolerance
                    double Tol = 1;

                    // Initiate a loop
                    do
                    {
                        // Get the strains and stresses
                        double ex = em[0],
                               ey = em[1],
                               yxy = em[2],
                               f1g = f1,
                               f2g = f2;

                        // Calculate principal strains by Mohr's Circle
                        double[] ep  = Auxiliary.PrincipalStrains(em);
                        double   e1a = ep[0],
                                 e2a = ep[1];

                        // Calculate the angle of compression principal strain
                        double thetaA = Math.Atan(0.5 * yxy / (ey - ex));

                        // Components of concrete stiffness matrix
                        double Ec1, Ec2;

                        if (f1g <= 0.0001)
                            Ec1 = Ec;
                        else
                            Ec1 = f1g / e1a;

                        if (f2g <= 0.0001)
                            Ec2 = Ec;
                        else
                            Ec2 = f2g / e2a;

                        double Gc12 = Ec1 * Ec2 / (Ec1 + Ec2);

                        // Concrete stiffness matrix
                        var Dc = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            {Ec1,   0,    0},
                            {  0, Ec2,    0},
                            {  0,   0, Gc12}
                        });

                        // Components of steel stiffness matrix
                        double Esx = Math.Min(Esxi, fyx / ex),
                               Esy = Math.Min(Esyi, fyy / ey);

                        // Steel stiffness matrix
                        var Ds = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            {psx * Esx,         0, 0},
                            {        0, psy * Esy, 0},
                            {        0,         0, 0}
                        });

                        // Components of concrete transformation matrix
                        double ang = Constants.pi - thetaA;
                        double cos2 = Math.Cos(ang) * Math.Cos(ang),
                            sin2 = Math.Sin(ang) * Math.Sin(ang),
                            cos2Tsin2 = cos2 * sin2,
                            cos2Msin2 = cos2 - sin2;

                        // Concrete transformation matrix
                        var T = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            {          cos2,          sin2,  cos2Tsin2},
                            {          sin2,          cos2, -cos2Tsin2},
                            {-2 * cos2Tsin2, 2 * cos2Tsin2,  cos2Msin2}
                        });

                        // Complete stiffness matrix
                        var D = T.Transpose() * Dc * T + Ds;

                        // Calculate new strains
                        em = D.Inverse() * f;

                        // Get the strains
                        double exm  = em[0],
                               eym  = em[1],
                               yxym = em[2];

                        // Calculate principal strains by Mohr's Circle
                        ep = Auxiliary.PrincipalStrains(em);
                        double e1 = ep[0],
                               e2 = ep[1];

                        // Calculate the new angle of compression principal strain
                        double theta     = Math.Atan(0.5 * yxym / (eym - exm)),
                               sinTheta  = Math.Sin(theta),
                               cosTheta  = Math.Cos(theta),
                               tanTheta  = Math.Tan(theta),
                               sin2Theta = Math.Sin(2 * theta),
                               cos2Theta = Math.Cos(2 * theta);

                        // Calculate the new reinforcement stresses
                        double fsx = Math.Min(Esx * exm, fyx),
                               fsy = Math.Min(Esy * eym, fyy);

                        // Calculate the maximum concrete compressive stress
                        double f2maxA = fc / (0.8 + 170 * e1),
                               f2max  = Math.Min(f2maxA, fc);

                        // Calculate the principal compressive stress in concrete
                        f2 = f2max * (2 * e2 / ec - e2 / ec * e2 / ec);

                        // Calculate the principal tensile stress in concrete
                        double f1a;
                        if (e1 <= ecr)
                            f1a = e1 * Ec;
                        else
                            f1a = fcr / (1 + Math.Sqrt(500 * e1));

                        // Limit the principal tensile stress by crack check procedure
                        double smTetha = 1 / (sinTheta / smx + cosTheta / smy),                     // Crack spacing
                               w       = e1 * smTetha,                                              // Crack opening
                               f1cx    = psx * (fyx - fsx),                                         // X reinforcement capacity reserve
                               f1cy    = psy * (fyy - fsy),                                         // Y reinforcement capacity reserve
                               vcimaxA = 0.18 * Math.Sqrt(fc) / (0.31 + 24 * w / (phiAg + 16)),     // Maximum possible shear on crack interface
                               vcimaxB = Math.Abs(f1cx - f1cy) * sinTheta * cosTheta,               // Maximum possible shear for biaxial yielding
                               vcimax  = Math.Min(vcimaxA, vcimaxB),                                // Maximum shear on crack
                               f1b     = f1cx * sinTheta * sinTheta + f1cy * cosTheta * cosTheta,   // Biaxial yielding condition
                               f1c     = f1cx + vcimax * tanTheta,                                  // Maximum tensile stress for equilibrium in X
                               f1d     = f1cy + vcimax * tanTheta;                                  // Maximum tensile stress for equilibrium in Y

                        // Calculate the final tensile stress
                        var f1List = new[] {f1a, f1b, f1c, f1d};
                        f1 = f1List.Min();

                        // Final stresses
                        double rads = (f1 - f2) / 2,
                               cens = (f1 + f2) / 2;

                        // Vector of final stresses
                        var fNew = Vector<double>.Build.DenseOfArray(new double[]
                        {
                            cens - rads * cos2Theta + psx * fsx,
                            cens - rads * cos2Theta + psy * fsy,
                            rads * sin2Theta
                        });

                        // Verify the tolerance
                        var TolVec = f - fNew;
                        Tol = TolVec.AbsoluteMaximum();

                        // If convergence is not reached, start a new loop with the calculated
                        // displacements (em) and principal stresses (f1 and f2)

                    } while (Tol > 0.00001);
                }
            }
        }
    }
}