using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using System.Windows.Media;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.Integration;
using MathNet.Numerics.Optimization;

namespace SPMTool
{
	public partial class Analysis
	{
		public partial class Panel
		{
			public partial class NonLinear
			{
				public  class Membrane
				{
					// Properties
					public bool Stop                                              { get; set; }
					public string StopMessage                                     { get; set; }
					public int LSCrack                                            { get; set; }
					public int LSYieldX                                           { get; set; }
					public int LSYieldY                                           { get; set; }
					public int LSPeak                                             { get; set; }
					public double[] StrainDirectionCosines                        { get; set; }
					public (double[] X, double[] Y) ReinforcementDirectionCosines { get; set; }
					public (double X, double Y) ReinforcementRatio                { get; set; }
					public double[] ConcreteStrains                               { get; set; }
					public double[] ConcreteStresses                              { get; set; }
					public Matrix<double> ConcreteMatrix                          { get; set; }
					public Matrix<double> SteelMatrix                             { get; set; }
					public Matrix<double> Stiffness                               { get; set; }
					public Vector<double> Strains                                 { get; set; }
					public Vector<double> Stresses                                { get; set; }

                    // Constructor
                    public Membrane()
					{
						Stop = Stop;
						StopMessage = StopMessage;
						LSCrack = LSCrack;
						LSYieldX = LSYieldX;
						LSYieldY = LSYieldY;
						LSPeak = LSPeak;
						StrainDirectionCosines = StrainDirectionCosines;
						ReinforcementDirectionCosines = ReinforcementDirectionCosines;
						ReinforcementRatio = ReinforcementRatio;
						ConcreteStrains = ConcreteStrains;
						ConcreteStresses = ConcreteStresses;
						ConcreteMatrix = ConcreteMatrix;
						SteelMatrix = SteelMatrix;
						Stiffness = ConcreteMatrix + SteelMatrix;
						Strains = Strains;
						Stresses = Stresses;
					}

					// Calculate the initial material matrix for initiating the iterations
					public static void InitialStiffness(Membrane membrane)
					{
						// Calculate initial Gc
						double Gc0 = 0.5 * Concrete.Eci;

						// Get the initial material stiffness
						membrane.ConcreteMatrix = ConcreteStiffness(dirCosTheta, Concrete.Eci, Concrete.Eci, Gc0);
						membrane.SteelMatrix = SteelStiffness(Steel.Es, Steel.Es);
					}

					// Calculate concrete principal strains
					private static void ConcretePrincipalStrains(Membrane membrane)
					{
						// Get the apparent strains and concrete net strains
						double
							ecx  = membrane.ConcreteStrains[0],
							ecy  = membrane.ConcreteStrains[1],
							ycxy = membrane.ConcreteStrains[2];

						// Calculate radius and center of Mohr's Circle
						double
							cen = 0.5 * (ecx + ecy),
							rad = 0.5 * Math.Sqrt((ecx - ecy) * (ecx - ecy) + ycxy * ycxy);

						// Calculate principal strains in concrete
						double
							ec1 = cen + rad,
							ec2 = cen - rad;

						membrane.ConcreteStrains = new [] { ec1, ec2 };
					}

					// Calculate slopes related to reinforcement
					private static void ReinforcementSlopes(Membrane membrane, double theta)
					{
						// Calculate angles
						double
							thetaNx = theta,
							thetaNy = theta - Constants.piOver2;

						// Calculate trigonometric relations
						double[]
							dircosNx = Auxiliary.DirectionCosines(thetaNx),
							dirCosNy = Auxiliary.DirectionCosines(thetaNy);

						membrane.ReinforcementDirectionCosines = (dircosNx, dirCosNy);
					}

					// Calculate steel stiffness matrix
					private static void SteelStiffness(Membrane membrane)
					{
						// Get the strains
						double
							ex = membrane.Strains[0],
							ey = membrane.Strains[1];

						// Calculate stresses and secant moduli
						double fsx, fsy, Esx, Esy;
						ReinforcementStresses();
						SteelSecantModuli();

                        // Steel matrix
                        var Ds = Matrix<double>.Build.Dense(3, 3);
						Ds[0, 0] = membrane.ReinforcementRatio.X * Esx;
						Ds[1, 1] = membrane.ReinforcementRatio.Y * Esy;

						membrane.SteelMatrix = Ds;

						// Calculate reinforcement stresses
						void ReinforcementStresses()
						{
							if (ex >= 0)
								fsx = Math.Min(Steel.Es * ex, Steel.fy);

							else
								fsx = Math.Max(Steel.Es * ex, -Steel.fy);

							if (ey >= 0)
								fsy = Math.Min(Steel.Es * ey, Steel.fy);

							else
								fsy = Math.Max(Steel.Es * ey, -Steel.fy);
						}

						// Calculate secant moduli of steel
						void SteelSecantModuli()
						{
							// Steel
							if (ex == 0 || fsx == 0)
								Esx = Steel.Es;

							else
								Esx = Math.Min(fsx / ex, Steel.Es);

							if (ey == 0 || fsy == 0)
								Esy = Steel.Es;

							else
								Esy = Math.Min(fsy / ey, Steel.Es);
						}
                    }

					// Calculate concrete stiffness matrix
					private static void ConcreteStiffness(Membrane membrane)
					{
						// Get the strains and stresses
						double
							ec1 = membrane.ConcreteStrains[0],
							ec2 = membrane.ConcreteStrains[1],
							fc1 = membrane.ConcreteStresses[0],
							fc2 = membrane.ConcreteStresses[1];

						// Get secant moduli
						double Ec1, Ec2, Gc;
						ConcreteSecantModuli();

						// Calculate concrete transformation matrix
						Matrix<double> T;
						TransMatrix();

						// Concrete matrix
						var Dc1 = Matrix<double>.Build.Dense(3, 3);
						Dc1[0, 0] = Ec1;
						Dc1[1, 1] = Ec2;
						Dc1[2, 2] = Gc;

						// Calculate Dc
						membrane.ConcreteMatrix = T.Transpose() * Dc1 * T;

						// Calculate secant moduli of concrete
						void ConcreteSecantModuli()
						{
							if (ec1 == 0 || fc1 == 0)
								Ec1 = Concrete.Eci;

							else
								Ec1 = fc1 / ec1;

							if (ec2 == 0 || fc2 == 0)
								Ec2 = Concrete.Eci;

							else
								Ec2 = fc2 / ec2;

							Gc = Ec1 * Ec2 / (Ec1 + Ec2);
						}

						// Calculate concrete transformation matrix
						void TransMatrix()
						{
							double
								cosTheta = membrane.StrainDirectionCosines[0],
								sinTheta = membrane.StrainDirectionCosines[1],
								cos2 = cosTheta * cosTheta,
								sin2 = sinTheta * sinTheta,
								cosSin = cosTheta * sinTheta;

							T = Matrix<double>.Build.DenseOfArray(new double[,]
							{
								{         cos2,       sin2,      cosSin },
								{         sin2,       cos2,    - cosSin },
								{ - 2 * cosSin, 2 * cosSin, cos2 - sin2 }
							});
                        }
                    }
                }
            }

            // Calculate crack spacings
            private static double
                smx = Steel.phiX / (5.4 * Steel.psx),
                smy = Steel.phiY / (5.4 * Steel.psy);

            // Calculate the initial material matrix for initiating the iterations
            public static Matrix<double> InitialStiffness(double[] dirCosTheta)
            {
                // Calculate initial Gc
                double Gc0 = 0.5 * Concrete.Ec;

                // Get the initial material stiffness
                var Dc = ConcreteStiffness(dirCosTheta, Concrete.Ec, Concrete.Ec, Gc0);
                var Ds = SteelStiffness(Steel.Esx, Steel.Esy);
                var Di = Dc + Ds;

                return Di;
            }

            // Calculate concrete principal strains
            private static (double ec1, double ec2) ConcreteStrains(Vector<double> ec)
            {
                // Get the apparent strains and concrete net strains
                double
                    ecx = ec[0],
                    ecy = ec[1],
                    ycxy = ec[2];

                // Calculate radius and center of Mohr's Circle
                double
                    cen = 0.5 * (ecx + ecy),
                    rad = 0.5 * Math.Sqrt((ecx - ecy) * (ecx - ecy) + ycxy * ycxy);

                // Calculate principal strains in concrete
                double
                    ec1 = cen + rad,
                    ec2 = cen - rad;

                return (ec1, ec2);
            }

            // Calculate slopes related to reinforcement
            private static (double[] dirCosNx, double[] dirCosNy) ReinforcementSlopes(double theta)
            {
                // Calculate angles
                double
                    thetaNx = theta,
                    thetaNy = theta - Constants.PiOver2;

                // Calculate trigonometric relations
                double[]
                    dircosNx = DirectionCosines(thetaNx),
                    dirCosNy = DirectionCosines(thetaNy);

                return (dircosNx, dirCosNy);
            }

            // Calculate reinforcement stresses
            private static (double fsx, double fsy) ReinforcementStresses(Vector<double> e)
            {
                // Initiate the stresses
                double fsx, fsy;

                // Get the strains
                double
                    ex = e[0],
                    ey = e[1];

                if (ex >= 0)
                    fsx = Math.Min(Steel.Esx * ex, Steel.fyx);

                else
                    fsx = Math.Max(Steel.Esx * ex, -Steel.fyx);

                if (ey >= 0)
                    fsy = Math.Min(Steel.Esy * ey, Steel.fyy);

                else
                    fsy = Math.Max(Steel.Esy * ey, -Steel.fyy);

                return (fsx, fsy);
            }

            // Calculate secant moduli of concrete
            private static (double Ec1, double Ec2, double Gc) ConcreteSecantModuli(double ec1, double ec2, double fc1, double fc2)
            {
                double Ec1, Ec2;

                if (ec1 == 0 || fc1 == 0)
                    Ec1 = Concrete.Ec;

                else
                    Ec1 = fc1 / ec1;

                if (ec2 == 0 || fc2 == 0)
                    Ec2 = Concrete.Ec;

                else
                    Ec2 = fc2 / ec2;

                double Gc = Ec1 * Ec2 / (Ec1 + Ec2);

                return (Ec1, Ec2, Gc);
            }

            // Calculate secant moduli of steel
            private static (double Esx, double Esy) SteelSecantModuli(double fsx, double fsy, Vector<double> e)
            {
                double Esx, Esy;

                // Get the strains
                double
                    ex = e[0],
                    ey = e[1];

                // Steel
                if (ex == 0 || fsx == 0)
                    Esx = Steel.Esx;

                else
                    Esx = Math.Min(fsx / ex, Steel.Esx);

                if (ey == 0 || fsy == 0)
                    Esy = Steel.Esy;

                else
                    Esy = Math.Min(fsy / ey, Steel.Esy);

                return (Esx, Esy);
            }

            // Calculate concrete stiffness matrix
            private static Matrix<double> ConcreteStiffness(double[] dirCosTheta, double Ec1, double Ec2, double Gc)
            {
                // Calculate concrete transformation matrix
                double
                    cosTheta = dirCosTheta[0],
                    sinTheta = dirCosTheta[1],
                    cos2 = cosTheta * cosTheta,
                    sin2 = sinTheta * sinTheta,
                    cosSin = cosTheta * sinTheta;

                var T = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    {         cos2,       sin2,      cosSin },
                    {         sin2,       cos2,    - cosSin },
                    { - 2 * cosSin, 2 * cosSin, cos2 - sin2 }
                });

                // Concrete matrix
                var Dc1 = Matrix<double>.Build.Dense(3, 3);
                Dc1[0, 0] = Ec1;
                Dc1[1, 1] = Ec2;
                Dc1[2, 2] = Gc;

                // Calculate Dc
                var Dc = T.Transpose() * Dc1 * T;

                return Dc;
            }

            // Calculate steel stiffness matrix
            private static Matrix<double> SteelStiffness(double Esx, double Esy)
            {
                // Steel matrix
                var Ds = Matrix<double>.Build.Dense(3, 3);
                Ds[0, 0] = Steel.psx * Esx;
                Ds[1, 1] = Steel.psy * Esy;

                return Ds;
            }
        }
    }
}
