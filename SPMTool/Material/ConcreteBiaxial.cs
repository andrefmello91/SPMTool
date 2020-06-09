using System;
using System.Linq;
using MathNet.Numerics.Interpolation;
using SPMTool.Core;

namespace SPMTool.Material
{
	// Concrete
	public partial class Concrete
	{
		public class Biaxial : Concrete
		{
            // Properties
            public (double ec1, double ec2) PrincipalStrains  { get; set; }
            public (double fc1, double fc2) PrincipalStresses { get; set; }

            public Biaxial(double strength, double aggregateDiameter, Model model, AggregateType aggregateType = AggregateType.Quartzite, double tensileStrength = 0, double elasticModule = 0, double plasticStrain = 0, double ultimateStrain = 0) : base(strength, aggregateDiameter, model, aggregateType, tensileStrength, elasticModule, plasticStrain, ultimateStrain)
            {
            }

            // Calculate secant module of concrete
            public (double Ec1, double Ec2) SecantModule
            {
	            get
	            {
		            // Verify strains
		            // Get values
		            var (ec1, ec2) = PrincipalStrains;
		            var (fc1, fc2) = PrincipalStresses;

					// Calculate modules
					double
						Ec1 = ConcreteBehavior.SecantModule(fc1, ec1),
						Ec2 = ConcreteBehavior.SecantModule(fc2, ec2);

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
            public void SetStresses((double ec1, double ec2) principalStrains, double referenceLength = 0, double theta1 = Constants.PiOver4, PanelReinforcement reinforcement = null)
            {
	            double
		            fc1 = ConcreteBehavior.TensileStress(principalStrains.ec1, referenceLength, theta1, reinforcement),
		            fc2 = ConcreteBehavior.CompressiveStress(principalStrains);

	            PrincipalStresses = (fc1, fc2);
            }

            // Set concrete strains and stresses
            public void SetStrainsAndStresses((double ec1, double ec2) principalStrains,
	            double referenceLength = 0, double theta1 = Constants.PiOver4, PanelReinforcement reinforcement = null)
            {
	            SetStrains(principalStrains);
	            SetStresses(principalStrains, referenceLength, theta1, reinforcement);
            }

            // Set tensile stress limited by crack check
            public void SetTensileStress(double fc1)
            {
	            // Get compressive stress
	            double fc2 = PrincipalStresses.fc2;

	            // Set
	            PrincipalStresses = (fc1, fc2);
            }
        }
	}
}