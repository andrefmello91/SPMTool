using System;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;

namespace SPMTool.Material
{
	public class StringerReinforcement
	{
		// Properties
		public int    NumberOfBars  { get; }
		public double BarDiameter   { get; }
		public Steel  Steel         { get; }

		// Constructor
		public StringerReinforcement(int numberOfBars, double barDiameter, Steel steel = null)
		{
			NumberOfBars = numberOfBars;
			BarDiameter  = barDiameter;
			Steel        = steel;
		}

		// Verify if reinforcement is set
		public bool IsSet => NumberOfBars > 0 && BarDiameter > 0;

		// Calculated reinforcement area
		public double Area
		{
			get
			{
				if (IsSet)
					return
						0.25 * NumberOfBars * Constants.Pi * BarDiameter * BarDiameter;

				return 0;
			}
		}

		public override string ToString()
		{
			// Approximate steel area
			double As = Math.Round(Area, 2);

            char phi = (char)Characters.Phi;

            return
                "Reinforcement: " + NumberOfBars + " " + phi + BarDiameter + " mm (" + As +
				" mm²)\n\n" + Steel;
		}
    }
}
