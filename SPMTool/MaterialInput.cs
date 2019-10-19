using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(SPMTool.Material))]

namespace SPMTool
{
    // Material related commands:
    public class Material
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
                PromptDoubleOptions fcOp = new PromptDoubleOptions("\nInput the concrete compressive strength (fc) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult fcRes = AutoCAD.edtr.GetDouble(fcOp);
                if (fcRes.Status == PromptStatus.OK)
                {
                    double fc = fcRes.Value;

                    // Ask the user to input the concrete Elastic Module
                    PromptDoubleOptions EcOp = new PromptDoubleOptions("\nInput the concrete Elastic Module (Ec) in MPa:")
                    {
                        AllowZero = false,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult EcRes = AutoCAD.edtr.GetDouble(EcOp);
                    double Ec = EcRes.Value;

                    // Get the NOD in the database
                    DBDictionary nod = (DBDictionary)trans.GetObject(AutoCAD.curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                    // Save the variables on the Xrecord
                    using (ResultBuffer rb = new ResultBuffer())
                    {
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AutoCAD.appName));     // 0
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));          // 1
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fc));                       // 2
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, Ec));                       // 3

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

        [CommandMethod("ViewMaterialParameters")]
        public void ViewMaterialParameters()
        {
            // Definition for the XData
            string xData = "Material Parameters";
            string concmsg = "";
            string steelmsg = "";

            // Get the values
            var (fc, Ec) = ConcreteParams();
            var (fy, Ey) = SteelParams();

            // Write the concrete parameters
            if (fc > 0 && Ec > 0)
            {
                // Get the parameters
                concmsg = "\nConcrete Parameters" +
                          "\nfc = " + fc.ToString() + " MPa" +
                          "\nEc = " + Ec.ToString() + " MPa";
            }
            else
            {
                concmsg = "\nConcrete Parameters NOT SET";
            }

            // Write the steel parameters
            if (fy > 0 && Ey > 0)
            {
                // Get the parameters
                steelmsg = "\nSteel Parameters" +
                           "\nfy = " + fy.ToString() + " MPa" +
                           "\nEs = " + Ey.ToString() + " MPa";
            }
            else
            {
                steelmsg = "\nSteel Parameters NOT SET";
            }
            //// Start a transaction
            //using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            //{
            //    // Get the NOD in the database
            //    DBDictionary nod = (DBDictionary)trans.GetObject(Global.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

            //    // Read the materials Xrecords
            //    ObjectId concPar = nod.GetAt("ConcreteParams");
            //    ObjectId steelPar = nod.GetAt("SteelParams");

            //    if (concPar != null && steelPar != null)
            //    {
            //        // Read the Concrete Xrecord
            //        Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
            //        ResultBuffer rb = concXrec.Data;
            //        TypedValue[] data = rb.AsArray();

            //        // Get the parameters
            //        concmsg = "\nConcrete Parameters" +
            //                  "\nfc = " + data[2].Value.ToString() + " MPa" +
            //                  "\nEc = " + data[3].Value.ToString() + " MPa";

            //        // Read the Steel Xrecord
            //        Xrecord steelXrec = (Xrecord)trans.GetObject(steelPar, OpenMode.ForRead);
            //        ResultBuffer rb2 = steelXrec.Data;
            //        TypedValue[] data2 = rb.AsArray();

            //        // Get the parameters
            //        steelmsg = "\nSteel Parameters" +
            //                   "\nfy = " + data2[2].Value.ToString() + " MPa" +
            //                   "\nEs = " + data2[3].Value.ToString() + " MPa";
            //    }
            //    else
            //    {
            //        concmsg = "\nMaterial Parameters NOT SET";
            //        steelmsg = "";
            //    }

            // Display the values returned
            Application.ShowAlertDialog(AutoCAD.appName + "\n\n" + xData + "\n" + concmsg + "\n" + steelmsg);
        }

        // Read the concrete parameters
        public static (double fc, double Ec) ConcreteParams()
        {
            // Initialize the parameters needed
            double fc = 0, Ec = 0;

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
                    fc = Convert.ToDouble(concData[2].Value);
                    Ec = Convert.ToDouble(concData[3].Value);
                }
                else
                {
                    Application.ShowAlertDialog("Please set concrete parameters.");
                }
            }
            return (fc, Ec);
        }

        // Read the steel parameters
        public static (double fy, double Ey) SteelParams()
        {
            // Initialize the parameters needed
            double fy = 0, Ey = 0;

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
                    fy = Convert.ToDouble(steelData[2].Value);
                    Ey = Convert.ToDouble(steelData[3].Value);
                }
                else
                {
                    Application.ShowAlertDialog("Please set steel parameters.");
                }
            }
            return (fy, Ey);
        }
    }
}