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
        public ObjectId                          ObjectId           { get; set; }
        public int                               Number             { get; set; }
        public int[]                             Grips              { get; set; }
        public Point3d[]                         Vertices           { get; set; }
        public double[]                          StringerDimensions { get; set; }
        public double                            Width              { get; set; }
        public (double X, double Y)              BarDiameter        { get; set; }
        public (double X, double Y)              BarSpacing         { get; set; }
        public Vector<double>                    Forces             { get; set; }
        public Linear                            LinearPanel        { get; set; }

        // Set global indexes from grips
        public  int[] Index => GlobalIndexes(Grips);
        private int[] GlobalIndexes(int[] grips)
        {
	        // Initialize the array
	        int[] ind = new int[grips.Length];

	        // Get the indexes
	        for (int i = 0; i < grips.Length; i++)
		        ind[i] = 2 * grips[i] - 2;

	        return ind;
        }

        // Constructor
        public Panel(ObjectId panelObject)
        {
	        ObjectId = panelObject;
        }

        // Get X and Y coordinates of a panel vertices
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

        // Set reinforcement ratio
        public  (double X, double Y) ReinforcementRatio => PanelReinforcement();
        private (double X, double Y) PanelReinforcement()
        {
	        return Reinforcement.PanelReinforcement(BarDiameter, BarSpacing, Width);
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
        public  (double[] Length, double[] Angle) Edges => EdgeLengthsAngles();
        private (double[] Length, double[] Angle) EdgeLengthsAngles()
        {
	        double[]
		        l = new double[4],
		        a = new double[4]
		        ;
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

        // Calculate shear stress
        public double ShearStress => ShearsStress();
        private double ShearsStress()
        {
	        // Get the dimensions as a vector
	        var lsV = Vector<double>.Build.DenseOfArray(Edges.Length);

	        // Calculate the shear stresses
	        var tau = Forces / (lsV * Width);

	        // Calculate the average stress
	        return Math.Round((-tau[0] + tau[1] - tau[2] + tau[3]) / 4, 2);
        }

        public class Linear
        {
			// Properties
			private Material.Concrete Concrete       { get; }
			private double            Gc             => Concrete.Eci / 2.4;
			public Matrix<double>     LocalStiffness { get; }
			public Matrix<double>     TransMatrix    { get; }

            public Linear(Panel panel, Material.Concrete concrete)
	        {
		        Concrete       = concrete;
		        LocalStiffness = Stiffness(panel);
		        TransMatrix    = TransformationMatrix(panel);
	        }

            // Calculate panel stiffness
            private Matrix<double> Stiffness(Panel panel)
            {
	            // If the panel is rectangular
	            if (RectangularPanel(panel))
		            return RectangularPanelStiffness(panel);

	            // If the panel is not rectangular
	            return NotRectangularPanelStiffness(panel);
            }

            // Calculate local stiffness of a rectangular panel
            private Matrix<double> RectangularPanelStiffness(Panel panel)
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
                return Gc * w * Matrix<double>.Build.DenseOfArray(new [,]
                {
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera},
                        {aOverb, -1, aOverb, -1},
                        {-1, bOvera, -1, bOvera}
                });
            }

            private Matrix<double> NotRectangularPanelStiffness(Panel panel)
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
            private Func<Panel, bool> RectangularPanel = delegate (Panel panel)
            {
                // Calculate the angles between the edges
                double ang2 = panel.Edges.Angle[1] - panel.Edges.Angle[0];
                double ang4 = panel.Edges.Angle[3] - panel.Edges.Angle[2];

                if (ang2 == Constants.PiOver2 && ang4 == Constants.PiOver2)
                    return true;
                else
                    return false;
            };

            // Get transformation matrix
            private Matrix<double> TransformationMatrix(Panel panel)
            {
	            // Get the angles
	            var edges = panel.Edges;
	            var alpha = edges.Angle;

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
	            return Matrix<double>.Build.DenseOfArray(new double[,]
	            {
		            {m1, n1, 0, 0, 0, 0, 0, 0},
		            {0, 0, m2, n2, 0, 0, 0, 0},
		            {0, 0, 0, 0, m3, n3, 0, 0},
		            {0, 0, 0, 0, 0, 0, m4, n4},

	            });
            }
        }

        public class NonLinear
        {
            // Properties
            private Panel Panel { get; }
			private Material.Concrete Concrete { get; }
			private Material.Steel Steel { get; }
            public Matrix<double> BAMatrix { get; set; }
	        public Matrix<double> QPMatrix { get; set; }
	        public Matrix<double> DMatrix { get; set; }
	        public Matrix<double> LocalStiffness { get; set; }

	        public NonLinear(Panel panel, Material.Concrete concrete, Material.Steel steel)
	        {
		        Panel = panel;
		        Concrete = concrete;
		        Steel = steel;
	        }

			// Set the dimensions for nonlinear analysis
            private (double a, double b, double c, double d) PanelDimensions()
	        {
		        // Get X and Y coordinates of the vertices
		        var (x, y) = Panel.VertexCoordinates;

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
		        var (px, py) = Panel.ReinforcementRatio;

		        // Get stringer dimensions
		        var c = Panel.StringerDimensions;

		        // Get X and Y coordinates of the vertices
		        var (x, y) = Panel.VertexCoordinates;

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

        }
    }
}
