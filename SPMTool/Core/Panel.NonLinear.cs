﻿using System;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.Material;

namespace SPMTool.Core
{
    public abstract partial class Panel
    {
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
					// Verify if analysis was done
		            if (IntegrationPoints[0].ConcreteStiffness == null)
			            return
				            InitialMaterialStiffness;

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
	            double d = 2E-10;

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
