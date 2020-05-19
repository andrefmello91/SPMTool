using System;
using StressUnit = SPMTool.Units.Stress;

namespace SPMTool
{
	// Material related commands:
	namespace Material
	{
		// Steel
		public class Steel
		{
			// Steel properties
			public double YieldStress   { get; }
			public double ElasticModule { get; }
			public double Strain        { get; set; }
			public double Stress        { get; set; }
			public double YieldStrain   => YieldStress / ElasticModule;

			// Read the steel parameters
			public Steel(double yieldStress, double elasticModule = 210000)
			{
				YieldStress = yieldStress;
				ElasticModule = elasticModule;
			}

            // Maximum plastic strain on steel
            public double esu = 0.01;

			// Set steel strain
			public void SetStrain(double strain)
			{
				Strain = strain;
			}

			// Calculate stress in reinforcement given strain
			public void SetStress(double strain)
			{
				// Compression yielding
				if (strain <= -YieldStrain)
					Stress = -YieldStress;

				// Elastic
				if (strain < YieldStrain)
					Stress = ElasticModule * strain;

				// Tension yielding
				else
					Stress = YieldStress;
			}

			// Calculate secant module of steel
			public double SecantModule
			{
				get
				{
					// Verify the strain
					if (Strain == 0)
						return ElasticModule;

					return
						Math.Min(Stress / Strain, ElasticModule);
				}
			}
		}
	}
}