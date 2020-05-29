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

		// Standard parameters
		public enum Standard
		{
			NBR6118,
			MC2010,
			MCFT,
			DSFM
		}

        // Implementation of concrete parameters
        public abstract class Parameters
		{
			public Units         Units             { get; }
			public double        AggregateDiameter { get; }
			public AggregateType Type              { get; }
			public double        Strength          { get; }
			public double        Poisson           { get; }
			public double        TensileStrength   { get; set; }
			public double        InitialModule     { get; set; }
			public double        PlasticStrain     { get; set; }
			public double        UltimateStrain    { get; set; }
			public double        CrackStrain       { get; set; }

			public Parameters(double strength, double aggregateDiameter, AggregateType aggregateType = AggregateType.Quartzite)
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

				}
			}
        }
    }
}