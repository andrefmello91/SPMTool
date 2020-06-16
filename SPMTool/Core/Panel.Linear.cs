using System;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;
using Concrete           = Material.Concrete.Biaxial;
using ConcreteParameters = Material.Concrete.Parameters;
using Reinforcement      = Material.Reinforcement.Biaxial;
using Behavior           = Material.Concrete.Behavior;

namespace SPMTool.Core
{
    public partial class Panel
    {
        public class Linear : Panel
        {
            // Private properties
            private Matrix<double> TransMatrix { get; }
	        
            public Linear(ObjectId panelObject, ConcreteParameters concreteParameters = null, Behavior behavior = null) : base(panelObject, concreteParameters, behavior)
            {
                // Get transformation matrix
                TransMatrix = TransformationMatrix();

                // Calculate panel stiffness
                LocalStiffness = Stiffness();
            }

            // Calculate global stiffness
            public override Matrix<double> GlobalStiffness => TransMatrix.Transpose() * LocalStiffness * TransMatrix;

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

            // Calculate transformation matrix
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
            private Matrix<double> Stiffness()
            {
                // If the panel is rectangular
                if (Rectangular)
                    return
                        RectangularPanelStiffness();

                // If the panel is not rectangular
                return
                    NonRectangularPanelStiffness();
            }

            // Calculate local stiffness of a rectangular panel
            private Matrix<double> RectangularPanelStiffness()
            {
                // Get the dimensions
                double
                    w = Width,
                    a = Edges.Length[0],
                    b = Edges.Length[1];

                // Calculate the parameters of the stiffness matrix
                double
                    a_b = a / b,
                    b_a = b / a;

				// Calculate Gc
				double Gc = Concrete.Ec / 2.4;

                // Calculate the stiffness matrix
                return 
	                Gc * w * Matrix<double>.Build.DenseOfArray(new[,]
	                {
		                {a_b,  -1, a_b,  -1},
		                { -1, b_a,  -1, b_a},
		                {a_b,  -1, a_b,  -1},
		                { -1, b_a,  -1, b_a}
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
                    l1 = Edges.Length[0],
                    l2 = Edges.Length[1],
                    l3 = Edges.Length[2],
                    l4 = Edges.Length[3];

                // Calculate Gc
                double Gc = Concrete.Ec / 2.4;

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
                    kf =  k1 + k2 + k3 + k4,
                    ku = -t1 * k1 + t2 * k2 - t3 * k3 + t4 * k4;

                // Calculate D
                double D = 16 * Gc * w / (kf * ku);

                // Get the vector B
                var B = Vector<double>.Build.DenseOfArray(new double[]
                {
	                -k1 * l1, k2 * l2, -k3 * l3, k4 * l4
                });

                // Get the stiffness matrix
                return 
	                B.ToColumnMatrix() * D * B.ToRowMatrix();
            }

            // Calculate panel forces
            public Vector<double> CalculateForces()
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

			// Calculate forces
			public override void Analysis(Vector<double> globalDisplacements = null)
			{
				// Set displacements
				if (globalDisplacements != null)
					SetDisplacements(globalDisplacements);

				Forces = CalculateForces();
			}
        }
    }
}
