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

namespace SPMTool
{
    partial class Analysis
    {
        public class NonLinear
        {
            // SPMTool default analysis methods
            public class Default
            {
                // Calculate the strain on a stringer given a force N and the concrete parameters
                public static double StringerStrain(double N, double Ac, double As, List<double> concParams, List<double> steelParams)
                {
                    // Get the parameters
                    concParams = Material.ConcreteParams();
                    steelParams = Material.SteelParams();

                    // Initialize the strain
                    double e = 0;

                    if (concParams != null)
                    {
                        // Get the values for concrete
                        double fcm = concParams[0],
                               fcr = concParams[1],
                               Eci = concParams[2],
                               Ec1 = concParams[3],
                               ec1 = concParams[4],
                               k = concParams[5];

                        // Get the values for steel
                        double fy = steelParams[0],
                               Es = steelParams[1],
                               ey = steelParams[2];

                        // Calculate ps and xi
                        double ps = As / Ac,
                               xi = ps * Es / Eci;

                        // Calculate maximum forces of concrete and steel
                        double Ncm = -fcm * Ac,
                               Ny = fy * As;

                        // Verify the value of N
                        if (N > 0) // tensioned stringer
                        {
                            // Calculate critical force for concrete remain uncracked
                            double Ncr = fcr * Ac * (1 + xi);

                            if (N <= Ncr) // uncracked
                                e = N / (Eci * Ac * (1 + xi));

                            else // cracked
                            {
                                // Calculate ssr
                                double ssr = (fcr / ps) * (1 + xi);

                                e = (1 / Es) * (N / As - 0.6 * ssr);
                            }
                        }

                        if (N < 0) // compressed stringer
                        {
                            // Calculate K1 and K2
                            double K1 = 1 / ec1 * (-Ncm / ec1 + Es * As * (k - 2)),
                                   K2 = 1 / ec1 * (Ncm * k - N * (k - 2)) + Es * As;

                            // Compare ey and ec1
                            if (ey < ec1) // steel yields before concrete crushing
                            {
                                // Calculate the yield force and the limit force on the stringer
                                double Nyc = -Ny + Ncm * (-k * ey / ec1 - Math.Pow(-ey / ec1, 2)) / (1 - (k - 2) * ey / ec1),
                                       Nlim = -Ny + Ncm;

                                // Verify the value of N
                                if (Nlim <= N && N <= Nyc)
                                {
                                    // Calculate the constants K3, K4 and K5
                                    double K3 = -Ncm / (ec1 * ec1),
                                           K4 = 1 / ec1 * (Ncm * k - (Ny + N) * (k - 2)),
                                           K5 = -Ny - N;

                                    // Calculate the strain
                                    e = (-K4 + Math.Sqrt(K4 * K4 - 4 * K3 * K5)) / (2 * K3);
                                }

                                else
                                    e = (-K2 + Math.Sqrt(K2 * K2 + 4 * K1 * N)) / (2 * K1);
                            }

                            else // steel yields together or after concrete crushing
                            {
                                e = (-K2 + Math.Sqrt(K2 * K2 + 4 * K1 * N)) / (2 * K1);
                            }
                        }
                    }

                    return e;
                }
            }

            // Classic SpanCAD Methods
            public class Classic
            {
                // Calculate the strain on a stringer given a force N and the concrete parameters
                public static double StringerStrain(double N, double Ac, double As)
                {
                    // Get the parameters of materials
                    var concParams = Material.ConcreteParams();
                    var steelParams = Material.SteelParams();

                    // Initialize the strain
                    double e = 0;

                    if (concParams != null)
                    {
                        // Get the values for concrete
                        double fcm = concParams[0],
                               fcr = concParams[1],
                               Eci = concParams[2],
                               Ec1 = concParams[3],
                               ec1 = concParams[4],
                               k = concParams[5];

                        // Get the values for steel
                        double fy = steelParams[0],
                               Es = steelParams[1],
                               ey = steelParams[2];

                        // Calculate ps and xi
                        double ps = As / Ac,
                               xi = ps * Es / Eci;

                        // Calculate maximum forces of concrete and steel
                        double Nc = -fcm * Ac,
                               Nyr = fy * As;

                        // Verify the value of N
                        if (N > 0) // tensioned stringer
                        {
                            // Calculate critical force for concrete remain uncracked
                            double Ncr = fcr * Ac * (1 + xi);

                            if (N <= Ncr) // uncracked
                                e = N / (Eci * Ac * (1 + xi));

                            else // cracked
                            {
                                // Calculate ssr
                                double Nr = Ncr / (Math.Sqrt(1 + xi));

                                e = (N * N - Nr * Nr) / (Es * As * N);
                            }
                        }

                        if (N < 0) // compressed stringer
                        {
                            // Calculate ec
                            double ec = -2 * fcm / Eci;

                            // Calculate the yield force
                            double Nyc = -fy * As + fcm * Ac * (2 * ey / ec - ey / ec * ey / ec);

                            // Compare ey and ec1
                            if (ey < ec) // steel yields before concrete crushing
                            {
                                // Calculate the ultimate force on the stringer
                                double Nt = -Nyr + Nc;

                                // Verify the value of N
                                if (N >= Nyc) // steel not yielding
                                    e = ec * (1 + xi - Math.Sqrt((1 + xi) * (1 + xi) - N / Nc));

                                else // steel yielding
                                    e = ec * (1 - Math.Sqrt(1 - (Nyr + N) / Nc));
                            }

                            else // steel yields together or after concrete crushing
                            {
                                e = ec * (1 + xi - Math.Sqrt((1 + xi) * (1 + xi) - N / Nc));
                            }
                        }
                    }

                    return e;
                }

                // Calculate the stringer stiffness
                public static Matrix<double> StringerStiffness(double L, double N1, double N3, double Ac, double As)
                {
                    // Calculate the required strains
                    double eps1 = StringerStrain(N1, Ac, As),
                           eps2 = StringerStrain(2 / 3 * N1 + N3 / 3, Ac, As),
                           eps3 = StringerStrain(N1 / 3 + 2 / 3 * N3, Ac, As),
                           eps4 = StringerStrain(N3, Ac, As);

                    // Calculate the flexibility matrix elements
                    double de1N1 = L / 24 * (3 * eps1 + 4 * eps2 + eps3),
                           de1N2 = L / 12 * (eps2 + eps3),
                           de2N2 = L / 24 * (eps2 + 4 * eps3 + 3 * eps4);

                    // Get the flexibility matrix
                    var F = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                        { de1N1, de1N2},
                        { de1N2, de2N2}
                        });

                    // Get the B matrix
                    var B = Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                        { -1,  1, 0},
                        {  0, -1, 1}
                        });

                    // Calculate local stiffness matrix and return the value
                    var Kl = B.Transpose() * F.Inverse() * B;

                    return Kl;
                }
            }
        }
    }
}
