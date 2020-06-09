using System;
using System.Linq;
using MathNet.Numerics.Interpolation;
using SPMTool.Core;

namespace SPMTool.Material
{
	// Concrete
	public partial class Concrete
	{
		// Aggregate type
		public enum AggregateType
		{
			Basalt,
			Quartzite,
			Limestone,
			Sandstone
		}

		// Model parameters
		public enum Model
		{
			NBR6118,
			MC2010,
			MCFT,
			DSFM,
			Custom
		}

        // Implementation of concrete parameters
        public abstract class Parameters
        {
	        public AggregateType Type              { get; }
	        public double        AggregateDiameter { get; }
	        public double        Strength          { get; }
	        public double        Poisson           { get; }
	        public double        TensileStrength   { get; set; }
	        public double        InitialModule     { get; set; }
	        public double        SecantModule      { get; set; }
	        public double        PlasticStrain     { get; set; }
	        public double        UltimateStrain    { get; set; }

	        // Automatic calculated properties
	        public double CrackStrain       => TensileStrength / InitialModule;
	        public double TransversalModule => SecantModule / 2.4;
	        public double FractureParameter => 0.075;

	        public Parameters(double strength, double aggregateDiameter,
		        AggregateType aggregateType = AggregateType.Quartzite)
	        {
		        Strength          = strength;
		        AggregateDiameter = aggregateDiameter;
		        Type              = aggregateType;
		        Poisson           = 0.2;
	        }

	        public class MC2010 : Parameters
	        {
		        // Calculate parameters according to FIB MC2010
		        public MC2010(double strength, double aggregateDiameter, AggregateType aggregateType = AggregateType.Quartzite) : base(strength, aggregateDiameter, aggregateType)
		        {
			        TensileStrength = fctm;
			        PlasticStrain   = ec1;
			        InitialModule   = Eci;
			        SecantModule    = Ec1;
			        UltimateStrain  = ecu;
		        }

		        private double alphaE
		        {
			        get
			        {
				        switch (Type)
				        {
					        case AggregateType.Basalt:
						        return 1.2;

					        case AggregateType.Quartzite:
						        return 1;
				        }

				        // Limestone or sandstone
				        return 0.9;
			        }
		        }

		        private double fctm
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

		        private double Eci => 21500 * alphaE * Math.Pow(Strength / 10, 0.33333333);
		        private double ec1 => -1.6 / 1000 * Math.Pow(Strength / 10, 0.25);
		        public  double Ec1 => Strength / ec1;
		        public  double k   => Eci / Ec1;

		        private double ecu
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
				        return
					        UltimateStrainSpline.Interpolate(Strength);
			        }
		        }

		        // Array of high strength concrete classes, C50 to C90 (MC2010)
		        private readonly double[] classes =
		        {
			        50, 55, 60, 70, 80, 90
		        };

		        // Array of ultimate strains for each concrete class, C50 to C90 (MC2010)
		        private readonly double[] ultimateStrain =
		        {
			        -0.0034, -0.0034, -0.0033, -0.0032, -0.0031, -0.003
		        };

		        // Interpolation for ultimate strains
		        private CubicSpline UltimateStrainSpline => CubicSpline.InterpolateAkimaSorted(classes, ultimateStrain);
	        }

	        public class NBR6118 : Parameters
	        {
		        // Calculate parameters according to FIB MC2010
		        public NBR6118(double strength, double aggregateDiameter,
			        AggregateType aggregateType = AggregateType.Quartzite) : base(strength, aggregateDiameter,
			        aggregateType)
		        {
			        TensileStrength   = fctm;
			        PlasticStrain     = ec2;
			        InitialModule     = Eci;
			        SecantModule      = Ecs;
			        UltimateStrain    = ecu;
		        }

		        private double alphaE
		        {
			        get
			        {
				        switch (Type)
				        {
					        case AggregateType.Basalt:
						        return 1.2;

					        case AggregateType.Quartzite:
						        return 1;

					        case AggregateType.Limestone:
						        return 0.9;
				        }

				        // Sandstone
				        return 0.7;
			        }
		        }

		        private double alphaI => Math.Min(0.8 + 0.2 * Strength / 80, 1);

		        private double fctm
		        {
			        get
			        {
				        if (Strength <= 50)
					        return
						        0.3 * Math.Pow(Strength, 0.66666667);
				        //else
				        return
					        2.12 * Math.Log(1 + 0.11 * Strength);
			        }
		        }

		        private double Eci
		        {
			        get
			        {
				        if (Strength <= 50)
					        return
						        alphaE * 5600 * Math.Sqrt(Strength);

				        return
					        21500 * alphaE * Math.Pow((0.1 * Strength + 1.25), 0.333333);
			        }
		        }

		        private double Ecs => alphaI * InitialModule;
		        private double ec2 = -0.002;

		        private double ecu
		        {
			        get
			        {
				        // Verify fcm
				        if (Strength <= 50)
					        return
						        -0.0035;

				        return
					        -0.0026 - 0.035 * Math.Pow(0.01 * (90 - Strength), 4);

			        }
		        }
	        }

	        // MCFT Parameters
	        public class MCFT : Parameters
	        {
		        public MCFT(double strength, double aggregateDiameter,
			        AggregateType aggregateType = AggregateType.Quartzite) : base(strength, aggregateDiameter,
			        aggregateType)
		        {
			        TensileStrength = fcr;
			        PlasticStrain   = ec;
			        InitialModule   = Ec;
			        UltimateStrain  = ecu;
		        }

		        private double fcr => 0.33 * Math.Sqrt(Strength);
		        private double ec  => -0.002;
		        private double ecu => -0.0035;
		        private double Ec  => -2 * Strength / ec;
	        }

	        // DSFM Parameters
	        public class DSFM : Parameters
	        {
		        public DSFM(double strength, double aggregateDiameter, AggregateType aggregateType = AggregateType.Quartzite) : base(strength, aggregateDiameter, aggregateType)
		        {
			        TensileStrength = fcr;
			        PlasticStrain   = ec;
			        InitialModule   = Ec;
			        UltimateStrain  = ecu;
		        }

		        private double fcr => 0.65 * Math.Pow(Strength, 0.33);
		        private double ec  => -0.002;
		        private double ecu => -0.0035;
		        private double Ec  => -2 * Strength / ec;
	        }

	        // Custom parameters
	        public class Custom : Parameters
	        {
		        public Custom(double strength, double aggregateDiameter, double tensileStrength, double elasticModule, double plasticStrain, double ultimateStrain) : base(strength, aggregateDiameter)
		        {
			        TensileStrength = tensileStrength;
			        InitialModule   = elasticModule;
			        PlasticStrain   = plasticStrain;
			        UltimateStrain  = ultimateStrain;
		        }
	        }
        }
	}
}