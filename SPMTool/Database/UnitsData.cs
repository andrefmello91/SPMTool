using System;
using System.Globalization;
using System.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
using SPMTool.UserInterface;
using UnitsNet.Units;
using UnitsNet;
using StressUnit = UnitsNet.Units.PressureUnit;

[assembly: CommandClass(typeof(SPMTool.Database.Settings.UnitsData))]

namespace SPMTool.Database.Settings
{
	// Concrete
	public static class UnitsData
	{
		// Unit names
		private static readonly string Units = "Units";

		// Unit options
		public static readonly string[]
			DimOpts =
			{
				Length.GetAbbreviation(LengthUnit.Millimeter),
				Length.GetAbbreviation(LengthUnit.Centimeter),
				Length.GetAbbreviation(LengthUnit.Meter)
			},
			FOpts   =
			{
				Force.GetAbbreviation(ForceUnit.Newton),
				Force.GetAbbreviation(ForceUnit.Kilonewton),
				Force.GetAbbreviation(ForceUnit.Meganewton)
			},
			StOpts  =
			{
				Pressure.GetAbbreviation(StressUnit.Pascal),
				Pressure.GetAbbreviation(StressUnit.Kilopascal),
				Pressure.GetAbbreviation(StressUnit.Megapascal),
				Pressure.GetAbbreviation(StressUnit.Gigapascal)
			};

		[CommandMethod("SetUnits")]
		public static void SetUnits()
		{
			// Read data
			var units = ReadUnits(false);

			// Start the window of units configuration
			var unitConfig = new UnitsConfig(units);
			Application.ShowModalWindow(Application.MainWindow.Handle, unitConfig, false);
		}

		/// <summary>
        /// Save this <paramref name="units"/> in database.
        /// </summary>
		public static void Save(Units units)
		{
			// Get the Xdata size
			int size = Enum.GetNames(typeof(UnitsIndex)).Length;
			var data = new TypedValue[size];

			// Set data
			data[(int) UnitsIndex.AppName]          = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  Database.DataBase.AppName);
			data[(int) UnitsIndex.XDataStr]         = new TypedValue((int) DxfCode.ExtendedDataAsciiString, Units);
			data[(int) UnitsIndex.Geometry]         = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Geometry);
			data[(int) UnitsIndex.Reinforcement]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Reinforcement);
			data[(int) UnitsIndex.Displacements]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Displacements);
			data[(int) UnitsIndex.AppliedForces]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.AppliedForces);
			data[(int) UnitsIndex.StringerForces]   = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.StringerForces);
			data[(int) UnitsIndex.PanelStresses]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.PanelStresses);
			data[(int) UnitsIndex.MaterialStrength] = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.MaterialStrength);

			// Create the entry in the NOD and add to the transaction
			using (var rb = new ResultBuffer(data))
				DataBase.SaveDictionary(rb, Units);
		}

        /// <summary>
        /// Read units on database.
        /// </summary>
        /// <param name="setUnits">Units must be set by user?</param>
        public static Units ReadUnits(bool setUnits = true)
		{
			TypedValue[] data = DataBase.ReadDictionaryEntry(Units);

			if (data is null)
			{
				if (setUnits)
					SetUnits();
				else
					return SPMTool.Units.Default;
			}

			// Get the parameters from XData
			return
				new Units
				{
					Geometry          = (LengthUnit) data[(int) UnitsIndex.Geometry].Value,
					Reinforcement     = (LengthUnit) data[(int) UnitsIndex.Reinforcement].Value,
					Displacements     = (LengthUnit) data[(int) UnitsIndex.Displacements].Value,
					AppliedForces     = (ForceUnit)  data[(int) UnitsIndex.AppliedForces].Value,
					StringerForces    = (ForceUnit)  data[(int) UnitsIndex.StringerForces].Value,
					PanelStresses     = (StressUnit) data[(int) UnitsIndex.PanelStresses].Value,
					MaterialStrength  = (StressUnit) data[(int) UnitsIndex.MaterialStrength].Value,
				};
		}
    }
}
