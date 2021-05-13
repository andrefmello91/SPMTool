using System.Diagnostics.CodeAnalysis;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Core;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet.Units;

namespace SPMTool.Application
{
	/// <summary>
	///     Application settings class.
	/// </summary>
	public class Settings : ExtendedObject
	{

		#region Fields

		/// <summary>
		///     <see cref="Application.Units" /> save name.
		/// </summary>
		private const string USaveName = "Units";

		/// <summary>
		///     <see cref="AnalysisSettings" /> save name.
		/// </summary>
		private const string ASSaveName = "Analysis Settings";
		
		/// <summary>
		///     <see cref="DisplaySettings" /> save name.
		/// </summary>
		private const string DSaveName = "Display Settings";

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
		private DisplaySettings _display;

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

		/// <inheritdoc />
		public override Layer Layer => default;

		/// <inheritdoc />
		public override string Name => $"{typeof(Settings)}";

		/// <summary>
		///     Get <see cref="Application.Units" /> saved in database.
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

		#region Methods

		/// <inheritdoc />
		public override DBObject CreateObject() => new Xrecord
		{
			Data = new ResultBuffer(_analysis.GetTypedValues())
		};

		protected override void GetProperties()
		{
			_analysis = GetAnalysisSettings();
			_units    = GetUnits();

		}

		protected override void SetProperties()
		{
			SetAnalysisSettings(_analysis);
			SetUnits(_units);
		}

		/// <summary>
		///     Read analysis settings on dictionary.
		/// </summary>
		[return: NotNull]
		private AnalysisSettings GetAnalysisSettings() => GetDictionary(ASSaveName).GetAnalysisSettings() ?? AnalysisSettings.Default;

		/// <summary>
		///     Read units on dictionary.
		/// </summary>
		[return: NotNull]
		private Units GetUnits() => GetDictionary(USaveName).GetUnits() ?? Units.Default;

		/// <summary>
		///     Save this <paramref name="settings" /> in database.
		/// </summary>
		private void SetAnalysisSettings(AnalysisSettings settings)
		{
			_analysis = settings;

			SetDictionary(settings.GetTypedValues(), ASSaveName);
		}

		/// <summary>
		///     Save this <paramref name="units" /> in database.
		/// </summary>
		private void SetUnits(Units units)
		{
			_units = units;

			SetDictionary(units.GetTypedValues(), USaveName);
		}

		#endregion

	}
}