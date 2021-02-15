using System;
using System.Diagnostics.CodeAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using SPMTool.Core;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet.Units;

namespace SPMTool.Application
{
	/// <summary>
	///     Application settings class.
	/// </summary>
	public class Settings
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

		private AnalysisSettings _analysis = ReadSettingsFromDatabase();
		private Units _units = ReadUnitsFromDatabase();

		#endregion

		#region Properties

		/// <summary>
		///     Get <see cref="AnalysisSettings" /> saved in database.
		/// </summary>
		public AnalysisSettings Analysis
		{
			get => _analysis;
			set => Save(value);
		}

		/// <summary>
		///     Get <see cref="SPMTool.Units" /> saved in database.
		/// </summary>
		public Units Units
		{
			get => _units;
			set => Save(value);
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read units on dictionary.
		/// </summary>
		[return: NotNull]
		public static Units ReadUnitsFromDatabase()
		{
			var data = DataBase.ReadDictionaryEntry(USaveName);

			switch (data)
			{
				case null:
					return Units.Default;

				default:

					// Remove later
					var crckOp = data.Length < 11
						? Units.Default.CrackOpenings
						: (LengthUnit) data[(int) UnitsIndex.CrackOpenings].ToInt();

					// Get the parameters from XData
					return
						new Units
						{
							Geometry              = (LengthUnit) data[(int) UnitsIndex.Geometry].ToInt(),
							Reinforcement         = (LengthUnit) data[(int) UnitsIndex.Reinforcement].ToInt(),
							Displacements         = (LengthUnit) data[(int) UnitsIndex.Displacements].ToInt(),
							AppliedForces         = (ForceUnit) data[(int) UnitsIndex.AppliedForces].ToInt(),
							StringerForces        = (ForceUnit) data[(int) UnitsIndex.StringerForces].ToInt(),
							PanelStresses         = (PressureUnit) data[(int) UnitsIndex.PanelStresses].ToInt(),
							MaterialStrength      = (PressureUnit) data[(int) UnitsIndex.MaterialStrength].ToInt(),
							DisplacementMagnifier = data[(int) UnitsIndex.DisplacementFactor].ToInt(),
							CrackOpenings         = crckOp
						};
			}
		}

		/// <summary>
		///     Read analysis settings on dictionary.
		/// </summary>
		[return: NotNull]
		public static AnalysisSettings ReadSettingsFromDatabase()
		{
			var data = DataBase.ReadDictionaryEntry(ASSaveName);

			return data.IsNullOrEmpty()
				? AnalysisSettings.Default
				: new AnalysisSettings
				{
					Tolerance     = data[(int) AnalysisIndex.Tolerance].ToDouble(),
					NumLoadSteps  = data[(int) AnalysisIndex.NumLoadSteps].ToInt(),
					MaxIterations = data[(int) AnalysisIndex.MaxIterations].ToInt()
				};
		}

		/// <summary>
		///     Save this <paramref name="units" /> in database.
		/// </summary>
		private void Save(Units units)
		{
			_units = units;

			// Get the Xdata size
			var size = Enum.GetNames(typeof(UnitsIndex)).Length;
			var data = new TypedValue[size];

			// Set data
			data[(int) UnitsIndex.AppName]            = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
			data[(int) UnitsIndex.XDataStr]           = new TypedValue((int) DxfCode.ExtendedDataAsciiString, USaveName);
			data[(int) UnitsIndex.Geometry]           = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Geometry);
			data[(int) UnitsIndex.Reinforcement]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Reinforcement);
			data[(int) UnitsIndex.Displacements]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Displacements);
			data[(int) UnitsIndex.AppliedForces]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.AppliedForces);
			data[(int) UnitsIndex.StringerForces]     = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.StringerForces);
			data[(int) UnitsIndex.PanelStresses]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.PanelStresses);
			data[(int) UnitsIndex.MaterialStrength]   = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.MaterialStrength);
			data[(int) UnitsIndex.DisplacementFactor] = new TypedValue((int) DxfCode.ExtendedDataReal,      units.DisplacementMagnifier);
			data[(int) UnitsIndex.CrackOpenings]      = new TypedValue((int) DxfCode.ExtendedDataReal,      (int) units.CrackOpenings);

			// Create the entry in the NOD and add to the transaction
			using var rb = new ResultBuffer(data);
			DataBase.SaveDictionary(rb, USaveName);
		}

		/// <summary>
		///     Save this <paramref name="settings" /> in database.
		/// </summary>
		private void Save(AnalysisSettings settings)
		{
			_analysis = settings;

			// Get the Xdata size
			var size = Enum.GetNames(typeof(AnalysisIndex)).Length;
			var data = new TypedValue[size];

			// Set data
			data[(int) AnalysisIndex.AppName]       = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			data[(int) AnalysisIndex.XDataStr]      = new TypedValue((int) DxfCode.ExtendedDataAsciiString, ASSaveName);
			data[(int) AnalysisIndex.Tolerance]     = new TypedValue((int) DxfCode.ExtendedDataReal, settings.Tolerance);
			data[(int) AnalysisIndex.NumLoadSteps]  = new TypedValue((int) DxfCode.ExtendedDataInteger32, settings.NumLoadSteps);
			data[(int) AnalysisIndex.MaxIterations] = new TypedValue((int) DxfCode.ExtendedDataInteger32, settings.MaxIterations);

			// Create the entry in the NOD and add to the transaction
			using var rb = new ResultBuffer(data);
			DataBase.SaveDictionary(rb, ASSaveName);
		}

		#endregion
	}
}