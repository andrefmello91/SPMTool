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
        public static Tuple<int, List<int>, double, double, double, double> StringerParams(ObjectId stringer)
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
                var grips = new List<int>();
                grips.Add(Convert.ToInt32(data[StringerXDataIndex.grip1].Value));
                grips.Add(Convert.ToInt32(data[StringerXDataIndex.grip2].Value));
                grips.Add(Convert.ToInt32(data[StringerXDataIndex.grip3].Value));

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
        public static Tuple<int, List<Point3d>, List<int>, List<double>, List<double>, double, List<double>> PanelParams(ObjectId panel)
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
                var grips = new List<int>();
                grips.Add(Convert.ToInt32(pnlData[PanelXDataIndex.grip1].Value));
                grips.Add(Convert.ToInt32(pnlData[PanelXDataIndex.grip2].Value));
                grips.Add(Convert.ToInt32(pnlData[PanelXDataIndex.grip3].Value));
                grips.Add(Convert.ToInt32(pnlData[PanelXDataIndex.grip4].Value));

                // Create lines to measure the angles between the edges and dimensions
                Line ln1 = new Line(nd1, nd2),
                     ln2 = new Line(nd2, nd3),
                     ln3 = new Line(nd3, nd4),
                     ln4 = new Line(nd4, nd1);

                // Create the list of vertices
                var verts = new List<Point3d>();
                verts.Add(nd1);
                verts.Add(nd2);
                verts.Add(nd3);
                verts.Add(nd4);

                // Create the list of dimensions
                var dims = new List<double>();
                dims.Add(ln1.Length);
                dims.Add(ln2.Length);
                dims.Add(ln3.Length);
                dims.Add(ln4.Length);

                // Create the list of angles
                var angs = new List<double>();
                angs.Add(ln1.Angle);
                angs.Add(ln2.Angle);
                angs.Add(ln3.Angle);
                angs.Add(ln4.Angle);

                // Calculate the reinforcement ratio and add to a list
                var (psx, psy) = Reinforcement.PanelReinforcement(phiX, sx, phiY, sy, t);
                var ps = new List<double>();
                ps.Add(psx);
                ps.Add(psy);

                // Add to the list of stringer parameters in the index
                // Num || vertices || grips || dimensions || angles || t || ps ||
                return Tuple.Create(num, verts, grips, dims, angs, t, ps);
            }
        }
    }
}