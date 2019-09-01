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
            AuxMethods.RegisterApp();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Ask the user to input the concrete compressive strength
                PromptDoubleOptions fcOp = new PromptDoubleOptions("\nInput the concrete compressive strength (fc) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult fcRes = Global.ed.GetDouble(fcOp);
                double fc = fcRes.Value;

                // Ask the user to input the concrete Elastic Module
                PromptDoubleOptions EcOp = new PromptDoubleOptions("\nInput the concrete Elastic Module (Ec) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult EcRes = Global.ed.GetDouble(EcOp);
                double Ec = EcRes.Value;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(Global.curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                // Save the variables on the Xrecord
                using (ResultBuffer rb = new ResultBuffer())
                {
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));            // 0
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
                trans.Dispose();
            }
        }

        [CommandMethod("SetSteelParameters")]
        public static void SetSteelParameters()
        {
            // Definition for the Extended Data
            string xdataStr = "Steel data";

            // Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
            AuxMethods.RegisterApp();

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Ask the user to input the steel tensile strength
                PromptDoubleOptions fyOp = new PromptDoubleOptions("\nInput the steel tensile strength (fy) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult fyRes = Global.ed.GetDouble(fyOp);
                double fy = fyRes.Value;

                // Ask the user to input the steel Elastic Module
                PromptDoubleOptions EsOp = new PromptDoubleOptions("\nInput the steel Elastic Module (Es) in MPa:")
                {
                    AllowZero = false,
                    AllowNegative = false
                };

                // Get the result
                PromptDoubleResult EsRes = Global.ed.GetDouble(EsOp);
                double Es = EsRes.Value;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(Global.curDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                // Save the variables on the Xrecord
                using (ResultBuffer rb = new ResultBuffer())
                {
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Global.appName));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fy));
                    rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, Es));

                    // Create and add data to an Xrecord
                    Xrecord xRec = new Xrecord();
                    xRec.Data = rb;

                    // Create the entry in the NOD and add to the transaction
                    nod.SetAt("SteelParams", xRec);
                    trans.AddNewlyCreatedDBObject(xRec, true);
                }

                // Save the new object to the database
                trans.Commit();
                trans.Dispose();
            }
        }

        [CommandMethod("ViewMaterialParameters")]
        public void ViewMaterialParameters()
        {
            // Definition for the XData
            string xData = "Material Parameters";
            string concmsg = "";
            string steelmsg = "";

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(Global.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

                // Read the materials Xrecords
                ObjectId concPar = nod.GetAt("ConcreteParams");
                ObjectId steelPar = nod.GetAt("SteelParams");

                if (concPar != null && steelPar != null)
                {
                    // Read the Concrete Xrecord
                    Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
                    ResultBuffer rb = concXrec.Data;
                    TypedValue[] data = rb.AsArray();

                    // Get the parameters
                    concmsg = "\nConcrete Parameters" +
                              "\nfc = " + data[2].Value.ToString() + " MPa" +
                              "\nEc = " + data[3].Value.ToString() + " MPa";

                    // Read the Steel Xrecord
                    Xrecord steelXrec = (Xrecord)trans.GetObject(steelPar, OpenMode.ForRead);
                    ResultBuffer rb2 = steelXrec.Data;
                    TypedValue[] data2 = rb.AsArray();

                    // Get the parameters
                    steelmsg = "\nSteel Parameters" +
                               "\nfy = " + data2[2].Value.ToString() + " MPa" +
                               "\nEs = " + data2[3].Value.ToString() + " MPa";
                }
                else
                {
                    concmsg = "\nMaterial Parameters NOT SET";
                    steelmsg = "";
                }

                // Display the values returned
                Application.ShowAlertDialog(Global.appName + "\n\n" + xData + "\n" + concmsg + "\n" + steelmsg);

                // Dispose the transaction
                trans.Dispose();
            }
        }
    }
}