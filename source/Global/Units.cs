using System;
using Extensions;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool
{
	/// <summary>
	///     Units class.
	/// </summary>
	public class Units : IEquatable<Units>
	{
		#region Fields

		/// <summary>
		///     Default units object.
		///     <para>Default units: mm, kN, MPa.</para>
		/// </summary>
		public static readonly Units Default = new Units
		{
			Geometry              = LengthUnit.Millimeter,
			Reinforcement         = LengthUnit.Millimeter,
			Displacements         = LengthUnit.Millimeter,
			CrackOpenings         = LengthUnit.Millimeter,
			AppliedForces         = ForceUnit.Kilonewton,
			StringerForces        = ForceUnit.Kilonewton,
			PanelStresses         = PressureUnit.Megapascal,
			MaterialStrength      = PressureUnit.Megapascal,
			DisplacementMagnifier = 200
		};

		/// <summary>
		///     Default tolerance for <see cref="Length" /> comparisons.
		/// </summary>
		public static readonly Length LengthTolerance = Length.FromMillimeters(1E-3);

		/// <summary>
		///     Default tolerance for crack openings comparisons.
		/// </summary>
		public static readonly Length CrackTolerance = Length.FromMillimeters(1E-9);

		/// <summary>
		///     Default tolerance for <see cref="Force" /> comparisons.
		/// </summary>
		public static readonly Force ForceTolerance = Force.FromNewtons(1E-3);

		/// <summary>
		///     Default tolerance for <see cref="Pressure" /> comparisons.
		/// </summary>
		public static readonly Pressure StressTolerance = Pressure.FromPascals(1E-3);

		/// <summary>
		///     Default tolerance for <see cref="Area" /> comparisons.
		/// </summary>
		public static readonly Area AreaTolerance = Area.FromSquareMillimeters(1E-3);

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the <see cref="ForceUnit" /> for applied forces.
		/// </summary>
		public ForceUnit  AppliedForces    { get; set; }

		/// <summary>
		///     Get/set the <see cref="LengthUnit" /> for crack openings.
		/// </summary>
		public LengthUnit CrackOpenings    { get; set; }

		/// <summary>
		///     Get/set the displacement magnifier factor.
		/// </summary>
		public int DisplacementMagnifier { get; set; }

		/// <summary>
		///     Get/set the <see cref="LengthUnit" /> for displacements.
		/// </summary>
		public LengthUnit Displacements    { get; set; }

		/// <summary>
		///     Get the displacement scale factor.
		/// </summary>
		public double DisplacementScaleFactor => DisplacementMagnifier > 0 ? DisplacementMagnifier * ScaleFactor : 200 * ScaleFactor;

		/// <summary>
		///     Get/set the <see cref="LengthUnit" /> for geometry.
		/// </summary>
		public LengthUnit Geometry         { get; set; }

		/// <summary>
		///     Get the <see cref="AreaUnit" /> for geometry.
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
		///     Returns true if this <see cref="Units" /> has the default values.
		///     <para>Default units: mm, kN, MPa.</para>
		/// </summary>
		public bool IsDefault => Equals(Default);

		/// <summary>
		///     Get/set the <see cref="PressureUnit" /> for material parameters.
		/// </summary>
		public PressureUnit MaterialStrength { get; set; }

		/// <summary>
		///     Get/set the <see cref="PressureUnit" /> for panel stresses.
		/// </summary>
		public PressureUnit PanelStresses    { get; set; }

		/// <summary>
		///     Get/set the <see cref="LengthUnit" /> for reinforcement.
		/// </summary>
		public LengthUnit Reinforcement    { get; set; }

		/// <summary>
		///     Get the <see cref="AreaUnit" /> for reinforcement.
		/// </summary>
		public AreaUnit ReinforcementArea
		{
			get
			{
				switch (Reinforcement)
				{
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
		///     Get the drawing scale factor.
		/// </summary>
		public double ScaleFactor => Geometry is LengthUnit.Millimeter ? 1 : 1.ConvertFromMillimeter(Geometry);

		/// <summary>
		///     Get/set the <see cref="ForceUnit" /> for stringer forces.
		/// </summary>
		public ForceUnit  StringerForces   { get; set; }

		/// <summary>
		///     Get the tolerance for geometry comparisons.
		/// </summary>
		public double Tolerance => 0.001.ConvertFromMillimeter(Geometry);

		#endregion

		#region  Methods

		/// <summary>
		///     Returns true if all units coincide.
		/// </summary>
		/// <param name="other">The other <see cref="Units" /> object.</param>
		public bool Equals(Units other) => !(other is null) && Geometry == other.Geometry && Reinforcement == other.Reinforcement && Displacements == other.Displacements && AppliedForces == other.AppliedForces && StringerForces == other.StringerForces && PanelStresses == other.PanelStresses && MaterialStrength == other.MaterialStrength;

		public override bool Equals(object obj) => obj is Units units && Equals(units);

		public override int GetHashCode() => base.GetHashCode();

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if all units coincide.
		/// </summary>
		public static bool operator == (Units left, Units right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if at least a unit do not coincide.
		/// </summary>
		public static bool operator != (Units left, Units right) => !(left is null) && !left.Equals(right);

		#endregion
	}
}