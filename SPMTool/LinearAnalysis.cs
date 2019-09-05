﻿using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

[assembly: CommandClass(typeof(SPMTool.LinearAnalysis))]

namespace SPMTool
{
    class LinearAnalysis
    {
        [CommandMethod("StringerStiffness")]
        public void StringerStifness()
        {
            // Initialize the parameters needed
            double Ec = 0; // concrete elastic modulus

            // Start a transaction
            using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable blkTbl = trans.GetObject(Global.curDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Get the NOD in the database
                DBDictionary nod = (DBDictionary)trans.GetObject(Global.curDb.NamedObjectsDictionaryId, OpenMode.ForRead);

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
                    double lngt = str.Length;
                    double alpha = str.Angle;             // angle with x coordinate
                    double beta = Math.PI / 2 - alpha;    // angle with y coordinate
                    double gamma = Math.PI / 2;           // angle with z coordinate

                    // Read the XData and get the necessary data
                    ResultBuffer strRb = str.GetXDataForApplication(Global.appName);
                    TypedValue[] strData = strRb.AsArray();
                    double strNum = Convert.ToDouble(strData[2].Value);
                    double strNd = Convert.ToDouble(strData[3].Value);
                    double endNd = Convert.ToDouble(strData[4].Value);
                    double wd = Convert.ToDouble(strData[6].Value);
                    double h = Convert.ToDouble(strData[7].Value);

                    // Calculate the cross sectional area
                    double A = wd * h;

                    // Get the direction cosines
                    double l, m, n;

                    // If the angle is 90 or 270 degrees, the cosine is zero
                    if (alpha == Math.PI / 2 || alpha == 3 * Math.PI / 2) l = 0;
                    else l = Math.Cos(alpha);
                    if (beta == Math.PI / 2 || beta == 3 * Math.PI / 2) m = 0;
                    else m = Math.Cos(beta);
                    if (gamma == Math.PI / 2 || gamma == 3 * Math.PI / 2) n = 0;
                    else n = Math.Cos(gamma);

                    // Obtain the transformation matrix
                    var T = Matrix<double>.Build.DenseOfArray(new double[,] {
                        {l, m, n, 0, 0, 0, 0 },
                        {0, 0, 0, 1, 0, 0, 0 },
                        {0, 0, 0, 0, l, m, n }
                    });

                    // Calculate the constant factor of stifness
                    double cnt = Ec * A / lngt;

                    // Calculate the local stiffness matrix
                    var kl = cnt * Matrix<double>.Build.DenseOfArray(new double[,] {
                        {  4, -6,  2 },
                        { -6, 12, -6 },
                        {  2, -6,  4 }
                    });

                    // Calculate the transformated stiffness matrix
                    var k = T.Transpose() * kl * T;

                    // Save to the XData
                    strData[9] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, kl.ToString());
                    strData[10] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, k.ToString());

                    // Save the new XData
                    strRb = new ResultBuffer(strData);
                    str.XData = strRb;
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }

            // If all went OK, notify the user
            Global.ed.WriteMessage("Linear stifness matrix of stringers obtained.");
        }

        [CommandMethod("ViewStringerStifness")]
        public void ViewStringerStifness()
        {
            // Initialize the message to display
            string msgstr = "";

            // Request the object to be selected in the drawing area
            PromptEntityOptions entOp = new PromptEntityOptions("\nSelect a stringer to print the stiffness matrix:");
            PromptEntityResult entRes = Global.ed.GetEntity(entOp);

            // If the prompt status is OK, objects were selected
            if (entRes.Status == PromptStatus.OK)
            {
                // Start a transaction
                using (Transaction trans = Global.curDb.TransactionManager.StartTransaction())
                {
                    // Open the selected object for read
                    Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

                    // If it's a stringer
                    if (ent.Layer == "Stringer")
                    {
                        // Get the extended data attached to each object for MY_APP
                        ResultBuffer rb = ent.GetXDataForApplication(Global.appName);

                        // Make sure the Xdata is not empty
                        if (rb != null)
                        {
                            // Get the XData as an array
                            TypedValue[] data = rb.AsArray();

                            // Get the parameters
                            string strNum = data[2].Value.ToString();
                            string kl = data[9].Value.ToString(), k = data[10].Value.ToString();

                            msgstr = "Stringer " + strNum + "\n\n" +
                                     "Local Stifness Matrix: \n" +
                                     kl + "\n\n" +
                                     "Transformated Stifness Matrix: \n" +
                                     k;
                        }
                        else msgstr = "NONE";
                    }
                    else msgstr = "Object is not a stringer.";

                    // Display the values returned
                    Global.ed.WriteMessage("\n" + msgstr);

                    // Dispose the transaction
                    trans.Dispose();
                }
            }
        }
    }
}