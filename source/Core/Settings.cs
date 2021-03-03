using System.Diagnostics.CodeAnalysis;
using Extensions;
using SPMTool.Core;
using SPMTool.Extensions;
using UnitsNet.Units;

namespace SPMTool.Application
{
	/// <summary>
	///     Application settings class.
	/// </summary>
	public class Settings : DictionaryCreator
	{
		#region Fields

		/// <summary>
		///     <see cref="SPMTool.Units" /> save name.
		/// </summary>
		private const string USaveName = "Units";

		/// <summary>
		///     <see cref="AnalysisSettings" /> save name.
		/// </summary>
		private const string ASSaveName = "Analysis Settings";

		/// <summary>
		///     Dimension unit options.
		/// </summary>
		public static readonly string[] DimensionUnits = { LengthUnit.Millimeter.Abbrev(), LengthUnit.Centimeter.Abbrev(), LengthUnit.Meter.Abbrev() };

		/// <summary>
		///     Force unit options.
		/// </summary>
		public static readonly string[] ForceUnits = { ForceUnit.Newton.Abbrev(), ForceUnit.Kilonewton.Abbrev(), ForceUnit.Meganewton.Abbrev() };

		/// <summary>
		///     Stress unit options.
		/// </summary>
		public static readonly string[] StressUnits = { PressureUnit.Pascal.Abbrev(), PressureUnit.Kilopascal.Abbrev(), PressureUnit.Megapascal.Abbrev(), PressureUnit.Gigapascal.Abbrev() };

		private AnalysisSettings _analysis;
		private Units _units;

		#endregion

		#region Properties

		/// <summary>
		///     Get <see cref="AnalysisSettings" /> saved in database.
		/// </summary>
		public AnalysisSettings Analysis
		{
			get => _analysis;
			set => SetAnalysisSettings(value);
		}

		/// <summary>
		///     Get <see cref="SPMTool.Units" /> saved in database.
		/// </summary>
		public Units Units
		{
			get => _units;
			set => SetUnits(value);
		}

		#endregion

		#region Constructors

		public Settings()
		{
			DictionaryId = DataBase.NodId;
			GetProperties();
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read units on dictionary.
		/// </summary>
		[return: NotNull]
		private Units GetUnits()
		{
			var data = GetDictionary(USaveName);

			return data switch
			{
				null => Units.Default,
				_    => new Units
				{
					Geometry              = (LengthUnit)   data[0].ToInt(),
					Reinforcement         = (LengthUnit)   data[1].ToInt(),
					Displacements         = (LengthUnit)   data[2].ToInt(),
					AppliedForces         = (ForceUnit)    data[3].ToInt(),
					StringerForces        = (ForceUnit)    data[4].ToInt(),
					PanelStresses         = (PressureUnit) data[5].ToInt(),
					MaterialStrength      = (PressureUnit) data[6].ToInt(),
					CrackOpenings         = (LengthUnit)   data[7].ToInt(),
					DisplacementMagnifier = data[8].ToInt()
				}
			};
		}

		/// <summary>
		///     Read analysis settings on dictionary.
		/// </summary>
		[return: NotNull]
		private AnalysisSettings GetAnalysisSettings()
		{
			var data = GetDictionary(ASSaveName);

			return data switch
			{
				null => AnalysisSettings.Default,
				_    => new AnalysisSettings
				{
					Tolerance     = data[0].ToDouble(),
					NumLoadSteps  = data[1].ToInt(),
					MaxIterations = data[2].ToInt()
				}
			};
		}

		/// <summary>
		///     Save this <paramref name="units" /> in database.
		/// </summary>
		private void SetUnits(Units units)
		{
			_units = units;

			SetDictionary(units.GetTypedValues(), USaveName);
		}

		/// <summary>
		///     Save this <paramref name="settings" /> in database.
		/// </summary>
		private void SetAnalysisSettings(AnalysisSettings settings)
		{
			_analysis = settings;

			SetDictionary(settings.GetTypedValues(), ASSaveName);
		}

		protected override bool GetProperties()
		{
			_analysis = GetAnalysisSettings();
			_units    = GetUnits();

			return true;
		}

		protected override void SetProperties()
		{
			SetAnalysisSettings(_analysis);
			SetUnits(_units);
		}

		#endregion
	}
}