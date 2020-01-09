using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.Data.Text;

[assembly: CommandClass(typeof(SPMTool.Analysis))]

namespace SPMTool
{
    public partial class Analysis
    {
        // Read the parameters of a stringer
        public static Tuple<int, int[], double, double, double, double> StringerParams(ObjectId stringer)
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Read the object as a line
                Line str = trans.GetObject(stringer, OpenMode.ForRead) as Line;

                // Get the length and angles
                double L = str.Length,
                alpha = str.Angle;                          // angle with x coordinate

                // Read the XData and get the necessary data
                ResultBuffer rb = str.GetXDataForApplication(AutoCAD.appName);
                TypedValue[] data = rb.AsArray();

                // Get the stringer number
                int num = Convert.ToInt32(data[StringerXDataIndex.num].Value);

                // Create the list of grips
                int[] grips =
                {
                    Convert.ToInt32(data[StringerXDataIndex.grip1].Value),
                    Convert.ToInt32(data[StringerXDataIndex.grip2].Value),
                    Convert.ToInt32(data[StringerXDataIndex.grip3].Value)
                };

                double w     = Convert.ToDouble(data[StringerXDataIndex.w].Value),
                       h     = Convert.ToDouble(data[StringerXDataIndex.h].Value),
                       nBars = Convert.ToDouble(data[StringerXDataIndex.nBars].Value),
                       phi   = Convert.ToDouble(data[StringerXDataIndex.phi].Value);

                // Calculate the cross sectional area
                double A = w * h;

                // Calculate the reinforcement area
                double As = Reinforcement.StringerReinforcement(nBars, phi);

                // Calculate the concrete area
                double Ac = A - As;

                // Return the parameters in the order
                // strNum || grips || L || alpha || Ac || As ||
                return Tuple.Create(num, grips, L, alpha, Ac, As);
            }
        }

        // Read the parameters of a panel
        public static Tuple<int, Point3d[], int[], double[], double[], double, double[]> PanelParams(ObjectId panel)
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Read as a solid
                Solid pnl = trans.GetObject(panel, OpenMode.ForWrite) as Solid;

                // Get the vertices
                Point3dCollection pnlVerts = new Point3dCollection();
                pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

                // Get the vertices in the order needed for calculations
                Point3d nd1 = pnlVerts[0],
                        nd2 = pnlVerts[1],
                        nd3 = pnlVerts[3],
                        nd4 = pnlVerts[2];

                // Read the XData and get the necessary data
                ResultBuffer pnlRb = pnl.GetXDataForApplication(AutoCAD.appName);
                TypedValue[] pnlData = pnlRb.AsArray();

                // Get the panel number and width
                int num  = Convert.ToInt32(pnlData[PanelXDataIndex.num].Value);
                double t = Convert.ToDouble(pnlData[PanelXDataIndex.w].Value);

                // Get the reinforcement
                double phiX = Convert.ToDouble(pnlData[PanelXDataIndex.phiX].Value),
                       sx   = Convert.ToDouble(pnlData[PanelXDataIndex.sx].Value),
                       phiY = Convert.ToDouble(pnlData[PanelXDataIndex.phiY].Value),
                       sy   = Convert.ToDouble(pnlData[PanelXDataIndex.sy].Value);

                // Create the list of grips
                int[] grips =
                {
                    Convert.ToInt32(pnlData[PanelXDataIndex.grip1].Value),
                    Convert.ToInt32(pnlData[PanelXDataIndex.grip2].Value),
                    Convert.ToInt32(pnlData[PanelXDataIndex.grip3].Value),
                    Convert.ToInt32(pnlData[PanelXDataIndex.grip4].Value)
                };

                // Create lines to measure the angles between the edges and dimensions
                Line ln1 = new Line(nd1, nd2),
                     ln2 = new Line(nd2, nd3),
                     ln3 = new Line(nd3, nd4),
                     ln4 = new Line(nd4, nd1);

                // Create the list of vertices
                Point3d[] verts =
                {
                    nd1, nd2, nd3, nd4
                };

                // Create the list of dimensions
                double[] dims =
                {
                    ln1.Length,
                    ln2.Length,
                    ln3.Length,
                    ln4.Length,
                };

                // Create the list of angles
                double[] angs =
                {
                    ln1.Angle,
                    ln2.Angle,
                    ln3.Angle,
                    ln4.Angle,
                };

                // Calculate the reinforcement ratio and add to a list
                double[] ps = Reinforcement.PanelReinforcement(phiX, sx, phiY, sy, t);

                // Add to the list of stringer parameters in the index
                // Num || vertices || grips || dimensions || angles || t || ps ||
                return Tuple.Create(num, verts, grips, dims, angs, t, ps);
            }
        }

        // Get the indexes of an element grips in the global matrix
        public static int[] GlobalIndexes(int[] grips)
        {
            // Initialize the array
            int[] ind = new int[grips.Length];

            // Get the indexes
            for (int i = 0; i < grips.Length; i++)
                ind[i] = 2 * (grips[i] - 1);

            return ind;
        }

        // Add the stringer stiffness to the global matrix
        public static void StringerGlobalStiffness(int[] index, Matrix<double> K, Matrix<double> Kg)
        {
            // Get the positions in the global matrix
            int i = index[0],
                j = index[1],
                k = index[2];

            // Initialize an index for lines of the local matrix
            int o = 0;

            // Add the local matrix to the global at the DoFs positions
            // n = index of the node in global matrix
            // o = index of the line in the local matrix
            foreach (int n in index)
            {
                // Line o
                // Check if the row is composed of zeroes
                if (K.Row(o).Exists(Auxiliary.NotZero))
                {
                    Kg[n, i] += K[o, 0]; Kg[n, i + 1] += K[o, 1];
                    Kg[n, j] += K[o, 2]; Kg[n, j + 1] += K[o, 3];
                    Kg[n, k] += K[o, 4]; Kg[n, k + 1] += K[o, 5];
                }

                // Increment the line index
                o++;

                // Line o + 1
                // Check if the row is composed of zeroes
                if (K.Row(o).Exists(Auxiliary.NotZero))
                {
                    Kg[n + 1, i] += K[o, 0]; Kg[n + 1, i + 1] += K[o, 1];
                    Kg[n + 1, j] += K[o, 2]; Kg[n + 1, j + 1] += K[o, 3];
                    Kg[n + 1, k] += K[o, 4]; Kg[n + 1, k + 1] += K[o, 5];
                }

                // Increment the line index
                o++;
            }
        }

        // Add the panel stiffness to the global matrix
        public static void PanelGlobalStiffness(int[] index, Matrix<double> K, Matrix<double> Kg)
        {
            // Get the positions in the global matrix
            int i = index[0],
                j = index[1],
                k = index[2],
                l = index[3];

            // Initialize an index for lines of the local matrix
            int o = 0;

            // Add the local matrix to the global at the DoFs positions
            // i = index of the node in global matrix
            // o = index of the line in the local matrix
            foreach (int n in index)
            {
                // Line o
                // Check if the row is composed of zeroes
                if (K.Row(o).Exists(Auxiliary.NotZero))
                {
                    Kg[n, i] += K[o, 0]; Kg[n, i + 1] += K[o, 1];
                    Kg[n, j] += K[o, 2]; Kg[n, j + 1] += K[o, 3];
                    Kg[n, k] += K[o, 4]; Kg[n, k + 1] += K[o, 5];
                    Kg[n, l] += K[o, 6]; Kg[n, l + 1] += K[o, 7];
                }

                // Increment the line index
                o++;

                // Line o + 1
                // Check if the row is composed of zeroes
                if (K.Row(o).Exists(Auxiliary.NotZero))
                {
                    Kg[n + 1, i] += K[o, 0]; Kg[n + 1, i + 1] += K[o, 1];
                    Kg[n + 1, j] += K[o, 2]; Kg[n + 1, j + 1] += K[o, 3];
                    Kg[n + 1, k] += K[o, 4]; Kg[n + 1, k + 1] += K[o, 5];
                    Kg[n + 1, l] += K[o, 6]; Kg[n + 1, l + 1] += K[o, 7];
                }

                // Increment the line index
                o++;
            }
        }
    }
}