using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace SPMTool
{
    public partial class Analysis
    {
        public partial class Panel
        {
            public partial class NonLinear
            {
                partial class Membrane
                {
                    public class MCFT
                    {
	                    private static Material.Concrete Concrete;
	                    private static Material.Steel    Steel;

                        // Calculate D matrix (input stress vector and initial D matrix)
                        public static Membrane MCFTMain(Membrane membrane, Vector<double> sigma, int ls)
                        {
                            // Max number of iterations
                            int maxIter = 1000;

							// Get the initial stiffness
							var Di = membrane.Stiffness;

                            // Initiate a loop for the iterations
                            double tol;
                            for (int it = 1; it <= maxIter; it++)
                            {
                                // Calculate the strains
                                var e = Di.Solve(sigma);

                                // Calculate the principal strains
                                ConcretePrincipalStrains(membrane);

                                // Calculate the angle of principal strains
                                StrainSlope(membrane);

								// Calculate reinforcement stresses
								ReinforcementsStresses(membrane);

                                // Calculate principal stresses in concrete
                                ConcreteStresses(membrane, ls);

                                // Calculate material stiffness
                                ConcreteStiffness(membrane);
                                SteelStiffness(membrane);

								// Get material stiffness
								var D = membrane.Stiffness;

                                // Verify the tolerance
                                var tolMat = D - Di;
                                tol = tolMat.Enumerate().MaximumAbsolute();

                                // Assign Di for a new loop
                                Di = D;

                                // Verify if convergence is reached
                                if (tol < 0.0001)  // Convergence reached
                                {
                                    // Verify if concrete cracked in this step
                                    if (membrane.LSCrack == 0 && membrane.ConcreteStrains.ec1 >= Concrete.ecr)
	                                    membrane.LSCrack = ls;

                                    // Verify if concrete reached it's peak stress
                                    if (membrane.LSPeak == 0 && membrane.ConcreteStrains.ec2 <= Concrete.ec1)
	                                    membrane.LSPeak = ls;

                                    break;
                                }

                                if (it == maxIter) // Not reached, analysis must stop
                                    membrane.Stop = true;
                            }

                            return membrane;
                        }

                        // Calculate strain slope
                        private static void StrainSlope(Membrane membrane)
                        {
							// Get the strains
							var e = membrane.Strains;

							// Calculate the strain slope
                            double theta;
                            if (e[2] == 0)
                                theta = 0;

                            else if (e[0] == e[1])
                                theta = Constants.PiOver4;

                            else
                            {
                                double tan2Angle = e[2] / (e[1] - e[0]);
                                theta = 0.5 * Math.Atan(tan2Angle);

                                // Theta must be positive
                                if (theta < 0)
                                    theta += Constants.PiOver2;
                            }

                            membrane.StrainSlope = theta;
                        }

                        // Calculate principal stresses in concrete
                        static void ConcreteStresses(Membrane membrane, int ls)
                        {
							// Get the values
							double
								fc  = Concrete.fcm,
								ec  = Concrete.ec1,
								Ec  = Concrete.Eci,
								ec1 = membrane.ConcreteStrains.ec1,
								ec2 = membrane.ConcreteStrains.ec2;

                            // Calculate the maximum concrete compressive stress
                            double
                                f2maxA = fc / (0.8 - 0.34 * ec1 / ec),
                                f2max = Math.Max(f2maxA, fc);

                            // Calculate the principal compressive stress in concrete
                            double
                                n = ec2 / ec,
                                fc2 = f2max * (2 * n - n * n);

                            // Calculate principal tensile stress
                            double fc1;

                            // Constitutive relation
                            if (ec1 <= Concrete.ecr) // Not cracked
                                fc1 = ec1 * Ec;

                            else // cracked
                            {
                                // Calculate the principal tensile stress in concrete by crack check procedure
                                fc1 = CrackCheck(membrane, ls);
                            }

                            membrane.ConcreteStresses = (fc1, fc2);
                        }

                        // Crack check procedure
                        private static double CrackCheck(Membrane membrane, int ls)
                        {
	                        // Get the values
	                        double
		                        fc    = Concrete.fcm,
		                        fcr   = Concrete.fctm,
		                        phiAg = Concrete.AggregateDiameter,
		                        ec1   = membrane.ConcreteStrains.ec1,
		                        theta = membrane.StrainSlope,
		                        px    = membrane.ReinforcementRatio.X,
		                        py    = membrane.ReinforcementRatio.Y,
		                        fsx   = membrane.ReinforcementStresses.fsx,
		                        fsy   = membrane.ReinforcementStresses.fsy,
		                        smx   = membrane.CrackSpacing.X,
		                        smy   = membrane.CrackSpacing.Y;

                            // Constitutive relation
                            double f1a = fcr / (1 + Math.Sqrt(500 * ec1));

                            // Calculate thetaC sine and cosine
                            var dirCos = Auxiliary.DirectionCosines(theta);
                            double
                                cosTheta = dirCos[0],
                                sinTheta = dirCos[1],
                                tanTheta = Auxiliary.Tangent(theta);

                            // Average crack spacing and opening
                            double
                                smTheta = 1 / (sinTheta / smx + cosTheta / smy),
                                w = smTheta * ec1;

                            // Reinforcement capacity reserve
                            double
                                f1cx = px * (Steel.fy - fsx),
                                f1cy = py * (Steel.fy - fsy);

                            // Maximum possible shear on crack interface
                            double vcimaxA = 0.18 * Math.Sqrt(Math.Abs(fc)) / (0.31 + 24 * w / (phiAg + 16));

                            // Maximum possible shear for biaxial yielding
                            double vcimaxB = Math.Abs(f1cx - f1cy) * sinTheta * cosTheta;

                            // Maximum shear on crack
                            double vcimax = Math.Min(vcimaxA, vcimaxB);

                            // Biaxial yielding condition
                            double f1b = f1cx * cosTheta * cosTheta + f1cy * sinTheta * sinTheta;

                            // Maximum tensile stress for equilibrium in X and Y
                            double
                                f1c = f1cx + vcimax / tanTheta,
                                f1d = f1cy + vcimax * tanTheta;

                            // Calculate the minimum tensile stress
                            var f1List = new[] { f1a, f1b, f1c, f1d };
                            var f1 = f1List.Min();

                            // Calculate shear on crack
                            StressesOnCrack(membrane, f1cx, f1cy, tanTheta, ls);

                            return f1;
                        }

                        // Calculate shear on crack
                        static void StressesOnCrack(Membrane membrane, double f1cx, double f1cy, double tanTheta, int ls)
                        {
	                        double
		                        f1  = membrane.ConcreteStresses.fc1,
		                        px  = membrane.ReinforcementRatio.X,
		                        py  = membrane.ReinforcementRatio.Y,
		                        fsx = membrane.ReinforcementStresses.fsx,
		                        fsy = membrane.ReinforcementStresses.fsy;

	                        // Initiate vci = 0 (for most common cases)
                            double vci = 0;

                            if (f1cx > f1cy && f1cy < f1) // Y dominant
                                vci = (f1 - f1cy) / tanTheta;

                            if (f1cx < f1cy && f1cx < f1) // X dominant
                                vci = (f1cx - f1) * tanTheta;

                            // Reinforcement stresses
                            double
                                fsxcr = (f1 + vci / tanTheta) / px + fsx,
                                fsycr = (f1 + vci * tanTheta) / py + fsy;

                            // Check if reinforcement yielded at crack
                            if (membrane.LSYieldX == 0 && fsxcr >= Steel.fy)
	                            membrane.LSYieldX = ls;

                            if (membrane.LSYieldY == 0 && fsycr >= Steel.fy)
	                            membrane.LSYieldY = ls;
                        }
                    }
                }
            }
        }
    }
}
