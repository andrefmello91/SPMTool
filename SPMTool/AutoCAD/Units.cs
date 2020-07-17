using System;
using System.Globalization;
using System.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.UserInterface;
using UnitsNet.Units;
using UnitsNet;
using UnitsData  = SPMTool.XData.Units;
using StressUnit = UnitsNet.Units.PressureUnit;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Config))]

namespace SPMTool.AutoCAD
{
	// Concrete
	public static partial class Config
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
			// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
			Auxiliary.RegisterApp();

			// Read data
			var units = ReadUnits() ?? new Units();

			// Start the window of units configuration
			var unitConfig = new UnitsConfig(units);
			Application.ShowModalWindow(Application.MainWindow.Handle, unitConfig, false);
		}

		public static void SaveUnits(Units units)
		{
			// Get the Xdata size
			int size = Enum.GetNames(typeof(UnitsData)).Length;
			var data = new TypedValue[size];

			// Set data
			data[(int) UnitsData.AppName]          = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  Current.appName);
			data[(int) UnitsData.XDataStr]         = new TypedValue((int) DxfCode.ExtendedDataAsciiString, Units);
			data[(int) UnitsData.Geometry]         = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Geometry);
			data[(int) UnitsData.Reinforcement]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Reinforcement);
			data[(int) UnitsData.Displacements]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Displacements);
			data[(int) UnitsData.AppliedForces]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.AppliedForces);
			data[(int) UnitsData.StringerForces]   = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.StringerForces);
			data[(int) UnitsData.PanelStresses]    = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.PanelStresses);
			data[(int) UnitsData.MaterialStrength] = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.MaterialStrength);

			// Create the entry in the NOD and add to the transaction
			Auxiliary.SaveObjectDictionary(Units, new ResultBuffer(data));
		}

		// Read units on database
		public static Units ReadUnits()
		{
			TypedValue[] data = Auxiliary.ReadDictionaryEntry(Units);

			if (data is null)
				return null;

			// Get the parameters from XData
			return
				new Units
				{
					Geometry          = (LengthUnit) data[(int) UnitsData.Geometry].Value,
					Reinforcement     = (LengthUnit) data[(int) UnitsData.Reinforcement].Value,
					Displacements     = (LengthUnit) data[(int) UnitsData.Displacements].Value,
					AppliedForces     = (ForceUnit)  data[(int) UnitsData.AppliedForces].Value,
					StringerForces    = (ForceUnit)  data[(int) UnitsData.StringerForces].Value,
					PanelStresses     = (StressUnit) data[(int) UnitsData.PanelStresses].Value,
					MaterialStrength  = (StressUnit) data[(int) UnitsData.MaterialStrength].Value,
				};
		}
    }
}
