using UnitsNet;
using UnitsNet.Units;
using StressUnit = UnitsNet.Units.PressureUnit;

namespace SPMTool
{
	/// <summary>
    /// Units class.
    /// </summary>
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
       
		/// <summary>
        /// Default units object (mm, kN, MPa).
        /// </summary>
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

		/// <summary>
        /// Get the area unit for geometry.
        /// </summary>
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

		/// <summary>
		/// Get the area unit for reinforcement.
		/// </summary>
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

		/// <summary>
        /// Convert length to millimeters.
        /// </summary>
        /// <param name="dimension">Length value.</param>
        /// <param name="fromUnit">Current unit.</param>
		public double ConvertToMillimeter(double dimension, LengthUnit fromUnit) =>
			UnitConverter.Convert(dimension, fromUnit, LengthUnit.Millimeter);

        /// <summary>
        /// Convert length from millimeters.
        /// </summary>
        /// <param name="millimeter">Length value, in mm.</param>
        /// <param name="toUnit">Length unit to convert.</param>
        public double ConvertFromMillimeter(double millimeter, LengthUnit toUnit) =>
			UnitConverter.Convert(millimeter, LengthUnit.Millimeter, toUnit);

        /// <summary>
        /// Convert force to Newtons.
        /// </summary>
        /// <param name="force">Force value.</param>
        /// <param name="fromUnit">Current unit.</param>
        public double ConvertToNewton(double force, ForceUnit fromUnit) =>
			UnitConverter.Convert(force, fromUnit, ForceUnit.Newton);

		/// <summary>
        /// Convert force from Newtons.
        /// </summary>
        /// <param name="newton">Force value, in N.</param>
        /// <param name="toUnit">Force unit to convert.</param>
		public double ConvertFromNewton(double newton, ForceUnit toUnit) =>
			UnitConverter.Convert(newton, ForceUnit.Newton, toUnit);

		/// <summary>
		/// Convert force to KiloNewtons.
		/// </summary>
		/// <param name="force">Force value.</param>
		/// <param name="fromUnit">Current unit.</param>
		public double ConvertToKiloNewton(double force, ForceUnit fromUnit) =>
			UnitConverter.Convert(force, fromUnit, ForceUnit.Kilonewton);

        /// <summary>
        /// Convert force from KiloNewtons.
        /// </summary>
        /// <param name="kiloNewton">Force value, in kN.</param>
        /// <param name="toUnit">Force unit to convert.</param>
        public double ConvertFromKiloNewton(double kiloNewton, ForceUnit toUnit) =>
			UnitConverter.Convert(kiloNewton, ForceUnit.Kilonewton, toUnit);

        /// <summary>
        /// Convert stress/pressure to MegaPascals.
        /// </summary>
        /// <param name="stress">Stress value.</param>
        /// <param name="fromUnit">Current unit.</param>
        public double ConvertToMPa(double stress, StressUnit fromUnit) =>
			UnitConverter.Convert(stress, fromUnit, StressUnit.Megapascal);

        /// <summary>
        /// Convert stress/pressure from MegaPascals.
        /// </summary>
        /// <param name="megapascal">Stress/pressure value, in MPa.</param>
        /// <param name="toUnit">Stress/pressure unit to convert.</param>
		public double ConvertFromMPa(double megapascal, StressUnit toUnit) =>
			UnitConverter.Convert(megapascal, StressUnit.Megapascal, toUnit);
    }
}
