using UnitsNet;
using UnitsNet.Units;
using StressUnit = UnitsNet.Units.PressureUnit;

namespace SPMTool
{
	public class Units
    {
	    // Properties
		public LengthUnit Geometry         { get; set; }
		public LengthUnit Reinforcement    { get; set; }
		public LengthUnit Displacements    { get; set; }
		public ForceUnit  AppliedForces    { get; set; }
		public ForceUnit  StringerForces   { get; set; }
		public StressUnit PanelStresses    { get; set; }
		public StressUnit MaterialStrength { get; set; }
       
		// Default Constructor
		public Units()
		{
			Geometry         = LengthUnit.Millimeter;
			Reinforcement    = LengthUnit.Millimeter;
			Displacements    = LengthUnit.Millimeter;
			AppliedForces    = ForceUnit.Kilonewton;
			StringerForces   = ForceUnit.Kilonewton;
			PanelStresses    = StressUnit.Megapascal;
			MaterialStrength = StressUnit.Megapascal;
		}

		public AreaUnit GeometryArea
		{
			get
			{
				switch (Geometry)
				{
                    case LengthUnit.Millimeter:
	                    return AreaUnit.SquareMillimeter;

                    case LengthUnit.Centimeter:
	                    return AreaUnit.SquareCentimeter;

                    case LengthUnit.Meter:
	                    return AreaUnit.SquareMeter;
				}

				return AreaUnit.SquareMillimeter;
			}
		}

		public AreaUnit ReinforcementArea
		{
			get
			{
				switch (Reinforcement)
				{
                    case LengthUnit.Millimeter:
	                    return AreaUnit.SquareMillimeter;

                    case LengthUnit.Centimeter:
	                    return AreaUnit.SquareCentimeter;

                    case LengthUnit.Meter:
	                    return AreaUnit.SquareMeter;
				}

				return AreaUnit.SquareMillimeter;
			}
		}

		// Get dimension in mm
		public double ConvertToMillimeter(double dimension, LengthUnit fromUnit) =>
			UnitConverter.Convert(dimension, fromUnit, LengthUnit.Millimeter);

		// Convert mm to dimension
		public double ConvertFromMillimeter(double millimeter, LengthUnit toUnit) =>
			UnitConverter.Convert(millimeter, LengthUnit.Millimeter, toUnit);

		// Get force in N
		public double ConvertToNewton(double force, ForceUnit fromUnit) =>
			UnitConverter.Convert(force, fromUnit, ForceUnit.Newton);

		// Convert from N
		public double ConvertFromNewton(double newton, ForceUnit toUnit) =>
			UnitConverter.Convert(newton, ForceUnit.Newton, toUnit);
		
		// Get force in kN
		public double ConvertToKiloNewton(double force, ForceUnit fromUnit) =>
			UnitConverter.Convert(force, fromUnit, ForceUnit.Kilonewton);

		// Convert from N
		public double ConvertFromKiloNewton(double kiloNewton, ForceUnit toUnit) =>
			UnitConverter.Convert(kiloNewton, ForceUnit.Kilonewton, toUnit);

		// Get stress unit related to MPa
		public double ConvertToMPa(double stress, StressUnit fromUnit) =>
			UnitConverter.Convert(stress, fromUnit, StressUnit.Megapascal);

		// Transform stress from MPa to unit
		public double ConvertFromMPa(double megapascal, StressUnit toUnit) =>
			UnitConverter.Convert(megapascal, StressUnit.Megapascal, toUnit);
    }
}
