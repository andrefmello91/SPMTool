using System;
using System.Linq;
using MathNet.Numerics.Interpolation;
using SPMTool.Core;

namespace SPMTool.Material
{
	// Concrete
	public partial class Concrete
	{
		public class Uniaxial : Concrete
		{
            // Properties
            public double Strain  { get; set; }
            public double Stress  { get; set; }

            public Uniaxial(double strength, double aggregateDiameter, Model model, AggregateType aggregateType = AggregateType.Quartzite, double tensileStrength = 0, double elasticModule = 0, double plasticStrain = 0, double ultimateStrain = 0) : base(strength, aggregateDiameter, model, aggregateType, tensileStrength, elasticModule, plasticStrain, ultimateStrain)
            {
            }

            // Calculate secant module of concrete
            public double SecantModule => ConcreteBehavior.SecantModule(Stress, Strain);

            // Set concrete principal strains
            public void SetStrain(double strain)
            {
	            Strain = strain;
            }

            // Set concrete stresses given strains
            public void SetStress(double strain, double referenceLength = 0, double theta1 = Constants.PiOver4, PanelReinforcement reinforcement = null)
            {
	            if (strain == 0)
		            Stress = 0;

				else if (strain > 0)
		            Stress = ConcreteBehavior.TensileStress(strain, referenceLength, theta1, reinforcement);

	            else
		            Stress = ConcreteBehavior.CompressiveStress(strain);
            }

            // Set concrete strains and stresses
            public void SetStrainsAndStresses(double strain, double referenceLength = 0, double theta1 = Constants.PiOver4, PanelReinforcement reinforcement = null)
            {
	            SetStrain(strain);
	            SetStress(strain, referenceLength, theta1, reinforcement);
            }
		}
	}
}