﻿using System.Diagnostics.CodeAnalysis;
using andrefmello91.Extensions;
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
		private Units GetUnits() => GetDictionary(USaveName).GetUnits() ?? Units.Default;

		/// <summary>
		///     Read analysis settings on dictionary.
		/// </summary>
		[return: NotNull]
		private AnalysisSettings GetAnalysisSettings() => GetDictionary(ASSaveName).GetAnalysisSettings() ?? AnalysisSettings.Default;

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