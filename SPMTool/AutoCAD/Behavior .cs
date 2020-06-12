using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using StringerBehavior = SPMTool.Core.Stringer.Behavior;
using PanelBehavior = SPMTool.Core.Panel.Behavior;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Config))]

namespace SPMTool.AutoCAD
{
		// Concrete
		public static partial class Config
		{
			// Behavior names
			private static readonly string
				ElementsBehavior = "ElementsBehavior",
				Default          = "Default",
				Classic          = "Classic",
				Custom           = "Custom",
				StrClassic       = StringerBehavior.NonLinearClassic.ToString(),
				StrMC2010        = StringerBehavior.NonLinearMC2010.ToString(),
				PnlMCFT          = PanelBehavior.NonLinearMCFT.ToString(),
				PnlDSFM          = PanelBehavior.NonLinearDSFM.ToString();

			[CommandMethod("SetElementsBehavior")]
			public static void SetElementsBehavior()
			{
				// Definition for the Extended Data
				string xdataStr = "Elements behavior";

				// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
				Auxiliary.RegisterApp();

				// Initiate elements behavior for default values
				var strBehavior = StringerBehavior.NonLinearMC2010;
				var pnlBehavior = PanelBehavior.NonLinearDSFM;

				// Ask the user choose the general behavior
				var bhOps = new[]
				{
					Default,
					Classic,
					Custom
				};

				var bh = UserInput.SelectKeyword("Choose general behavior of Stringer-Panel Model", bhOps, Default);

				if (!bh.HasValue)
					return;

				string behavior = bh.Value.keyword;

				// Set classic behavior
				if (behavior == Classic)
				{
					strBehavior = StringerBehavior.NonLinearClassic;
					pnlBehavior = PanelBehavior.NonLinearMCFT;
				}

				// Set custom behavior
				else if (behavior == Custom)
				{
					// Ask the user choose stringer behavior
					var strBhOps = new[]
					{
						StrMC2010,
						StrClassic
					};

					var strBh = UserInput.SelectKeyword("Choose stringer behavior:", strBhOps, StrClassic);

					if (!strBh.HasValue)
						return;

					// Ask the user choose panel behavior
					var pnlBhOps = new[]
					{
						PnlDSFM,
						PnlMCFT
					};

					var pnlBh = UserInput.SelectKeyword("Choose panel behavior:", pnlBhOps, PnlMCFT);

					if (!pnlBh.HasValue)
						return;

					// Get the values
					strBehavior = (StringerBehavior) Enum.Parse(typeof(StringerBehavior), strBh.Value.keyword);
					pnlBehavior = (PanelBehavior)    Enum.Parse(typeof(PanelBehavior),    pnlBh.Value.keyword);
				}

				// Save the variables on the Xrecord
				using (ResultBuffer rb = new ResultBuffer())
				{
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Current.appName));     // 0
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));           // 1
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)strBehavior));    // 2
					rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)pnlBehavior));    // 3

					// Create and add data to an Xrecord
					Xrecord xRec = new Xrecord();
					xRec.Data = rb;

					// Create the entry in the NOD
					Auxiliary.SaveObjectDictionary(ElementsBehavior, rb);
				}

				Current.edtr.WriteMessage("\nStringer behavior: " + strBehavior + "\nPanel behavior: " + pnlBehavior);
			}

			// Read the concrete parameters
			public static (StringerBehavior stringer, PanelBehavior panel) ReadBehavior()
			{
				TypedValue[] data = Auxiliary.ReadDictionaryEntry(ElementsBehavior);

				if (data == null)
					return
						(StringerBehavior.NonLinearClassic, PanelBehavior.NonLinearMCFT);

				// Get the parameters from XData
				var strBehavior = (StringerBehavior) Convert.ToInt32(data[2].Value);
				var pnlBehavior = (PanelBehavior)    Convert.ToInt32(data[3].Value);

				return
					(strBehavior, pnlBehavior);
			}
		}
	}
