using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;

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
                    double alpha = str.Angle;                          // angle with x coordinate

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
                    double l;                                         // cosine with x

                    // If the angle is 90 or 270 degrees, the cosine is zero
                    if (alpha == Global.piOver2 || alpha == Global.pi3Over2) l = 0;
                    else l = MathNet.Numerics.Trig.Cos(alpha);

                    double m = MathNet.Numerics.Trig.Sin(alpha);      // cosine with y
                    double n = 0;                                     // cosine with z

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

        [CommandMethod("PanelStiffness")]
        public void PanelStifness()
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

                // Get the panel collection
                ObjectIdCollection pnls = AuxMethods.GetEntitiesOnLayer("Panel");

                foreach (ObjectId obj in pnls)
                {
                    // Read each panel as a solid
                    Solid pnl = trans.GetObject(obj, OpenMode.ForWrite) as Solid;

                    // Get the vertices
                    Point3dCollection pnlVerts = new Point3dCollection();
                    pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                    // Read the XData and get the necessary data
                    ResultBuffer pnlRb = pnl.GetXDataForApplication(Global.appName);
                    TypedValue[] pnlData = pnlRb.AsArray();

                    // Get the number of the panel
                    double pnlNum = Convert.ToDouble(pnlData[2].Value);

                    // Get the number of the nodes
                    DoubleCollection pnlNds = new DoubleCollection();
                    for(int i = 3; i <= 6; i++)
                    {
                        pnlNds.Add(Convert.ToDouble(pnlData[i].Value));
                    }

                    // Create lines to measure the angles between the edges
                    Line ln0 = new Line(pnlVerts[0], pnlVerts[1]);
                    Line ln1 = new Line(pnlVerts[1], pnlVerts[3]);
                    Line ln2 = new Line(pnlVerts[3], pnlVerts[2]);
                    Line ln3 = new Line(pnlVerts[2], pnlVerts[0]);

                    // Get the angles
                    double ang1 = ln1.Angle - ln0.Angle;
                    double ang3 = ln3.Angle - ln2.Angle;

                    // If the panel is rectangular (all angles will be equal, and equal to 90 degrees)
                    if (ang1.Equals(Global.piOver2) || ang3.Equals(Global.piOver2))
                    {
                        Global.ed.WriteMessage("\n Painel " + pnlNum.ToString() + " is rectangular!");
                    }

                    else
                    {
                        Global.ed.WriteMessage("\n Painel " + pnlNum.ToString() + " isn't rectangular!");
                    }

                    //Global.ed.WriteMessage("\n Painel " + pnlNum.ToString() + ": \n ang1 = " + ang1.ToString() + "\n ang2 = " + ang2.ToString() + "\n ang3 = " + ang3.ToString());

                    //double strNd = Convert.ToDouble(strData[3].Value);
                    //double endNd = Convert.ToDouble(strData[4].Value);
                    //double wd = Convert.ToDouble(strData[6].Value);
                    //double h = Convert.ToDouble(strData[7].Value);

                    //// Calculate the cross sectional area
                    //double A = wd * h;

                    //// Get the direction cosines
                    //double l;                                         // cosine with x

                    //// If the angle is 90 or 270 degrees, the cosine is zero
                    //if (alpha == MathNet.Numerics.Constants.PiOver2 || alpha == MathNet.Numerics.Constants.Pi3Over2) l = 0;
                    //else l = MathNet.Numerics.Trig.Cos(alpha);

                    //double m = MathNet.Numerics.Trig.Sin(alpha);      // cosine with y
                    //double n = 0;                                     // cosine with z

                    //// Obtain the transformation matrix
                    //var T = Matrix<double>.Build.DenseOfArray(new double[,] {
                    //    {l, m, n, 0, 0, 0, 0 },
                    //    {0, 0, 0, 1, 0, 0, 0 },
                    //    {0, 0, 0, 0, l, m, n }
                    //});

                    //// Calculate the constant factor of stifness
                    //double cnt = Ec * A / lngt;

                    //// Calculate the local stiffness matrix
                    //var kl = cnt * Matrix<double>.Build.DenseOfArray(new double[,] {
                    //    {  4, -6,  2 },
                    //    { -6, 12, -6 },
                    //    {  2, -6,  4 }
                    //});

                    //// Calculate the transformated stiffness matrix
                    //var k = T.Transpose() * kl * T;

                    // Save to the XData
                    //strData[9] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, kl.ToString());
                    //strData[10] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, k.ToString());

                    //// Save the new XData
                    //strRb = new ResultBuffer(strData);
                    //str.XData = strRb;
                }

                // Commit and dispose the transaction
                trans.Commit();
                trans.Dispose();
            }

            // If all went OK, notify the user
            //Global.ed.WriteMessage("Linear stifness matrix of stringers obtained.");
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
