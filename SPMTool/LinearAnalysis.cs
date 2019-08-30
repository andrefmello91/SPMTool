using System;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

[assembly: CommandClass(typeof(SPMTool.LinearAnalysis))]

namespace SPMTool
{
    class LinearAnalysis
    {
        [CommandMethod("StringerStiffness")]
        public void StringerStifness()
        {
            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";

            // Initialize the parameters needed
            double Ec = 0;       // concrete elastic modulus

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

                // Read the concrete Xrecord
                ObjectId concPar = nod.GetAt("ConcreteParams");
                if (concPar != null)
                {
                    // Read the Concrete Xrecord
                    Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
                    ResultBuffer concRb = concXrec.Data;
                    TypedValue[] concData = concRb.AsArray();

                    // Get the elastic modulus
                    Ec = Convert.ToDouble(concData[3].Value);
                }
                else
                {
                    Application.ShowAlertDialog("Please set the material parameters.");
                }

                // Get the stringer collection
                ObjectIdCollection strs = AuxMethods.GetEntitiesOnLayer("Stringer");

                foreach (ObjectId obj in strs)
                {
                    // Read each stringer as a line
                    Line str = trans.GetObject(obj, OpenMode.ForWrite) as Line;

                    // Get the length and angles
                    double l = str.Length;
                    double alpha = str.Angle;             // angle with x coordinate
                    double beta = Math.PI / 2 - alpha;    // angle with y coordinate
                    double gamma = Math.PI / 2;           // angle with z coordinate

                    // Read the XData and get the necessary data
                    ResultBuffer strRb = str.GetXDataForApplication(appName);
                    TypedValue[] strData = strRb.AsArray();
                    double strNum = Convert.ToDouble(strData[2].Value);
                    double strNd = Convert.ToDouble(strData[3].Value);
                    double endNd = Convert.ToDouble(strData[4].Value);
                    double wd = Convert.ToDouble(strData[6].Value);
                    double h = Convert.ToDouble(strData[7].Value);

                    // Calculate the cross sectional area
                    double A = wd * h;

                    // Calculate the constant factor of stifness
                    double cnt = Ec * A / l;

                    // Calculate the local stiffness matrix
                    double k00 = 4 * cnt;
                    double k01 = -6 * cnt;
                    double k02 = 2 * cnt;
                    double k11 = 12 * cnt;
                    double[,] kl =
                    {
                        {k00, k01, k02 },
                        {k01, k11, k01 },
                        {k02, k01, k00 }
                    };

                    // Save to the XData
                    strData[9] = new TypedValue((int)DxfCode.ExtendedDataReal, k00);
                    strData[10] = new TypedValue((int)DxfCode.ExtendedDataReal, k01);
                    strData[11] = new TypedValue((int)DxfCode.ExtendedDataReal, k02);
                    strData[12] = new TypedValue((int)DxfCode.ExtendedDataReal, k11);

                    // Save the new XData
                    strRb = new ResultBuffer(strData);
                    str.XData = strRb;
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }

            // If all went OK, notify the user
            ed.WriteMessage("Linear stifness matrix of stringers obtained.");
        }

        [CommandMethod("ViewStringerStifness")]
        public void ViewStringerStifness()
        {
            // Get the current document, database and editor
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;
            Editor ed = curDoc.Editor;

            // Definition for the Extended Data
            string appName = "SPMTool";
            string msgstr = "";

            // Start a transaction
            using (Transaction trans = curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                PromptSelectionOptions selOps = new PromptSelectionOptions();
                PromptSelectionResult selRes = ed.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    SelectionSet set = selRes.Value;

                    // Step through the objects in the selection set
                    foreach (SelectedObject obj in set)
                    {
                        // Open the selected object for read
                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                        // If it's a stringer
                        if (ent.Layer == "Stringer")
                        {
                            // Get the extended data attached to each object for MY_APP
                            ResultBuffer rb = ent.GetXDataForApplication(appName);

                            // Make sure the Xdata is not empty
                            if (rb != null)
                            {
                                // Get the XData as an array
                                TypedValue[] data = rb.AsArray();

                                // Get the parameters
                                string k00 = data[9].Value.ToString(), k01 = data[10].Value.ToString();
                                string k02 = data[11].Value.ToString(), k11 = data[12].Value.ToString();
                                //double k00 = Convert.ToDouble(data[9].Value), k01 = Convert.ToDouble(data[10].Value);
                                //double k02 = Convert.ToDouble(data[11].Value), k11 = Convert.ToDouble(data[12].Value);

                                msgstr = "Stringer Local Stifness Matrix \n\n" +
                                         "[ " + k00  + " , " + k01  + " , " + k02  + " ]\n" +
                                         "[ " + k01  + " , " + k11  + " , " + k01  + " ]\n" +
                                         "[ " + k02  + " , " + k01  + " , " + k00  + " ]" ;
                            }
                            else
                            {
                                msgstr = "NONE";
                            }
                        }

                        // Display the values returned
                        Application.ShowAlertDialog(appName + "\n\n" + msgstr);
                    }
                }
            }
        }
    }
}
