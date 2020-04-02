using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.StatusBar;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace SPMTool
{
    public class Panel
    {
	    // Enum for setting stringer behavior
	    public enum Behavior
	    {
		    Linear = 1,
		    NonLinear = 2
	    }

        // Panel parameters
        public ObjectId               ObjectId         { get; }
        public int                    Number           { get; }
        public int[]                  Grips            { get; }
        public Point3d[]              Vertices         { get; }
        public double                 Width            { get; }
		public Reinforcement.Panel    Reinforcement    { get; }
        public virtual Matrix<double> TransMatrix      { get; }
        public virtual Matrix<double> InitialStiffness { get; }
        public virtual Matrix<double> LocalStiffness   { get; }
        public virtual Matrix<double> GlobalStiffness  { get; }
        public Vector<double>         Displacements    { get; set; }
        public virtual Vector<double> Forces           { get; }
        public virtual double         ShearStress      { get; }

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
        public int[] DoFIndex => Auxiliary.GlobalIndexes(Grips);

        // Get X and Y coordinates of a panel vertices
        public  (double[] x, double[] y) VertexCoordinates
        {
	        get
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
        }

		// Panel dimensions
        public (double a, double b, double c, double d) Dimensions
        {
	        get
	        {
		        var (x, y) = VertexCoordinates;

		        // Calculate the necessary dimensions of the panel
		        double
			        a = (x[1] + x[2]) / 2 - (x[0] + x[3]) / 2,
			        b = (y[2] + y[3]) / 2 - (y[0] + y[1]) / 2,
			        c = (x[2] + x[3]) / 2 - (x[0] + x[1]) / 2,
			        d = (y[1] + y[2]) / 2 - (y[0] + y[3]) / 2;

		        return (a, b, c, d);
	        }
        }

        // Set the center point
        public Point3d CenterPoint
        {
	        get
	        {
		        // Calculate the approximated center point
		        var Pt1 = Auxiliary.MidPoint(Vertices[0], Vertices[2]);
		        var Pt2 = Auxiliary.MidPoint(Vertices[1], Vertices[3]);
		        return Auxiliary.MidPoint(Pt1, Pt2);
	        }
        }

        // Set edge lengths and angles
        private double[] Lengths => Edges.Length;
        private double[] Angles  => Edges.Angle;
        public (double[] Length, double[] Angle) Edges
        {
	        get
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
        }

		// Calculate direction cosines of each edge
		public (double cos, double sin)[] DirectionCosines
		{
			get
			{
				(double cos, double sin)[] directionCosines = new (double cos, double sin)[4];

                var angles = Angles;

                for (int i = 0; i < 4; i++)
	                directionCosines[i] = Auxiliary.DirectionCosines(angles[i]);

                return directionCosines;
			}
		}

        // Get panel displacements from global displacement vector
		public void Displacement(Vector<double> globalDisplacementVector)
		{
			var u = globalDisplacementVector;
			int[] ind = DoFIndex;

            // Get the displacements
            var up = Vector<double>.Build.Dense(8);
            for (int i = 0; i < ind.Length; i++)
            {
				// Indexers
				int j = ind[i];

				// Set values
	            up[i] = u[j];
            }

			// Set
			Displacements = up;
		}

		// Maximum panel force
		public double MaxForce => Forces.AbsoluteMaximum();

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
	            var dirCos = DirectionCosines;
	            var (m1, n1) = dirCos[0];
	            var (m2, n2) = dirCos[1];
	            var (m3, n3) = dirCos[2];
	            var (m4, n4) = dirCos[3];

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
					w = Width,
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
                var (x, y) = VertexCoordinates;
                var (a, b, c, d) = Dimensions;
                double
					w  = Width,
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
            public override Vector<double> Forces
            {
	            get
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
            }

            // Calculate shear stress
            public override double ShearStress
            {
	            get
	            {
		            // Get the dimensions as a vector
		            var lsV = Vector<double>.Build.DenseOfArray(Edges.Length);

		            // Calculate the shear stresses
		            var tau = Forces / (lsV * Width);

		            // Calculate the average stress
		            return Math.Round((-tau[0] + tau[1] - tau[2] + tau[3]) / 4, 2);
	            }
            }

            // Function to verify if a panel is rectangular
            private readonly Func<double[], bool> RectangularPanel = delegate (double[] angles)
            {
                // Calculate the angles between the edges
                double ang2 = angles[1] - angles[0];
                double ang4 = angles[3] - angles[2];

                if (ang2 == Constants.PiOver2 && ang4 == Constants.PiOver2)
                    return true;

	            return false;
            };
        }

        public class NonLinear : Panel
        {
            // Public Properties
			public Membrane.MCFT[]          IntPointsMembrane  { get; set; }
            public double[]                 StringerDimensions { get; }

            // Private Properties
            private Material.Concrete Concrete { get; }
			private int               LoadStep { get; set; }

            // Reinforcement ratio
            private double psx => Reinforcement.Ratio.X;
            private double psy => Reinforcement.Ratio.Y;

            public NonLinear(ObjectId panelObject, Material.Concrete concrete, Stringer[] stringers) : base(panelObject)
            {
	            Concrete = concrete;

				// Get stringer dimensions and effective ratio
				StringerDimensions = StringersDimensions(stringers);
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

            // Calculate BA matrix
            public Matrix<double> BAMatrix => matrixBA.Value;
            private Lazy<Matrix<double>> matrixBA => new Lazy<Matrix<double>>(MatrixBA);
            private Matrix<double> MatrixBA()
            {
	            var (a, b, c, d) = Dimensions;

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

            // Calculate Q matrix
            private Matrix<double> QMatrix => matrixQ.Value;
            private Lazy<Matrix<double>> matrixQ => new Lazy<Matrix<double>>(MatrixQ);
            private Matrix<double> MatrixQ()
            {
                // Get dimensions
                var (a, b, c, d) = Dimensions;

                // Calculate t4
                double t4 = a * a + b * b;

                // Calculate the components of Q matrix
                double
                    a2 = a * a,
                    bc = b * c,
                    bdMt4 = b * d - t4,
                    ab = a * b,
                    MbdMt4 = -b * d - t4,
                    Tt4 = 2 * t4,
                    acMt4 = a * c - t4,
                    ad = a * d,
                    b2 = b * b,
                    MacMt4 = -a * c - t4;

                // Create Q matrix
                return
	                1 / Tt4 * Matrix<double>.Build.DenseOfArray(new [,]
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
            }

            // Calculate P matrices for concrete and steel
            private (Matrix<double> Pc, Matrix<double> Ps) PMatrix => matrixP.Value;
            private Lazy<(Matrix<double> Pc, Matrix<double> Ps)> matrixP => new Lazy<(Matrix<double> Pc, Matrix<double> Ps)>(MatrixP);
            private (Matrix<double> Pc, Matrix<double> Ps) MatrixP()
            {
				// Get dimensions
				var (x, y) = VertexCoordinates;
				double t = Width;
	            var hs = StringerDimensions;

                // Create P matrices
                var Pc = Matrix<double>.Build.Dense(8, 12);
                var Ps = Matrix<double>.Build.Dense(8, 12);

                // Calculate the components of Pc
                Pc[0, 0] = Pc[1, 2]   = t * (y[1] - y[0]);
                Pc[0, 2]              = t * (x[0] - x[1]);
                Pc[1, 1]              = t * (x[0] - x[1] + hs[1] + hs[3]);
                Pc[2, 3]              = t * (y[2] - y[1] - hs[2] - hs[0]);
                Pc[2, 5] = Pc[3, 4]   = t * (x[1] - x[2]);
                Pc[3, 5]              = t * (y[2] - y[1]);
                Pc[4, 6] = Pc[5, 8]   = t * (y[3] - y[2]);
                Pc[4, 8]              = t * (x[2] - x[3]);
                Pc[5, 7]              = t * (x[2] - x[3] - hs[1] - hs[3]);
                Pc[6, 9]              = t * (y[0] - y[3] + hs[0] + hs[2]);
                Pc[6, 11] = Pc[7, 10] = t * (x[3] - x[0]);
                Pc[7, 11]             = t * (y[0] - y[3]);

                // Calculate the components of Ps
                Ps[0, 0]  = Pc[0, 0];
                Ps[1, 1]  = t * (x[0] - x[1]);
                Ps[2, 3]  = t * (y[2] - y[1]);
                Ps[3, 4]  = Pc[3, 4];
                Ps[4, 6]  = Pc[4, 6];
                Ps[5, 7]  = t * (x[2] - x[3]);
                Ps[6, 9]  = t * (y[0] - y[3]);
                Ps[7, 10] = Pc[7, 10];

                return (Pc, Ps);
            }

            // Calculate QP matrix
            //public Matrix<double> QPMatrix => matrixQP.Value;
            //private Lazy<Matrix<double>> matrixQP => new Lazy<Matrix<double>>(MatrixQP);
            //private Matrix<double> MatrixQP()
            //{
            //    return QMatrix * PMatrix;
            //}

			// Calculate panel strain vector
			public Vector<double> StrainVector => BAMatrix * Displacements;

            // Calculate D matrix and stress vector by MCFT
            public void MCFTAnalysis()
            {
				// Get MCFT results at integration points
				var membranes = new Membrane.MCFT[4];

				// Get the vector strains and stresses
				var ev = StrainVector;

				// Get effective ratio
				//var (pxEf, pyEf) = EffectiveRatio;

                // Calculate the material matrix of each int. point by MCFT
                for (int i = 0; i < 4; i++)
                {
	                // Get the strains and stresses
	                var e = ev.SubVector(3 * i, 3);

                    // Get the reinforcement and effective ratio
                 //   var reinforcement = new Reinforcement.Panel(Reinforcement.BarDiameter, Reinforcement.BarSpacing, Reinforcement.Steel, Width);
	                //reinforcement.SetEffectiveRatio((pxEf[i], pyEf[i]));

                    // Calculate stiffness by MCFT
                    var membrane = new Membrane.MCFT(Concrete, Reinforcement, e, LoadStep);

	                // Set to panel
	                membranes[i] = membrane;
                }

                // Set results to panel
                IntPointsMembrane = membranes;
            }

            // Calculate DMatrix
            public (Matrix<double> D, Matrix<double> Dc, Matrix<double> Ds) DMatrix
            {
	            get
	            {
		            var Dt = Matrix<double>.Build.Dense(12, 12);
		            var Dc = Matrix<double>.Build.Dense(12, 12);
		            var Ds = Matrix<double>.Build.Dense(12, 12);

                    // Get the initial parameters
                    Membrane.MCFT[] membranes;
		            if (IntPointsMembrane != null)
			            membranes = IntPointsMembrane;
		            else
			            membranes = InitialMCFT;

		            for (int i = 0; i < 4; i++)
		            {
			            // Get the stiffness
			            var Di = membranes[i].Stiffness;
			            var Dci = membranes[i].ConcreteStiffness;
			            var Dsi = membranes[i].SteelStiffness;

			            // Set to stiffness
			            Dt.SetSubMatrix(3 * i, 3 * i, Di);
			            Dc.SetSubMatrix(3 * i, 3 * i, Dci);
			            Ds.SetSubMatrix(3 * i, 3 * i, Dsi);
		            }

		            return (Dt, Dc, Ds);
	            }
            }

            // Calculate stiffness
            public override Matrix<double> GlobalStiffness
            {
	            get
	            {
		            var (Pc, Ps)    = PMatrix;
		            var (_, Dc, Ds) = DMatrix;

		            return
			            QMatrix * (Pc * Dc + Ps * Ds) * BAMatrix;
	            }
            }

            // Get stress vector
            public (Vector<double> sigma, Vector<double> sigmaC, Vector<double> sigmaS) StressVector
            {
	            get
	            {
		            var sigma  = Vector<double>.Build.Dense(12);
		            var sigmaC = Vector<double>.Build.Dense(12);
		            var sigmaS = Vector<double>.Build.Dense(12);

		            // Get the initial parameters
		            var membranes = IntPointsMembrane;

		            for (int i = 0; i < 4; i++)
		            {
			            // Get the stiffness
			            var sig  = membranes[i].Stresses;
			            var sigC = membranes[i].ConcreteStresses;
			            var sigS = membranes[i].ReinforcementStressVector;

			            // Set to stiffness
			            sigma.SetSubVector(3 * i, 3, sig);
			            sigmaC.SetSubVector(3 * i, 3, sigC);
			            sigmaS.SetSubVector(3 * i, 3, sigS);
		            }

		            return (sigma, sigmaC, sigmaS);
	            }
            }

            // Calculate panel forces
            public override Vector<double> Forces
            {
	            get
	            {
					// Get P matrices
					var (Pc, Ps) = PMatrix;

					// Get stresses
					var (_, sigC, sigS) = StressVector;

					return
						QMatrix * (Pc * sigC + Ps * sigS);
	            }
            }

            // Initial MCFT parameters
            private Membrane.MCFT[] InitialMCFT
            {
	            get
	            {
		            var initialMCFT = new Membrane.MCFT[4];
		            //var (pxEf, pyEf) = EffectiveRatio;

                    for (int i = 0; i < 4; i++)
		            {
			            //var reinforcement = new Reinforcement.Panel(Reinforcement.BarDiameter, Reinforcement.BarSpacing, Reinforcement.Steel, Width);

			            // Get effective ratio
			            //reinforcement.SetEffectiveRatio((pxEf[i], pyEf[i]));

			            // Get parameters
			            initialMCFT[i] = new Membrane.MCFT(Concrete, Reinforcement);
		            }

		            return initialMCFT;
	            }
            }

        }
    }
}
