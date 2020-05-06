using System;

namespace SPMTool
{
	// Material related commands:
	namespace Material
	{
		// Steel
		public class Steel
		{
			// Steel properties
			public double YieldStress { get; }
			public double ElasticModule { get; }
			public double Strain { get; set; }
			public double Stress { get; set; }
			public double YieldStrain
			{
				get
				{
					if (IsSet)
						return
							YieldStress / ElasticModule;
					//else
					return
						0;
				}
			}

			// Read the steel parameters
			public Steel(double yieldStress, double elasticModule = 210000)
			{
				YieldStress = yieldStress;
				ElasticModule = elasticModule;
			}

			// Maximum plastic strain on steel
			public double esu = 0.01;

			// Verify if steel is set
			public bool IsSet
			{
				get
				{
					if (YieldStress == 0 || ElasticModule == 0)
						return
							false;
					//else
					return
						true;
				}
			}

			// Set steel strain
			public void SetStrain(double strain)
			{
				Strain = strain;
			}

			// Set steel stress given strain

			// Calculate stress in reinforcement given strain
			public void SetStress(double strain)
			{
				// Compression yielding
				if (strain <= -YieldStrain)
					Stress = -YieldStress;

				// Elastic
				if (strain < YieldStrain)
					Stress = ElasticModule * strain;

				// Tension yielding
				else
					Stress = YieldStress;
			}

			// Calculate secant module of steel
			public double SecantModule
			{
				get
				{
					// Verify the strain
					if (Strain == 0)
						return ElasticModule;

					return
						Math.Min(Stress / Strain, ElasticModule);
				}
			}

   //         [CommandMethod("SetSteelParameters")]
			//public static void SetSteelParameters()
			//{
			//	// Definition for the Extended Data
			//	string xdataStr = "Steel data";

			//	// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
			//	Auxiliary.RegisterApp();

			//	// Start a transaction
			//	using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
			//	{
			//		// Ask the user to input the steel tensile strength
			//		PromptDoubleOptions fyOp =
			//			new PromptDoubleOptions("\nInput the steel tensile strength (fy) in MPa:")
			//			{
			//				AllowZero = false,
			//				AllowNegative = false
			//			};

			//		// Get the result
			//		PromptDoubleResult fyRes = ACAD.Current.edtr.GetDouble(fyOp);
			//		if (fyRes.Status == PromptStatus.OK)
			//		{
			//			double fy = fyRes.Value;

			//			// Ask the user to input the steel Elastic Module
			//			PromptDoubleOptions EsOp =
			//				new PromptDoubleOptions("\nInput the steel Elastic Module (Es) in MPa:")
			//				{
			//					AllowZero = false,
			//					AllowNegative = false
			//				};

			//			// Get the result
			//			PromptDoubleResult EsRes = ACAD.Current.edtr.GetDouble(EsOp);
			//			double Es = EsRes.Value;

			//			// Get the NOD in the database
			//			DBDictionary nod = (DBDictionary) trans.GetObject(ACAD.Current.db.NamedObjectsDictionaryId,
			//				OpenMode.ForWrite);

			//			// Save the variables on the Xrecord
			//			using (ResultBuffer rb = new ResultBuffer())
			//			{
			//				rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName,
			//					ACAD.Current.appName));            // 0
			//				rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString,
			//					xdataStr));                  // 1   
			//				rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,
			//					fy));                               // 2
			//				rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,
			//					Es));                               // 3

			//				// Create and add data to an Xrecord
			//				Xrecord xRec = new Xrecord();
			//				xRec.Data = rb;

			//				// Create the entry in the NOD and add to the transaction
			//				nod.SetAt("SteelParams", xRec);
			//				trans.AddNewlyCreatedDBObject(xRec, true);
			//			}

			//			// Save the new object to the database
			//			trans.Commit();
			//		}
			//	}
			//}

			//// Read steel parameters
			//private void ReadSteelData()
			//{
			//	// Start a transaction
			//	using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
			//	{
			//		// Open the Block table for read
			//		BlockTable blkTbl = trans.GetObject(ACAD.Current.db.BlockTableId, OpenMode.ForRead) as BlockTable;

			//		// Get the NOD in the database
			//		DBDictionary nod =
			//			(DBDictionary) trans.GetObject(ACAD.Current.db.NamedObjectsDictionaryId, OpenMode.ForRead);

			//		// Check if it exists
			//		if (nod.Contains("SteelParams"))
			//		{
			//			// Read the Steel Xrecord
			//			ObjectId steelPar = nod.GetAt("SteelParams");
			//			Xrecord steelXrec = (Xrecord) trans.GetObject(steelPar, OpenMode.ForRead);
			//			ResultBuffer steelRb = steelXrec.Data;
			//			TypedValue[] steelData = steelRb.AsArray();

			//			// Get the parameters
			//			fy = Convert.ToDouble(steelData[2].Value);
			//			Es = Convert.ToDouble(steelData[3].Value);

			//			// Maximum plastic strain on steel
			//		}
			//		else
			//		{
			//			Application.ShowAlertDialog("Please set steel parameters.");
			//		}
			//	}
			//}
		}
	}
}