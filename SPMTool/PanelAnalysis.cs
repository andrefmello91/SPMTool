using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using MathNet.Numerics.Data.Text;

[assembly: CommandClass(typeof(SPMTool.Analysis.Panel))]

namespace SPMTool
{
    public partial class Analysis
    {
	    public partial class Panel:SPMTool.Panel
	    {
            public Panel(ObjectId panelObject) : base(panelObject)
            {
            }

            // Read the parameters of a collection of panel objects
            public static Panel[] Parameters(ObjectIdCollection panelObjects)
            {
                Panel[] panels = new Panel[panelObjects.Count];

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
	                foreach (ObjectId pnlObj in panelObjects)
	                {
		                // Read as a solid
		                Solid pnl = trans.GetObject(pnlObj, OpenMode.ForWrite) as Solid;

		                // Get the vertices
		                Point3dCollection pnlVerts = new Point3dCollection();
		                pnl.GetGripPoints(pnlVerts, new IntegerCollection(), new IntegerCollection());

		                // Get the vertices in the order needed for calculations
		                Point3d
			                nd1 = pnlVerts[0],
			                nd2 = pnlVerts[1],
			                nd3 = pnlVerts[3],
			                nd4 = pnlVerts[2];

		                // Read the XData and get the necessary data
		                ResultBuffer pnlRb = pnl.GetXDataForApplication(AutoCAD.appName);
		                TypedValue[] pnlData = pnlRb.AsArray();

		                // Get the panel parameters
		                int num = Convert.ToInt32(pnlData[(int) XData.Panel.Number].Value);
		                double
			                w = Convert.ToDouble(pnlData[(int) XData.Panel.Width].Value),
			                phiX = Convert.ToDouble(pnlData[(int) XData.Panel.XDiam].Value),
			                phiY = Convert.ToDouble(pnlData[(int) XData.Panel.YDiam].Value),
			                sx = Convert.ToDouble(pnlData[(int) XData.Panel.Sx].Value),
			                sy = Convert.ToDouble(pnlData[(int) XData.Panel.Sy].Value);

		                // Create the list of grips
		                int[] grips =
		                {
			                Convert.ToInt32(pnlData[(int) XData.Panel.Grip1].Value),
			                Convert.ToInt32(pnlData[(int) XData.Panel.Grip2].Value),
			                Convert.ToInt32(pnlData[(int) XData.Panel.Grip3].Value),
			                Convert.ToInt32(pnlData[(int) XData.Panel.Grip4].Value)
		                };

		                // Create the list of vertices
		                Point3d[] verts =
		                {
			                nd1, nd2, nd3, nd4
		                };

		                // Create the panel
		                Panel panel = new Panel(pnlObj)
		                {
			                Number      = num,
			                Grips       = grips,
			                Vertices    = verts,
			                Width       = w,
			                BarDiameter = (phiX, phiY),
			                BarSpacing  = (sx, sy)
		                };

		                // Get the index
		                int i = panel.Number - 1;

		                // Set to the array
		                panels[i] = panel;
	                }
                }

                return panels;
            }

            // Add the panel stiffness to the global matrix
            public static void GlobalStiffness(int[] index, Matrix<double> K, Matrix<double> Kg)
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
                foreach (int ind in index)
                {
                    for (int n = ind; n <= ind + 1; n++)
                    {
                        // Line o
                        // Check if the row is composed of zeroes
                        if (K.Row(o).Exists(Auxiliary.NotZero))
                        {
                            Kg[n, i] += K[o, 0];         Kg[n, i + 1] += K[o, 1];
                            Kg[n, j] += K[o, 2];         Kg[n, j + 1] += K[o, 3];
                            Kg[n, k] += K[o, 4];         Kg[n, k + 1] += K[o, 5];
                            Kg[n, l] += K[o, 6];         Kg[n, l + 1] += K[o, 7];
                        }

                        // Increment the line index
                        o++;
                    }
                }
            }

            // Calculate panel forces
            public static void PanelForces(Panel[] panels, Vector<double> u)
            {
                foreach (var pnl in panels)
                {
                    // Get the parameters
                    int[] ind = pnl.Index;
                    var Kl = pnl.LinearPanel.LocalStiffness;
                    var T = pnl.LinearPanel.TransMatrix;

                    // Get the displacements
                    var uStr = Vector<double>.Build.DenseOfArray(new double[]
                    {
                        u[ind[0]], u[ind[0] + 1], u[ind[1]], u[ind[1] + 1], u[ind[2]] , u[ind[2] + 1], u[ind[3]] , u[ind[3] + 1]
                    });

                    // Get the displacements in the direction of the stringer
                    var ul = T * uStr;

                    // Calculate the vector of forces
                    var fl = Kl * ul;

                    // Aproximate small values to zero
                    fl.CoerceZero(0.000001);

                    // Save the forces to panel
                    pnl.Forces = fl;
                }
            }

            // Get the dimensions of surrounding stringers
            public static void StringersDimensions(Panel panel, Stringer[] stringers)
            {
	            // Initiate the stringer dimensions
	            double[] strDims = new double[4];

	            // Analyse panel grips
	            for (int i = 0; i < 4; i++)
	            {
		            int grip = panel.Grips[i];

		            // Verify if its an internal grip of a stringer
		            foreach (var stringer in stringers)
		            {
			            if (grip == stringer.Grips[1])
			            {
				            // The dimension is the half of stringer height
				            strDims[i] = 0.5 * stringer.Height;
				            break;
			            }
		            }
	            }

	            // Save to panel
	            panel.StringerDimensions = strDims;
            }

            public class Linear:SPMTool.Panel.Linear
            {
	            public Linear(SPMTool.Panel panel, Material.Concrete concrete) : base(panel, concrete)
	            {
	            }

                // Calculate the stiffness matrix of a panel, get the dofs and save to XData, returns the all the matrices in an ordered list
                public static void PanelsStiffness(Panel[] panels, Material.Concrete concrete, Matrix<double> Kg)
                {
                    // Get the panels stiffness matrix and add to the global stiffness matrix
                    foreach (var pnl in panels)
                    {
                        // Get the linear parameters
                        pnl.LinearPanel = new Linear(pnl, concrete);

                        // T matrix
                        var T = pnl.LinearPanel.TransMatrix;

                        // Stiffness matrix
                        var Kl = pnl.LinearPanel.LocalStiffness;

                        // Global stiffness matrix
                        var K = T.Transpose() * Kl * T;

                        // Add to the global matrix
                        GlobalStiffness(pnl.Index, K, Kg);
                    }
                }
            }

    //        public partial class NonLinear
    //        {
				//// Calculate panel initial nonlinear parameters
				//public static void InitialParameters(Panel[] panels, Stringer[] stringers)
				//{
				//	foreach (var panel in panels)
				//	{
				//	    // Get surrounding stringers dimensions
				//		StringersDimensions(panel, stringers);

				//		// Calculate B*A and Q*P
				//		BAMatrix(panel);
				//		QPMatrix(panel);

				//		// Get the effective ratio off panel
				//		var (pxEf, pyEf) = panel.EffectiveRatio;

				//		// Calculate the initial membrane stiffness each int. point
				//		Membrane[] membranes = new Membrane[4];
				//		for (int i = 0; i < 4; i++)
				//			membranes[i] = Membrane.InitialStiffness(panel, (pxEf[i], pyEf[i]));

				//		// Set to panel
				//		panel.IntPointsMembrane = membranes;
				//	}
    //            }

    //            // Calculate panel stiffness using MCFT
    // //           public static void Analysis(Panel panel)
    // //           {
    // //               // Calculate B*A and Q*P
    // //               BAMatrix(panel);
    // //               QPMatrix(panel);

				//	//// Get the effective ratio off panels
				//	//var (pxEf, pyEf) = panel.EffReinforcementRatio;

    // //               // Calculate the initial membrane stiffness each int. point
				//	//Membrane[] membranes = new Membrane[4];
				//	//for (int i = 0; i < 4; i++)
				//	//	membranes[i] = Membrane.InitialStiffness(panel, (pxEf[i], pyEf[i]));

    // //               // Initiate the load steps
    // //               for (int ls = 1; ls <= 100; ls++)
    // //               {
    // //                   // Calculate the load
    // //                   var f = 0.01 * ls * Input.f;

    // //                   // Calculate D Matrix
    // //                   var D = DMatrixMCFT(DList, QP, f, ls);

    // //                   // Break the loop if convergence is not reached
    // //                   if (Membrane.stop)
    // //                   {
    // //                       Console.WriteLine("Convergence not reached at step " + ls);
    // //                       break;
    // //                   }

    // //                   // Calculate panel stiffness
    // //                   //var K = QP * D * BA;

    // //                   // Get the strains
    // //                   var e = epsMatrix.Row(ls - 1);

    // //                   // Calculate the displacements
    // //                   //var u = K.PseudoInverse() * f;
    // //                   var u = BA.PseudoInverse() * e;

    // //                   // Store f and u at the matrix
    // //                   fMatrix.SetRow(ls - 1, f);
    // //                   uMatrix.SetRow(ls - 1, u);
    // //               }

    // //               // Write results
    // //               if (Membrane.lsCrack != 0)
    // //                   Console.WriteLine("Concrete cracked at step " + Membrane.lsCrack);

    // //               if (Membrane.lsYieldX != 0)
    // //                   Console.WriteLine("X reinforcement yielded at step " + Membrane.lsYieldX);

    // //               if (Membrane.lsYieldY != 0)
    // //                   Console.WriteLine("Y reinforcement yielded at step " + Membrane.lsYieldY);

    // //               if (Membrane.lsPeak != 0)
    // //                   Console.WriteLine("Concrete reached it's peak stress at step " + Membrane.lsPeak);

    // //               if (Membrane.lsUlt != 0)
    // //                   Console.WriteLine("Concrete crushed at step " + Membrane.lsUlt);

    // //               // Write csvs
    // //               DelimitedWriter.Write("D:/f.csv", fMatrix, ";");
    // //               DelimitedWriter.Write("D:/u.csv", uMatrix, ";");
    // //               DelimitedWriter.Write("D:/sigma.csv", sigMatrix, ";");
    // //               DelimitedWriter.Write("D:/epsilon.csv", epsMatrix, ";");
    // //               DelimitedWriter.Write("D:/sigma1.csv", sig1Matrix, ";");
    // //               DelimitedWriter.Write("D:/epsilon1.csv", eps1Matrix, ";");
    // //           }

    //            // Calculate BA matrix
    //            static void BAMatrix(Panel panel)
    //            {
    //                // Get the dimensions
    //                double
    //                    a = panel.Dimensions.a,
    //                    b = panel.Dimensions.b,
    //                    c = panel.Dimensions.c,
    //                    d = panel.Dimensions.d;

    //                // Calculate t1, t2 and t3
    //                double
    //                    t1 = a * b - c * d,
    //                    t2 = 0.5 * (a * a - c * c) + b * b - d * d,
    //                    t3 = 0.5 * (b * b - d * d) + a * a - c * c;

    //                // Calculate the components of A matrix
    //                double
    //                    aOvert1 = a / t1,
    //                    bOvert1 = b / t1,
    //                    cOvert1 = c / t1,
    //                    dOvert1 = d / t1,
    //                    aOvert2 = a / t2,
    //                    bOvert3 = b / t3,
    //                    aOver2t1 = aOvert1 / 2,
    //                    bOver2t1 = bOvert1 / 2,
    //                    cOver2t1 = cOvert1 / 2,
    //                    dOver2t1 = dOvert1 / 2;

    //                // Create A matrix
    //                var A = Matrix<double>.Build.DenseOfArray(new [,]
    //                {
    //                    {   dOvert1,        0,   bOvert1,        0, -dOvert1,         0, -bOvert1,         0 },
    //                    {         0, -aOvert1,         0, -cOvert1,        0,   aOvert1,        0,   cOvert1 },
    //                    { -aOver2t1, dOver2t1, -cOver2t1, bOver2t1, aOver2t1, -dOver2t1, cOver2t1, -bOver2t1 },
    //                    { -aOvert2,         0,   aOvert2,        0, -aOvert2,         0,  aOvert2,         0 },
    //                    {        0,   bOvert3,         0, -bOvert3,        0,   bOvert3,        0,  -bOvert3 }
    //                });

    //                // Calculate the components of B matrix
    //                double
    //                    cOvera = c / a,
    //                    dOverb = d / b;

    //                // Create B matrix
    //                var B = Matrix<double>.Build.DenseOfArray(new [,]
    //                {
    //                    {1, 0, 0, -cOvera,       0 },
    //                    {0, 1, 0,       0,      -1 },
    //                    {0, 0, 2,       0,       0 },
    //                    {1, 0, 0,       1,       0 },
    //                    {0, 1, 0,       0,  dOverb },
    //                    {0, 0, 2,       0,       0 },
    //                    {1, 0, 0,  cOvera,       0 },
    //                    {0, 1, 0,       0,       1 },
    //                    {0, 0, 2,       0,       0 },
    //                    {1, 0, 0,      -1,       0 },
    //                    {0, 1, 0,       0, -dOverb },
    //                    {0, 0, 2,       0,       0 }
    //                });

    //                // Calculate B*A
    //                panel.BAMatrix = B * A;
    //            }

    //            // Calculate QP matrix
    //            static void QPMatrix(Panel panel)
    //            {
	   //             // Get vertex coordinates
	   //             var (x, y) = panel.VertexCoordinates;

	   //             // Get the dimensions
    //                double
    //                    a = panel.Dimensions.a,
    //                    b = panel.Dimensions.b,
    //                    c = panel.Dimensions.c,
    //                    d = panel.Dimensions.d;

				//	// Get the height of surrounding stringers
				//	var h = panel.StringerDimensions;

				//	// Get panel width
				//	var w = panel.Width;

    //                // Calculate t4
    //                double t4 = a * a + b * b;

    //                // Calculate the components of Q matrix
    //                double
    //                    a2     = a * a,
    //                    bc     = b * c,
    //                    bdMt4  = b * d - t4,
    //                    ab     = a * b,
    //                    MbdMt4 = -b * d - t4,
    //                    Tt4    = 2 * t4,
    //                    acMt4  = a * c - t4,
    //                    ad     = a * d,
    //                    b2     = b * b,
    //                    MacMt4 = -a * c - t4;

    //                // Create Q matrix
    //                var Q = 1 / Tt4 * Matrix<double>.Build.DenseOfArray(new double[,]
    //                {
	   //                 {  a2,     bc,  bdMt4, -ab, -a2,    -bc, MbdMt4, ab },
	   //                 {   0,    Tt4,      0,   0,   0,      0,      0,   0 },
	   //                 {   0,      0,    Tt4,   0,   0,      0,      0,   0 },
	   //                 { -ab,  acMt4,     ad,  b2,  ab, MacMt4,    -ad, -b2 },
	   //                 { -a2,    -bc, MbdMt4,  ab,  a2,     bc,  bdMt4, -ab },
	   //                 {   0,      0,      0,   0,   0,    Tt4,      0,   0 },
	   //                 {   0,      0,      0,   0,   0,      0,    Tt4,   0 },
	   //                 {  ab, MacMt4,    -ad, -b2, -ab,  acMt4,     ad,  b2 }
    //                });

    //                // Create P matrix
    //                var P = Matrix<double>.Build.Dense(8, 12);

    //                // Calculate the components of P
    //                P[0, 0]  = P[1, 2]  = w * (y[1] - y[0]);
    //                P[0, 2]             = w * (x[0] - x[1]);
    //                P[1, 1]             = w * (x[0] - x[1] + h[1] + h[3]);
    //                P[2, 3]             = w * (y[2] - y[1] - h[2] - h[0]);
    //                P[2, 5]  = P[3, 4]  = w * (x[1] - x[2]);
    //                P[3, 5]             = w * (y[2] - y[1]);
    //                P[4, 6]  = P[5, 8]  = w * (y[3] - y[2]);
    //                P[4, 8]             = w * (x[2] - x[3]);
    //                P[5, 7]             = w * (x[2] - x[3] - h[1] - h[3]);
    //                P[6, 9]             = w * (y[0] - y[3] + h[0] + h[2]);
    //                P[6, 11] = P[7, 10] = w * (x[3] - x[0]);
    //                P[7, 11]            = w * (y[0] - y[3]);

    //                // Calculate Q*P
    //                panel.QPMatrix = Q * P;
    //            }

    //            // Calculate D matrix by MCFT
    //            static void DMatrix(Panel panel, Vector<double> f, int ls)
    //            {
    //                // Calculate the stresses in integration points
    //                var sigma = panel.QPMatrix.PseudoInverse() * f;

    //                // Approximate small numbers to zero
    //                sigma.CoerceZero(1E-6);

    //                // Get the stresses at each int. point in a list
    //                var sigList = new List<Vector<double>>();
    //                for (int i = 0; i <= 9; i += 3)
    //                    sigList.Add(sigma.SubVector(i, 3));

    //                // Create lists for storing different stresses and membrane elements
    //                // D will not be calculated for equal stresses
    //                var difsigList = new List<Vector<double>>();
    //                var difMembList = new List<Membrane>();

    //                // Create the matrix of the panel
    //                var Dt = Matrix<double>.Build.Dense(12, 12);

    //                // Calculate the material matrix of each int. point by MCFT
    //                for (int i = 0; i < 4; i++)
    //                {
    //                    // Initiate the membrane element
    //                    Membrane membrane;

    //                    // Get the stresses
    //                    var sig = sigList[i];

    //                    // Verify if it's already calculated
    //                    if (difsigList.Count > 0 && difsigList.Contains(sig)) // Already calculated
    //                    {
    //                        // Get the index of the stress vector
    //                        int j = difsigList.IndexOf(sig);

    //                        // Set membrane element
    //                        membrane = difMembList[j];
    //                    }

    //                    else // Not calculated
    //                    {
    //                        // Get the initial membrane element
    //                        var initialMembrane = panel.IntPointsMembrane[i];

    //                        // Calculate stiffness by MCFT
    //                        membrane = Membrane.MCFT.MCFTMain(initialMembrane, sig, ls);
							
    //                        // Add them to the list of different stresses and membranes
    //                        difsigList.Add(sig);
    //                        difMembList.Add(membrane);
    //                    }

    //                    // Set to panel
    //                    panel.IntPointsMembrane[i] = membrane;

    //                    // Set the submatrices
    //                    Dt.SetSubMatrix(3 * i, 3 * i, membrane.Stiffness);
    //                }

				//	// Set to panel
    //                panel.DMatrix = Dt;
    //            }
    //        }
        }
    }
}