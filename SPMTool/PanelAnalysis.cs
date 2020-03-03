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
	    public partial class Panel
	    {
            // Panel parameters
            public ObjectId                 ObjectId              { get; }
		    public int                      Number                { get; }
		    public int[]                    Grips                 { get; }
		    public Point3d[]                Vertices              { get; }
		    public (double[] x, double[] y) VertexCoordinates     { get; }
		    public Point3d                  CenterPoint           { get; }
		    public (double[] Length, double[] Angle) Edges        { get; }
		    public double[]                 StringerDimensions    { get; set; }
		    public double                   Width                 { get; }
            public (double X, double Y)     BarDiameter           { get; }
            public (double X, double Y)     BarSpacing            { get; }
            public (double X, double Y)     ReinforcementRatio    { get; }
            public Matrix<double>           LocalStiffness        { get; set; }
            public Matrix<double>           TransMatrix           { get; }
            public Matrix<double>           BAMatrix              { get; set; }
		    public Matrix<double>           QPMatrix              { get; set; }
		    public Matrix<double>           DMatrix               { get; set; }
		    public Vector<double>           Forces                { get; set; }
		    public double                   ShearStress           { get; set; }
		    public NonLinear.Membrane[]     IntPointsMembrane     { get; set; }
			public Linear					LinearPanel           { get; set; }
			
		    // Set global indexes from grips
		    public int[] Index => GlobalIndexes(Grips);

            // Constructor
            // Get the parameters from a panel object
            public Panel(ObjectId panelObject)
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Read as a solid
                    Solid pnl = trans.GetObject(panelObject, OpenMode.ForWrite) as Solid;

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
                    int num = Convert.ToInt32(pnlData[(int)XData.Panel.Number].Value);
                    double
                        w = Convert.ToDouble(pnlData[(int)XData.Panel.Width].Value),
                        phiX = Convert.ToDouble(pnlData[(int)XData.Panel.XDiam].Value),
                        phiY = Convert.ToDouble(pnlData[(int)XData.Panel.YDiam].Value),
                        sx = Convert.ToDouble(pnlData[(int)XData.Panel.Sx].Value),
                        sy = Convert.ToDouble(pnlData[(int)XData.Panel.Sy].Value);

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

                    // Set values
                    ObjectId = panelObject;
                    Number = num;
                    Grips = grips;
                    Vertices = verts;
                    Width = w;
                    BarDiameter = (phiX, phiY);
                    BarSpacing = (sx, sy);
                    VertexCoordinates = VertexxCoordinates();
                    ReinforcementRatio = PanelReinforcement();
                    CenterPoint = PanelCenterPoint();
                    Edges = EdgeLengthsAngles();
                    TransMatrix = TransformationMatrix();
                }
            }

            // Get X and Y coordinates of a panel vertices
            private (double[] x, double[] y) VertexxCoordinates()
            {
	            double[]
		            x = new double[4],
		            y = new double[4];

                // Get X and Y coordinates of the vertices
                for (int i = 0; i < 4; i++)
	            {
		            x[i] = Vertices[i].X;
		            y[i] = Vertices[i].Y;
	            }

	            return (x, y);
            }

            // Set reinforcement ratio
            private (double X, double Y) PanelReinforcement()
            {
	            return Reinforcement.PanelReinforcement(BarDiameter, BarSpacing, Width);
            }
			
            // Set the center point
            private Point3d PanelCenterPoint()
            {
	            // Get the vertices
	            var verts = Vertices;

                // Calculate the approximated center point
                var Pt1 = Auxiliary.MidPoint(verts[0], verts[2]);
	            var Pt2 = Auxiliary.MidPoint(verts[1], verts[3]);
	            return Auxiliary.MidPoint(Pt1, Pt2);
            }

            // Set edge lengths and angles
            private (double[] Length, double[] Angle) EdgeLengthsAngles()
			{
				// Create lines to measure the angles between the edges and dimensions
				Line
					ln1 = new Line(Vertices[0], Vertices[1]),
					ln2 = new Line(Vertices[1], Vertices[2]),
					ln3 = new Line(Vertices[2], Vertices[3]),
					ln4 = new Line(Vertices[3], Vertices[0]);

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

				return  (dims, angs);
			}

            // Set the dimensions for nonlinear analysis
            private (double a, double b, double c, double d) PanelDimensions()
            {
	            // Get X and Y coordinates of the vertices
	            var (x, y) = VertexCoordinates;

	            // Calculate the necessary dimensions of the panel
	            double
		            a = (x[1] + x[2]) / 2 - (x[0] + x[3]) / 2,
		            b = (y[2] + y[3]) / 2 - (y[0] + y[1]) / 2,
		            c = (x[2] + x[3]) / 2 - (x[0] + x[1]) / 2,
		            d = (y[1] + y[2]) / 2 - (y[0] + y[3]) / 2;

	            return (a, b, c, d);
            }
            public (double a, double b, double c, double d) Dimensions;

            // Calculate the effective reinforcement ratio off a panel for considering stringer dimensions
            private (double[] X, double[] Y) EffectiveRRatio()
            {
	            // Get reinforcement ratio
	            var (px, py) = ReinforcementRatio;

	            // Get stringer dimensions
	            var c = StringerDimensions;

	            // Get X and Y coordinates of the vertices
	            var (x, y) = VertexCoordinates;

	            // Calculate effective ratio for each edge
	            double[]
		            pxEf = new double[4],
		            pyEf = new double[4];

	            // Grip 1
	            pxEf[0] = px;
	            pyEf[0] = py * (x[0] - x[1]) / (x[0] - x[1] + c[1] + c[3]);

	            // Grip 2
	            pxEf[1] = px * (y[1] - y[2]) / (y[1] - y[2] + c[0] + c[2]);
	            pyEf[1] = py;

	            // Grip 3
	            pxEf[2] = px;
	            pyEf[2] = py * (x[2] - x[3]) / (x[2] - x[3] - c[1] - c[3]);

	            // Grip 4
	            pxEf[3] = px * (y[0] - y[3]) / (y[0] - y[3] + c[0] + c[2]);
	            pyEf[3] = py;

	            return (pxEf, pyEf);
            }
            public (double[] X, double[] Y) EffectiveRatio;

            // Get transformation matrix
            private Matrix<double> TransformationMatrix()
            {
	            // Get the angles
	            var alpha = Edges.Angle;

	            // Get the transformation matrix
	            // Direction cosines
	            double[]
		            dirCos1 = Auxiliary.DirectionCosines(alpha[0]),
		            dirCos2 = Auxiliary.DirectionCosines(alpha[1]),
		            dirCos3 = Auxiliary.DirectionCosines(alpha[2]),
		            dirCos4 = Auxiliary.DirectionCosines(alpha[3]);

	            double
		            m1 = dirCos1[0],
		            n1 = dirCos1[1],
		            m2 = dirCos2[0],
		            n2 = dirCos2[1],
		            m3 = dirCos3[0],
		            n3 = dirCos3[1],
		            m4 = dirCos4[0],
		            n4 = dirCos4[1];

	            // T matrix
	            var T = Matrix<double>.Build.DenseOfArray(new double[,]
	            {
		            {m1, n1, 0, 0, 0, 0, 0, 0},
		            {0, 0, m2, n2, 0, 0, 0, 0},
		            {0, 0, 0, 0, m3, n3, 0, 0},
		            {0, 0, 0, 0, 0, 0, m4, n4},

	            });

	            return T;
            }

            // Read the parameters of a collection of panel objects
            public static Panel[] Parameters(ObjectIdCollection panelObjects)
            {
                Panel[] panels = new Panel[panelObjects.Count];

                foreach (ObjectId pnlObj in panelObjects)
                {
					// Create a panel
					Panel panel = new Panel(pnlObj);

					// Get the index
					int i = panel.Number - 1;

					// Set to the array
					panels[i] = panel;
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
                    var Kl = pnl.LocalStiffness;
                    var T = pnl.TransMatrix;

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

            public class Linear
            {
                // Calculate the stiffness matrix of a panel, get the dofs and save to XData, returns the all the matrices in an ordered list
                public static void PanelsStiffness(Panel[] panels, double Gc, Matrix<double> Kg)
                {
                    // Get the panels stiffness matrix and add to the global stiffness matrix
                    foreach (var pnl in panels)
                    {
                        // Read the parameters
                        var verts = pnl.Vertices;
                        var L = pnl.Edges.Length;
                        double t = pnl.Width;
						
                        // Initialize the stifness matrix
                        var Kl = Matrix<double>.Build.Dense(4, 4);

                        // If the panel is rectangular (ang2 and ang4 will be equal to 90 degrees)
                        if (RectangularPanel(pnl))
                        {
                            Kl = RectangularPanelStiffness(pnl, Gc);
                        }

                        // If the panel is not rectangular
                        else
                        {
                            Kl = NotRectangularPanelStiffness(pnl, Gc);
                        }

                        // T matrix
                        var T = pnl.TransMatrix;

                        // Global stifness matrix
                        var K = T.Transpose() * Kl * T;

                        // Add to the global matrix
                        GlobalStiffness(pnl.Index, K, Kg);

                        // Save to panel parameters
                        pnl.LocalStiffness = Kl;
                    }
                }

                // Calculate local stiffness of a rectangular panel
                static Matrix<double> RectangularPanelStiffness(Panel panel, double Gc)
                {
                    // Get the dimensions
                    double
                        a = panel.Edges.Length[0],
                        b = panel.Edges.Length[1],
                        w = panel.Width;

                    // Calculate the parameters of the stifness matrix
                    double
                        aOverb = a / b,
                        bOvera = b / a;

                    // Calculate the stiffness matrix
                    return Gc * w * Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera},
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera}
                    });
                }

                static Matrix<double> NotRectangularPanelStiffness(Panel panel, double Gc)
                {
                    // Get the vertices
                    Point3d
                        nd1 = panel.Vertices[0],
                        nd2 = panel.Vertices[1],
                        nd3 = panel.Vertices[2],
                        nd4 = panel.Vertices[3];

                    // Get the dimensions
                    double
                        l1 = panel.Edges.Length[0],
                        l2 = panel.Edges.Length[1],
                        l3 = panel.Edges.Length[2],
                        l4 = panel.Edges.Length[3],
                        w  = panel.Width;

                    // Equilibrium parameters
                    double 
                        c1 = nd2.X - nd1.X,
                        c2 = nd3.X - nd2.X,
                        c3 = nd4.X - nd3.X,
                        c4 = nd1.X - nd4.X,
                        s1 = nd2.Y - nd1.Y,
                        s2 = nd3.Y - nd2.Y,
                        s3 = nd4.Y - nd3.Y,
                        s4 = nd1.Y - nd4.Y,
                        r1 = nd1.X * nd2.Y - nd2.X * nd1.Y,
                        r2 = nd2.X * nd3.Y - nd3.X * nd2.Y,
                        r3 = nd3.X * nd4.Y - nd4.X * nd3.Y,
                        r4 = nd4.X * nd1.Y - nd1.X * nd4.Y;

                    // Kinematic parameters
                    double
                        a = (c1 - c3) / 2,
                        b = (s2 - s4) / 2,
                        c = (c2 - c4) / 2,
                        d = (s1 - s3) / 2;

                    double
                        t1 = -b * c1 - c * s1,
                        t2 = a * s2 + d * c2,
                        t3 = b * c3 + c * s3,
                        t4 = -a * s4 - d * c4;

                    // Matrices to calculate the determinants
                    var km1 = Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        {c2, c3, c4},
                        {s2, s3, s4},
                        {r2, r3, r4},
                    });

                    var km2 = Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        {c1, c3, c4},
                        {s1, s3, s4},
                        {r1, r3, r4},
                    });

                    var km3 = Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        {c1, c2, c4},
                        {s1, s2, s4},
                        {r1, r2, r4},
                    });

                    var km4 = Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        {c1, c2, c3},
                        {s1, s2, s3},
                        {r1, r2, r3},
                    });

                    // Calculate the determinants
                    double
                        k1 = km1.Determinant(),
                        k2 = km2.Determinant(),
                        k3 = km3.Determinant(),
                        k4 = km4.Determinant();

                    // Calculate kf and ku
                    double
                        kf = k1 + k2 + k3 + k4,
                        ku = -t1 * k1 + t2 * k2 - t3 * k3 + t4 * k4;

                    // Calculate D
                    double D = 16 * Gc * w / (kf * ku);

                    // Get the vector B
                    var B = Vector<double>.Build.DenseOfArray(new double[]
                    {
                        -k1 * l1, k2 * l2, -k3 * l3, k4 * l4
                    });

                    // Get the stiffness matrix
                    return B.ToColumnMatrix() * D * B.ToRowMatrix();
                }

                // Function to verify if a panel is rectangular
                private static Func<Panel, bool> RectangularPanel = delegate(Panel panel)
                {
                    // Calculate the angles between the edges
                    double ang2 = panel.Edges.Angle[1] - panel.Edges.Angle[0];
                    double ang4 = panel.Edges.Angle[3] - panel.Edges.Angle[2];

                    if (ang2 == Constants.PiOver2 && ang4 == Constants.PiOver2)
                        return true;
                    else
                        return false;
                };

                // Calculate shear stress
                public static double ShearStress(Panel panel)
                {
                    // Get the dimensions as a vector
                    var lsV = Vector<double>.Build.DenseOfArray(panel.Edges.Length);

                    // Calculate the shear stresses
                    var tau = panel.Forces / (lsV * panel.Width);

                    // Calculate the average stress
                    double tauAvg = Math.Round((-tau[0] + tau[1] - tau[2] + tau[3]) / 4, 2);

                    // Set
                    panel.ShearStress = tauAvg;

                    return tauAvg;
                }
            }

            public partial class NonLinear
            {
				// Calculate panel initial nonlinear parameters
				public static void InitialParameters(Panel[] panels, Stringer[] stringers)
				{
					foreach (var panel in panels)
					{
					    // Get surrounding stringers dimensions
						StringersDimensions(panel, stringers);

						// Calculate B*A and Q*P
						BAMatrix(panel);
						QPMatrix(panel);

						// Get the effective ratio off panel
						var (pxEf, pyEf) = panel.EffectiveRatio;

						// Calculate the initial membrane stiffness each int. point
						Membrane[] membranes = new Membrane[4];
						for (int i = 0; i < 4; i++)
							membranes[i] = Membrane.InitialStiffness(panel, (pxEf[i], pyEf[i]));

						// Set to panel
						panel.IntPointsMembrane = membranes;
					}
                }

                // Calculate panel stiffness using MCFT
     //           public static void Analysis(Panel panel)
     //           {
     //               // Calculate B*A and Q*P
     //               BAMatrix(panel);
     //               QPMatrix(panel);

					//// Get the effective ratio off panels
					//var (pxEf, pyEf) = panel.EffReinforcementRatio;

     //               // Calculate the initial membrane stiffness each int. point
					//Membrane[] membranes = new Membrane[4];
					//for (int i = 0; i < 4; i++)
					//	membranes[i] = Membrane.InitialStiffness(panel, (pxEf[i], pyEf[i]));

     //               // Initiate the load steps
     //               for (int ls = 1; ls <= 100; ls++)
     //               {
     //                   // Calculate the load
     //                   var f = 0.01 * ls * Input.f;

     //                   // Calculate D Matrix
     //                   var D = DMatrixMCFT(DList, QP, f, ls);

     //                   // Break the loop if convergence is not reached
     //                   if (Membrane.stop)
     //                   {
     //                       Console.WriteLine("Convergence not reached at step " + ls);
     //                       break;
     //                   }

     //                   // Calculate panel stiffness
     //                   //var K = QP * D * BA;

     //                   // Get the strains
     //                   var e = epsMatrix.Row(ls - 1);

     //                   // Calculate the displacements
     //                   //var u = K.PseudoInverse() * f;
     //                   var u = BA.PseudoInverse() * e;

     //                   // Store f and u at the matrix
     //                   fMatrix.SetRow(ls - 1, f);
     //                   uMatrix.SetRow(ls - 1, u);
     //               }

     //               // Write results
     //               if (Membrane.lsCrack != 0)
     //                   Console.WriteLine("Concrete cracked at step " + Membrane.lsCrack);

     //               if (Membrane.lsYieldX != 0)
     //                   Console.WriteLine("X reinforcement yielded at step " + Membrane.lsYieldX);

     //               if (Membrane.lsYieldY != 0)
     //                   Console.WriteLine("Y reinforcement yielded at step " + Membrane.lsYieldY);

     //               if (Membrane.lsPeak != 0)
     //                   Console.WriteLine("Concrete reached it's peak stress at step " + Membrane.lsPeak);

     //               if (Membrane.lsUlt != 0)
     //                   Console.WriteLine("Concrete crushed at step " + Membrane.lsUlt);

     //               // Write csvs
     //               DelimitedWriter.Write("D:/f.csv", fMatrix, ";");
     //               DelimitedWriter.Write("D:/u.csv", uMatrix, ";");
     //               DelimitedWriter.Write("D:/sigma.csv", sigMatrix, ";");
     //               DelimitedWriter.Write("D:/epsilon.csv", epsMatrix, ";");
     //               DelimitedWriter.Write("D:/sigma1.csv", sig1Matrix, ";");
     //               DelimitedWriter.Write("D:/epsilon1.csv", eps1Matrix, ";");
     //           }

                // Calculate BA matrix
                static void BAMatrix(Panel panel)
                {
                    // Get the dimensions
                    double
                        a = panel.Dimensions.a,
                        b = panel.Dimensions.b,
                        c = panel.Dimensions.c,
                        d = panel.Dimensions.d;

                    // Calculate t1, t2 and t3
                    double
                        t1 = a * b - c * d,
                        t2 = 0.5 * (a * a - c * c) + b * b - d * d,
                        t3 = 0.5 * (b * b - d * d) + a * a - c * c;

                    // Calculate the components of A matrix
                    double
                        aOvert1 = a / t1,
                        bOvert1 = b / t1,
                        cOvert1 = c / t1,
                        dOvert1 = d / t1,
                        aOvert2 = a / t2,
                        bOvert3 = b / t3,
                        aOver2t1 = aOvert1 / 2,
                        bOver2t1 = bOvert1 / 2,
                        cOver2t1 = cOvert1 / 2,
                        dOver2t1 = dOvert1 / 2;

                    // Create A matrix
                    var A = Matrix<double>.Build.DenseOfArray(new [,]
                    {
                        {   dOvert1,        0,   bOvert1,        0, -dOvert1,         0, -bOvert1,         0 },
                        {         0, -aOvert1,         0, -cOvert1,        0,   aOvert1,        0,   cOvert1 },
                        { -aOver2t1, dOver2t1, -cOver2t1, bOver2t1, aOver2t1, -dOver2t1, cOver2t1, -bOver2t1 },
                        { -aOvert2,         0,   aOvert2,        0, -aOvert2,         0,  aOvert2,         0 },
                        {        0,   bOvert3,         0, -bOvert3,        0,   bOvert3,        0,  -bOvert3 }
                    });

                    // Calculate the components of B matrix
                    double
                        cOvera = c / a,
                        dOverb = d / b;

                    // Create B matrix
                    var B = Matrix<double>.Build.DenseOfArray(new [,]
                    {
                        {1, 0, 0, -cOvera,       0 },
                        {0, 1, 0,       0,      -1 },
                        {0, 0, 2,       0,       0 },
                        {1, 0, 0,       1,       0 },
                        {0, 1, 0,       0,  dOverb },
                        {0, 0, 2,       0,       0 },
                        {1, 0, 0,  cOvera,       0 },
                        {0, 1, 0,       0,       1 },
                        {0, 0, 2,       0,       0 },
                        {1, 0, 0,      -1,       0 },
                        {0, 1, 0,       0, -dOverb },
                        {0, 0, 2,       0,       0 }
                    });

                    // Calculate B*A
                    panel.BAMatrix = B * A;
                }

                // Calculate QP matrix
                static void QPMatrix(Panel panel)
                {
	                // Get vertex coordinates
	                var (x, y) = panel.VertexCoordinates;

	                // Get the dimensions
                    double
                        a = panel.Dimensions.a,
                        b = panel.Dimensions.b,
                        c = panel.Dimensions.c,
                        d = panel.Dimensions.d;

					// Get the height of surrounding stringers
					var h = panel.StringerDimensions;

					// Get panel width
					var w = panel.Width;

                    // Calculate t4
                    double t4 = a * a + b * b;

                    // Calculate the components of Q matrix
                    double
                        a2     = a * a,
                        bc     = b * c,
                        bdMt4  = b * d - t4,
                        ab     = a * b,
                        MbdMt4 = -b * d - t4,
                        Tt4    = 2 * t4,
                        acMt4  = a * c - t4,
                        ad     = a * d,
                        b2     = b * b,
                        MacMt4 = -a * c - t4;

                    // Create Q matrix
                    var Q = 1 / Tt4 * Matrix<double>.Build.DenseOfArray(new double[,]
                    {
	                    {  a2,     bc,  bdMt4, -ab, -a2,    -bc, MbdMt4, ab },
	                    {   0,    Tt4,      0,   0,   0,      0,      0,   0 },
	                    {   0,      0,    Tt4,   0,   0,      0,      0,   0 },
	                    { -ab,  acMt4,     ad,  b2,  ab, MacMt4,    -ad, -b2 },
	                    { -a2,    -bc, MbdMt4,  ab,  a2,     bc,  bdMt4, -ab },
	                    {   0,      0,      0,   0,   0,    Tt4,      0,   0 },
	                    {   0,      0,      0,   0,   0,      0,    Tt4,   0 },
	                    {  ab, MacMt4,    -ad, -b2, -ab,  acMt4,     ad,  b2 }
                    });

                    // Create P matrix
                    var P = Matrix<double>.Build.Dense(8, 12);

                    // Calculate the components of P
                    P[0, 0]  = P[1, 2]  = w * (y[1] - y[0]);
                    P[0, 2]             = w * (x[0] - x[1]);
                    P[1, 1]             = w * (x[0] - x[1] + h[1] + h[3]);
                    P[2, 3]             = w * (y[2] - y[1] - h[2] - h[0]);
                    P[2, 5]  = P[3, 4]  = w * (x[1] - x[2]);
                    P[3, 5]             = w * (y[2] - y[1]);
                    P[4, 6]  = P[5, 8]  = w * (y[3] - y[2]);
                    P[4, 8]             = w * (x[2] - x[3]);
                    P[5, 7]             = w * (x[2] - x[3] - h[1] - h[3]);
                    P[6, 9]             = w * (y[0] - y[3] + h[0] + h[2]);
                    P[6, 11] = P[7, 10] = w * (x[3] - x[0]);
                    P[7, 11]            = w * (y[0] - y[3]);

                    // Calculate Q*P
                    panel.QPMatrix = Q * P;
                }

                // Calculate D matrix by MCFT
                static void DMatrix(Panel panel, Vector<double> f, int ls)
                {
                    // Calculate the stresses in integration points
                    var sigma = panel.QPMatrix.PseudoInverse() * f;

                    // Approximate small numbers to zero
                    sigma.CoerceZero(1E-6);

                    // Get the stresses at each int. point in a list
                    var sigList = new List<Vector<double>>();
                    for (int i = 0; i <= 9; i += 3)
                        sigList.Add(sigma.SubVector(i, 3));

                    // Create lists for storing different stresses and membrane elements
                    // D will not be calculated for equal stresses
                    var difsigList = new List<Vector<double>>();
                    var difMembList = new List<Membrane>();

                    // Create the matrix of the panel
                    var Dt = Matrix<double>.Build.Dense(12, 12);

                    // Calculate the material matrix of each int. point by MCFT
                    for (int i = 0; i < 4; i++)
                    {
                        // Initiate the membrane element
                        Membrane membrane;

                        // Get the stresses
                        var sig = sigList[i];

                        // Verify if it's already calculated
                        if (difsigList.Count > 0 && difsigList.Contains(sig)) // Already calculated
                        {
                            // Get the index of the stress vector
                            int j = difsigList.IndexOf(sig);

                            // Set membrane element
                            membrane = difMembList[j];
                        }

                        else // Not calculated
                        {
                            // Get the initial membrane element
                            var initialMembrane = panel.IntPointsMembrane[i];

                            // Calculate stiffness by MCFT
                            membrane = Membrane.MCFT.MCFTMain(initialMembrane, sig, ls);
							
                            // Add them to the list of different stresses and membranes
                            difsigList.Add(sig);
                            difMembList.Add(membrane);
                        }

                        // Set to panel
                        panel.IntPointsMembrane[i] = membrane;

                        // Set the submatrices
                        Dt.SetSubMatrix(3 * i, 3 * i, membrane.Stiffness);
                    }

					// Set to panel
                    panel.DMatrix = Dt;
                }
            }
        }
    }
}