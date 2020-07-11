using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using UnitsData = SPMTool.XData.Units;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Config))]

namespace SPMTool.AutoCAD
{
		// Concrete
		public static partial class Config
		{
			// Unit names
			private static readonly string Units = "Units";
			
			[CommandMethod("SetUnits")]
			public static void SetUnits()
			{
				// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
				Auxiliary.RegisterApp();

				// Read data
				var units = ReadUnits() ?? new Units();

				// Get options
				string[]
					dimOpts = Enum.GetNames(typeof(DimensionUnit)),
					fOpts   = Enum.GetNames(typeof(ForceUnit)),
					stOpts  = Enum.GetNames(typeof(StressUnit));

				// Get defaults
				string[]
					dimDefs =
					{
						units.Geometry.ToString(),
						units.Reinforcement.ToString(),
						units.Displacements.ToString()
					},
					fDefs =
					{
						units.AppliedForces.ToString(),
						units.StringerForces.ToString()
					},
					stDefs =
					{
						units.PanelStresses.ToString(),
						units.MaterialStrength.ToString()
					};

				// Ask the user to choose dimension units
				string[] dimTypes =
				{
					"geometry",
					"reinforcement",
					"displacements"
				};
				
				DimensionUnit[] dimUnits = new DimensionUnit[3];

				for (int i = 0; i < dimTypes.Length; i++)
				{
					var dimOps = UserInput.SelectKeyword("Choose " + dimTypes[i] + " dimensions unit:", dimOpts, dimDefs[i]);

					if (!dimOps.HasValue)
						return;

					// Save value
					dimUnits[i] = (DimensionUnit) Enum.Parse(typeof(DimensionUnit), dimOps.Value.keyword);
				}

				// Ask the user to choose force units
				string[] fTypes =
				{
					"applied forces",
					"stringer forces"
				};
				
				ForceUnit[] fUnits = new ForceUnit[2];

				for (int i = 0; i < fTypes.Length; i++)
				{
					var fOps = UserInput.SelectKeyword("Choose " + fTypes[i] + " unit:", fOpts, fDefs[i]);

					if (!fOps.HasValue)
						return;

					// Save value
					fUnits[i] = (ForceUnit) Enum.Parse(typeof(ForceUnit), fOps.Value.keyword);
				}

				// Ask the user to choose stress units
				string[] stTypes =
				{
					"panel stresses",
					"material strength/module"
				};

				StressUnit[] stUnits = new StressUnit[2];

				for (int i = 0; i < stTypes.Length; i++)
				{
					var stOps = UserInput.SelectKeyword("Choose " + stTypes[i] + " unit:", stOpts, stDefs[i]);

					if (!stOps.HasValue)
						return;

					// Save value
					stUnits[i] = (StressUnit)Enum.Parse(typeof(StressUnit), stOps.Value.keyword);
				}

				// Get the Xdata size
				int size = Enum.GetNames(typeof(UnitsData)).Length;
				var data = new TypedValue[size];

				// Set data
				data[(int)UnitsData.AppName]           = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  Current.appName);
				data[(int)UnitsData.XDataStr]          = new TypedValue((int)DxfCode.ExtendedDataAsciiString, Units);
				data[(int)UnitsData.Geometry]          = new TypedValue((int)DxfCode.ExtendedDataInteger32,   dimUnits[0]);
				data[(int)UnitsData.Reinforcement]     = new TypedValue((int)DxfCode.ExtendedDataInteger32,   dimUnits[1]);
				data[(int)UnitsData.Displacements]     = new TypedValue((int)DxfCode.ExtendedDataInteger32,   dimUnits[2]);
				data[(int)UnitsData.AppliedForces]     = new TypedValue((int)DxfCode.ExtendedDataInteger32,   fUnits[0]);
				data[(int)UnitsData.StringerForces]    = new TypedValue((int)DxfCode.ExtendedDataInteger32,   fUnits[1]);
				data[(int)UnitsData.PanelStresses]     = new TypedValue((int)DxfCode.ExtendedDataInteger32,   stUnits[0]);
				data[(int)UnitsData.MaterialStrength]  = new TypedValue((int)DxfCode.ExtendedDataInteger32,   stUnits[1]);

				// Create the entry in the NOD and add to the transaction
				Auxiliary.SaveObjectDictionary(Units, new ResultBuffer(data));
			}

			public static Units ReadUnits()
			{
				TypedValue[] data = Auxiliary.ReadDictionaryEntry(Units);

				if (data is null)
					return null;

				// Get the parameters from XData
				return
					new Units
					{
						Geometry          = (DimensionUnit) data[(int)UnitsData.Geometry].Value,
						Reinforcement     = (DimensionUnit) data[(int)UnitsData.Reinforcement].Value,
						Displacements     = (DimensionUnit) data[(int)UnitsData.Displacements].Value,
						AppliedForces     = (ForceUnit)     data[(int)UnitsData.AppliedForces].Value,
						StringerForces    = (ForceUnit)     data[(int)UnitsData.StringerForces].Value,
						PanelStresses     = (StressUnit)    data[(int)UnitsData.PanelStresses].Value,
						MaterialStrength  = (StressUnit)    data[(int)UnitsData.MaterialStrength].Value,
					};
			}
		}
	}
