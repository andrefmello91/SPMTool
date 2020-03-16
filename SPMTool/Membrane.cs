using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
	public class Membrane
	{
        // Public Properties
        public Material.Concrete                Concrete                  { get; }
        public Reinforcement.Panel              Reinforcement             { get; }
        public (bool S, string Message)         Stop                      { get; set; }
		public int                              LSCrack                   { get; set; }
		public (int X, int Y)                   LSYield                   { get; set; }
		public int                              LSPeak                    { get; set; }
		public Vector<double>                   Strains                   { get; set; }
		public virtual (double ec1, double ec2) ConcretePrincipalStrains  { get; set; }
		public virtual (double fc1, double fc2) ConcretePrincipalStresses { get; set; }
		public virtual double                   Ec                        { get; }
		private int                             LoadStep                  { get; }

        // Constructor
        public Membrane(Material.Concrete concrete, Reinforcement.Panel reinforcement, Vector<double> appliedStrain = null, int loadStep = 0)
		{
			// Get materials
			Concrete      = concrete;
			Reinforcement = reinforcement;

			// Get current load step
			LoadStep = loadStep;

			// Get the strains
			if (appliedStrain != null)
				Strains = appliedStrain;
			else
				Strains = Vector<double>.Build.Dense(3);
		}

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
		private double psx => Reinforcement.Ratio.X;
		private double psy => Reinforcement.Ratio.Y;

        // Calculate reinforcement stresses
        public  (double fsx, double fsy) ReinforcementStresses
        {
	        get
	        {
		        // Get the strains
		        double
			        esx = Strains[0],
			        esy = Strains[1];

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
        }

        // Calculate strain slope
        public  double StrainAngle
        {
	        get
	        {
		        double theta;

		        // Get the strains
		        var e = Strains;

		        // Verify the strains
		        if (e.Exists(Auxiliary.NotZero))
		        {
			        // Calculate the strain slope
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
		        }
		        else
			        theta = Constants.PiOver4;

		        return theta;
	        }
        }

        // Calculate secant module of steel
        private (double Esx, double Esy) SteelSecantModule
        {
	        get
	        {
		        double Esx, Esy;

		        // Verify the strains
		        if (Strains.Exists(Auxiliary.NotZero))
		        {
			        // Get values
			        double
				        esx = Strains[0],
				        esy = Strains[1];
			        var (fsx, fsy) = ReinforcementStresses;

			        // Steel
			        if (esx == 0 || fsx == 0)
				        Esx = Esxi;

			        else
				        Esx = Math.Min(fsx / esx, Esxi);

			        if (esy == 0 || fsy == 0)
				        Esy = Esyi;

			        else
				        Esy = Math.Min(fsy / esy, Esyi);
		        }
		        else
		        {
			        Esx = Esxi;
			        Esy = Esyi;
		        }

		        return (Esx, Esy);

            }
        }

        // Calculate secant module of concrete
        private (double Ec1, double Ec2) ConcreteSecantModule
        {
	        get
	        {
		        double Ec1, Ec2;

		        // Verify strains
		        if (Strains.Exists(Auxiliary.NotZero))
		        {
			        // Get values
			        var (ec1, ec2) = ConcretePrincipalStrains;
			        var (fc1, fc2) = ConcretePrincipalStresses;

			        if (ec1 == 0 || fc1 == 0)
				        Ec1 = Ec;

			        else
				        Ec1 = fc1 / ec1;

			        if (ec2 == 0 || fc2 == 0)
				        Ec2 = Ec;

			        else
				        Ec2 = fc2 / ec2;
		        }
		        else
			        Ec1 = Ec2 = Ec;

		        return (Ec1, Ec2);
	        }
        }

        // Calculate stiffness
        public Matrix<double> Stiffness => ConcreteStiffness + SteelStiffness;

        // Calculate steel stiffness matrix
        private Matrix<double> SteelStiffness
        {
	        get
	        {
		        var (Esx, Esy) = SteelSecantModule;

		        // Steel matrix
		        var Ds = Matrix<double>.Build.Dense(3, 3);
		        Ds[0, 0] = psx * Esx;
		        Ds[1, 1] = psy * Esy;

		        return Ds;
	        }
        }

        // Calculate concrete stiffness matrix
        private Matrix<double> ConcreteStiffness
        {
	        get
	        {
		        var (Ec1, Ec2) = ConcreteSecantModule;
		        double Gc = Ec1 * Ec2 / (Ec1 + Ec2);

		        // Concrete matrix
		        var Dc1 = Matrix<double>.Build.Dense(3, 3);
		        Dc1[0, 0] = Ec1;
		        Dc1[1, 1] = Ec2;
		        Dc1[2, 2] = Gc;

		        // Get transformation matrix
		        var T = TransformationMatrix;

		        // Calculate Dc
		        return T.Transpose() * Dc1 * T;
	        }
        }

        // Calculate concrete transformation matrix
		private Matrix<double> TransformationMatrix
		{
			get
			{
				// Get psi angle
				// Calculate Psi angle
				double psi = Constants.Pi - StrainAngle;
				double[] dirCos = Auxiliary.DirectionCosines(psi);

				double
					cos    = dirCos[0],
					sin    = dirCos[1],
					cos2   = cos * cos,
					sin2   = sin * sin,
					cosSin = cos * sin;

				return Matrix<double>.Build.DenseOfArray(new[,]
				{
					{         cos2,       sin2,      cosSin },
					{         sin2,       cos2,    - cosSin },
					{ - 2 * cosSin, 2 * cosSin, cos2 - sin2 }
				});
			}
		}

        // Calculate stresses
        public Vector<double> Stresses
        {
	        get
	        {
		        // Verify strains
		        if (Strains.Exists(Auxiliary.NotZero))
		        {
			        return Stiffness * Strains;
		        }
				return Vector<double>.Build.Dense(3);
            }
        }

        // Calculate slopes related to reinforcement
        private (double X, double Y) ReinforcementAngles
        {
	        get
	        {
		        // Calculate angles
		        double
					theta   = StrainAngle,
			        thetaNx = theta,
			        thetaNy = theta - Constants.PiOver2;

		        return (thetaNx, thetaNy);
	        }
        }

        public class MCFT : Membrane
        {
            // Private properties
            private int maxIter = 1000;

            // Calculate concrete parameters for MCFT
            private double         fc    => Concrete.fcm;
            private double         ec    =  0.002;
            public override double Ec    => 2 * fc / ec;
            private double         fcr   => 0.33 * Math.Sqrt(fc);
            private double         ecr   => fcr / Ec;
            private double         phiAg => Concrete.AggregateDiameter;

            // Calculate crack spacings
            private double smx => phiX / (5.4 * psx);
            private double smy => phiY / (5.4 * psy);

            // Constructor
            public MCFT(Material.Concrete concrete, Reinforcement.Panel reinforcement, Vector<double> appliedStrain = null, int loadStep = 0): base(concrete, reinforcement, appliedStrain, loadStep)
            {
            }

            public override (double ec1, double ec2) ConcretePrincipalStrains
            {
	            get
	            {
		            // Get the apparent strains and concrete net strains
		            var e = Strains;

		            double
			            ecx  = e[0],
			            ecy  = e[1],
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
            }

            // Calculate principal stresses in concrete
            public override (double fc1, double fc2) ConcretePrincipalStresses
            {
	            get
	            {
		            // Get the strains
		            var (ec1, ec2) = ConcretePrincipalStrains;

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
			            fc1 = CrackCheck((ec1, ec2));
		            }

		            return (fc1, fc2);
	            }
            }

            // Crack check procedure
            private double CrackCheck((double ec1, double ec2) concreteStrains)
            {
                // Get the values
                double ec1   = concreteStrains.ec1;
                double theta = StrainAngle;
                var (fsx, fsy) = ReinforcementStresses;

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
        }

    }

}