using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SPMTool.Core;
using SPMTool.Material;
using AggregateType = SPMTool.Material.Concrete.AggregateType;
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
				PromptKeywordOptions bhOp = new PromptKeywordOptions("\nChoose general behavior of Stringer-Panel Model");
				bhOp.Keywords.Add(Default);
				bhOp.Keywords.Add(Classic);
				bhOp.Keywords.Add(Custom);
				bhOp.Keywords.Default = Default;
				bhOp.AllowNone = false;

				// Get the result
				PromptResult bhRes = Current.edtr.GetKeywords(bhOp);

				if (bhRes.Status == PromptStatus.Cancel)
					return;

				string behavior = bhRes.StringResult;

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
					PromptKeywordOptions strOp = new PromptKeywordOptions("\nChoose stringer behavior:");
					strOp.Keywords.Add(StrMC2010);
					strOp.Keywords.Add(StrClassic);
					strOp.Keywords.Default = StrMC2010;
					strOp.AllowNone = false;

					// Get the result
					PromptResult strRes = Current.edtr.GetKeywords(strOp);

					if (strRes.Status == PromptStatus.Cancel)
						return;

					// Ask the user choose panel behavior
					PromptKeywordOptions pnlOp = new PromptKeywordOptions("\nChoose panel behavior:");
					pnlOp.Keywords.Add(PnlDSFM);
					pnlOp.Keywords.Add(PnlMCFT);
					pnlOp.Keywords.Default = PnlDSFM;
					pnlOp.AllowNone = false;

					// Get the result
					PromptResult pnlRes = Current.edtr.GetKeywords(pnlOp);

					if (pnlRes.Status == PromptStatus.Cancel)
						return;

					// Get the values
					strBehavior = (StringerBehavior) Enum.Parse(typeof(StringerBehavior), strRes.StringResult);
					pnlBehavior = (PanelBehavior)    Enum.Parse(typeof(PanelBehavior),    pnlRes.StringResult);
				}

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Get the NOD in the database
					DBDictionary nod = (DBDictionary) trans.GetObject(Current.nod, OpenMode.ForWrite);

					// Save the variables on the Xrecord
					using (ResultBuffer rb = new ResultBuffer())
					{
						rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName));     // 0
						rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr));           // 1
						rb.Add(new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) strBehavior));    // 2
						rb.Add(new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) pnlBehavior));    // 3

						// Create and add data to an Xrecord
						Xrecord xRec = new Xrecord();
						xRec.Data = rb;

						// Create the entry in the NOD and add to the transaction
						nod.SetAt(ElementsBehavior, xRec);
						trans.AddNewlyCreatedDBObject(xRec, true);
					}

					// Save the new object to the database
					trans.Commit();
				}

				Current.edtr.WriteMessage("\nStringer behavior: " + strBehavior + "\nPanel behavior: " + pnlBehavior);
			}

			// Read the concrete parameters
			public static (StringerBehavior stringer, PanelBehavior panel) ReadBehavior()
			{
				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Get the NOD in the database
					DBDictionary nod = (DBDictionary)trans.GetObject(Current.nod, OpenMode.ForRead);

					// Check if it exists
					if (nod.Contains(ElementsBehavior))
					{
						// Read the concrete Xrecord
						ObjectId elPar = nod.GetAt(ElementsBehavior);
						Xrecord elXrec = (Xrecord)trans.GetObject(elPar, OpenMode.ForRead);
						ResultBuffer elRb = elXrec.Data;
						TypedValue[] data = elRb.AsArray();

						// Get the parameters from XData
						var strBehavior = (StringerBehavior) Convert.ToInt32(data[2].Value);
						var pnlBehavior = (PanelBehavior)    Convert.ToInt32(data[3].Value);

						return
							(strBehavior, pnlBehavior);
					}

					// If is not set return default values
					return
						(StringerBehavior.NonLinearClassic, PanelBehavior.NonLinearMCFT);
				}
			}
        }
	}
