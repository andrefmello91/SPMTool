using System;
using System.Linq;
using MathNet.Numerics.Interpolation;
using SPMTool.Core;

namespace SPMTool.Material
{
	// Concrete
	public partial class Concrete
	{

		// Properties
		public Units                    Units              { get; }
		public Model                    ConcreteModel      { get; }
		public Parameters               ConcreteParameters { get; }
		public (double ec1, double ec2) PrincipalStrains   { get; set; }
		public (double fc1, double fc2) PrincipalStresses  { get; set; }
		public double                   ReferenceLength    { get; set; }

		public AggregateType Type              => ConcreteParameters.Type;
		public double        AggregateDiameter => ConcreteParameters.AggregateDiameter;

        // Read the concrete parameters
        public Concrete(double strength, double aggregateDiameter, Model model, AggregateType aggregateType = AggregateType.Quartzite, double tensileStrength = 0, double elasticModule = 0, double plasticStrain = 0, double ultimateStrain = 0)
		{
			// Initiate parameters
			ConcreteModel      = model;
			ConcreteParameters = Concrete_Parameters(strength, aggregateDiameter, aggregateType, tensileStrength, elasticModule, plasticStrain, ultimateStrain);
		}

		// Get parameters
		private Parameters Concrete_Parameters(double strength, double aggregateDiameter, AggregateType aggregateType, double tensileStrength, double elasticModule, double plasticStrain, double ultimateStrain)
		{
			switch (ConcreteModel)
			{
                case Model.MC2010:
					return new Parameters.MC2010(strength, aggregateDiameter, aggregateType);

                case Model.NBR6118:
					return new Parameters.NBR6118(strength, aggregateDiameter, aggregateType);

                case Model.MCFT:
					return new Parameters.MCFT(strength, aggregateDiameter, aggregateType);

                case Model.DSFM:
					return new Parameters.DSFM(strength, aggregateDiameter, aggregateType);
			}

            // Custom parameters
            return new Parameters.Custom(strength, aggregateDiameter, tensileStrength, elasticModule, plasticStrain, ultimateStrain);
		}

        // Verify if concrete was set
        public bool IsSet => fc > 0;

		// Verify if concrete is cracked
		public bool Cracked => PrincipalStrains.ec1 > ecr;

        // Get parameters
        public double fc  => ConcreteParameters.Strength;
        public double fcr => ConcreteParameters.TensileStrength;
        public double Ec  => ConcreteParameters.InitialModule;
		public double ec  => ConcreteParameters.PlasticStrain;
		public double ecu => ConcreteParameters.UltimateStrain;
		public double Ecs => ConcreteParameters.SecantModule;
		public double ecr => ConcreteParameters.CrackStrain;
		public double nu  => ConcreteParameters.Poisson;

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

		public override string ToString()
		{
			return
				"Concrete Parameters:\n"                +
				"\nfc = "   + fc                        + " MPa" +
				"\nfcr = "  + Math.Round(fcr, 2)        + " MPa"  +
				"\nEc = "   + Math.Round(Ec, 2)         + " MPa"  +
				"\nεc = "   + Math.Round(1000 * ec, 2)  + " E-03" +
				"\nεcu = "  + Math.Round(1000 * ecu, 2) + " E-03" +
				"\nφ,ag = " + AggregateDiameter         + " mm";
        }

        public class MCFT : Concrete
		{
			public MCFT(double fc, double aggregateDiameter, Model model = Model.MCFT) : base(fc, aggregateDiameter, model)
			{
			}

			// Principal stresses by classic formulation
			public override double CompressiveStress((double ec1, double ec2) principalStrains)
			{
				// Get the strains
				var (ec1, ec2) = principalStrains;

				// Calculate the maximum concrete compressive stress
				double
					f2maxA = -fc / (0.8 - 0.34 * ec1 / ec),
					f2max = Math.Max(f2maxA, -fc);

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
			public DSFM(double fc, double aggregateDiameter, double referenceLength, Model model = Model.DSFM) : base(fc, aggregateDiameter, model)
			{
				ReferenceLength = referenceLength;
			}

			// DSFM parameters
			private double Gf  = 0.075;
			private double ets => 2 * Gf / (fcr * ReferenceLength);

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
					fp = -betaD * fc,
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