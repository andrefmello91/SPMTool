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
using MathNet.Numerics.Statistics;

[assembly: CommandClass(typeof(SPMTool.Analysis.Stringer))]

namespace SPMTool
{
    public partial class Analysis
    {
        public class Stringer
        {
            // Stringer properties
            public ObjectId       ObjectId        { get; set; }
            public int            Number          { get; set; }
            public int[]          Grips           { get; set; }
            public int[]          Index           { get; set; }
            public Point3d[]      PointsConnected { get; set; }
            public int            NumberOfBars    { get; set; }
            public double         Length          { get; set; }
            public double         Angle           { get; set; }
            public double         Width           { get; set; }
            public double         Height          { get; set; }
            public double         BarDiameter     { get; set; }
            public double         ConcreteArea    { get; set; }
            public double         SteelArea       { get; set; }
            public Matrix<double> TransMatrix     { get; set; }
            public Matrix<double> LocalStiffness  { get; set; }
            public Vector<double> Forces          { get; set; }

            // Read the parameters of a stringer
            public static Stringer[] Parameters(ObjectIdCollection stringerObjects)
            {
                Stringer[] stringers = new Stringer[stringerObjects.Count];

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId strObj in stringerObjects)
                    {
                        // Read the object as a line
                        Line strLine = trans.GetObject(strObj, OpenMode.ForRead) as Line;

                        // Get the length and angles
                        double
                            L = strLine.Length,
                            alpha = strLine.Angle;

                        // Calculate midpoint
                        var midPt = Auxiliary.MidPoint(strLine.StartPoint, strLine.EndPoint);

                        // Read the XData and get the necessary data
                        ResultBuffer rb = strLine.GetXDataForApplication(AutoCAD.appName);
                        TypedValue[] data = rb.AsArray();

                        // Get the stringer number
                        int
                            num   = Convert.ToInt32(data[(int) XData.Stringer.Number].Value),
                            nBars = Convert.ToInt32(data[(int) XData.Stringer.NumOfBars].Value);

                        // Create the list of grips
                        int[] grips =
                        {
                            Convert.ToInt32(data[(int) XData.Stringer.Grip1].Value),
                            Convert.ToInt32(data[(int) XData.Stringer.Grip2].Value),
                            Convert.ToInt32(data[(int) XData.Stringer.Grip3].Value)
                        };

                        double
                            w   = Convert.ToDouble(data[(int) XData.Stringer.Width].Value),
                            h   = Convert.ToDouble(data[(int) XData.Stringer.Height].Value),
                            phi = Convert.ToDouble(data[(int) XData.Stringer.BarDiam].Value);

                        // Calculate the cross sectional area
                        double A = w * h;

                        // Calculate the reinforcement area
                        double As = Reinforcement.StringerReinforcement(nBars, phi);

                        // Calculate the concrete area
                        double Ac = A - As;

                        // Get the index
                        int i = num - 1;

                        // Get the global indexes as an array
                        int[] ind = GlobalIndexes(grips);

                        // Set the values
                        stringers[i] = new Stringer
                        {
                            ObjectId = strObj,
                            Number = num,
                            Grips = grips,
                            Index = ind,
                            PointsConnected = new []{ strLine.StartPoint, midPt, strLine.EndPoint },
                            NumberOfBars = nBars,
                            Length = L,
                            Angle = alpha,
                            Width = w,
                            Height = h,
                            BarDiameter = phi,
                            ConcreteArea = Ac,
                            SteelArea = As
                        };
                    }

                    // Return the parameters
                    return stringers;
                }
            }

            // Get the list of continued stringers
            public static List<Tuple<int, int>> ContinuedStringers(Stringer[] stringers)
            {
                // Initialize a Tuple to store the continued stringers
                var contStrs = new List<Tuple<int, int>>();
            
                // Calculate the parameter of continuity
                double par = Math.Sqrt(2) / 2;

                // Verify in the list what stringers are continuous
                foreach (var str1 in stringers)
                {
                    // Access the number
                    int num1 = str1.Number;

                    foreach (var str2 in stringers)
                    {
                        // Access the number
                        int num2 = str2.Number;

                        // Verify if it's other stringer
                        if (num1 != num2)
                        {
                            // Create a tuple with the minimum stringer number first
                            var contStr = Tuple.Create(Math.Min(num1, num2), Math.Max(num1, num2));

                            // Verify if it's already on the list
                            if (!contStrs.Contains(contStr))
                            {
                                // Verify the cases
                                // Case 1: stringers initiate or end at the same node
                                if (str1.Grips[0] == str2.Grips[0] || str1.Grips[2] == str2.Grips[2])
                                {
                                    // Get the direction cosines
                                    double[]
                                        dir1 = Auxiliary.DirectionCosines(str1.Angle),
                                        dir2 = Auxiliary.DirectionCosines(str2.Angle);
                                    double 
                                        l1 = dir1[0], 
                                        m1 = dir1[1],
                                        l2 = dir2[0], 
                                        m2 = dir2[1];

                                    // Calculate the condition of continuity
                                    double cont = l1 * l2 + m1 * m2;

                                    // Verify the condition
                                    if (cont < -par) // continued stringer
                                    {
                                        // Add to the list
                                        contStrs.Add(contStr);
                                    }
                                }

                                // Case 2: a stringer initiate and the other end at the same node
                                if (str1.Grips[0] == str2.Grips[2] || str1.Grips[2] == str2.Grips[0])
                                {
                                    // Get the direction cosines
                                    double[]
                                        dir1 = Auxiliary.DirectionCosines(str1.Angle),
                                        dir2 = Auxiliary.DirectionCosines(str2.Angle);
                                    double
                                        l1 = dir1[0],
                                        m1 = dir1[1],
                                        l2 = dir2[0],
                                        m2 = dir2[1];

                                    // Calculate the condition of continuity
                                    double cont = l1 * l2 + m1 * m2;

                                    // Verify the condition
                                    if (cont > par) // continued stringer
                                    {
                                        // Add to the list
                                        contStrs.Add(contStr);
                                    }
                                }
                            }
                        }
                    }
                }

                // Order the list
                contStrs = contStrs.OrderBy(str => str.Item2).ThenBy(str => str.Item1).ToList();

                // Return the list
                return contStrs;
            }

            // View the continued stringers
            [CommandMethod("ViewContinuedStringers")]
            public static void ViewContinuedStringers()
            {
                // Update and get the elements collection
                ObjectIdCollection 
                    nds = Geometry.Node.UpdateNodes(),
                    strs = Geometry.Stringer.UpdateStringers();

                // Get the parameters
                var stringers = Parameters(strs);

                // Get the list of continued stringers
                var contStrs = Stringer.ContinuedStringers(stringers);

                // Initialize a message to show
                string msg = "Continued stringers: ";

                // If there is none
                if (contStrs.Count == 0)
                    msg += "None.";

                // Write all the continued stringers
                else
                {
                    foreach (var contStr in contStrs)
                    {
                        msg += contStr.Item1 + " - " + contStr.Item2 + ", ";
                    }
                }

                // Write the message in the editor
                AutoCAD.edtr.WriteMessage(msg);
            }

            // Add the stringer stiffness to the global matrix
            public static void GlobalStiffness(Stringer stringer, Matrix<double> K, Matrix<double> Kg)
            {
                // Get the positions in the global matrix
                var index = stringer.Index;
                int i = index[0],
                    j = index[1],
                    k = index[2];

                // Initialize an index for lines of the local matrix
                int o = 0;

                // Add the local matrix to the global at the DoFs positions
                // n = index of the node in global matrix
                // o = index of the line in the local matrix
                foreach (int ind in index)
                {
                    for (int n = ind; n <= ind + 1; n++)
                    {
                        // Line o
                        // Check if the row is composed of zeroes
                        if (K.Row(o).Exists(Auxiliary.NotZero))
                        {
                            Kg[n, i] += K[o, 0];         Kg[n, i + 1] += K[o, 1];
                            Kg[n, j] += K[o, 2];         Kg[n, j + 1] += K[o, 3];
                            Kg[n, k] += K[o, 4];         Kg[n, k + 1] += K[o, 5];
                        }

                        // Increment the line index
                        o++;
                    }
                }
            }

            // Calculate the transformation matrix
            public static Matrix<double> TransformationMatrix(Stringer stringer)
            {
                // Get the direction cosines
                double[] dirCos = Auxiliary.DirectionCosines(stringer.Angle);
                double
                    l = dirCos[0],
                    m = dirCos[1];

                // Obtain the transformation matrix
                var T = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    {l, m, 0, 0, 0, 0 },
                    {0, 0, l, m, 0, 0 },
                    {0, 0, 0, 0, l, m }
                });

                return T;
            }

            // Calculate stringer forces and return the maximum absolute stringer force
            public static double StringerForces(Stringer[] stringers, Vector<double> u)
            {
                // Create a matrix to store the stringer forces
                var strForces = Matrix<double>.Build.Dense(stringers.Length, 3);

                foreach (var str in stringers)
                {
                    // Get the parameters
                    int[] ind = str.Index;
                    var Kl = str.LocalStiffness;
                    var T = str.TransMatrix;

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

                    // Save to the stringer
                    str.Forces = fl;

                    // Set to the matrix of forces
                    int i = str.Number - 1;
                    strForces.SetRow(i, fl);

                    //Global.ed.WriteMessage("\nStringer " + strNum.ToString() + ":\n" + fl.ToString());
                }

                // Verify the maximum stringer force in the model to draw in an uniform scale
                double fMax = strForces.Enumerate().MaximumAbsolute();

                return fMax;
            }

            public class Linear
            {
                // Calculate the stiffness matrix stringers, save to XData and add to global stiffness matrix, set the matrices to each element
                public static void StringersStiffness(Stringer[] stringers, double Ec, Matrix<double> Kg)
                {
                    // Calculate the stringers stiffness matrix and add to the global stiffness matrix
                    foreach (var str in stringers)
                    {
                        // Read the parameters
                        double 
                            L  = str.Length,
                            Ac = str.ConcreteArea;

                        // Obtain the transformation matrix
                        var T = TransformationMatrix(str);

                        // Calculate the constant factor of stifness
                        double EcAOverL = Ec * Ac / L;

                        // Calculate the local stiffness matrix
                        var Kl = EcAOverL * Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            {  4, -6,  2 },
                            { -6, 12, -6 },
                            {  2, -6,  4 }
                        });

                        // Calculate the transformated stiffness matrix
                        var K = T.Transpose() * Kl * T;

                        // Add to the global matrix
                        GlobalStiffness(str, K, Kg);

                        // Save the stringer parameters
                        str.TransMatrix = T;
                        str.LocalStiffness = Kl;

                        //DelimitedWriter.Write("D:/SPMTooldataS" + num + ".csv", K, ";");
                    }
                }
            }

            public class NonLinear
            {
                // SPMTool default analysis methods
                public class Default
                {
                    // Calculate the strain on a stringer given a force N and the concrete parameters
                    //public static double StringerStrain(double N, double Ac, double As, List<double> concParams, List<double> steelParams)
                    //{
                    //    // Get the parameters
                    //    concParams = Material.Concrete.ConcreteParams();
                    //    steelParams = Material.Steel.SteelParams();

                    //    // Initialize the strain
                    //    double e = 0;

                    //    if (concParams != null)
                    //    {
                    //        // Get the values for concrete
                    //        double fcm = concParams[0],
                    //            fcr = concParams[1],
                    //            Eci = concParams[2],
                    //            Ec1 = concParams[3],
                    //            ec1 = concParams[4],
                    //            k = concParams[5];

                    //        // Get the values for steel
                    //        double fy = steelParams[0],
                    //            Es = steelParams[1],
                    //            ey = steelParams[2];

                    //        // Calculate ps and xi
                    //        double ps = As / Ac,
                    //            xi = ps * Es / Eci;

                    //        // Calculate maximum forces of concrete and steel
                    //        double Ncm = -fcm * Ac,
                    //            Ny = fy * As;

                    //        // Verify the value of N
                    //        if (N > 0) // tensioned stringer
                    //        {
                    //            // Calculate critical force for concrete remain uncracked
                    //            double Ncr = fcr * Ac * (1 + xi);

                    //            if (N <= Ncr) // uncracked
                    //                e = N / (Eci * Ac * (1 + xi));

                    //            else // cracked
                    //            {
                    //                // Calculate ssr
                    //                double ssr = (fcr / ps) * (1 + xi);

                    //                e = (1 / Es) * (N / As - 0.6 * ssr);
                    //            }
                    //        }

                    //        if (N < 0) // compressed stringer
                    //        {
                    //            // Calculate K1 and K2
                    //            double K1 = 1 / ec1 * (-Ncm / ec1 + Es * As * (k - 2)),
                    //                K2 = 1 / ec1 * (Ncm * k - N * (k - 2)) + Es * As;

                    //            // Compare ey and ec1
                    //            if (ey < ec1) // steel yields before concrete crushing
                    //            {
                    //                // Calculate the yield force and the limit force on the stringer
                    //                double Nyc = -Ny + Ncm * (-k * ey / ec1 - Math.Pow(-ey / ec1, 2)) / (1 - (k - 2) * ey / ec1),
                    //                    Nlim = -Ny + Ncm;

                    //                // Verify the value of N
                    //                if (Nlim <= N && N <= Nyc)
                    //                {
                    //                    // Calculate the constants K3, K4 and K5
                    //                    double K3 = -Ncm / (ec1 * ec1),
                    //                        K4 = 1 / ec1 * (Ncm * k - (Ny + N) * (k - 2)),
                    //                        K5 = -Ny - N;

                    //                    // Calculate the strain
                    //                    e = (-K4 + Math.Sqrt(K4 * K4 - 4 * K3 * K5)) / (2 * K3);
                    //                }

                    //                else
                    //                    e = (-K2 + Math.Sqrt(K2 * K2 + 4 * K1 * N)) / (2 * K1);
                    //            }

                    //            else // steel yields together or after concrete crushing
                    //            {
                    //                e = (-K2 + Math.Sqrt(K2 * K2 + 4 * K1 * N)) / (2 * K1);
                    //            }
                    //        }
                    //    }

                    //    return e;
                    //}
                }

                // Classic SpanCAD Methods
                public class Classic
                {
                    //// Calculate the strain on a stringer given a force N and the concrete parameters
                    //public static double StringerStrain(double N, double Ac, double As)
                    //{
                    //    // Get the parameters of materials
                    //    var concParams = Material.Concrete.ConcreteParams();
                    //    var steelParams = Material.Steel.SteelParams();

                    //    // Initialize the strain
                    //    double e = 0;

                    //    if (concParams != null)
                    //    {
                    //        // Get the values for concrete
                    //        double fcm = concParams[0],
                    //            fcr = concParams[1],
                    //            Eci = concParams[2],
                    //            Ec1 = concParams[3],
                    //            ec1 = concParams[4],
                    //            k = concParams[5];

                    //        // Get the values for steel
                    //        double fy = steelParams[0],
                    //            Es = steelParams[1],
                    //            ey = steelParams[2];

                    //        // Calculate ps and xi
                    //        double ps = As / Ac,
                    //            xi = ps * Es / Eci;

                    //        // Calculate maximum forces of concrete and steel
                    //        double Nc = -fcm * Ac,
                    //            Nyr = fy * As;

                    //        // Verify the value of N
                    //        if (N > 0) // tensioned stringer
                    //        {
                    //            // Calculate critical force for concrete remain uncracked
                    //            double Ncr = fcr * Ac * (1 + xi);

                    //            if (N <= Ncr) // uncracked
                    //                e = N / (Eci * Ac * (1 + xi));

                    //            else // cracked
                    //            {
                    //                // Calculate ssr
                    //                double Nr = Ncr / (Math.Sqrt(1 + xi));

                    //                e = (N * N - Nr * Nr) / (Es * As * N);
                    //            }
                    //        }

                    //        if (N < 0) // compressed stringer
                    //        {
                    //            // Calculate ec
                    //            double ec = -2 * fcm / Eci;

                    //            // Calculate the yield force
                    //            double Nyc = -fy * As + fcm * Ac * (2 * ey / ec - ey / ec * ey / ec);

                    //            // Compare ey and ec1
                    //            if (ey < ec) // steel yields before concrete crushing
                    //            {
                    //                // Calculate the ultimate force on the stringer
                    //                double Nt = -Nyr + Nc;

                    //                // Verify the value of N
                    //                if (N >= Nyc) // steel not yielding
                    //                    e = ec * (1 + xi - Math.Sqrt((1 + xi) * (1 + xi) - N / Nc));

                    //                else // steel yielding
                    //                    e = ec * (1 - Math.Sqrt(1 - (Nyr + N) / Nc));
                    //            }

                    //            else // steel yields together or after concrete crushing
                    //            {
                    //                e = ec * (1 + xi - Math.Sqrt((1 + xi) * (1 + xi) - N / Nc));
                    //            }
                    //        }
                    //    }

                    //    return e;
                    //}

                    //// Calculate the stringer stiffness
                    //public static Matrix<double> StringerStiffness(double L, double N1, double N3, double Ac, double As, double ec, double ey)
                    //{
                    //    // Calculate the approximated strains
                    //    double eps1 = StringerStrain(N1, Ac, As),
                    //        eps2 = StringerStrain(2 / 3 * N1 + N3 / 3, Ac, As),
                    //        eps3 = StringerStrain(N1 / 3 + 2 / 3 * N3, Ac, As),
                    //        eps4 = StringerStrain(N3, Ac, As);

                    //    // Calculate the flexibility matrix elements
                    //    double de1N1 = L / 24 * (3 * eps1 + 4 * eps2 + eps3),
                    //        de1N2 = L / 12 * (eps2 + eps3),
                    //        de2N2 = L / 24 * (eps2 + 4 * eps3 + 3 * eps4);

                    //    // Get the flexibility matrix
                    //    var F = Matrix<double>.Build.DenseOfArray(new double[,]
                    //    {
                    //        { de1N1, de1N2},
                    //        { de1N2, de2N2}
                    //    });

                    //    // Get the B matrix
                    //    var B = Matrix<double>.Build.DenseOfArray(new double[,]
                    //    {
                    //        { -1,  1, 0},
                    //        {  0, -1, 1}
                    //    });

                    //    // Calculate local stiffness matrix and return the value
                    //    var Kl = B.Transpose() * F.Inverse() * B;

                    //    return Kl;
                    //}

                    //// Calculate the total plastic generalized strain in a stringer
                    //public static double StringerPlasticStrain(double eps, double ec, double ey, double L)
                    //{
                    //    // Initialize the plastic strain
                    //    double ep = 0;

                    //    // Case of tension
                    //    if (eps > ey)
                    //        ep = L / 8 * (eps - ey);

                    //    // Case of compression
                    //    if (eps < ec)
                    //        ep = L / 8 * (eps - ec);

                    //    return ep;
                    //}

                    //// Calculate the maximum plastic strain in a stringer for tension and compression
                    //public static Tuple<double, double> StringerMaxPlasticStrain(double L, double b, double h, double ey, double esu, double ec1, double ecu)
                    //{
                    //    // Calculate the maximum plastic strain for tension
                    //    double eput = 0.3 * esu * L;

                    //    // Calculate the maximum plastic strain for compression
                    //    double et = Math.Max(ec1, -ey);
                    //    double a = Math.Min(b, h);
                    //    double epuc = (ecu - et) * a;

                    //    // Return a tuple in order Tension || Compression
                    //    return Tuple.Create(eput, epuc);
                    //}
                }
            }
        }
    }
}