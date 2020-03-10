using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace SPMTool
{
    public class MCFT
    {
	    // Public Properties
		public Membrane                 FinalMembrane         { get; }
	    public (bool S, string Message) Stop                  { get; set; }
	    public int                      LSCrack               { get; set; }
	    public (int X, int Y)           LSYield               { get; set; }
	    public int                      LSPeak                { get; set; }
	    public Vector<double>           Strains               { get; set; }
	    public Vector<double>           Stresses              { get; set; }
	    public double                   StrainAngle           { get; set; }
	    public (double ec1, double ec2) ConcreteStrains       { get; set; }
	    public (double fc1, double fc2) ConcreteStresses      { get; set; }
	    public (double fsx, double fsy) ReinforcementStresses { get; set; }

	    // Private properties
	    private Material.Concrete   Concrete      { get; }
		private Reinforcement.Panel Reinforcement { get; }
	    private int                 LoadStep      { get; }
	    private int                 maxIter       = 1000;

        // Calculate concrete parameters for MCFT
        private double fc    => Concrete.fcm;
        private double ec    =  0.002;
        private double Ec    => 2 * fc / ec;
        private double fcr   => 0.33 * Math.Sqrt(fc);
        private double ecr   => fcr / Ec;
        private double phiAg => Concrete.AggregateDiameter;

		// Get steel parameters
		private double fyx  => Reinforcement.Steel.X.fy;
		private double Esxi => Reinforcement.Steel.X.Es;
		private double eyx  => Reinforcement.Steel.X.ey;
		private double fyy  => Reinforcement.Steel.Y.fy;
		private double Esyi => Reinforcement.Steel.Y.Es;
		private double eyy  => Reinforcement.Steel.Y.ey;

		// Get reinforcement
		private double phiX => Reinforcement.BarDiameter.X;
		private double phiY => Reinforcement.BarDiameter.Y;
		private double psx  => Reinforcement.Ratio.X;
		private double psy  => Reinforcement.Ratio.Y;

		// Calculate crack spacings
		private double smx => phiX / (5.4 * psx);
		private double smy => phiY / (5.4 * psy);

        // Constructor
        public MCFT(Membrane initialMembrane, Material.Concrete concrete, Vector<double> appliedStrain, Vector<double> initialStress, int loadStep)
        {
            // Get concrete
            Concrete = concrete;

            // Get reinforcement
            Reinforcement = initialMembrane.Reinforcement;

            // Get current load step
            LoadStep = loadStep;

            // Get the strains
            var ei = appliedStrain;

			// Get the initial stress vector
			var fi = initialStress;

            // Initiate a loop for the iterations
            double tol;
            for (int it = 1; it <= maxIter; it++)
            {
                // Calculate the principal strains
                var concreteStrains = ConcretePrincipalStrains(ei);

                // Calculate the angle of principal strains
                double theta = StrainsAngle(ei);

                // Calculate reinforcement stresses
                var reinforcementsStresses = SteelStresses(ei);

                // Calculate principal stresses in concrete
                var concreteStresses = ConcretePrincipalStresses(concreteStrains, reinforcementsStresses, theta);

                // Calculate material secant module
                var concreteSecantModule = ConcreteSecantModule(concreteStrains, concreteStresses);
                var steelSecantModule = SteelSecantModule(ei, reinforcementsStresses);

                // Get the new membrane
                var membrane = new Membrane(concreteSecantModule, steelSecantModule, Reinforcement, theta);

                // Get membrane stiffness
                var D = membrane.Stiffness;

				// Calculate the stresses
				var ff = D * ei;

                // Verify the tolerance
                var tolVec = ff - fi;
                tol = tolVec.AbsoluteMaximum();

                // Assign fi for a new loop
                fi = ff;

                // Verify if convergence is reached
                if (tol < 0.000001)  // Convergence reached
                {
                    // Assign the results
                    FinalMembrane         = membrane;
                    Strains               = ei;
                    Stresses              = ff;
                    StrainAngle           = theta;
                    ConcreteStrains       = concreteStrains;
                    ConcreteStresses      = concreteStresses;
                    ReinforcementStresses = reinforcementsStresses;

                    // Verify if concrete cracked in this step
                    if (LSCrack == 0 && concreteStrains.ec1 >= Concrete.ecr)
                        LSCrack = LoadStep;

                    // Verify if concrete reached it's peak stress
                    if (LSPeak == 0 && concreteStrains.ec2 <= Concrete.ec1)
                        LSPeak = LoadStep;

                    break;
                }

                if (it == maxIter) // Not reached, analysis must stop
                    Stop = (true, "Convergence not reached at load step " + LoadStep);
            }

        }

		// Constructor for initial stiffness
        public MCFT(Material.Concrete concrete, Reinforcement.Panel reinforcement)
        {
	        // Get concrete
	        Concrete = concrete;

	        // Get reinforcement
	        Reinforcement = reinforcement;

			// Get initial parameters
			FinalMembrane = new Membrane((Ec, Ec), (Esxi, Esyi), reinforcement, Constants.PiOver4);
			Strains       = Vector<double>.Build.Dense(3);
			Stresses      = Vector<double>.Build.Dense(3,0.01);
        }

        // Calculate concrete principal strains
        private (double ec1, double ec2) ConcretePrincipalStrains(Vector<double> strainVector)
        {
	        // Get the apparent strains and concrete net strains
	        var e = strainVector;

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
        private (double X, double Y) ReinforcementAngles(double theta)
        {
	        // Calculate angles
	        double
		        thetaNx = theta,
		        thetaNy = theta - Constants.PiOver2;

	        return (thetaNx, thetaNy);
        }

        // Calculate reinforcement stresses
        private (double fsx, double fsy) SteelStresses(Vector<double> strainVector)
        {
	        // Get the strains
	        double
		        esx = strainVector[0],
		        esy = strainVector[1];

	        // Calculate stresses and secant moduli
	        double fsx, fsy;
	        if (esx >= 0)
		        fsx = Math.Min(Esxi * esx, fyx);

	        else
		        fsx = Math.Max(Esxi * esx, -fyx);

	        if (esy >= 0)
		        fsy = Math.Min(Esyi * esy, fyy);

	        else
		        fsy = Math.Max(Esyi * esy, -fyy);

	        return (fsx, fsy);
        }

        // Calculate strain slope
        private double StrainsAngle(Vector<double> strainVector)
        {
            // Get the strains
            var e = strainVector;

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

            return theta;
        }

        // Calculate principal stresses in concrete
        private (double fc1, double fc2) ConcretePrincipalStresses((double ec1, double ec2) concreteStrains, (double fsx, double fsy) reinforcentStresses, double theta)
        {
            // Get the values
            double
                ec1 = concreteStrains.ec1,
                ec2 = concreteStrains.ec2;

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
            if (ec1 <= ecr) // Not cracked
                fc1 = ec1 * Ec;

            else // cracked
            {
                // Calculate the principal tensile stress in concrete by crack check procedure
                fc1 = CrackCheck(concreteStrains, reinforcentStresses, theta);
            }

            return (fc1, fc2);
        }

        // Crack check procedure
        private double CrackCheck((double ec1, double ec2) concreteStrains, (double fsx, double fsy) steelStresses, double theta)
        {
            // Get the values
            double
	            ec1 = concreteStrains.ec1,
	            fsx = steelStresses.fsx,
	            fsy = steelStresses.fsy;

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
                f1cx = psx * (fyx - fsx),
                f1cy = psy * (fyy - fsy);

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
            var fc1 = f1List.Min();

            // Calculate critical stresses on crack
            StressesOnCrack();
            void StressesOnCrack()
            {
	            // Initiate vci = 0 (for most common cases)
	            double vci = 0;

	            if (f1cx > f1cy && f1cy < fc1) // Y dominant
		            vci = (fc1 - f1cy) / tanTheta;

	            if (f1cx < f1cy && f1cx < fc1) // X dominant
		            vci = (f1cx - fc1) * tanTheta;

	            // Reinforcement stresses
	            double
		            fsxcr = (fc1 + vci / tanTheta) / psx + fsx,
		            fsycr = (fc1 + vci * tanTheta) / psy + fsy;

	            // Check if reinforcement yielded at crack
	            int
		            lsYieldX = 0,
		            lsYieldY = 0;

	            if (LSYield.X == 0 && fsxcr >= fyx)
		            lsYieldX = LoadStep;

	            if (LSYield.Y == 0 && fsycr >= fyy)
		            lsYieldY = LoadStep;

	            LSYield = (lsYieldX, lsYieldY);
            }

            return fc1;
        }

        // Calculate secant moduli of steel
        private (double Esx, double Esy) SteelSecantModule(Vector<double> strainVector, (double fsx, double fsy) reinforcementStresses)
        {
	        double Esx, Esy;

			// Get values
			double
				esx = strainVector[0],
				esy = strainVector[1],
				fsx = reinforcementStresses.fsx,
				fsy = reinforcementStresses.fsy;

	        // Steel
	        if (esx == 0 || fsx == 0)
		        Esx = Esxi;

	        else
		        Esx = Math.Min(fsx / esx, Esxi);

	        if (esy == 0 || fsy == 0)
		        Esy = Esyi;

	        else
		        Esy = Math.Min(fsy / esy, Esyi);

	        return (Esx, Esy);
        }

        // Calculate secant moduli of concrete
        private (double Ec1, double Ec2) ConcreteSecantModule((double ec1, double ec2) conscreteStrains, (double fc1, double fc2) concreteStresses)
        {
	        double Ec1, Ec2;

			// Get values
			double
				ec1 = ConcreteStrains.ec1,
				ec2 = ConcreteStrains.ec2,
				fc1 = concreteStresses.fc1,
				fc2 = concreteStresses.fc2;

	        if (ec1 == 0 || fc1 == 0)
		        Ec1 = Concrete.Eci;

	        else
		        Ec1 = fc1 / ec1;

	        if (ec2 == 0 || fc2 == 0)
		        Ec2 = Concrete.Eci;

	        else
		        Ec2 = fc2 / ec2;

	        return (Ec1, Ec2);
        }
    }
}
