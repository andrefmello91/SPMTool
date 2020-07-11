using UnitsNet;
using UnitsNet.Units;

namespace SPMTool
{
	// Dimension units related to mm
	public enum DimensionUnit
	{
		mm = LengthUnit.Millimeter,
		cm = LengthUnit.Centimeter,
		m  = LengthUnit.Meter
	}
	
	// Force units related to N
	public enum ForceUnit
	{
		N  = UnitsNet.Units.ForceUnit.Newton,
		kN = UnitsNet.Units.ForceUnit.Kilonewton,
		MN = UnitsNet.Units.ForceUnit.Meganewton
	}

	// Stress units
	public enum StressUnit
	{
		Pa  = PressureUnit.Pascal,
		kPa = PressureUnit.Kilopascal,
		MPa = PressureUnit.Megapascal,
		GPa = PressureUnit.Gigapascal
	}

	public class Units
    {
	    // Properties
		public DimensionUnit Geometry         { get; set; }
		public DimensionUnit Reinforcement    { get; set; }
		public DimensionUnit Displacements    { get; set; }
		public ForceUnit     AppliedForces    { get; set; }
		public ForceUnit     StringerForces   { get; set; }
		public StressUnit    PanelStresses    { get; set; }
		public StressUnit    MaterialStrength { get; set; }
       
		// Default Constructor
		public Units()
		{
			Geometry         = DimensionUnit.mm;
			Reinforcement    = DimensionUnit.mm;
			Displacements    = DimensionUnit.mm;
			AppliedForces    = ForceUnit.kN;
			StringerForces   = ForceUnit.kN;
			PanelStresses    = StressUnit.MPa;
			MaterialStrength = StressUnit.MPa;
		}

		// Get dimension in mm
		public double ConvertToMilimeter(double dimension, DimensionUnit fromUnit) =>
			UnitConverter.Convert(dimension, (LengthUnit) fromUnit, (LengthUnit) DimensionUnit.mm);

		// Convert mm to dimension
		public double ConvertfromMilimeter(double mm, DimensionUnit toUnit) =>
			UnitConverter.Convert(mm, (LengthUnit) DimensionUnit.mm, (LengthUnit) toUnit);

		// Get force in N
		public double ConvertToNewton(double force, ForceUnit fromUnit) =>
			UnitConverter.Convert(force, (UnitsNet.Units.ForceUnit) fromUnit, (UnitsNet.Units.ForceUnit) ForceUnit.N);

		// Convert from N
		public double ConvertFromNewton(double newton, ForceUnit toUnit) =>
			UnitConverter.Convert(newton, (UnitsNet.Units.ForceUnit) ForceUnit.N, (UnitsNet.Units.ForceUnit) toUnit);
		
		// Get force in kN
		public double ConvertToKiloNewton(double force, ForceUnit fromUnit) =>
			UnitConverter.Convert(force, (UnitsNet.Units.ForceUnit) fromUnit, (UnitsNet.Units.ForceUnit) ForceUnit.kN);

		// Convert from N
		public double ConvertFromKiloNewton(double kiloNewton, ForceUnit toUnit) =>
			UnitConverter.Convert(kiloNewton, (UnitsNet.Units.ForceUnit) ForceUnit.kN, (UnitsNet.Units.ForceUnit) toUnit);

		// Get stress unit related to MPa
		public double ConvertToMPa(double stress, StressUnit fromUnit) =>
			UnitConverter.Convert(stress, (PressureUnit) fromUnit, (PressureUnit) StressUnit.MPa);

		// Transform stress from MPa to unit
		public double ConvertFromMPa(double mpa, StressUnit toUnit) =>
			UnitConverter.Convert(mpa, (PressureUnit) StressUnit.MPa, (PressureUnit) toUnit);
    }
}
