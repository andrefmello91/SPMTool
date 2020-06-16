using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DimensionUnits = SPMTool.Units.Dimension;
using ForceUnits     = SPMTool.Units.Force;
using StressUnits    = SPMTool.Units.Stress;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Config))]

namespace SPMTool.AutoCAD
{
		// Concrete
		public static partial class Config
		{
			// Unit names
			private static readonly string
				Units = "Units",
				mm    = DimensionUnits.mm.ToString(),
				cm    = DimensionUnits.cm.ToString(),
				m     = DimensionUnits.m.ToString(),
				N     = ForceUnits.N.ToString(),
				kN    = ForceUnits.kN.ToString(),
				MN    = ForceUnits.MN.ToString(),
				Pa    = StressUnits.Pa.ToString(),
				kPa   = StressUnits.kPa.ToString(),
				MPa   = StressUnits.MPa.ToString();

			[CommandMethod("SetUnits")]
			public static void SetUnits()
			{
				// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
				Auxiliary.RegisterApp();

				// Get data set
				var units = ReadUnits();

				// Ask the user dimension unit
				PromptKeywordOptions dimOp = new PromptKeywordOptions("\nChoose dimension unit:");
				dimOp.Keywords.Add(mm);
				dimOp.Keywords.Add(cm);
				dimOp.Keywords.Add(m);
				dimOp.Keywords.Default = units.DimensionUnit.ToString();
				dimOp.AllowNone = false;

				// Get the result
				PromptResult dimRes = Current.edtr.GetKeywords(dimOp);

				if (dimRes.Status == PromptStatus.Cancel)
					return;

				// Ask the user force unit
				PromptKeywordOptions fOp = new PromptKeywordOptions("\nChoose force unit:");
				fOp.Keywords.Add(N);
				fOp.Keywords.Add(kN);
				fOp.Keywords.Add(MN);
				fOp.Keywords.Default = units.ForceUnit.ToString();
				fOp.AllowNone = false;

				// Get the result
				PromptResult fRes = Current.edtr.GetKeywords(fOp);

				if (fRes.Status == PromptStatus.Cancel)
					return;

				// Ask the user stress unit
				PromptKeywordOptions sOp = new PromptKeywordOptions("\nChoose stress unit:");
				sOp.Keywords.Add(Pa);
				sOp.Keywords.Add(kPa);
				sOp.Keywords.Add(MPa);
				sOp.Keywords.Default = units.StressUnit.ToString();
				sOp.AllowNone = false;

				// Get the result
				PromptResult sRes = Current.edtr.GetKeywords(sOp);

				if (sRes.Status == PromptStatus.Cancel)
					return;

				var dim = (DimensionUnits) Enum.Parse(typeof(DimensionUnits), dimRes.StringResult);
				var f   = (ForceUnits)     Enum.Parse(typeof(ForceUnits),     fRes.StringResult);
				var sig = (StressUnits)    Enum.Parse(typeof(StressUnits),    sRes.StringResult);

				// Save the variables on the Xrecord
				using (ResultBuffer rb = new ResultBuffer())
				{
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Current.appName));  // 0
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, Units));           // 1
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)dim));          // 2
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)f));            // 3
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)sig));          // 4

					// Create the entry in the NOD and add to the transaction
					Auxiliary.SaveObjectDictionary(Units, rb);
				}

				Current.edtr.WriteMessage("\nUnits set: " + dim + ", " + f + ", " + sig);
			}

			public static Units ReadUnits()
			{
				TypedValue[] data = Auxiliary.ReadDictionaryEntry(Units);

				if (data is null)
					return
						new Units();

				// Get the parameters from XData
				var dim = (DimensionUnits) Convert.ToInt32(data[2].Value);
				var f   = (ForceUnits)     Convert.ToInt32(data[3].Value);
				var sig = (StressUnits)    Convert.ToInt32(data[4].Value);

				return
					new Units(dim, f, sig);
			}
		}
	}
