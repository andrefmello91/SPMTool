using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.StatusBar;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace SPMTool
{
    public class Panel
    {
        // Panel parameters
        public ObjectId               ObjectId        { get; }
        public int                    Number          { get; }
        public int[]                  Grips           { get; }
        public Point3d[]              Vertices        { get; }
        public double                 Width           { get; }
		public Reinforcement.Panel    Reinforcement   { get; }
        public virtual Matrix<double> TransMatrix     { get; }
        public virtual Matrix<double> LocalStiffness  { get; }
        public virtual Matrix<double> GlobalStiffness { get; }
        public Vector<double>         Displacements   { get; set; }
        public virtual Vector<double> Forces          { get; }
        public virtual double         ShearStress     { get; }

        // Constructor
        public Panel(ObjectId panelObject)
        {
	        ObjectId = panelObject;

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
	            Number = Convert.ToInt32(pnlData[(int) XData.Panel.Number].Value);
	            Width = Convert.ToDouble(pnlData[(int) XData.Panel.Width].Value);

	            // Create the list of grips
	            Grips = new []
	            {
		            Convert.ToInt32(pnlData[(int) XData.Panel.Grip1].Value),
		            Convert.ToInt32(pnlData[(int) XData.Panel.Grip2].Value),
		            Convert.ToInt32(pnlData[(int) XData.Panel.Grip3].Value),
		            Convert.ToInt32(pnlData[(int) XData.Panel.Grip4].Value)
	            };

	            // Create the list of vertices
	            Vertices = new []
	            {
		            nd1, nd2, nd3, nd4
	            };

	            // Get reinforcement
	            double
		            phiX = Convert.ToDouble(pnlData[(int)XData.Panel.XDiam].Value),
		            phiY = Convert.ToDouble(pnlData[(int)XData.Panel.YDiam].Value),
		            sx = Convert.ToDouble(pnlData[(int)XData.Panel.Sx].Value),
		            sy = Convert.ToDouble(pnlData[(int)XData.Panel.Sy].Value);

				// Get steel data
				double
					fyx = Convert.ToDouble(pnlData[(int) XData.Panel.fyx].Value),
					Esx = Convert.ToDouble(pnlData[(int) XData.Panel.Esx].Value),
					fyy = Convert.ToDouble(pnlData[(int) XData.Panel.fyy].Value),
					Esy = Convert.ToDouble(pnlData[(int) XData.Panel.Esy].Value);

				var steel = 
				(
					new Material.Steel(fyx, Esx), 
					new Material.Steel(fyy, Esy)
				);

				// Set reinforcement
				Reinforcement = new Reinforcement.Panel((phiX, phiY), (sx, sy), steel, Width);
            }
        }

        // Set global indexes from grips
        public int[] Index => GlobalIndexes();
        private int[] GlobalIndexes()
        {
	        // Initialize the array
	        int[] ind = new int[Grips.Length];

	        // Get the indexes
	        for (int i = 0; i < Grips.Length; i++)
		        ind[i] = 2 * Grips[i] - 2;

	        return ind;
        }

        // Get X and Y coordinates of a panel vertices
        private double[] x => VertexCoordinates.x;
        private double[] y => VertexCoordinates.y;
        public  (double[] x, double[] y) VertexCoordinates => VertexxCoordinates();
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

        // Calculate dimensions
        private double a => Dimensions.a;
        private double b => Dimensions.b;
        private double c => Dimensions.c;
        private double d => Dimensions.d;
        private double w => Width;
        public (double a, double b, double c, double d) Dimensions => PanelDimensions();
        private (double a, double b, double c, double d) PanelDimensions()
        {
	        // Calculate the necessary dimensions of the panel
	        double
		        a = (x[1] + x[2]) / 2 - (x[0] + x[3]) / 2,
		        b = (y[2] + y[3]) / 2 - (y[0] + y[1]) / 2,
		        c = (x[2] + x[3]) / 2 - (x[0] + x[1]) / 2,
		        d = (y[1] + y[2]) / 2 - (y[0] + y[3]) / 2;

	        return (a, b, c, d);
        }

        // Set the center point
        public Point3d CenterPoint => PanelCenterPoint();
        private Point3d PanelCenterPoint()
        {
	        // Calculate the approximated center point
	        var Pt1 = Auxiliary.MidPoint(Vertices[0], Vertices[2]);
	        var Pt2 = Auxiliary.MidPoint(Vertices[1], Vertices[3]);
	        return Auxiliary.MidPoint(Pt1, Pt2);
        }

        // Set edge lengths and angles
        private double[] Lengths => Edges.Length;
        private double[] Angles  => Edges.Angle;
        public (double[] Length, double[] Angle) Edges => EdgeLengthsAngles();
        private (double[] Length, double[] Angle) EdgeLengthsAngles()
        {
	        double[]
		        l = new double[4],
		        a = new double[4];

	        // Create lines to measure the angles between the edges and dimensions
	        Line[] ln =
	        {
		        new Line(Vertices[0], Vertices[1]),
		        new Line(Vertices[1], Vertices[2]),
		        new Line(Vertices[2], Vertices[3]),
		        new Line(Vertices[3], Vertices[0])
	        };

	        // Create the list of dimensions
	        for (int i = 0; i < 4; i++)
	        {
		        l[i] = ln[i].Length;
		        a[i] = ln[i].Angle;
	        }

	        return (l, a);
        }

		// Get panel displacements from global displacement vector
		public void Displacement(Vector<double> globalDisplacementVector)
		{
			var u = globalDisplacementVector;
			int[] ind = Index;

            // Get the displacements
            var up = Vector<double>.Build.Dense(8);
            for (int i = 0; i < 4; i++)
            {
				// Indexers
				int
					j = ind[i],
					k = 2 * i;

				// Set values
	            up[k] = u[j];
	            up[k + 1] = u[j + 1];
            }

			// Set
			Displacements = up;
		}

        public class Linear : Panel
        {
            // Private properties
            private double Gc { get; }

            public Linear(ObjectId panelObject, Material.Concrete concrete) : base(panelObject)
	        {
				// Get data
		        Gc = concrete.Eci / 2.4;
	        }

            // Get transformation matrix
            public override  Matrix<double> TransMatrix => transMatrix.Value;
            private Lazy<Matrix<double>>    transMatrix => new Lazy<Matrix<double>>(TransformationMatrix);
            private Matrix<double> TransformationMatrix()
            {
	            // Get the transformation matrix
	            // Direction cosines
	            double[]
		            dirCos1 = Auxiliary.DirectionCosines(Angles[0]),
		            dirCos2 = Auxiliary.DirectionCosines(Angles[1]),
		            dirCos3 = Auxiliary.DirectionCosines(Angles[2]),
		            dirCos4 = Auxiliary.DirectionCosines(Angles[3]);

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
	            return Matrix<double>.Build.DenseOfArray(new double[,]
	            {
		            {m1, n1,  0,  0,  0,  0,  0,  0},
		            { 0,  0, m2, n2,  0,  0,  0,  0},
		            { 0,  0,  0,  0, m3, n3,  0,  0},
		            { 0,  0,  0,  0,  0,  0, m4, n4}
	            });
            }

            // Calculate panel stiffness
            public override Matrix<double> LocalStiffness => localStiffness.Value;
            private Lazy<Matrix<double>>   localStiffness => new Lazy<Matrix<double>>(Stiffness);
            private Matrix<double> Stiffness()
            {
	            // If the panel is rectangular
	            if (RectangularPanel(Angles))
		            return RectangularPanelStiffness();

	            // If the panel is not rectangular
	            return NotRectangularPanelStiffness();
            }

            // Calculate global stiffness
            public override Matrix<double> GlobalStiffness => globalStiffness.Value;
            private Lazy<Matrix<double>> globalStiffness => new Lazy<Matrix<double>>(GloballStiffness);
            public Matrix<double> GloballStiffness()
            {
	            var T = TransMatrix;

	            return T.Transpose() * LocalStiffness * T;
            }

            // Calculate local stiffness of a rectangular panel
            private Matrix<double> RectangularPanelStiffness()
            {
                // Get the dimensions
                double
	                a = Lengths[0],
	                b = Lengths[1];

                // Calculate the parameters of the stiffness matrix
                double
                    aOverb = a / b,
                    bOvera = b / a;

                // Calculate the stiffness matrix
                return Gc * w * Matrix<double>.Build.DenseOfArray(new [,]
                {
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera},
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera}
                });
            }

            private Matrix<double> NotRectangularPanelStiffness()
            {
                // Get the dimensions
                double
	                l1 = Lengths[0],
	                l2 = Lengths[1],
	                l3 = Lengths[2],
	                l4 = Lengths[3];

                // Equilibrium parameters
                double
                    c1 = x[1] - x[0],
                    c2 = x[2] - x[1],
                    c3 = x[3] - x[2],
                    c4 = x[0] - x[3],
                    s1 = y[1] - y[0],
                    s2 = y[2] - y[1],
                    s3 = y[3] - y[2],
                    s4 = y[0] - y[3],
                    r1 = x[0] * y[1] - x[1] * y[0],
                    r2 = x[1] * y[2] - x[2] * y[1],
                    r3 = x[2] * y[3] - x[3] * y[2],
                    r4 = x[3] * y[0] - x[0] * y[3];

                // Kinematic parameters
                double
                    t1 = -b * c1 - c * s1,
                    t2 =  a * s2 + d * c2,
                    t3 =  b * c3 + c * s3,
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

            // Calculate panel forces
            public override Vector<double> Forces => PanelForces();
            private Vector<double> PanelForces()
            {
	            // Get the parameters
	            var up = Displacements;
	            var T  = TransMatrix;
	            var Kl = LocalStiffness;

	            // Get the displacements in the direction of the edges
	            var ul = T * up;

	            // Calculate the vector of forces
	            var fl = Kl * ul;

	            // Aproximate small values to zero
	            fl.CoerceZero(0.000001);

	            return fl;
            }

            // Calculate shear stress
            public override double ShearStress => ShearsStress();
            private double ShearsStress()
            {
	            // Get the dimensions as a vector
	            var lsV = Vector<double>.Build.DenseOfArray(Edges.Length);

	            // Calculate the shear stresses
	            var tau = Forces / (lsV * Width);

	            // Calculate the average stress
	            return Math.Round((-tau[0] + tau[1] - tau[2] + tau[3]) / 4, 2);
            }

            // Function to verify if a panel is rectangular
            private readonly Func<double[], bool> RectangularPanel = delegate (double[] angles)
            {
                // Calculate the angles between the edges
                double ang2 = angles[1] - angles[0];
                double ang4 = angles[3] - angles[2];

                if (ang2 == Constants.PiOver2 && ang4 == Constants.PiOver2)
                    return true;
                else
                    return false;
            };
        }

        public class NonLinear : Panel
        {
            // Public Properties
			public MCFT[]                   IntPointMCFT       { get; set; }
            public double[]                 StringerDimensions { get; }
            public (double[] X, double[] Y) EffectiveRatio     { get; }
            public Matrix<double>           DMatrix            { get; set; }
			public Vector<double>           StressVector       { get; set; }

            // Private Properties
            private Material.Concrete Concrete { get; }
			private int               LoadStep { get; }

            // Reinforcement ratio
            private double psx => Reinforcement.Ratio.X;
            private double psy => Reinforcement.Ratio.Y;

            public NonLinear(ObjectId panelObject, Material.Concrete concrete, int loadStep, Stringer[] stringers) : base(panelObject)
            {
	            Concrete = concrete;
	            LoadStep = loadStep;

				// Get stringer dimensions and effective ratio
				StringerDimensions = StringersDimensions(stringers);
				EffectiveRatio     = EffectiveRRatio();

				// Set the initial MCFT parameters
				IntPointMCFT = InitialParameters();
            }

            // Get the dimensions of surrounding stringers
            public double[] StringersDimensions(Stringer[] stringers)
            {
                // Initiate the stringer dimensions
                double[] hs = new double[4];

                // Analyse panel grips
                for (int i = 0; i < 4; i++)
                {
                    int grip = Grips[i];

                    // Verify if its an internal grip of a stringer
                    foreach (var stringer in stringers)
                    {
                        if (grip == stringer.Grips[1])
                        {
                            // The dimension is the half of stringer height
                            hs[i] = 0.5 * stringer.Height;
                            break;
                        }
                    }
                }

                // Save to panel
                return hs;
            }

            // Calculate the effective reinforcement ratio off a panel for considering stringer dimensions
            private (double[] X, double[] Y) EffectiveRRatio()
	        {
		        var hs = StringerDimensions;

		        // Calculate effective ratio for each edge
		        double[]
			        pxEf = new double[4],
			        pyEf = new double[4];
				
		        // Grip 1
		        pxEf[0] = psx;
		        pyEf[0] = psy * (x[0] - x[1]) / (x[0] - x[1] + hs[1] + hs[3]);

		        // Grip 2
		        pxEf[1] = psx * (y[1] - y[2]) / (y[1] - y[2] + hs[0] + hs[2]);
		        pyEf[1] = psy;

		        // Grip 3
		        pxEf[2] = psx;
		        pyEf[2] = psy * (x[2] - x[3]) / (x[2] - x[3] - hs[1] - hs[3]);

		        // Grip 4
		        pxEf[3] = psx * (y[0] - y[3]) / (y[0] - y[3] + hs[0] + hs[2]);
		        pyEf[3] = psy;

		        return (pxEf, pyEf);
	        }

            // Calculate BA matrix
            public Matrix<double> BAMatrix => matrixBA.Value;
            private Lazy<Matrix<double>> matrixBA => new Lazy<Matrix<double>>(MatrixBA);
            private Matrix<double> MatrixBA()
            {
                // Calculate t1, t2 and t3
                double
                    t1 = a * b - c * d,
                    t2 = 0.5 * (a * a - c * c) + b * b - d * d,
                    t3 = 0.5 * (b * b - d * d) + a * a - c * c;

                // Calculate the components of A matrix
                double
                    aOvert1  = a / t1,
                    bOvert1  = b / t1,
                    cOvert1  = c / t1,
                    dOvert1  = d / t1,
                    aOvert2  = a / t2,
                    bOvert3  = b / t3,
                    aOver2t1 = aOvert1 / 2,
                    bOver2t1 = bOvert1 / 2,
                    cOver2t1 = cOvert1 / 2,
                    dOver2t1 = dOvert1 / 2;

                // Create A matrix
                var A = Matrix<double>.Build.DenseOfArray(new[,]
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
                var B = Matrix<double>.Build.DenseOfArray(new[,]
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
                return B * A;
            }

            // Calculate QP matrix
            public Matrix<double> QPMatrix => matrixQP.Value;
            private Lazy<Matrix<double>> matrixQP => new Lazy<Matrix<double>>(MatrixQP);
            private Matrix<double> MatrixQP()
            {
	            var hs = StringerDimensions;

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
	                {  a2,     bc,  bdMt4, -ab, -a2,    -bc, MbdMt4,  ab },
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
                P[0, 0] = P[1, 2]   = w * (y[1] - y[0]);
                P[0, 2]             = w * (x[0] - x[1]);
                P[1, 1]             = w * (x[0] - x[1] + hs[1] + hs[3]);
                P[2, 3]             = w * (y[2] - y[1] - hs[2] - hs[0]);
                P[2, 5] = P[3, 4]   = w * (x[1] - x[2]);
                P[3, 5]             = w * (y[2] - y[1]);
                P[4, 6] = P[5, 8]   = w * (y[3] - y[2]);
                P[4, 8]             = w * (x[2] - x[3]);
                P[5, 7]             = w * (x[2] - x[3] - hs[1] - hs[3]);
                P[6, 9]             = w * (y[0] - y[3] + hs[0] + hs[2]);
                P[6, 11] = P[7, 10] = w * (x[3] - x[0]);
                P[7, 11]            = w * (y[0] - y[3]);

                // Calculate Q*P
                return Q * P;
            }

			// Calculate panel strain vector
			private Vector<double> StrainVector => BAMatrix * Displacements;

            // Calculate D matrix and stress vector by MCFT
            public void MCFTAnalysis()
            {
				// Get MCFT results at integration points
				var intPointMCFT = IntPointMCFT;

				// Get the vector strains and stresses
				var ev   = StrainVector;

                // Create lists for storing different stresses and membrane elements
                // D will not be calculated for equal stresses
                var difeList = new List<Vector<double>>();
                var difMCFTList = new List<MCFT>();

                // Create the matrix of the panel
                var Dt = Matrix<double>.Build.Dense(12, 12);

				// Create the final stress vector
				var sigf = Vector<double>.Build.Dense(12);

                // Calculate the material matrix of each int. point by MCFT
                for (int i = 0; i < 4; i++)
                {
                    // Initiate MCFT
                    MCFT mcft;

                    // Get the strains and stresses
                    var e   = ev.SubVector(i, 3);
                    var sig = intPointMCFT[i].Stresses;

                    // Verify if it's already calculated
                    if (difeList.Count > 0 && difeList.Contains(e)) // Already calculated
                    {
                        // Get the index of the stress vector
                        int j = difeList.IndexOf(e);

                        // Set membrane element and stresses
                        mcft = difMCFTList[j];
                    }

                    else // Not calculated
                    {
                        // Get the initial membrane element
                        var initialMembrane = intPointMCFT[i].FinalMembrane;

                        // Calculate stiffness by MCFT
                        mcft = new MCFT(initialMembrane, Concrete, e, sig, LoadStep);

                        // Add them to the list of different stresses and membranes
                        difeList.Add(e);
                        difMCFTList.Add(mcft);
                    }

                    // Set to panel
                    intPointMCFT[i] = mcft;

                    // Set the submatrices and subvectors
                    Dt.SetSubMatrix(3 * i, 3 * i, mcft.FinalMembrane.Stiffness);
                    sigf.SetSubVector(3 * i, 3, mcft.Stresses);
                }

                // Set results to panel
                IntPointMCFT = intPointMCFT;
                DMatrix      = Dt;
                StressVector = sigf;
            }

            // Calculate stiffness
            public override Matrix<double> GlobalStiffness => QPMatrix * DMatrix * BAMatrix;

            // Initial MCFT parameters
            private MCFT[] InitialMCFT => InitialParameters();
			private MCFT[] InitialParameters()
			{
				MCFT[] initialMCFT = new MCFT[4];

				for (int i = 0; i < 4; i++)
				{
					var reinforcement = Reinforcement;

					// Get effective ratio
					var pxEf = EffectiveRatio.X[i];
					var pyEf = EffectiveRatio.Y[i];
					reinforcement.Ratio = (pxEf, pyEf);

					// Get parameters
					initialMCFT[i] = new MCFT(Concrete, reinforcement);
				}

				return initialMCFT;
			}

			// Initial DMatrix
			public Matrix<double>  InitialDMatrix => InitiallDMatrix();
			private Matrix<double> InitiallDMatrix()
			{
				var Dt = Matrix<double>.Build.Dense(12, 12);

				// Get the initial parameters
				var initialMCFT = InitialMCFT;

				for (int i = 0; i < 4; i++)
				{
					// Get the stiffness
					var Di = initialMCFT[i].FinalMembrane.Stiffness;

					// Set to stiffness
					Dt.SetSubMatrix(3 * i, 3 * i, Di);
				}

				return Dt;
			}

			// Initial stiffness
			public Matrix<double> InitialStiffness => QPMatrix * InitialDMatrix * BAMatrix;
        }
    }
}
