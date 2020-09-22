using System;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool
{
	/// <summary>
    /// Units struct.
    /// </summary>
	public struct Units : IEquatable<Units>
    {
		/// <summary>
        /// Get/set the <see cref="LengthUnit"/> for geometry.
        /// </summary>
		public LengthUnit Geometry         { get; set; }

		/// <summary>
		/// Get/set the <see cref="LengthUnit"/> for reinforcement.
		/// </summary>
		public LengthUnit Reinforcement    { get; set; }

		/// <summary>
		/// Get/set the <see cref="LengthUnit"/> for displacements.
		/// </summary>
		public LengthUnit Displacements    { get; set; }

        /// <summary>
        /// Get/set the <see cref="ForceUnit"/> for applied forces.
        /// </summary>
        public ForceUnit  AppliedForces    { get; set; }

        /// <summary>
        /// Get/set the <see cref="ForceUnit"/> for stringer forces.
        /// </summary>
        public ForceUnit  StringerForces   { get; set; }

        /// <summary>
        /// Get/set the <see cref="PressureUnit"/> for panel stresses.
        /// </summary>
        public PressureUnit PanelStresses    { get; set; }

        /// <summary>
        /// Get/set the <see cref="PressureUnit"/> for material parameters.
        /// </summary>
        public PressureUnit MaterialStrength { get; set; }

        /// <summary>
        /// Get the <see cref="AreaUnit"/> for geometry.
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

					default:
						return AreaUnit.SquareMillimeter;
				}
			}
		}

        /// <summary>
        /// Get the <see cref="AreaUnit"/> for reinforcement.
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
        /// Returns true if this <see cref="Units"/> has the default values.
        /// <para>Default units: mm, kN, MPa.</para>
        /// </summary>
        public bool IsDefault => Equals(Default);

        /// <summary>
        /// Default units object.
        /// <para>Default units: mm, kN, MPa.</para>
        /// </summary>
        public static Units Default => new Units
		{
			Geometry         = LengthUnit.Millimeter,
			Reinforcement    = LengthUnit.Millimeter,
			Displacements    = LengthUnit.Millimeter,
			AppliedForces    = ForceUnit.Kilonewton,
			StringerForces   = ForceUnit.Kilonewton,
			PanelStresses    = PressureUnit.Megapascal,
			MaterialStrength = PressureUnit.Megapascal
		};

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
        public double ConvertToMPa(double stress, PressureUnit fromUnit) =>
			UnitConverter.Convert(stress, fromUnit, PressureUnit.Megapascal);

        /// <summary>
        /// Convert stress/pressure from MegaPascals.
        /// </summary>
        /// <param name="megapascal">Stress/pressure value, in MPa.</param>
        /// <param name="toUnit">Stress/pressure unit to convert.</param>
		public double ConvertFromMPa(double megapascal, PressureUnit toUnit) =>
			UnitConverter.Convert(megapascal, PressureUnit.Megapascal, toUnit);

		/// <summary>
        /// Returns true if all units coincide.
        /// </summary>
        /// <param name="other">The other <see cref="Units"/> object.</param>
        public bool Equals(Units other) => Geometry == other.Geometry && Reinforcement == other.Reinforcement && Displacements == other.Displacements && AppliedForces == other.AppliedForces && StringerForces == other.StringerForces && PanelStresses == other.PanelStresses && MaterialStrength == other.MaterialStrength;

		/// <summary>
		/// Returns true if all units coincide.
		/// </summary>
		public static bool operator == (Units left, Units right) => left.Equals(right);

		/// <summary>
		/// Returns true if at least a unit do not coincide.
		/// </summary>
		public static bool operator != (Units left, Units right) => !left.Equals(right);
    }
}
