using System.Diagnostics.CodeAnalysis;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Core;
using SPMTool.Enums;
using UnitsNet.Units;
#nullable enable

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
		///     <see cref="AnalysisParameters" /> save name.
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

		private AnalysisParameters _analysis;
		private DisplaySettings _display;
		private Units _units;

		#endregion

		#region Properties

		/// <summary>
		///     Get <see cref="AnalysisParameters" /> saved in database.
		/// </summary>
		public AnalysisParameters Analysis
		{
			get => _analysis;
			set => Set(value);
		}

		/// /
		/// <summary>
		///     Get <see cref="Application.DisplaySettings" /> saved in database.
		/// </summary>
		public DisplaySettings Display
		{
			get => _display;
			set => Set(value);
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
			set => Set(value);
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Create a settings objects
		/// </summary>
		/// <param name="database">The AutoCAD database.</param>
		public Settings(Database database)
			: base(database.BlockTableId)
		{
			DictionaryId = database.NamedObjectsDictionaryId;
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
			_display  = GetDisplaySettings();
		}

		protected override void SetProperties()
		{
			Set(_analysis);
			Set(_units);
			Set(_display);
		}

		/// <summary>
		///     Read analysis settings on dictionary.
		/// </summary>
		[return: NotNull]
		private AnalysisParameters GetAnalysisSettings() => GetDictionary(ASSaveName).GetAnalysisParameters() ?? AnalysisParameters.Default;

		/// <summary>
		///     Read display settings on dictionary.
		/// </summary>
		[return: NotNull]
		private DisplaySettings GetDisplaySettings() => GetDictionary(DSaveName).GetDisplaySettings() ?? DisplaySettings.Default;

		/// <summary>
		///     Read units on dictionary.
		/// </summary>
		[return: NotNull]
		private Units GetUnits() => GetDictionary(USaveName).GetUnits() ?? Units.Default;

		/// <summary>
		///     Save this <paramref name="parameters" /> in database.
		/// </summary>
		private void Set(AnalysisParameters parameters)
		{
			_analysis = parameters;

			SetDictionary(parameters.GetTypedValues(), ASSaveName);
		}

		/// <summary>
		///     Save this <paramref name="units" /> in database.
		/// </summary>
		private void Set(Units units)
		{
			_units = units;

			SetDictionary((TypedValue[]) units, USaveName);
		}

		/// <summary>
		///     Save this <paramref name="display" /> in database.
		/// </summary>
		private void Set(DisplaySettings display)
		{
			_display.NodeScale             = display.NodeScale;
			_display.ConditionScale        = display.ConditionScale;
			_display.ResultScale           = display.ResultScale;
			_display.TextScale             = display.TextScale;
			_display.DisplacementMagnifier = display.DisplacementMagnifier;

			SetDictionary((TypedValue[]) display, DSaveName);
		}

		#endregion

	}
}