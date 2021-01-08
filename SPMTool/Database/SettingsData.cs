using System;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using Extensions.AutoCAD;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using UnitsNet.Units;

namespace SPMTool.Database
{
	/// <summary>
    /// Units data class.
    /// </summary>
	public static class SettingsData
	{
		/// <summary>
		/// <see cref="Units"/> save name.
		/// </summary>
		private const string USaveName = "Units";

		/// <summary>
		/// <see cref="AnalysisSettings"/> save name.
		/// </summary>
		private const string ASSaveName = "Analysis Settings";

		/// <summary>
		/// Dimension unit options.
		/// </summary>
		public static readonly string[] DimensionUnits = { LengthUnit.Millimeter.Abbrev(), LengthUnit.Centimeter.Abbrev(), LengthUnit.Meter.Abbrev() };

		/// <summary>
		/// Force unit options.
		/// </summary>
		public static readonly string[] ForceUnits = { ForceUnit.Newton.Abbrev(), ForceUnit.Kilonewton.Abbrev(), ForceUnit.Meganewton.Abbrev() };

		/// <summary>
		/// Stress unit options.
		/// </summary>
		public static readonly string[] StressUnits = { PressureUnit.Pascal.Abbrev(), PressureUnit.Kilopascal.Abbrev(), PressureUnit.Megapascal.Abbrev(), PressureUnit.Gigapascal.Abbrev() };

		/// <summary>
		/// Auxiliary <see cref="Units"/> field.
		/// </summary>
		private static Units _units;

		/// <summary>
		/// Auxiliary <see cref="AnalysisSettings"/> field.
		/// </summary>
		private static AnalysisSettings _settings;

		/// <summary>
		/// Get <see cref="Units"/> saved in database.
		/// </summary>
		public static Units SavedUnits => _units ?? Read(true);

		/// <summary>
		/// Get <see cref="AnalysisSettings"/> saved in database.
		/// </summary>
		public static AnalysisSettings SavedAnalysisSettings => _settings ?? Read();

		/// <summary>
		/// Save this <paramref name="units"/> in database.
		/// </summary>
		public static void Save(Units units)
		{
			_units = units;

			// Get the Xdata size
			int size = Enum.GetNames(typeof(UnitsIndex)).Length;
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
			using (var rb = new ResultBuffer(data))
				DataBase.SaveDictionary(rb, USaveName);
		}

		/// <summary>
		/// Save this <paramref name="settings"/> in database.
		/// </summary>
		public static void Save(AnalysisSettings settings)
		{
			_settings = settings;

			// Get the Xdata size
			int size = Enum.GetNames(typeof(AnalysisIndex)).Length;
			var data = new TypedValue[size];

			// Set data
			data[(int)AnalysisIndex.AppName]       = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			data[(int)AnalysisIndex.XDataStr]      = new TypedValue((int)DxfCode.ExtendedDataAsciiString, ASSaveName);
			data[(int)AnalysisIndex.Tolerance]     = new TypedValue((int)DxfCode.ExtendedDataReal, settings.Tolerance);
			data[(int)AnalysisIndex.NumLoadSteps]  = new TypedValue((int)DxfCode.ExtendedDataInteger32, settings.NumLoadSteps);
			data[(int)AnalysisIndex.MaxIterations] = new TypedValue((int)DxfCode.ExtendedDataInteger32, settings.MaxIterations);

			// Create the entry in the NOD and add to the transaction
			using (var rb = new ResultBuffer(data))
				DataBase.SaveDictionary(rb, ASSaveName);
		}

		/// <summary>
		/// Read units on dictionary.
		/// </summary>
		/// <param name="setUnits">Units must be set by user if it's not set yet?</param>
		public static Units Read(bool setUnits = true) => _units ?? ReadFromDatabase(setUnits);

		/// <summary>
		/// Read units on dictionary.
		/// </summary>
		/// <param name="setUnits">Units must be set by user if it's not set yet?</param>
		public static Units ReadFromDatabase(bool setUnits = true)
        {
	        var data = DataBase.ReadDictionaryEntry(USaveName);

	        switch (data)
	        {
		        case null when setUnits:
			        Settings.SetUnits();
			        data = DataBase.ReadDictionaryEntry(USaveName);
			        break;
		        case null:
			        return Units.Default;
	        }

			// Remove later
	        var crckOp = data.Length < 11
		        ? Units.Default.CrackOpenings
		        : (LengthUnit) data[(int) UnitsIndex.CrackOpenings].ToInt();

			// Get the parameters from XData
			_units = new Units
	        {
		        Geometry              = (LengthUnit)data[(int)UnitsIndex.Geometry].ToInt(),
		        Reinforcement         = (LengthUnit)data[(int)UnitsIndex.Reinforcement].ToInt(),
		        Displacements         = (LengthUnit)data[(int)UnitsIndex.Displacements].ToInt(),
		        AppliedForces         = (ForceUnit)data[(int)UnitsIndex.AppliedForces].ToInt(),
		        StringerForces        = (ForceUnit)data[(int)UnitsIndex.StringerForces].ToInt(),
		        PanelStresses         = (PressureUnit)data[(int)UnitsIndex.PanelStresses].ToInt(),
		        MaterialStrength      = (PressureUnit)data[(int)UnitsIndex.MaterialStrength].ToInt(),
                DisplacementMagnifier = data[(int)UnitsIndex.DisplacementFactor].ToInt(),
				CrackOpenings         = crckOp
            };

	        return _units;
        }

		/// <summary>
		/// Read saved analysis settings.
		/// </summary>
		public static AnalysisSettings Read() => _settings ?? ReadFromDatabase();

		/// <summary>
		/// Read analysis settings on dictionary.
		/// </summary>
		public static AnalysisSettings ReadFromDatabase()
		{
			var data = DataBase.ReadDictionaryEntry(ASSaveName);

			if (data is null)
				return AnalysisSettings.Default;

			// Get the parameters from XData
			_settings = new AnalysisSettings
			{
				Tolerance     = data[(int)AnalysisIndex.Tolerance].ToDouble(),
				NumLoadSteps  = data[(int)AnalysisIndex.NumLoadSteps].ToInt(),
				MaxIterations = data[(int)AnalysisIndex.MaxIterations].ToInt()
			};

			return _settings;
		}
	}
}
