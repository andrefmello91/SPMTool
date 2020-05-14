using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SPMTool.Material;
using AggregateType = SPMTool.Material.Concrete.AggregateType;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Material))]

namespace SPMTool.AutoCAD
{
	// Concrete
	public static partial class Material
	{
		private static readonly string ConcreteParams = "ConcreteParams";

		// Aggregate type names
		private static readonly string
			Basalt    = AggregateType.Basalt.ToString(),
			Quartzite = AggregateType.Quartzite.ToString(),
			Limestone = AggregateType.Limestone.ToString(),
			Sandstone = AggregateType.Sandstone.ToString();

		[CommandMethod("SetConcreteParameters")]
		public static void SetConcreteParameters()
		{
			// Definition for the Extended Data
			string xdataStr = "Concrete data";

			// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
			Auxiliary.RegisterApp();

			// Initiate default values
			double
				fc    = 30,
				phiAg = 20;

			string agrgt = Quartzite;

			// Read data
			var concrete = ReadConcreteData();

			if (concrete != null)
			{
				fc    = concrete.Strength;
				phiAg = concrete.AggregateDiameter;
				agrgt = concrete.Type.ToString();
			}

			// Ask the user to input the concrete compressive strength
			var fcOp = new PromptDoubleOptions("\nInput the concrete mean compressive strength (fcm) in MPa:")
			{
				AllowZero     = false,
				AllowNegative = false,
				DefaultValue  = fc
			};

			// Get the result
			var fcRes = Current.edtr.GetDouble(fcOp);

			if (fcRes.Status == PromptStatus.Cancel)
				return;

			fc = fcRes.Value;

			// Ask the user choose the type of the aggregate
			var agOp = new PromptKeywordOptions("\nChoose the type of the aggregate");
			agOp.Keywords.Add(Basalt);
			agOp.Keywords.Add(Quartzite);
			agOp.Keywords.Add(Limestone);
			agOp.Keywords.Add(Sandstone);
			agOp.Keywords.Default = agrgt;
			agOp.AllowNone = false;

			// Get the result
			PromptResult agRes = Current.edtr.GetKeywords(agOp);

			if (agRes.Status == PromptStatus.Cancel)
				return;

			agrgt = agRes.StringResult;

			// Ask the user to input the maximum aggregate diameter
			var phiAgOp = new PromptDoubleOptions("\nInput the maximum diameter for concrete aggregate:")
			{
				AllowZero     = false,
				AllowNegative = false,
				DefaultValue  = phiAg
			};

			// Get the result
			PromptDoubleResult phiAgRes = Current.edtr.GetDouble(phiAgOp);

			if (phiAgRes.Status == PromptStatus.Cancel)
				return;
			phiAg = phiAgRes.Value;

			// Aggregate type
			var aggType = (AggregateType) Enum.Parse(typeof(AggregateType), agrgt);

			// Start a transaction
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var nod = (DBDictionary) trans.GetObject(Current.db.NamedObjectsDictionaryId, OpenMode.ForWrite);

				// Save the variables on the Xrecord
				using (ResultBuffer rb = new ResultBuffer())
				{
					rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName));     // 0
					rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr));           // 1
					rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, fc));                        // 2
					rb.Add(new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) aggType));        // 3
					rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, phiAg));                     // 4

					// Create and add data to an Xrecord
					Xrecord xRec = new Xrecord();
					xRec.Data = rb;

					// Create the entry in the NOD and add to the transaction
					nod.SetAt(ConcreteParams, xRec);
					trans.AddNewlyCreatedDBObject(xRec, true);
				}

				// Save the new object to the database
				trans.Commit();
			}
		}

		[CommandMethod("ViewConcreteParameters")]
		public static void ViewConcreteParameters()
		{
			// Get the values
			var concrete = ReadConcreteData();

			// Write the concrete parameters
			if (concrete.IsSet)
			{
				// Get the parameters
				string concmsg =
					"\nConcrete Parameters:\n"                                    +
					"\nfcm = "    + concrete.Strength                   + " MPa"  +
					"\nfctm = "   + Math.Round(concrete.fcr, 2)         + " MPa"  +
					"\nEci = "    + Math.Round(concrete.Ec, 2)          + " MPa"  +
					"\nεc1 = "    + Math.Round(1000 * concrete.ec, 2)   + " E-03" +
					"\nεc,lim = " + Math.Round(1000 * concrete.ecu, 2)  + " E-03" +
					"\nφ,ag = "   + concrete.AggregateDiameter          + " mm";

				// Display the values returned
				Application.ShowAlertDialog(Current.appName + "\n\n" + concmsg);
			}
		}

		// Read the concrete parameters
		public static Concrete ReadConcreteData()
		{
			// Start a transaction
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var nod = (DBDictionary) trans.GetObject(Current.db.NamedObjectsDictionaryId, OpenMode.ForRead);

				// Check if it exists
				if (nod.Contains(ConcreteParams))
				{
					// Read the concrete Xrecord
					var concPar  = nod.GetAt(ConcreteParams);
					var concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
					var concRb   = concXrec.Data;
					var concData = concRb.AsArray();

					// Get the parameters from XData
					double
						fc      = Convert.ToDouble(concData[2].Value),
						aggType = Convert.ToInt32(concData[3].Value),
						phiAg   = Convert.ToDouble(concData[4].Value);

					return
						new Concrete(fc, phiAg, (AggregateType) aggType);
				}

				//Application.ShowAlertDialog("Please set concrete parameters.");
				// Not set
				return null;
			}
		}
	}
}
