using System;
using System.Linq;
using MathNet.Numerics.Interpolation;
using SPMTool.Core;

namespace SPMTool.Material
{
	// Concrete
	public partial class Concrete
	{
        // Implementation of concrete parameters
        public abstract class Behavior
        {
			// Properties
			public Parameters Parameters        { get; }
			public bool       ConsiderCrackSlip { get; set; }
			public bool       Cracked           { get; set; }

			// Constructor
			public Behavior(Parameters parameters, bool considerCrackSlip = false)
			{
				Parameters        = parameters;
				ConsiderCrackSlip = considerCrackSlip;
			}

            // Get concrete parameters
            private double fc  => Parameters.Strength;
            private double fcr => Parameters.TensileStrength;
            private double Ec  => Parameters.InitialModule;
            private double ec  => Parameters.PlasticStrain;
            private double ecu => Parameters.UltimateStrain;
            private double Ecs => Parameters.SecantModule;
            private double ecr => Parameters.CrackStrain;
            private double nu  => Parameters.Poisson;
            private double Gf  => Parameters.FractureParameter;
            private double Cs
            {
	            get
	            {
		            if (ConsiderCrackSlip)
			            return 0.55;

		            return 1;
	            }
            }

            // Calculate concrete stresses
            public abstract double TensileStress(double strain, double referenceLength = 0, double theta1 = Constants.PiOver4, PanelReinforcement reinforcement = null);
	        public abstract double CompressiveStress((double ec1, double ec2) principalStrains);
	        public abstract double CompressiveStress(double strain);

			// Calculate secant module
			public double SecantModule(double stress, double strain)
			{
				if (stress == 0 || strain == 0)
					return Ec;

				return
					stress / strain;
			}

            public class MCFT : Behavior
	        {
		        // Constructor
		        public MCFT(Parameters parameters, bool considerCrackSlip = false) : base(parameters, considerCrackSlip)
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

                public override double CompressiveStress(double strain)
                {
	                double n = strain / ec;

	                return
		                -fc * (2 * n - n * n);
                }

                // Calculate tensile stress in concrete
                public override double TensileStress(double strain, double referenceLength = 0, double theta1 = Constants.PiOver4, PanelReinforcement reinforcement = null)
		        {
			        // Constitutive relation
			        if (strain <= ecr) // Not cracked
				        return
					        strain * Ec;

			        // Else, cracked
			        // Constitutive relation
			        return
				        fcr / (1 + Math.Sqrt(500 * strain));
		        }
            }

            public class DSFM : Behavior
	        {
                // Constructor
                public DSFM(Parameters parameters, bool considerCrackSlip = true) : base(parameters, considerCrackSlip)
                {
                }

                public override double TensileStress(double strain, double Lr, double thetaC1, PanelReinforcement reinforcement)
                {
                    // Check if concrete is cracked
                    if (!Cracked) // Not cracked
                        return
                            Ec * strain;

                    // Cracked
                    // Calculate concrete post-cracking stress associated with tension softening
                    double ets = 2 * Gf / (fcr * Lr);
                    double fc1a = fcr * (1 - (strain - ecr) / (ets - ecr));

                    // Calculate coefficient for tension stiffening effect
                    double m = reinforcement.TensionStiffeningCoefficient(thetaC1);

                    // Calculate concrete postcracking stress associated with tension stiffening
                    double fc1b = fcr / (1 + Math.Sqrt(2.2 * m * strain));
                    //double fc1b = fcr / (1 + Math.Sqrt(500 * ec1));

                    // Calculate maximum tensile stress
                    double fc1c = Math.Max(fc1a, fc1b);

                    // Check the maximum value of fc1 that can be transmitted across cracks
                    double fc1s = reinforcement.MaximumPrincipalTensileStress(thetaC1);

                    // Calculate concrete tensile stress
                    return
                        Math.Min(fc1c, fc1s);
                }

                public override double CompressiveStress((double ec1, double ec2) principalStrains)
                {
                    // Get strains
                    var (ec1, ec2) = principalStrains;

                    //if (ec2 >= 0)
                    //    return 0;

                    // Calculate the coefficients
                    //double Cd = 0.27 * (ec1 / ec - 0.37);
                    double Cd = 0.35 * Math.Pow(-ec1 / ec2 - 0.28, 0.8);
                    if (double.IsNaN(Cd))
                        Cd = 1;

                    double betaD = Math.Min(1 / (1 + Cs * Cd), 1);

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
                        ec2_ep = ec2 / ep;

                    // Calculate the principal compressive stress in concrete
                    return
                        fp * n * ec2_ep / (n - 1 + Math.Pow(ec2_ep, n * k));
                }

                public override double CompressiveStress(double strain)
                {
                    // Calculate the principal compressive stress in concrete
                    return
	                    CompressiveStress((0, strain));
                }
            }
        }
	}
}