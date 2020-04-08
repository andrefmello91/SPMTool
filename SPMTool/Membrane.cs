using System;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RootFinding;

namespace SPMTool
{
	public abstract class Membrane
	{
        // Properties
        public Material.Concrete                Concrete                  { get; }
        public Reinforcement.Panel              Reinforcement             { get; }
        public (bool S, string Message)         Stop                      { get; set; }
		public int                              LSCrack                   { get; set; }
		public (int X, int Y)                   LSYield                   { get; set; }
		public int                              LSPeak                    { get; set; }
		public Vector<double>                   Strains                   { get; }
		public virtual (double ec1, double ec2) ConcretePrincipalStrains  { get; }
		public virtual (double fc1, double fc2) ConcretePrincipalStresses { get; }
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

        // Calculate concrete parameters for membrane element
        private double fc    => Concrete.fcm;
        private double ec    => Concrete.ec1;
        private double Ec    => Concrete.Eci;
        private double fcr   => Concrete.fctm;
        private double ecr   => Concrete.ecr;
        //private double ec    = -0.002;
        //private double Ec    => -2 * fc / ec;
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

        // Calculate reinforcement stresses
        public (double fsx, double fsy) ReinforcementStresses
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

		// Get reinforcement stresses as a vector multiplied by reinforcement ratio
		public Vector<double> ReinforcementStressVector
		{
			get
			{
				if (Strains.Exists(Auxiliary.NotZero))
					return
						SteelStiffness * Strains;

				return
					Vector<double>.Build.Dense(3);
			}
        }

        // Calculate strain slope
        public virtual double StrainAngle
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

		        return
			        (Esx, Esy);
            }
        }

        // Calculate secant module of concrete
        private (double Ec1, double Ec2) ConcreteSecantModule => ConcreteSecantModules(Strains);
        private (double Ec1, double Ec2) ConcreteSecantModules(Vector<double> strains)
        {
	        double Ec1, Ec2;

	        // Verify strains
	        if (strains.Exists(Auxiliary.NotZero))
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

	        return
		        (Ec1, Ec2);
        }

        // Calculate stiffness
        public Matrix<double> Stiffness => ConcreteStiffness + SteelStiffness;

        // Calculate steel stiffness matrix
        public Matrix<double> SteelStiffness
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
        public Matrix<double> ConcreteStiffness
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
		        return
			        T.Transpose() * Dc1 * T;
	        }
        }

        // Calculate concrete transformation matrix
		public Matrix<double> TransformationMatrix
		{
			get
			{
				var (cos, sin) = Auxiliary.DirectionCosines(StrainAngle);
				double
					cos2   = cos * cos,
					sin2   = sin * sin,
					cosSin = cos * sin;

				return
					Matrix<double>.Build.DenseOfArray(new[,]
					{
						{         cos2,       sin2,      cosSin },
						{         sin2,       cos2,    - cosSin },
						{ - 2 * cosSin, 2 * cosSin, cos2 - sin2 }
					});
			}
		}

		// Calculate concrete stresses
		public Vector<double> ConcreteStresses
		{
			get
			{
				if (Strains.Exists(Auxiliary.NotZero))
					return
						ConcreteStiffness * Strains;

				return
					Vector<double>.Build.Dense(3);
			}
		}

        // Calculate stresses
        public virtual Vector<double> Stresses
        {
	        get
	        {
		        if (Strains.Exists(Auxiliary.NotZero))
			        return
				        Stiffness * Strains;

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
            // Constructor
            public MCFT(Material.Concrete concrete, Reinforcement.Panel reinforcement, Vector<double> appliedStrain = null, int loadStep = 0) : base(concrete, reinforcement, appliedStrain, loadStep)
            {
            }

            // Calculate concrete parameters for MCFT
            //private double fcr => 0.33 * Math.Sqrt(fc);
            //private double ecr => fcr / Ec;

            // Calculate principal strains in concrete
            public override (double ec1, double ec2) ConcretePrincipalStrains
            {
	            get
	            {
		            // Get the strains
		            var e = Strains;

		            double
			            ecx  = e[0],
			            ecy  = e[1],
			            ycxy = e[2];

		            // Calculate radius and center of Mohr's Circle
		            double
			            cen = 0.5 * (ecx + ecy),
			            rad = 0.5 * Math.Sqrt((ecy - ecx) * (ecy - ecx) + ycxy * ycxy);

		            // Calculate principal strains in concrete
		            double
			            ec1 = cen + rad,
			            ec2 = cen - rad;

		            return (ec1, ec2);
	            }
            }

            // Calculate principal stresses in concrete (VECCHIO, 1993)
            public override (double fc1, double fc2) ConcretePrincipalStresses
            {
	            get
	            {
		            // Get the strains
		            var concreteStrains = ConcretePrincipalStrains;

					// Calculate stresses
					double
						fc1 = ConcreteTensileStress(concreteStrains),
						fc2 = ClassicConcreteCompressiveStress(concreteStrains);

					return
                        (fc1, fc2);
	            }
            }

            // Calculate compressive stress in concrete (VECCHIO, 1993)
            private double ConcreteCompressiveStress((double ec1, double ec2) concretePrincipalStrains)
            {
	            var (ec1, ec2) = concretePrincipalStrains;

	            // Calculate coefficients Kc and Kf
	            double Kc;

	            if (ec1 == 0 || ec2 == 0 || -ec1 / ec2 <= 0.28)
		            Kc = 1;
	            else
	            {
					// Get the limit tensile strain
					double e1 = LimitTensileStrain(ec1);

		            Kc = Math.Max(0.35 * Math.Pow(-e1 / ec2 - 0.28, 0.8), 1);
	            }

	            double Kf = Math.Max(0.1825 * Math.Sqrt(fc), 1);

	            // Calculate beta
	            double beta = 1 / (1 + Kc * Kf);

	            double fp, ep;

	            if (ec2 > beta * ec)
	            {
		            // Calculate fp and ep
		            fp = beta * fc;
		            ep = beta * ec;
	            }
	            else
	            {
		            fp = fc;
		            ep = ec;
	            }

	            // Calculate n
	            double n = 0.8 + fp / 17;

	            // Calculate k
	            double k;

	            if (ec < ep)
		            k = 1;
	            else
		            k = 0.67 + fp / 62;

	            // Calculate compressive stress
	            double ec2ep = ec2 / ep;

				// Calculate fc2
				double fc2 = -fp * n * ec2ep / (n - 1 + Math.Pow(ec2ep, n * k));

				if (ec2 > beta * ec)
					return
						fc2;

				// Else fc2 = fc2base
				return
					beta * fc2;
            }

            // Calculate tensile stress in concrete
			private double ConcreteTensileStress ((double ec1, double ec2) concretePrincipalStrains)
			{
				var (ec1, ec2) = concretePrincipalStrains;

				// Constitutive relation
				if (ec1 <= ecr) // Not cracked
					return
						ec1 * Ec;

				// Else, cracked
				// Calculate the principal tensile stress in concrete by crack check procedure
				return
					CrackCheck((ec1, ec2));
			}

			// Principal stresses by classic formulation
            private double ClassicConcreteCompressiveStress((double ec1, double ec2) concretePrincipalStrains)
            {
	            // Get the strains
	            var (ec1, ec2) = concretePrincipalStrains;

	            // Calculate the maximum concrete compressive stress
	            double
		            f2maxA = - fc / (0.8 - 0.34 * ec1 / ec),
		            f2max  = Math.Max(f2maxA, - fc);

	            // Calculate the principal compressive stress in concrete
	            double n   = ec2 / ec;

	            return
		            f2max * (2 * n - n * n);
            }

			// Calculate maximum tensile strain for using on softening formulation
			private double LimitTensileStrain(double appliedStrain)
			{
				if (appliedStrain <= ecr)
					return appliedStrain;

                // Get reinforcement stresses
                var (fsx, fsy) = ReinforcementStresses;

                // Reinforcement capacity reserve
                double
	                f1cx = psx * (fyx - fsx),
	                f1cy = psy * (fyy - fsy);

                // Calculate theta sine and cosine
                var (cosTheta,_) = Auxiliary.DirectionCosines(StrainAngle);

				// Calculate e1L
				double
					a = (f1cy - f1cx) * cosTheta * cosTheta - f1cy,
					b = a + fcr,
					n = a * a,
					d = 500 * b * b,
					e1L = n / d;

                return
					Math.Min(appliedStrain, e1L);
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
                var (cosTheta, sinTheta) = Auxiliary.DirectionCosines(theta);
                double tanTheta = Auxiliary.Tangent(theta);

                // Average crack spacing and opening
                double
                    smTheta = 1 / (sinTheta / smx + cosTheta / smy),
                    w = smTheta * ec1;

                // Reinforcement capacity reserve
                double
                    f1cx = psx * (fyx - fsx),
                    f1cy = psy * (fyy - fsy);

                // Maximum possible shear on crack interface
                double vcimaxA = 0.18 * Math.Sqrt(fc) / (0.31 + 24 * w / (phiAg + 16));

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

		public class DSFM : Membrane
		{
			// Private properties
			private Vector<double> InitialConcreteStrain { get; }
            private double         Lr                    { get; }

			// Constructor
            public DSFM(Material.Concrete concrete, Reinforcement.Panel reinforcement, double referenceLength, Vector<double> initialConcreteStrain, Vector<double> appliedStrain = null, int loadStep = 0) : base(concrete, reinforcement, appliedStrain, loadStep)
            {
	            Lr = referenceLength;
	            InitialConcreteStrain = initialConcreteStrain;
            }

            // Calculate concrete parameters for DSFM
			private double fcr => 0.65 * Math.Pow(fc, 0.33);
			private double ecr => fcr / Ec;
			private double     Gf  = 0.075;
			private double     ets => 2 * Gf / (fcr * Lr);

			// Concrete strain angle
			public override double StrainAngle => StrainAngles.theta;

			// Calculate strain angles
			private (double thetaE, double theta) StrainAngles
			{
				get
				{
					double
						thetaE = CalculateStrainAngle(Strains),
						theta  = CalculateStrainAngle(InitialConcreteStrain);

					return (thetaE, theta);
				}
            }

			// Calculate strain angle
			private double CalculateStrainAngle(Vector<double> strains)
			{
				double angle;
				// Calculate the inclination of apparent strains
				if (strains[2] == 0)
					angle = 0;

				else if (strains[0] == strains[1])
					angle = Constants.PiOver4;

				else
				{
					double tan2Angle = strains[2] / (strains[0] - strains[1]);
					angle = 0.5 * Math.Atan(tan2Angle);

					// Theta must be positive
					if (angle < 0)
						angle += Constants.PiOver2;
				}

				return angle;
            }

            // Calculate principal strains in concrete
            public override (double ec1, double ec2) ConcretePrincipalStrains
			{
				get
				{
					// Get concrete net strains
					var ec = InitialConcreteStrain;

					double
						ecx  = ec[0],
						ecy  = ec[1],
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
			}

            // Calculate principal stresses in concrete
            public override (double fc1, double fc2) ConcretePrincipalStresses
            {
	            get
	            {
					// Get strains
					var (ec1, ec2) = ConcretePrincipalStrains;

                    // Calculate the coefficients
                    double Cd, betaD;
                    if (ec1 == 0 || ec2 == 0 || -ec1 / ec2 <= 0.28)
                        Cd = 1;

                    else
                        Cd = Math.Max(0.35 * Math.Pow(-ec1 / ec2 - 0.28, 0.8), 1);

                    betaD = Math.Min(1 / (1 + 0.55 * Cd), 1);

                    // Calculate fp and ep
                    double
                        fp = -betaD * fc,
                        ep =  betaD * ec;

                    // Calculate parameters of concrete
                    double k;
                    if (ep <= ec2)
                        k = 1;
                    else
                        k = 0.67 - fp / 62;

                    double
                        n = 0.8 - fp / 17,
                        ec2ep = ec2 / ep;

                    // Calculate the principal compressive stress in concrete
                    double fc2 = fp * n * ec2ep / (n - 1 + Math.Pow(ec2ep, n * k));

                    // Initiate fc1
                    double fc1;

                    // Check if concrete is cracked
                    if (ec1 <= ecr) // Not cracked
	                    fc1 = Ec * ec1;

                    else // Cracked
                    {
                        // Calculate concrete postcracking stress associated with tension softening
                        double fc1a = fcr * (1 - (ec1 - ecr) / (ets - ecr));

						// Get reinforcement angles and stresses
						var (thetaNx, thetaNy) = ReinforcementAngles;
						var (fsx, fsy)         = ReinforcementStresses;

                        // Calculate coefficient for tension stiffening effect
                        double
                            cosNx = Auxiliary.DirectionCosines(thetaNx).cos,
                            cosNy = Auxiliary.DirectionCosines(thetaNy).cos,
                            m     = 0.25 / (psx / phiX * Math.Abs(cosNx) + psy / phiY * Math.Abs(cosNy));

                        // Calculate concrete postcracking stress associated with tension stiffening
                        double fc1b = fcr / (1 + Math.Sqrt(2.2 * m * ec1));

                        // Calculate concrete tensile stress
                        double fc1c = Math.Max(fc1a, fc1b);

                        // Check the maximum value of fc1 that can be transmitted across cracks
                        double
                            cos2x = cosNx * cosNx,
                            cos2y = cosNy * cosNy,
                            fc1s = psx * (fyx - fsx) * cos2x + psy * (fyy - fsy) * cos2y;

                        // Choose the minimum value of fc1
                        fc1 = Math.Min(fc1c, fc1s);
                    }

                    return (fc1, fc2);
                }
            }

            // Calculate local stresses on crack
            private (double fscrx, double fscry, double vci) CrackLocalStresses
            {
	            get
	            {
                    // Initiate stresses
                    double
                        fscrx = 0,
                        fscry = 0,
                        vci   = 0;

                    // Get the strains
                    double
                        ex = Strains[0],
                        ey = Strains[1];

					// Get concrete tensile stress
					double fc1 = ConcretePrincipalStresses.fc1;

                    // Get reinforcement angles and stresses
                    var (thetaNx, thetaNy) = ReinforcementAngles;
                    var (fsx, fsy)         = ReinforcementStresses;

                    // Calculate cosines and sines
                    var (cosNx, sinNx) = Auxiliary.DirectionCosines(thetaNx);
                    var (cosNy, sinNy) = Auxiliary.DirectionCosines(thetaNy);
                    double
                        cosNx2 = cosNx * cosNx,
                        cosNy2 = cosNy * cosNy;

                    // Function to check equilibrium
                    Func<double, double> crackEquilibrium = delegate (double de1crIt)
                    {
                        // Calculate local strains
                        double
                            escrx = ex + de1crIt * cosNx2,
                            escry = ey + de1crIt * cosNy2;

                        // Calculate reinforcement stresses
                        fscrx = Math.Min(escrx * Esxi, fyx);
                        fscry = Math.Min(escry * Esyi, fyy);

                        // Check equilibrium (must be zero)
                        double equil = psx * (fscrx - fsx) * cosNx2 + psy * (fscry - fsy) * cosNy2 - fc1;

                        return equil;
                    };

                    // Solve the nonlinear equation by Brent Method
                    double de1cr;
                    bool solution = Brent.TryFindRoot(crackEquilibrium, 1E-9, 0.01, 1E-6, 1000, out de1cr);

                    // Verify if it reached convergence
                    if (solution)
                    {
                        // Calculate local strains
                        double
                            escrx = ex + de1cr * cosNx2,
                            escry = ey + de1cr * cosNy2;

                        // Calculate reinforcement stresses
                        fscrx = Math.Min(escrx * Esxi, fyx);
                        fscry = Math.Min(escry * Esyi, fyy);

                        // Calculate shear stress
                        vci = psx * (fscrx - fsx) * cosNx * sinNx + psy * (fscry - fsy) * cosNy * sinNy;
                    }

                    // Analysis must stop
                    else
                        Stop = (true, "Equilibrium on crack not reached at step ");

                    return (fscrx, fscry, vci);
                }
            }

			// Calculate crack slip
			private Vector<double> CrackSlipStrains
			{
				get
				{
					// Get concrete principal tensile strain
					double ec1 = ConcretePrincipalStrains.ec1;

					// Verify if concrete is cracked
					if (ec1 <= ecr) // Not cracked
						return
							Vector<double>.Build.Dense(3);

					// Cracked
					// Get the strains
					double
						ex  = Strains[0],
						ey  = Strains[1],
						yxy = Strains[2];

					// Get the angles
					var (thetaE, theta) = StrainAngles;
					var (cosTheta, sinTheta) = Auxiliary.DirectionCosines(thetaE);

					// Calculate crack spacings and width
					double s = 1 / (sinTheta / smx + cosTheta / smy);

					// Calculate crack width
					double w = ec1 * s;

					// Calculate shear slip strain by stress-based approach
					var (_, _, vci) = CrackLocalStresses;
					double
						ds = vci / (1.8 * Math.Pow(w, -0.8) + (0.234 * Math.Pow(w, -0.707) - 0.2) * fc),
						ysa = ds / s;

					// Calculate shear slip strain by rotation lag approach
					double
						thetaIc = Constants.PiOver4,
						dThetaE = thetaE - thetaIc,
						thetaL = Trig.DegreeToRadian(5),
						dThetaS;

					if (Math.Abs(dThetaE) > thetaL)
						dThetaS = dThetaE - thetaL;

					else
						dThetaS = dThetaE;

					double
						thetaS = thetaIc + dThetaS;

					var (cos2ThetaS, sin2ThetaS) = Auxiliary.DirectionCosines(2 * thetaS);

					double ysb = yxy * cos2ThetaS + (ey - ex) * sin2ThetaS;

					// Calculate shear slip strains
					var (cos2Theta, sin2Theta) = Auxiliary.DirectionCosines(2 * theta);
					double
						ys   = Math.Max(ysa, ysb),
						exs  = -ys / 2 * sin2Theta,
						eys  = ys / 2 * sin2Theta,
						yxys = ys * cos2Theta;

					// Calculate the vector of shear slip strains
					return 
						Vector<double>.Build.DenseOfArray(new [] { exs, eys, yxys });
				}
			}

			// Calculate the pseudo-prestress
			private Vector<double> PseudoPrestress
			{
				get
				{
					// Get concrete stiffness and crack slip strains
					var Dc = ConcreteStiffness;
					var es = CrackSlipStrains;

					// Check if es = {0, 0, 0}
					if (es.Exists(Auxiliary.NotZero))
						return
							Dc * es;

					return 
						Vector<double>.Build.Dense(3);
                }
            }

			// Calculate the final concrete strain
			public Vector<double> ConcreteStrains
			{
				get
				{
					// Get crack slip strains
					var es = CrackSlipStrains;

					// Calculate the concrete strains
					return 
						Strains - es;
				}
			}
			
			// Calculate concrete secant module
			private new (double Ec1, double Ec2) ConcreteSecantModule => ConcreteSecantModules(ConcreteStrains);

			// Calculate stresses
			public override Vector<double> Stresses
			{
				get
				{
					if (Strains.Exists(Auxiliary.NotZero))
						return 
							Stiffness * Strains + PseudoPrestress;

					return
						Vector<double>.Build.Dense(3);
				}
			}

        }
    }

}