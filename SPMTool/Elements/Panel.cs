using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.Material;
using PanelData = SPMTool.XData.Panel;

namespace SPMTool.Elements
{
    public abstract class Panel
    {
        // Enum for panel Stringer behavior
        public enum Behavior
        {
            Linear,
            NonLinearMCFT,
            NonLinearDSFM
        }

        // Panel parameters
        public Behavior                                      PanelBehavior     { get; }
        public ObjectId                                      ObjectId          { get; }
        public int                                           Number            { get; }
        public int[]                                         Grips             { get; }
        public Point3d[]                                     Vertices          { get; }
        public double                                        Width             { get; }
        public Concrete                                      Concrete          { get; }
        public Reinforcement.Panel                           Reinforcement     { get; }
        public abstract Matrix<double>                       LocalStiffness    { get; }
        public abstract Matrix<double>                       GlobalStiffness   { get; }
        public Vector<double>                                Displacements     { get; set; }
        public abstract Vector<double>                       Forces            { get; }
        public abstract Vector<double>                       AverageStresses   { get; }
        public abstract (Vector<double> sigma, double theta) PrincipalStresses { get; }

        // Constructor
        public Panel(ObjectId panelObject, Concrete concrete = null, Behavior behavior = Behavior.Linear)
        {
            ObjectId      = panelObject;
            PanelBehavior = behavior;

            // Get concrete
            if (concrete == null)
                Concrete = AutoCAD.Material.ReadData();
            else
                Concrete = concrete;

            // Start a transaction
            using (Transaction trans = AutoCAD.Current.db.TransactionManager.StartTransaction())
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
                ResultBuffer pnlRb = pnl.GetXDataForApplication(AutoCAD.Current.appName);
                TypedValue[] pnlData = pnlRb.AsArray();

                // Get the panel parameters
                Number = Convert.ToInt32(pnlData[(int)PanelData.Number].Value);
                Width  = Convert.ToDouble(pnlData[(int)PanelData.Width].Value);

                // Create the list of grips
                Grips = new[]
                {
                    Convert.ToInt32(pnlData[(int) PanelData.Grip1].Value),
                    Convert.ToInt32(pnlData[(int) PanelData.Grip2].Value),
                    Convert.ToInt32(pnlData[(int) PanelData.Grip3].Value),
                    Convert.ToInt32(pnlData[(int) PanelData.Grip4].Value)
                };

                // Create the list of vertices
                Vertices = new[]
                {
                    nd1, nd2, nd3, nd4
                };

                // Get reinforcement
                double
                    phiX = Convert.ToDouble(pnlData[(int)PanelData.XDiam].Value),
                    phiY = Convert.ToDouble(pnlData[(int)PanelData.YDiam].Value),
                    sx   = Convert.ToDouble(pnlData[(int)PanelData.Sx].Value),
                    sy   = Convert.ToDouble(pnlData[(int)PanelData.Sy].Value);

                // Get steel data
                double
                    fyx = Convert.ToDouble(pnlData[(int)PanelData.fyx].Value),
                    Esx = Convert.ToDouble(pnlData[(int)PanelData.Esx].Value),
                    fyy = Convert.ToDouble(pnlData[(int)PanelData.fyy].Value),
                    Esy = Convert.ToDouble(pnlData[(int)PanelData.Esy].Value);

                var steel =
                (
                    new Steel(fyx, Esx),
                    new Steel(fyy, Esy)
                );

                // Set reinforcement
                Reinforcement = new Reinforcement.Panel((phiX, phiY), (sx, sy), steel, Width);
            }
        }

        // Set global indexes from grips
        public int[] DoFIndex => GlobalAuxiliary.GlobalIndexes(Grips);

        // Get X and Y coordinates of a panel vertices
        public (double[] x, double[] y) VertexCoordinates
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
                    a = 0.5 * (x[1] + x[2] - x[0] - x[3]),
                    b = 0.5 * (y[2] + y[3] - y[0] - y[1]),
                    c = 0.5 * (x[2] + x[3] - x[0] - x[1]),
                    d = 0.5 * (y[1] + y[2] - y[0] - y[3]);

                return
                    (a, b, c, d);
            }
        }

		// Calculate reference length
		public double ReferenceLength
		{
			get
			{
				var (a, b, _, _) = Dimensions;

				return
					Math.Min(a, b);
			}
		}

        // Set the center point
        public Point3d CenterPoint
        {
            get
            {
                // Calculate the approximated center point
                var Pt1 = GlobalAuxiliary.MidPoint(Vertices[0], Vertices[2]);
                var Pt2 = GlobalAuxiliary.MidPoint(Vertices[1], Vertices[3]);
                return GlobalAuxiliary.MidPoint(Pt1, Pt2);
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
                    directionCosines[i] = GlobalAuxiliary.DirectionCosines(angles[i]);

                return
                    directionCosines;
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

        // Function to verify if a panel is rectangular
        private readonly Func<double[], bool> RectangularPanel = angles =>
        {
            // Calculate the angles between the edges
            double ang2 = angles[1] - angles[0];
            double ang4 = angles[3] - angles[2];

            if (ang2 == Constants.PiOver2 && ang4 == Constants.PiOver2)
                return true;

            return false;
        };

        public class Linear : Panel
        {
            // Private properties
            private double Gc => Concrete.Ec / 2.4;

            public Linear(ObjectId panelObject, Concrete concrete = null, Behavior behavior = Behavior.Linear) : base(panelObject, concrete, behavior)
            {
                // Get transformation matrix
                TransMatrix = TransformationMatrix();

                // Calculate panel stiffness
                LocalStiffness = Stiffness();
            }

            // Calculate transformation matrix
            private Matrix<double> TransMatrix { get; }
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
            public override Matrix<double> LocalStiffness { get; }
            private Matrix<double> Stiffness()
            {
                // If the panel is rectangular
                if (RectangularPanel(Angles))
                    return
                        RectangularPanelStiffness();

                // If the panel is not rectangular
                return
                    NonRectangularPanelStiffness();
            }

            // Calculate global stiffness
            public override Matrix<double> GlobalStiffness
            {
                get
                {
                    var T = TransMatrix;

                    return
                        T.Transpose() * LocalStiffness * T;
                }
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
                return Gc * w * Matrix<double>.Build.DenseOfArray(new[,]
                {
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera},
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera}
                });
            }

            // Calculate local stiffness of a nonrectangular panel
            private Matrix<double> NonRectangularPanelStiffness()
            {
                // Get the dimensions
                var (x, y) = VertexCoordinates;
                var (a, b, c, d) = Dimensions;
                double
                    w = Width,
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

            // Calculate panel forces
            public override Vector<double> Forces
            {
                get
                {
                    // Get the parameters
                    var up = Displacements;
                    var T = TransMatrix;
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

            // Calculate panel stresses
            public override Vector<double> AverageStresses
            {
                get
                {
                    // Get the dimensions as a vector
                    var lsV = Vector<double>.Build.DenseOfArray(Edges.Length);

                    // Calculate the shear stresses
                    var tau = Forces / (lsV * Width);

                    // Calculate the average stress
                    double tauAvg = (-tau[0] + tau[1] - tau[2] + tau[3]) / 4;

                    return
                        Vector<double>.Build.DenseOfArray(new[] { 0, 0, tauAvg });
                }
            }

            // Calculate principal stresses by Equilibrium Plasticity Truss Model
            // Theta is the angle of sigma 2
            public override (Vector<double> sigma, double theta) PrincipalStresses
            {
                get
                {
                    double sig2;

                    // Get shear stress
                    double tau = AverageStresses[2];

                    // Get steel strengths
                    double
                        fyx = Reinforcement.Steel.X.YieldStress,
                        fyy = Reinforcement.Steel.Y.YieldStress;

                    if (fyx == fyy)
                        sig2 = -2 * Math.Abs(tau);

                    else
                    {
                        // Get relation of steel strengths
                        double rLambda = Math.Sqrt(fyx / fyy);
                        sig2 = -Math.Abs(tau) * (rLambda + 1 / rLambda);
                    }

                    var sigma = Vector<double>.Build.DenseOfArray(new[] { 0, sig2, 0 });

                    // Calculate theta
                    double theta;

                    if (tau <= 0)
                        theta = Constants.PiOver4;

                    else
                        theta = -Constants.PiOver4;

                    return
                        (sigma, theta);
                }
            }
        }

        public class NonLinear : Panel
        {
            // Public Properties
            public Membrane[] IntegrationPoints  { get; set; }
            public double[]   StringerDimensions { get; }

            // Private Properties
            private int LoadStep { get; set; }

            public NonLinear(ObjectId panelObject, Concrete concrete, Stringer[] stringers, Behavior behavior = Behavior.NonLinearMCFT) : base(panelObject, concrete, behavior)
            {
                // Get Stringer dimensions and effective ratio
                StringerDimensions = StringersDimensions(stringers);

                // Calculate initial matrices
                BAMatrix = MatrixBA();
                QMatrix  = MatrixQ();
                PMatrix  = MatrixP();

                // Initiate integration points
                IntegrationPoints = new Membrane[4];

                if (PanelBehavior == Behavior.NonLinearMCFT)
                    for (int i = 0; i < 4; i++)
	                    IntegrationPoints[i] = new Membrane.MCFT(Concrete, Reinforcement, Width);
                else
                    for (int i = 0; i < 4; i++)
                        IntegrationPoints[i] = new Membrane.DSFM(Concrete, Reinforcement, Width, ReferenceLength);

            }

            // Get the dimensions of surrounding stringers
            public double[] StringersDimensions(Stringer[] stringers)
            {
                // Initiate the Stringer dimensions
                double[] hs = new double[4];

                // Analyse panel grips
                for (int i = 0; i < 4; i++)
                {
                    int grip = Grips[i];

                    // Verify if its an internal grip of a Stringer
                    foreach (var stringer in stringers)
                    {
                        if (grip == stringer.Grips[1])
                        {
                            // The dimension is the half of Stringer height
                            hs[i] = 0.5 * stringer.Height;
                            break;
                        }
                    }
                }

                // Save to panel
                return hs;
            }

            // Calculate BA matrix
            private Matrix<double> BAMatrix { get; }
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
                    a_t1 = a / t1,
                    b_t1 = b / t1,
                    c_t1 = c / t1,
                    d_t1 = d / t1,
                    a_t2 = a / t2,
                    b_t3 = b / t3,
                    a_2t1 = a_t1 / 2,
                    b_2t1 = b_t1 / 2,
                    c_2t1 = c_t1 / 2,
                    d_2t1 = d_t1 / 2;

                // Create A matrix
                var A = Matrix<double>.Build.DenseOfArray(new[,]
                {
                    {   d_t1,     0,   b_t1,     0, -d_t1,      0, -b_t1,      0 },
                    {      0, -a_t1,      0, -c_t1,     0,   a_t1,     0,   c_t1 },
                    { -a_2t1, d_2t1, -c_2t1, b_2t1, a_2t1, -d_2t1, c_2t1, -b_2t1 },
                    { - a_t2,     0,   a_t2,     0, -a_t2,      0,  a_t2,      0 },
                    {      0,  b_t3,      0, -b_t3,     0,   b_t3,     0,  -b_t3 }
                });

                // Calculate the components of B matrix
                double
                    c_a = c / a,
                    d_b = d / b,
                    a2_b = 2 * a / b,
                    b2_a = 2 * b / a,
                    c2_b = 2 * c / b,
                    d2_a = 2 * d / a;

                // Create B matrix
                var B = Matrix<double>.Build.DenseOfArray(new[,]
                {
                    {1, 0, 0,  -c_a,     0 },
                    {0, 1, 0,     0,    -1 },
                    {0, 0, 2,  b2_a,  c2_b },
                    {1, 0, 0,     1,     0 },
                    {0, 1, 0,     0,   d_b },
                    {0, 0, 2, -d2_a, -a2_b },
                    {1, 0, 0,   c_a,     0 },
                    {0, 1, 0,     0,     1 },
                    {0, 0, 2, -b2_a, -c2_b },
                    {1, 0, 0,    -1,     0 },
                    {0, 1, 0,     0,  -d_b },
                    {0, 0, 2,  d2_a,  a2_b }
                });

                //var B = Matrix<double>.Build.DenseOfArray(new[,]
                //{
                // {1, 0, 0, -c_a,    0 },
                // {0, 1, 0,    0,   -1 },
                // {0, 0, 2,    0,    0 },
                // {1, 0, 0,    1,    0 },
                // {0, 1, 0,    0,  d_b },
                // {0, 0, 2,    0,    0 },
                // {1, 0, 0,  c_a,    0 },
                // {0, 1, 0,    0,    1 },
                // {0, 0, 2,    0,    0 },
                // {1, 0, 0,   -1,    0 },
                // {0, 1, 0,    0, -d_b },
                // {0, 0, 2,    0,    0 }
                //});

                return
                    B * A;
            }

            // Calculate Q matrixS
            private Matrix<double> QMatrix { get; }
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
                    1 / Tt4 * Matrix<double>.Build.DenseOfArray(new[,]
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
            private (Matrix<double> Pc, Matrix<double> Ps) PMatrix { get; }
            private (Matrix<double> Pc, Matrix<double> Ps) MatrixP()
            {
                // Get dimensions
                var (x, y) = VertexCoordinates;
                double t = Width;
                var c = StringerDimensions;

                // Create P matrices
                var Pc = Matrix<double>.Build.Dense(8, 12);
                var Ps = Matrix<double>.Build.Dense(8, 12);

                // Calculate the components of Pc
                Pc[0, 0] = Pc[1, 2] = t * (y[1] - y[0]);
                Pc[0, 2] = t * (x[0] - x[1]);
                Pc[1, 1] = t * (x[0] - x[1] + c[1] + c[3]);

                Pc[2, 3] = t * (y[2] - y[1] - c[2] - c[0]);
                Pc[2, 5] = Pc[3, 4] = t * (x[1] - x[2]);
                Pc[3, 5] = t * (y[2] - y[1]);

                Pc[4, 6] = Pc[5, 8] = t * (y[3] - y[2]);
                Pc[4, 8] = t * (x[2] - x[3]);
                Pc[5, 7] = t * (x[2] - x[3] - c[1] - c[3]);

                Pc[6, 9] = t * (y[0] - y[3] + c[0] + c[2]);
                Pc[6, 11] = Pc[7, 10] = t * (x[3] - x[0]);
                Pc[7, 11] = t * (y[0] - y[3]);

                // Calculate the components of Ps
                Ps[0, 0] = Pc[0, 0];
                Ps[1, 1] = t * (x[0] - x[1]);

                Ps[2, 3] = t * (y[2] - y[1]);
                Ps[3, 4] = Pc[3, 4];

                Ps[4, 6] = Pc[4, 6];
                Ps[5, 7] = t * (x[2] - x[3]);

                Ps[6, 9] = t * (y[0] - y[3]);
                Ps[7, 10] = Pc[7, 10];

                return
                    (Pc, Ps);
            }

            // Calculate panel strain vector
            public Vector<double> StrainVector => BAMatrix * Displacements;

            // Calculate D matrix and stress vector by MCFT
            public void Analysis()
            {
	            // Get the vector strains and stresses
	            var ev = StrainVector;

	            // Calculate the material matrix of each int. point by MCFT
	            for (int i = 0; i < 4; i++)
	            {
		            // Get the strains and stresses
		            var e = ev.SubVector(3 * i, 3);

		            // Calculate stresses by MCFT
		            IntegrationPoints[i].Analysis(e);
	            }
            }

            // Set results to panel integration points
            public void Results()
            {
	            foreach (var intPoint in IntegrationPoints)
		            intPoint.Results();
            }

            // Calculate DMatrix
            public (Matrix<double> Dc, Matrix<double> Ds) MaterialStiffness
            {
	            get
	            {
		            var Dc = Matrix<double>.Build.Dense(12, 12);
		            var Ds = Matrix<double>.Build.Dense(12, 12);

		            for (int i = 0; i < 4; i++)
		            {
			            // Get the stiffness
			            var Dci = IntegrationPoints[i].ConcreteStiffness;
			            var Dsi = IntegrationPoints[i].ReinforcementStiffness;

			            // Set to stiffness
			            Dc.SetSubMatrix(3 * i, 3 * i, Dci);
			            Ds.SetSubMatrix(3 * i, 3 * i, Dsi);
		            }

		            return
			            (Dc, Ds);
	            }
            }

            // Calculate stiffness
            public override Matrix<double> GlobalStiffness
            {
	            get
	            {
		            var (Pc, Ps) = PMatrix;
		            var (Dc, Ds) = InitialMaterialStiffness;
		            var QPs = QMatrix * Ps;
		            var QPc = QMatrix * Pc;

		            var kc = QPc * Dc * BAMatrix;
		            var ks = QPs * Ds * BAMatrix;

		            return
			            kc + ks;
	            }
            }

            // Calculate tangent stiffness
            public Matrix<double> TangentStiffness()
            {
	            // Get displacements
	            var u = Displacements;

	            // Set step size
	            double d = 1E-12;

	            // Calculate elements of matrix
	            var K = Matrix<double>.Build.Dense(8, 8);
	            for (int i = 0; i < 8; i++)
	            {
		            // Get row update vector
		            var ud = CreateVector.Dense<double>(8);
		            ud[i] = d;

		            // Set displacements and do analysis
		            Displacement(u + ud);
		            Analysis();

		            // Get updated panel forces
		            var fd1 = Forces;

		            // Set displacements and do analysis
		            Displacement(u - ud);
		            Analysis();

		            // Get updated panel forces
		            var fd2 = Forces;

		            // Calculate ith column
		            var km = 0.5 / d * (fd1 - fd2);

		            // Set column
		            K.SetColumn(i, km);
	            }

	            // Set displacements again
	            Displacement(u);

	            return K;
            }

            // Get stress vector
            public (Vector<double> sigma, Vector<double> sigmaC, Vector<double> sigmaS) StressVector
            {
	            get
	            {
		            var sigma = Vector<double>.Build.Dense(12);
		            var sigmaC = Vector<double>.Build.Dense(12);
		            var sigmaS = Vector<double>.Build.Dense(12);

		            for (int i = 0; i < 4; i++)
		            {
			            // Get the stiffness
			            var sig = IntegrationPoints[i].Stresses;
			            var sigC = IntegrationPoints[i].ConcreteStresses;
			            var sigS = IntegrationPoints[i].ReinforcementStresses;

			            // Set to stiffness
			            sigma.SetSubVector(3 * i, 3, sig);
			            sigmaC.SetSubVector(3 * i, 3, sigC);
			            sigmaS.SetSubVector(3 * i, 3, sigS);
		            }

		            return
			            (sigma, sigmaC, sigmaS);
	            }
            }

            // Get principal strains in concrete
            public Vector<double> ConcretePrincipalStrains
            {
	            get
	            {
		            var epsilon = Vector<double>.Build.Dense(8);

		            for (int i = 0; i < 4; i++)
		            {
			            var (ec1, ec2) = IntegrationPoints[i].Concrete.PrincipalStrains;
			            epsilon[2 * i] = ec1;
			            epsilon[2 * i + 1] = ec2;
		            }

		            return epsilon;
	            }
            }

            public Vector<double> StrainAngles
            {
	            get
	            {
		            var theta = Vector<double>.Build.Dense(4);

		            for (int i = 0; i < 4; i++)
			            theta[i] = IntegrationPoints[i].PrincipalAngles.theta2;

		            return theta;
	            }
            }

            // Get principal stresses in concrete
            public Vector<double> ConcretePrincipalStresses
            {
	            get
	            {
		            var sigma = Vector<double>.Build.Dense(8);

		            for (int i = 0; i < 4; i++)
		            {
			            var (fc1, fc2) = IntegrationPoints[i].Concrete.PrincipalStresses;
			            sigma[2 * i] = fc1;
			            sigma[2 * i + 1] = fc2;
		            }

		            return sigma;
	            }
            }

            // Calculate panel forces
            public override Vector<double> Forces
            {
                get
                {
                    // Get dimensions
                    var (x, y) = VertexCoordinates;
                    var c = StringerDimensions;

                    // Get stresses
                    var (sig, sigC, sigS) = StressVector;
                    var (sig1, sigC1, sigS1) = (sig.SubVector(0, 3), sigC.SubVector(0, 3), sigS.SubVector(0, 3));
                    var (sig2, sigC2, sigS2) = (sig.SubVector(3, 3), sigC.SubVector(3, 3), sigS.SubVector(3, 3));
                    var (sig3, sigC3, sigS3) = (sig.SubVector(6, 3), sigC.SubVector(6, 3), sigS.SubVector(6, 3));
                    var (sig4, sigC4, sigS4) = (sig.SubVector(9, 3), sigC.SubVector(9, 3), sigS.SubVector(9, 3));

                    // Calculate forces
                    var f = Width * Vector<double>.Build.DenseOfArray(new[]
                    {
                        -sig1[0]  * (y[0] - y[1]) - sig1[2] * (x[1] - x[0]),
                        -sigC1[1] * (x[1] - x[0] - c[1] - c[3]) - sigS1[1] * (x[1] - x[0]) - sig1[2] * (y[0] - y[1]),
                         sigC2[0] * (y[2] - y[1] - c[2] - c[0]) + sigS2[0] * (y[2] - y[1]) - sig2[2] * (x[2] - x[1]),
                        -sig2[1]  * (x[2] - x[1]) + sig2[2] * (y[2] - y[1]),
                        -sig3[0]  * (y[2] - y[3]) + sig3[2] * (x[2] - x[3]),
                         sigC3[1] * (x[2] - x[3] - c[1] - c[3]) + sigS3[1] * (x[2] - x[3]) - sig3[2] * (y[2] - y[3]),
                        -sigC4[0] * (y[3] - y[0] - c[0] - c[2]) - sigS4[0] * (y[3] - y[0]) - sig4[2] * (x[0] - x[3]),
                        -sig4[1]  * (x[0] - x[3]) - sig4[2] * (y[3] - y[0])
                    });

                    return
                        QMatrix * f;

                    //// Get P matrices
                    //var (Pc, Ps) = PMatrix;

                    //// Get stresses
                    //var (_, sigC, sigS) = StressVector;

                    //// Calculate forces
                    //var fc = Pc * sigC;
                    //var fs = Ps * sigS;
                    //var f = fc + fs;

                    //return
                    //    QMatrix * f;
                }
            }

            // Calculate panel stresses
            public override Vector<double> AverageStresses
            {
                get
                {
                    // Get stress vector
                    var sigma = StressVector.sigma;

                    // Calculate average stresses
                    double
                        sigxm = (sigma[0] + sigma[3] + sigma[6] + sigma[9]) / 4,
                        sigym = (sigma[1] + sigma[4] + sigma[7] + sigma[10]) / 4,
                        sigxym = (sigma[2] + sigma[5] + sigma[8] + sigma[11]) / 4;

                    return
                        Vector<double>.Build.DenseOfArray(new[] { sigxm, sigym, sigxym });
                }
            }

            // Calculate principal stresses
            // Theta is the angle of sigma 2
            public override (Vector<double> sigma, double theta) PrincipalStresses
            {
                get
                {
                    // Get average stresses
                    var sigm = AverageStresses;

                    // Calculate principal stresses by Mohr's Circle
                    double
                        rad = 0.5 * (sigm[0] + sigm[1]),
                        cen = Math.Sqrt(0.25 * (sigm[0] - sigm[1]) * (sigm[0] - sigm[1]) + sigm[2] * sigm[2]),
                        sig1 = cen + rad,
                        sig2 = cen - rad,
                        theta = Math.Atan((sig1 - sigm[1]) / sigm[2]) - Constants.PiOver2;

                    var sigma = Vector<double>.Build.DenseOfArray(new[] { sig1, sig2, 0 });

                    return
                        (sigma, theta);
                }
            }

            // Initial material stiffness
            public (Matrix<double> Dc, Matrix<double> Ds) InitialMaterialStiffness
            {
	            get
	            {
		            var Dc = Matrix<double>.Build.Dense(12, 12);
		            var Ds = Matrix<double>.Build.Dense(12, 12);

		            for (int i = 0; i < 4; i++)
		            {
			            // Get the stiffness
			            var Dci = IntegrationPoints[i].InitialConcreteStiffness;
			            var Dsi = IntegrationPoints[i].InitialReinforcementStiffness;

			            // Set to stiffness
			            Dc.SetSubMatrix(3 * i, 3 * i, Dci);
			            Ds.SetSubMatrix(3 * i, 3 * i, Dsi);
		            }

		            return
			            (Dc, Ds);
	            }
            }

            // Initial stiffness
            public Matrix<double> InitialStiffness
            {
	            get
	            {
		            var (Pc, Ps) = PMatrix;
		            var (Dc, Ds) = InitialMaterialStiffness;
		            //var PD = Pc * Dc + Ps * Ds;
		            var QPs = QMatrix * Ps;
		            var QPc = QMatrix * Pc;

		            var kc = QPc * Dc * BAMatrix;
		            var ks = QPs * Ds * BAMatrix;

		            return
			            kc + ks;
	            }
            }


            // Properties not needed
            public override Matrix<double> LocalStiffness => throw new NotImplementedException();
        }
    }
}
