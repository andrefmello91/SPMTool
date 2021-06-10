using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using andrefmello91.Material.Concrete;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics;
using SPMTool.Application;
using SPMTool.Enums;
using UnitsNet.Units;

namespace SPMTool
{
	public partial class Extensions
	{
		/// <summary>
		///     Convert this <paramref name="value" /> to a <see cref="double" />.
		/// </summary>
		public static double ToDouble(this TypedValue value) => System.Convert.ToDouble(value.Value);

		/// <summary>
		///     Convert this <paramref name="value" /> to an <see cref="int" />.
		/// </summary>
		public static int ToInt(this TypedValue value) => System.Convert.ToInt32(value.Value);

		/// <summary>
		///     Get an <see cref="AnalysisSettings" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent an <see cref="AnalysisSettings" />.</param>
		public static AnalysisSettings? GetAnalysisSettings(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 5)
				return null;

			return new AnalysisSettings
			{
				ForceTolerance        = values.ElementAt(0).ToDouble(),
				DisplacementTolerance = values.ElementAt(1).ToDouble(),
				NumberOfSteps         = values.ElementAt(2).ToInt(),
				MaxIterations         = values.ElementAt(3).ToInt(),
				Solver                = (NonLinearSolver) values.ElementAt(4).ToInt()
			};
		}
		
				/// <summary>
		///     Get a <see cref="Constraint" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="Constraint" />.</param>
		public static Constraint? GetConstraint(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 1
				? null
				: Constraint.FromDirection((ComponentDirection) values.ElementAt(0).ToInt());

		/// <summary>
		///     Get a <see cref="CrossSection" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="CrossSection" />.</param>
		public static CrossSection? GetCrossSection(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 2
				? null
				: new CrossSection(values.ElementAt(0).ToDouble(), values.ElementAt(1).ToDouble());

		/// <summary>
		///     Get a <see cref="PlaneDisplacement" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="PlaneDisplacement" />.</param>
		public static PlaneDisplacement? GetDisplacement(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 2
				? null
				: new PlaneDisplacement(values.ElementAt(0).ToDouble(), values.ElementAt(1).ToDouble());

		/// <summary>
		///     Get a <see cref="DisplaySettings" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent an <see cref="DisplaySettings" />.</param>
		public static DisplaySettings? GetDisplaySettings(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 5)
				return null;

			return new DisplaySettings
			{
				NodeScale             = values.ElementAt(0).ToDouble(),
				ConditionScale        = values.ElementAt(1).ToDouble(),
				ResultScale           = values.ElementAt(2).ToDouble(),
				TextScale             = values.ElementAt(3).ToDouble(),
				DisplacementMagnifier = values.ElementAt(4).ToInt()
			};
		}
		
		/// <summary>
		///     Get an int that represents an <see cref="Enum" /> value from <see cref="TypedValue" />'s.
		/// </summary>
		public static int? GetEnumValue(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 1
				? null
				: values.ElementAt(0).ToInt();

		/// <summary>
		///     Get a <see cref="PlaneForce" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="PlaneForce" />.</param>
		public static PlaneForce? GetForce(this IEnumerable<TypedValue>? values) =>
			values.IsNullOrEmpty() || values.Count() != 2
				? null
				: new PlaneForce(values.ElementAt(0).ToDouble(), values.ElementAt(1).ToDouble());
		
		/// <summary>
		///     Get a <see cref="IParameters" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="IParameters" />.</param>
		public static IParameters? GetParameters(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 8)
				return null;

			var model = (ParameterModel) values.ElementAt(0).ToInt();
			var type  = (AggregateType) values.ElementAt(1).ToInt();
			var fc    = values.ElementAt(2).ToDouble();
			var phiAg = values.ElementAt(3).ToDouble();

			if (model != ParameterModel.Custom)
				return new Parameters(fc, phiAg, model, type);

			var ft  = values.ElementAt(4).ToDouble();
			var Ec  = values.ElementAt(5).ToDouble();
			var ec  = -values.ElementAt(6).ToDouble().Abs();
			var ecu = -values.ElementAt(7).ToDouble().Abs();

			return new CustomParameters(fc, ft, Ec, phiAg, ec, ecu);
		}
		
		/// <summary>
		///     Get a <see cref="UniaxialReinforcement" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="UniaxialReinforcement" />.</param>
		public static UniaxialReinforcement? GetReinforcement(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 4)
				return null;

			var nBars = values.ElementAt(0).ToInt();
			var phi   = values.ElementAt(1).ToDouble();

			if (nBars == 0 || phi.ApproxZero(1E-3))
				return null;

			var fy = values.ElementAt(2).ToDouble();
			var Es = values.ElementAt(3).ToDouble();

			return new UniaxialReinforcement(nBars, phi, new Steel(fy, Es));
		}

		/// <summary>
		///     Get a <see cref="WebReinforcementDirection" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent a <see cref="WebReinforcementDirection" />.</param>
		public static WebReinforcementDirection? GetReinforcementDirection(this IEnumerable<TypedValue>? values, Axis direction)
		{
			if (values.IsNullOrEmpty() || values.Count() != 4)
				return null;

			var phi = values.ElementAt(0).ToDouble();
			var s   = values.ElementAt(1).ToDouble();

			if (phi.ApproxZero(1E-3) || s.ApproxZero(1E-3))
				return null;

			var fy = values.ElementAt(2).ToDouble();
			var Es = values.ElementAt(3).ToDouble();

			var angle = direction is Axis.X
				? 0
				: Constants.PiOver2;

			return new WebReinforcementDirection(phi, s, new Steel(fy, Es), 0, angle);
		}
		
				/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="PlaneDisplacement" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this PlaneDisplacement displacement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, displacement.X.Millimeters),
				new TypedValue((int) DxfCode.Real, displacement.Y.Millimeters)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="PlaneForce" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this PlaneForce force) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, force.X.Newtons),
				new TypedValue((int) DxfCode.Real, force.Y.Newtons)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="Constraint" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this Constraint constraint) =>
			new[]
			{
				new TypedValue((int) DxfCode.Int32, (int) constraint.Direction)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="CrossSection" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this CrossSection crossSection) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, crossSection.Width.Millimeters),
				new TypedValue((int) DxfCode.Real, crossSection.Height.Millimeters)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="UniaxialReinforcement" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this UniaxialReinforcement? reinforcement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Int32, reinforcement?.NumberOfBars ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.BarDiameter.Millimeters ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.YieldStress.Megapascals ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.ElasticModule.Megapascals ?? 0)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="WebReinforcementDirection" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this WebReinforcementDirection? reinforcement) =>
			new[]
			{
				new TypedValue((int) DxfCode.Real, reinforcement?.BarDiameter.Millimeters ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.BarSpacing.Millimeters ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.YieldStress.Megapascals ?? 0),
				new TypedValue((int) DxfCode.Real, reinforcement?.Steel?.ElasticModule.Megapascals ?? 0)
			};

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="WebReinforcementDirection" />.
		/// </summary>
		public static TypedValue[] GetTypedValues(this IParameters parameters) =>
			new[]
			{
				new TypedValue((int) DxfCode.Int32, (int) parameters.Model),
				new TypedValue((int) DxfCode.Int32, (int) parameters.Type),
				new TypedValue((int) DxfCode.Real, parameters.Strength.Megapascals),
				new TypedValue((int) DxfCode.Real, parameters.AggregateDiameter.Millimeters),
				new TypedValue((int) DxfCode.Real, parameters.TensileStrength.Megapascals),
				new TypedValue((int) DxfCode.Real, parameters.ElasticModule.Megapascals),
				new TypedValue((int) DxfCode.Real, parameters.PlasticStrain.Abs()),
				new TypedValue((int) DxfCode.Real, parameters.UltimateStrain.Abs())
			};


		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from an <see cref="Units" />.
		/// </summary>
		/// <returns>
		///     An array based in <see cref="Units.Default" /> if the object is null.
		/// </returns>
		public static TypedValue[] GetTypedValues(this Units? units)
		{
			units ??= Units.Default;

			return new[]
			{
				new TypedValue((int) DxfCode.Int32, (int) units.Geometry),
				new TypedValue((int) DxfCode.Int32, (int) units.Reinforcement),
				new TypedValue((int) DxfCode.Int32, (int) units.Displacements),
				new TypedValue((int) DxfCode.Int32, (int) units.CrackOpenings),
				new TypedValue((int) DxfCode.Int32, (int) units.AppliedForces),
				new TypedValue((int) DxfCode.Int32, (int) units.StringerForces),
				new TypedValue((int) DxfCode.Int32, (int) units.PanelStresses),
				new TypedValue((int) DxfCode.Int32, (int) units.MaterialStrength)
			};
		}

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from an <see cref="AnalysisSettings" />.
		/// </summary>
		/// <returns>
		///     An array based in <see cref="AnalysisSettings.Default" /> if the object is null.
		/// </returns>
		public static TypedValue[] GetTypedValues(this AnalysisSettings? settings)
		{
			settings ??= AnalysisSettings.Default;

			return new[]
			{
				new TypedValue((int) DxfCode.Real,  settings.ForceTolerance),
				new TypedValue((int) DxfCode.Real,  settings.DisplacementTolerance),
				new TypedValue((int) DxfCode.Int32, settings.NumberOfSteps),
				new TypedValue((int) DxfCode.Int32, settings.MaxIterations),
				new TypedValue((int) DxfCode.Int32, (int) settings.Solver)
			};
		}

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from a <see cref="DisplaySettings" />.
		/// </summary>
		/// <returns>
		///     An array based in <see cref="DisplaySettings.Default" /> if the object is null.
		/// </returns>
		public static TypedValue[] GetTypedValues(this DisplaySettings? displaySettings)
		{
			displaySettings ??= DisplaySettings.Default;

			return new[]
			{
				new TypedValue((int) DxfCode.Real, displaySettings.NodeScale),
				new TypedValue((int) DxfCode.Real, displaySettings.ConditionScale),
				new TypedValue((int) DxfCode.Real, displaySettings.ResultScale),
				new TypedValue((int) DxfCode.Real, displaySettings.TextScale),
				new TypedValue((int) DxfCode.Int32, displaySettings.DisplacementMagnifier)
			};
		}

		/// <summary>
		///     Get an array of <see cref="TypedValue" /> from an <see cref="Enum" /> value.
		/// </summary>
		/// <typeparam name="TEnum">An <see cref="Enum" /> type.</typeparam>
		public static TypedValue[] GetTypedValues<TEnum>(this TEnum enumValue) where TEnum : Enum =>
			new[] { new TypedValue((int) DxfCode.Int32, (int) (object) enumValue) };

		/// <summary>
		///     Get a <see cref="Units" /> from <see cref="TypedValue" />'s.
		/// </summary>
		/// <param name="values">The <see cref="TypedValue" />'s that represent an <see cref="Units" />.</param>
		public static Units? GetUnits(this IEnumerable<TypedValue>? values)
		{
			if (values.IsNullOrEmpty() || values.Count() != 8)
				return null;

			return new Units
			{
				Geometry         = (LengthUnit) values.ElementAt(0).ToInt(),
				Reinforcement    = (LengthUnit) values.ElementAt(1).ToInt(),
				Displacements    = (LengthUnit) values.ElementAt(2).ToInt(),
				CrackOpenings    = (LengthUnit) values.ElementAt(3).ToInt(),
				AppliedForces    = (ForceUnit) values.ElementAt(4).ToInt(),
				StringerForces   = (ForceUnit) values.ElementAt(5).ToInt(),
				PanelStresses    = (PressureUnit) values.ElementAt(6).ToInt(),
				MaterialStrength = (PressureUnit) values.ElementAt(7).ToInt()
			};
		}
	}
}