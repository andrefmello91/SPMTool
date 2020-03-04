using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(SPMTool.Material))]
[assembly: CommandClass(typeof(SPMTool.Material.Concrete))]
[assembly: CommandClass(typeof(SPMTool.Material.Steel))]

namespace SPMTool
{
    // Material related commands:
    public class Material
    {
        // Concrete
        public class Concrete
        {
            // Properties
			public double AggregateDiameter { get; set; }
            public double fcm               { get; set; }
            public double fctm              { get; set; }
            public double Eci               { get; set; }
            public double ec1               { get; set; }
            public double Ec1 => fcm / ec1;
            public double k   => Eci / Ec1;
            public double ecr => fctm / Eci;

            // Read the concrete parameters
            public Concrete()
            {
				ReadConcreteData();
            }

            [CommandMethod("SetConcreteParameters")]
            public static void SetConcreteParameters()
            {
	            // Definition for the Extended Data
	            string xdataStr = "Concrete data";

	            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
	            Auxiliary.RegisterApp();

	            // Ask the user to input the concrete compressive strength
	            PromptDoubleOptions fcOp =
		            new PromptDoubleOptions("\nInput the concrete mean compressive strength (fcm) in MPa:")
		            {
			            AllowZero = false,
			            AllowNegative = false
		            };

	            // Get the result
	            PromptDoubleResult fcRes = AutoCAD.edtr.GetDouble(fcOp);
	            if (fcRes.Status == PromptStatus.OK)
	            {
		            double fc = fcRes.Value;

		            // Ask the user choose the type of the agregate
		            PromptKeywordOptions agOp = new PromptKeywordOptions("\nChoose the type of the aggregate");
		            agOp.Keywords.Add("Basalt");
		            agOp.Keywords.Add("Quartzite");
		            agOp.Keywords.Add("Limestone");
		            agOp.Keywords.Add("Sandstone");
		            agOp.Keywords.Default = "Quartzite";
		            agOp.AllowNone = false;

		            // Get the result
		            PromptResult agRes = AutoCAD.edtr.GetKeywords(agOp);

		            if (agRes.Status == PromptStatus.OK)
		            {
			            string agrgt = agRes.StringResult;

			            // Ask the user to input the maximum aggregate diameter
			            PromptDoubleOptions phiAgOp =
				            new PromptDoubleOptions("\nInput the maximum diameter for concrete aggregate:")
				            {
					            AllowZero = false,
					            AllowNegative = false
				            };

			            // Get the result
			            PromptDoubleResult phiAgRes = AutoCAD.edtr.GetDouble(phiAgOp);

			            if (phiAgRes.Status == PromptStatus.OK)
			            {
				            double phiAg = phiAgRes.Value;

				            // Get the value of aE
				            double aE = 1;
				            switch (agrgt)
				            {
					            case "Basalt":
						            aE = 1.2;
						            break;

					            case "Quartzite":
						            // aE = 1 already
						            break;

					            case "Limestone":
						            aE = 0.9;
						            break;

					            case "Sandstone":
						            aE = 0.9;
						            break;
				            }

				            // Start a transaction
				            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
				            {

					            // Get the NOD in the database
					            DBDictionary nod =
						            (DBDictionary) trans.GetObject(AutoCAD.curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

					            // Save the variables on the Xrecord
					            using (ResultBuffer rb = new ResultBuffer())
					            {
						            rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName, AutoCAD.appName));     // 0
						            rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr));           // 1
						            rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, fc));                        // 2
						            rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, aE));                        // 3
						            rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, phiAg));                     // 4

						            // Create and add data to an Xrecord
						            Xrecord xRec = new Xrecord();
						            xRec.Data = rb;

						            // Create the entry in the NOD and add to the transaction
						            nod.SetAt("ConcreteParams", xRec);
						            trans.AddNewlyCreatedDBObject(xRec, true);
					            }

					            // Save the new object to the database
					            trans.Commit();
				            }
			            }
		            }
	            }
            }

			// Read concrete parameters
			private void ReadConcreteData()
			{
				// Start a transaction
				using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
				{
					// Get the NOD in the database
					DBDictionary nod = (DBDictionary)trans.GetObject(AutoCAD.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

					// Check if it exists
					if (nod.Contains("ConcreteParams"))
					{
						// Read the concrete Xrecord
						ObjectId concPar = nod.GetAt("ConcreteParams");
						Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
						ResultBuffer concRb = concXrec.Data;
						TypedValue[] concData = concRb.AsArray();

						// Get the parameters from XData
						fcm = Convert.ToDouble(concData[2].Value);
						AggregateDiameter = Convert.ToDouble(concData[4].Value);

						// Calculate the parameters according do FIB MC2010
						// fctm (dependant on fcm value)
						if (fcm <= 50)
							fctm = 0.3 * Math.Pow(fcm, 0.66666667);
						else
							fctm = 2.12 * Math.Log(1 + 0.1 * fcm);

						// Set to Concrete
						double aE = Convert.ToDouble(concData[3].Value);
						Eci = 21500 * aE * Math.Pow(fcm / 10, 0.33333333);
						ec1 = -1.6 / 1000 * Math.Pow(fcm / 10, 0.25);
					}
					else
					{
						Application.ShowAlertDialog("Please set concrete parameters.");
					}
				}
			}

            // Calculate concrete parameters for MCFT
            public void MCFTConcrete(Concrete concrete)
            {
	            double fc = concrete.fcm;

                // Calculate the parameters
                double ec = 0.002,
                       Ec = 2 * fc / ec,
                       ft = 0.33 * Math.Sqrt(fc),
                       ecr = ft / Ec;

                concrete.ec1 = ec;
                concrete.Eci = Ec;
                concrete.fctm = ft;
            }
        }

        // Steel
        public class Steel
        {
            // Steel properties
            public double fy { get; set; }
            public double Es { get; set; }
            public double ey => fy / Es;

            // Maximum plastic strain on steel
            public double esu => 0.01;

            // Read the steel parameters
            public Steel()
            {
				ReadSteelData();
            }

            [CommandMethod("SetSteelParameters")]
            public static void SetSteelParameters()
            {
                // Definition for the Extended Data
                string xdataStr = "Steel data";

                // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
                Auxiliary.RegisterApp();

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Ask the user to input the steel tensile strength
                    PromptDoubleOptions fyOp = new PromptDoubleOptions("\nInput the steel tensile strength (fy) in MPa:")
                    {
                        AllowZero = false,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult fyRes = AutoCAD.edtr.GetDouble(fyOp);
                    if (fyRes.Status == PromptStatus.OK)
                    {
                        double fy = fyRes.Value;

                        // Ask the user to input the steel Elastic Module
                        PromptDoubleOptions EsOp = new PromptDoubleOptions("\nInput the steel Elastic Module (Es) in MPa:")
                        {
                            AllowZero = false,
                            AllowNegative = false
                        };

                        // Get the result
                        PromptDoubleResult EsRes = AutoCAD.edtr.GetDouble(EsOp);
                        double Es = EsRes.Value;

                        // Get the NOD in the database
                        DBDictionary nod = (DBDictionary)trans.GetObject(AutoCAD.curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                        // Save the variables on the Xrecord
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));            // 0
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));                  // 1   
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fy));                               // 2
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, Es));                               // 3
							 
                            // Create and add data to an Xrecord
                            Xrecord xRec = new Xrecord();
                            xRec.Data = rb;

                            // Create the entry in the NOD and add to the transaction
                            nod.SetAt("SteelParams", xRec);
                            trans.AddNewlyCreatedDBObject(xRec, true);
                        }

                        // Save the new object to the database
                        trans.Commit();
                    }
                }
            }

			// Read steel parameters
			private void ReadSteelData()
            {                
	            // Start a transaction
	            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
	            {
		            // Open the Block table for read
		            BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

		            // Get the NOD in the database
		            DBDictionary nod =
			            (DBDictionary)trans.GetObject(AutoCAD.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

		            // Check if it exists
		            if (nod.Contains("SteelParams"))
		            {
			            // Read the Steel Xrecord
			            ObjectId steelPar = nod.GetAt("SteelParams");
			            Xrecord steelXrec = (Xrecord)trans.GetObject(steelPar, OpenMode.ForRead);
			            ResultBuffer steelRb = steelXrec.Data;
			            TypedValue[] steelData = steelRb.AsArray();

			            // Get the parameters
			            fy = Convert.ToDouble(steelData[2].Value);
			            Es = Convert.ToDouble(steelData[3].Value);

			            // Maximum plastic strain on steel
		            }
		            else
		            {
			            Application.ShowAlertDialog("Please set steel parameters.");
		            }
	            }
            }
        }

        [CommandMethod("ViewMaterialParameters")]
        public void ViewMaterialParameters()
        {
            // Definition for the XData
            string xData = "Material Parameters";
            string concmsg;
            string steelmsg;

            // Get the values
            var concrete = new Concrete();
            var steel = new Steel();

            // Write the concrete parameters
            if (concrete != null)
            {
                // Get the parameters
                concmsg = "\nConcrete Parameters" +
                          "\nfcm = " + concrete.fcm + " MPa" +
                          "\nfctm = " + Math.Round(concrete.fctm, 2) + " MPa" +
                          "\nEci = " + Math.Round(concrete.Eci, 2) + " MPa" +
                          "\nεc1 = " + Math.Round(1000 * concrete.ec1,2) + " E-03";
            }
            else
            {
                concmsg = "\nConcrete Parameters NOT SET";
            }

            // Write the steel parameters
            if (steel != null)
            {
                // Get the parameters
                steelmsg = "\nSteel Parameters" +
                           "\nfy = " + steel.fy + " MPa" +
                           "\nEs = " + steel.Es + " MPa" +
                           "\nεy = " + Math.Round(1000 * steel.ey, 2) + " E-03";

            }
            else
            {
                steelmsg = "\nSteel Parameters NOT SET";
            }

            // Display the values returned
            Application.ShowAlertDialog(AutoCAD.appName + "\n\n" + xData + "\n" + concmsg + "\n" + steelmsg);
        }
    }
}