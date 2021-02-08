using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using UnitsNet.Units;
using UnitsNet;

namespace SPMTool.Database
{
	/// <summary>
    /// Analysis data class.
    /// </summary>
	public static class AnalysisData
	{
		/// <summary>
		/// <see cref="AnalysisSettings"/> save name.
		/// </summary>
		private const string ASSaveName = "Analysis Settings";

		/// <summary>
		/// Auxiliary <see cref="AnalysisSettings"/> field.
		/// </summary>
		private static AnalysisSettings _settings;

		/// <summary>
		/// Get <see cref="AnalysisSettings"/> saved in database.
		/// </summary>
		public static AnalysisSettings SavedAnalysisSettings => _settings ?? Read();

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
			data[(int) AnalysisIndex.AppName]       = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
			data[(int) AnalysisIndex.XDataStr]      = new TypedValue((int) DxfCode.ExtendedDataAsciiString, ASSaveName);
			data[(int) AnalysisIndex.Tolerance]     = new TypedValue((int) DxfCode.ExtendedDataReal,        settings.Tolerance);
			data[(int) AnalysisIndex.NumLoadSteps]  = new TypedValue((int) DxfCode.ExtendedDataInteger32,   settings.NumLoadSteps);
			data[(int) AnalysisIndex.MaxIterations] = new TypedValue((int) DxfCode.ExtendedDataInteger32,   settings.MaxIterations);

			// Create the entry in the NOD and add to the transaction
			using (var rb = new ResultBuffer(data))
				DataBase.SaveDictionary(rb, ASSaveName);
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
