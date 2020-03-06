using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace SPMTool
{
    public partial class MCFT
    {
	    // Public Properties
	    public bool Stop { get; set; }
	    public string StopMessage { get; set; }
	    public int LSCrack { get; set; }
	    public int LSYieldX { get; set; }
	    public int LSYieldY { get; set; }
	    public int LSPeak { get; set; }
	    public Vector<double> Strains { get; set; }
	    public double StrainSlope { get; set; }
	    public Vector<double> AppliedStresses { get; set; }
	    public (double ec1, double ec2) ConcreteStrains { get; set; }
	    public (double X, double Y) ReinforcementSlopes { get; set; }
	    public (double X, double Y) BarDiameter { get; set; }
	    public (double X, double Y) BarSpacing { get; set; }
	    public (double X, double Y) CrackSpacing { get; set; }
	    public (double X, double Y) ReinforcementRatio { get; set; }
	    public (double fc1, double fc2) ConcreteStresses { get; set; }
	    public (double fsx, double fsy) ReinforcementStresses { get; set; }
	    public Matrix<double> ConcreteMatrix { get; set; }
	    public Matrix<double> SteelMatrix { get; set; }
	    public Vector<double> Stresses { get; set; }
	    public Matrix<double> Stiffness => ConcreteMatrix + SteelMatrix;

	    // Private properties
	    private Material.Concrete Concrete { get; }
	    private Material.Steel Steel { get; }

        // Calculate concrete parameters for MCFT
        public void MCFTConcrete(Membrane membrane, Material.Concrete concrete, Material.Steel steel)
        {
            double fc = concrete.fcm;

            // Calculate the parameters
            double
                ec = 0.002,
                Ec = 2 * fc / ec,
                ft = 0.33 * Math.Sqrt(fc),
                ecr = ft / Ec;

            concrete.ec1 = ec;
            concrete.Eci = Ec;
            concrete.fctm = ft;
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

        // Calculate concrete principal strains
        private (double ec1, double ec2) ConcretePrincipalStrains()
        {
	        // Get the apparent strains and concrete net strains
	        var e = Strains;

	        double
		        ecx = e[0],
		        ecy = e[1],
		        ycxy = e[2];

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
        private static void ReinforcementSlope(Membrane membrane, double theta)
        {
	        // Calculate angles
	        double
		        thetaNx = theta,
		        thetaNy = theta - Constants.PiOver2;

	        membrane.ReinforcementSlopes = (thetaNx, thetaNy);
        }

        // Calculate reinforcement stresses
        static void ReinforcementsStresses(Membrane membrane)
        {
	        // Get the strains
	        double
		        ex = membrane.Strains[0],
		        ey = membrane.Strains[1];

	        // Calculate stresses and secant moduli
	        double fsx, fsy;
	        if (ex >= 0)
		        fsx = Math.Min(Steel.Es * ex, Steel.fy);

	        else
		        fsx = Math.Max(Steel.Es * ex, -Steel.fy);

	        if (ey >= 0)
		        fsy = Math.Min(Steel.Es * ey, Steel.fy);

	        else
		        fsy = Math.Max(Steel.Es * ey, -Steel.fy);

	        membrane.ReinforcementStresses = (fsx, fsy);
        }

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
                fc = Concrete.fcm,
                ec = Concrete.ec1,
                Ec = Concrete.Eci,
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
                fc = Concrete.fcm,
                fcr = Concrete.fctm,
                phiAg = Concrete.AggregateDiameter,
                ec1 = membrane.ConcreteStrains.ec1,
                theta = membrane.StrainSlope,
                px = membrane.ReinforcementRatio.X,
                py = membrane.ReinforcementRatio.Y,
                fsx = membrane.ReinforcementStresses.fsx,
                fsy = membrane.ReinforcementStresses.fsy,
                smx = membrane.CrackSpacing.X,
                smy = membrane.CrackSpacing.Y;

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
                f1 = membrane.ConcreteStresses.fc1,
                px = membrane.ReinforcementRatio.X,
                py = membrane.ReinforcementRatio.Y,
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
