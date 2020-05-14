using MathNet.Numerics.LinearAlgebra;

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

		// Calculated reinforcement area
		public double Area
		{
			get
			{
				// Initialize As
				double As = 0;

				if (NumberOfBars > 0 && BarDiameter > 0)
					As = 0.25 * NumberOfBars * Constants.Pi * BarDiameter * BarDiameter;

				return As;
			}
		}
	}
}
