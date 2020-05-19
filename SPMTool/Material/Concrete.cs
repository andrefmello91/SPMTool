using System;
using System.Linq;
using MathNet.Numerics.Interpolation;

namespace SPMTool.Material
{
	// Concrete
	public class Concrete
	{
		// Aggregate type
		public enum AggregateType
		{
			Basalt,
			Quartzite,
			Limestone,
			Sandstone
		}

		// Properties
		public Units                    Units             { get; }
		public double                   AggregateDiameter { get; }
		public AggregateType            Type              { get; }
		public  double                  Strength          { get; }
		public (double ec1, double ec2) PrincipalStrains  { get; set; }
		public (double fc1, double fc2) PrincipalStresses { get; set; }
		public double                   ReferenceLength   { get; set; }

		// Read the concrete parameters
		public Concrete(double strength, double aggregateDiameter, AggregateType aggregateType = AggregateType.Quartzite)
		{
			// Get stress in MPa
			Strength          = strength;

			// Get dimension in mm
			AggregateDiameter = aggregateDiameter;
			Type              = aggregateType;
		}

		// Verify if concrete was set
		public bool IsSet
		{
			get
			{
				if (Strength > 0)
					return true;

				// Else
				return false;
			}
		}

		// Verify if concrete is cracked
		public bool Cracked
		{
			get
			{
				if (PrincipalStrains.ec1 > ecr)
					return true;

				return false;
			}
		}

		// Calculate parameters according to FIB MC2010
		public virtual double fcr
		{
			get
			{
				if (Strength <= 50)
					return
						0.3 * Math.Pow(Strength, 0.66666667);
				//else
				return
					2.12 * Math.Log(1 + 0.1 * Strength);
			}
		}

		public double alphaE
		{
			get
			{
				if (Type == AggregateType.Basalt)
					return 1.2;

				if (Type == AggregateType.Quartzite)
					return 1;

				// Limestone or sandstone
				return 0.9;
			}
		}

		public virtual double Ec  => 21500 * alphaE * Math.Pow(Strength / 10, 0.33333333);
		public virtual double ec  => -1.6 / 1000 * Math.Pow(Strength / 10, 0.25);
		public double Ec1         => Strength / ec;
		public double k           => Ec / Ec1;
		public double ecr         => fcr / Ec;
		public double nu          => 0.2;

		public virtual double ecu
		{
			get
			{
				// Verify fcm
				if (Strength < 50)
					return
						-0.0035;

				if (Strength >= 90)
					return
						-0.003;

				// Get classes and ultimate strains
				if (classes.Contains(Strength))
				{
					int i = Array.IndexOf(classes, Strength);

					return
						ultimateStrain[i];
				}

				// Interpolate values
				var spline = ultStrainSpline.Value;

				return
					spline.Interpolate(Strength);
			}
		}

		// Array of high strength concrete classes, C50 to C90 (MC2010)
		private double[] classes =
		{
			50, 55, 60, 70, 80, 90
		};

		// Array of ultimate strains for each concrete class, C50 to C90 (MC2010)
		private double[] ultimateStrain =
		{
			-0.0034, -0.0034, -0.0033, -0.0032, -0.0031, -0.003
		};

		// Interpolation for ultimate strains
		private Lazy<CubicSpline> ultStrainSpline => new Lazy<CubicSpline>(UltimateStrainSpline);
		private CubicSpline UltimateStrainSpline()
		{
			return
				CubicSpline.InterpolateAkimaSorted(classes, ultimateStrain);
		}

		// Calculate concrete stresses
		public virtual double TensileStress(double ec1, PanelReinforcement reinforcement = null,
			(double x, double y) reinforcementAngles = default)
		{
			throw new NotImplementedException();
		}

		public virtual double CompressiveStress((double ec1, double ec2) principalStrains)
		{
			throw new NotImplementedException();
		}

		// Calculate secant module of concrete
		public (double Ec1, double Ec2) SecantModule
		{
			get

			{
				double Ec1, Ec2;

				// Verify strains
				// Get values
				var (ec1, ec2) = PrincipalStrains;
				var (fc1, fc2) = PrincipalStresses;

				if (ec1 == 0 || fc1 == 0)
					Ec1 = Ec;

				else
					Ec1 = fc1 / ec1;

				if (ec2 == 0 || fc2 == 0)
					Ec2 = Ec;

				else
					Ec2 = fc2 / ec2;

				return
					(Ec1, Ec2);
			}
		}

		// Set concrete principal strains
		public void SetStrains((double ec1, double ec2) principalStrains)
		{
			PrincipalStrains = principalStrains;
		}

		// Set concrete stresses given strains
		public void SetStresses((double ec1, double ec2) principalStrains, PanelReinforcement reinforcement = null,
			(double x, double y) reinforcementAngles = default)
		{
			PrincipalStresses = (TensileStress(principalStrains.ec1, reinforcement, reinforcementAngles),
				CompressiveStress(principalStrains));
		}

		// Set concrete strains and stresses
		public void SetStrainsAndStresses((double ec1, double ec2) principalStrains,
			PanelReinforcement reinforcement = null, (double x, double y) reinforcementAngles = default)
		{
			SetStrains(principalStrains);
			SetStresses(principalStrains, reinforcement, reinforcementAngles);
		}

		// Set tensile stress limited by crack check
		public void SetTensileStress(double fc1)
		{
			// Get compressive stress
			double fc2 = PrincipalStresses.fc2;

			// Set
			PrincipalStresses = (fc1, fc2);
		}

		public class MCFT : Concrete
		{
			public MCFT(double strength, double aggregateDiameter) : base(strength, aggregateDiameter)
			{
			}

			// MCFT parameters
			public override double fcr => 0.33 * Math.Sqrt(Strength);
			public override double ec => -0.002;
			public override double ecu => -0.0035;
			public override double Ec => -2 * Strength / ec;

			// Principal stresses by classic formulation
			public override double CompressiveStress((double ec1, double ec2) principalStrains)
			{
				// Get the strains
				var (ec1, ec2) = principalStrains;

				// Calculate the maximum concrete compressive stress
				double
					f2maxA = -Strength / (0.8 - 0.34 * ec1 / ec),
					f2max = Math.Max(f2maxA, -Strength);

				// Calculate the principal compressive stress in concrete
				double n = ec2 / ec;

				return
					f2max * (2 * n - n * n);
			}

			// Calculate tensile stress in concrete
			public override double TensileStress(double ec1, PanelReinforcement reinforcement = null,
				(double x, double y) reinforcementAngles = default)
			{
				// Constitutive relation
				if (ec1 <= ecr) // Not cracked
					return
						ec1 * Ec;

				// Else, cracked
				// Constitutive relation
				return
					fcr / (1 + Math.Sqrt(500 * ec1));
			}
		}

		public class DSFM : Concrete
		{
			public DSFM(double strength, double aggregateDiameter, double referenceLength) : base(strength, aggregateDiameter)
			{
				ReferenceLength = referenceLength;
			}

			// DSFM parameters
			public override double fcr => 0.65 * Math.Pow(Strength, 0.33);
			public override double ec  => -0.002;
			public override double ecu => -0.0035;
			public override double Ec  => -2 * Strength / ec;
			private double         Gf  = 0.075;
			private double         ets => 2 * Gf / (fcr * ReferenceLength);

			public override double TensileStress(double ec1, PanelReinforcement reinforcement, (double x, double y) reinforcementAngles)
			{
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
					var (thetaNx, thetaNy) = reinforcementAngles;
					var (fsx, fsy)         = reinforcement.Stresses;
					var (psx, psy)         = reinforcement.Ratio;
					var (phiX, phiY)       = reinforcement.BarDiameter;
					double fyx             = reinforcement.Steel.X.YieldStress;
					double fyy             = reinforcement.Steel.Y.YieldStress;

					// Calculate coefficient for tension stiffening effect
					double
						cosNx = GlobalAuxiliary.DirectionCosines(thetaNx).cos,
						cosNy = GlobalAuxiliary.DirectionCosines(thetaNy).cos,
						m     = 0.25 / (psx / phiX * Math.Abs(cosNx) + psy / phiY * Math.Abs(cosNy));

					// Calculate concrete postcracking stress associated with tension stiffening
					double fc1b = fcr / (1 + Math.Sqrt(2.2 * m * ec1));

					// Calculate concrete tensile stress
					fc1 = Math.Max(fc1a, fc1b);

					//// Check the maximum value of fc1 that can be transmitted across cracks
					//double
					// cos2x = cosNx * cosNx,
					// cos2y = cosNy * cosNy,
					// fc1s = psx * (fyx - fsx) * cos2x + psy * (fyy - fsy) * cos2y;

					//// Choose the minimum value of fc1
					//fc1 = Math.Min(fc1c, fc1s);
				}

				return fc1;
			}

			public override double CompressiveStress((double ec1, double ec2) principalStrains)
			{
				// Get strains
				var (ec1, ec2) = principalStrains;

				// Calculate the coefficients
				double Cd, betaD;
				if (ec1 == 0 || ec2 == 0 || -ec1 / ec2 <= 0.28)
					Cd = 1;

				else
					Cd = Math.Max(0.35 * Math.Pow(-ec1 / ec2 - 0.28, 0.8), 1);

				betaD = Math.Min(1 / (1 + 0.55 * Cd), 1);

				// Calculate fp and ep
				double
					fp = -betaD * Strength,
					ep = betaD * ec;

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
				return
					fp * n * ec2ep / (n - 1 + Math.Pow(ec2ep, n * k));
			}
		}
	}
}