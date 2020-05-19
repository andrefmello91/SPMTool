namespace SPMTool
{
    public class Units
    {
        // Dimension units related to mm
        public enum Dimension
        {
            mm = 1,
            cm = 10,
            m  = 1000,
        }

        // Force units related to N
        public enum Force
        {
            N  = 1,
            kN = 1000,
            MN = 1000000
        }

        // Stress units
        public enum Stress
        {
            Pa,
            kPa,
            MPa,
			GPa
        }

		// Properties
		public Dimension DimensionUnit { get; }
		public Force     ForceUnit     { get; }
		public Stress    StressUnit    { get; }
       
		// Constructor
		public Units(Dimension dimensionUnit = Dimension.mm, Force forceUnit = Force.kN, Stress stressUnit = Stress.MPa)
		{
			DimensionUnit = dimensionUnit;
			ForceUnit     = forceUnit;
			StressUnit    = stressUnit;
		}

		// Get dimension in mm
		public int DimensionTomm => (int) DimensionUnit;

		// Get force in N
		public int ForceToN => (int) ForceUnit;

		// Get stress unit related to MPa
		public double StressToMPa => Stress_to_MPa(StressUnit);

		// Get stress unit related to MPa
		public static double Stress_to_MPa(Stress unit)
		{
			if (unit == Stress.Pa)
				return 1E-6;

			if (unit == Stress.kPa)
				return 1E-3;

			if (unit == Stress.MPa)
				return 1;

			// If GPa
			return 1000;
        }
    }
}
