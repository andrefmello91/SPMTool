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
            [CommandMethod("SetConcreteParameters")]
            public static void SetConcreteParameters()
            {
                // Definition for the Extended Data
                string xdataStr = "Concrete data";

                // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
                Auxiliary.RegisterApp();

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Ask the user to input the concrete compressive strength
                    PromptDoubleOptions fcOp = new PromptDoubleOptions("\nInput the concrete mean compressive strength (fcm) in MPa:")
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
                        string agrgt = agRes.StringResult;

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

                        // Get the NOD in the database
                        DBDictionary nod = (DBDictionary)trans.GetObject(AutoCAD.curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                        // Save the variables on the Xrecord
                        using (ResultBuffer rb = new ResultBuffer())
                        {
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));     // 0
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));          // 1
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fc));                       // 2
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, aE));                       // 3

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

            // Read the concrete parameters
            public static List<double> ConcreteParams()
            {
                // Initialize a list
                var concParams = new List<double>();

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

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

                        // Get the parameters
                        double fcm = Convert.ToDouble(concData[2].Value),
                            aE = Convert.ToDouble(concData[3].Value);

                        // Calculate the parameters according do FIB MC2010
                        double fctm, Eci, Ec1, ec1, k;

                        // fctm (dependant on fcm value)
                        if (fcm <= 50)
                            fctm = 0.3 * Math.Pow(fcm, 0.66666667);
                        else
                            fctm = 2.12 * Math.Log(1 + 0.1 * fcm);

                        Eci = 21500 * aE * Math.Pow(fcm / 10, 0.33333333);
                        ec1 = -1.6 / 1000 * Math.Pow(fcm / 10, 0.25);
                        Ec1 = fcm / ec1;
                        k = Eci / Ec1;

                        // Add the values to the list
                        concParams.Add(fcm);
                        concParams.Add(fctm);
                        concParams.Add(Eci);
                        concParams.Add(Ec1);
                        concParams.Add(ec1);
                        concParams.Add(k);
                    }
                    else
                    {
                        Application.ShowAlertDialog("Please set concrete parameters.");
                    }
                }
                return concParams;
            }
        }

        // Steel
        public class Steel
        {
            // Maximum plastic strain on steel
            public static double esu = 0.01;

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
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));                 // 1   
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fy));                              // 2
                            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, Es));                              // 3

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

            // Read the steel parameters
            public static List<double> SteelParams()
            {
                // Initialize a list
                var steelParams = new List<double>();

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable blkTbl = trans.GetObject(AutoCAD.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Get the NOD in the database
                    DBDictionary nod = (DBDictionary)trans.GetObject(AutoCAD.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

                    // Check if it exists
                    if (nod.Contains("SteelParams"))
                    {
                        // Read the Steel Xrecord
                        ObjectId steelPar = nod.GetAt("SteelParams");
                        Xrecord steelXrec = (Xrecord)trans.GetObject(steelPar, OpenMode.ForRead);
                        ResultBuffer steelRb = steelXrec.Data;
                        TypedValue[] steelData = steelRb.AsArray();

                        // Get the parameters
                        double fy = Convert.ToDouble(steelData[2].Value),
                            Es = Convert.ToDouble(steelData[3].Value),
                            ey = fy / Es;

                        // Add to the list
                        steelParams.Add(fy);
                        steelParams.Add(Es);
                        steelParams.Add(ey);
                    }
                    else
                    {
                        Application.ShowAlertDialog("Please set steel parameters.");
                    }
                }
                return steelParams;
            }
        }

        [CommandMethod("ViewMaterialParameters")]
        public void ViewMaterialParameters()
        {
            // Definition for the XData
            string xData = "Material Parameters";
            string concmsg = "";
            string steelmsg = "";

            // Get the values
            var concParams = Concrete.ConcreteParams();
            var steelParams = Steel.SteelParams();

            // Write the concrete parameters
            if (concParams != null)
            {
                // Get the parameters
                concmsg = "\nConcrete Parameters" +
                          "\nfcm = " + concParams[0].ToString() + " MPa" +
                          "\nfctm = " + Math.Round(concParams[1],2).ToString() + " MPa" +
                          "\nEci = " + Math.Round(concParams[2],2).ToString() + " MPa" +
                          "\nεc1 = " + Math.Round(1000*concParams[4],2).ToString() + " E-03";
            }
            else
            {
                concmsg = "\nConcrete Parameters NOT SET";
            }

            // Write the steel parameters
            if (steelParams != null)
            {
                // Get the parameters
                steelmsg = "\nSteel Parameters" +
                           "\nfy = " + steelParams[0].ToString() + " MPa" +
                           "\nEs = " + steelParams[1].ToString() + " MPa" +
                           "\nεs = " + Math.Round(1000 * steelParams[2], 2).ToString() + " E-03";

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