using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RootFinding;

namespace SPMTool
{
	public abstract class Stringer
	{
		// Enum for setting stringer behavior
		public enum Behavior
		{
			Linear = 1,
			NonLinearClassic = 2,
			NonLinearMC2010
		}

        // Stringer properties
        public ObjectId                ObjectId         { get; }
		public int                     Number           { get; }
		public int[]                   Grips            { get; }
		public Point3d[]               PointsConnected  { get; }
		public double                  Length           { get; }
		public double                  Angle            { get; }
		public double                  Width            { get; }
		public double                  Height           { get; }
		public Material.Concrete       Concrete         { get; }
        public Reinforcement.Stringer  Reinforcement    { get; }
        public abstract Matrix<double> LocalStiffness   { get; }
		public abstract Vector<double> Forces           { get; }
		public Vector<double>          Displacements    { get; set; }

		// Constructor
		public Stringer(ObjectId stringerObject, Material.Concrete concrete = null)
		{
			ObjectId = stringerObject;
			Concrete = concrete;

			// Start a transaction
			using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
			{
				// Read the object as a line
				Line strLine = trans.GetObject(stringerObject, OpenMode.ForRead) as Line;

				// Get the length and angles
				Length = strLine.Length;
				Angle  = strLine.Angle;

				// Calculate midpoint
				var midPt = Auxiliary.MidPoint(strLine.StartPoint, strLine.EndPoint);

				// Get the points
				PointsConnected = new[] { strLine.StartPoint, midPt, strLine.EndPoint };

				// Read the XData and get the necessary data
				ResultBuffer rb = strLine.GetXDataForApplication(AutoCAD.appName);
				TypedValue[] data = rb.AsArray();

				// Get the stringer number
				Number = Convert.ToInt32(data[(int) XData.Stringer.Number].Value);

				// Create the list of grips
				Grips = new []
				{
					Convert.ToInt32(data[(int) XData.Stringer.Grip1].Value),
					Convert.ToInt32(data[(int) XData.Stringer.Grip2].Value),
					Convert.ToInt32(data[(int) XData.Stringer.Grip3].Value)
				};

				// Get geometry
				Width  = Convert.ToDouble(data[(int) XData.Stringer.Width].Value);
				Height = Convert.ToDouble(data[(int) XData.Stringer.Height].Value);

				// Get reinforcement
				int numOfBars = Convert.ToInt32(data[(int) XData.Stringer.NumOfBars].Value);
				double phi = Convert.ToDouble(data[(int) XData.Stringer.BarDiam].Value);

				// Get steel data
				double
					fy = Convert.ToDouble(data[(int) XData.Stringer.Steelfy].Value),
					Es = Convert.ToDouble(data[(int) XData.Stringer.SteelEs].Value);

				// Set steel data
				var steel = new Material.Steel(fy, Es);

				// Set reinforcement
				Reinforcement = new Reinforcement.Stringer(numOfBars, phi, steel);
			}

            // Calculate transformation matrix
            TransMatrix = TransformationMatrix();
		}

		// Set global indexes from grips
		public int[] DoFIndex => Auxiliary.GlobalIndexes(Grips);

		// Calculate direction cosines
		public (double cos, double sin) DirectionCosines => Auxiliary.DirectionCosines(Angle);

		// Calculate steel area
		public double SteelArea => Reinforcement.Area;

		// Calculate concrete area
		public double ConcreteArea => Width * Height - SteelArea;

		// Calculate the transformation matrix
		public  Matrix<double> TransMatrix { get; }
		private Matrix<double> TransformationMatrix()
		{
			// Get the direction cosines
			var (l, m) = DirectionCosines;

			// Obtain the transformation matrix
			return Matrix<double>.Build.DenseOfArray(new double[,]
			{
				{l, m, 0, 0, 0, 0 },
				{0, 0, l, m, 0, 0 },
				{0, 0, 0, 0, l, m }
			});
		}

		// Calculate global stiffness
		public Matrix<double> GlobalStiffness
		{
			get
			{
				var T = TransMatrix;

				return
					T.Transpose() * LocalStiffness * T;
			}
		}

		// Get stringer displacements from global displacement vector
		public void Displacement(Vector<double> globalDisplacementVector)
		{
			var u = globalDisplacementVector;
			int[] ind = DoFIndex;

			// Get the displacements
			var us = Vector<double>.Build.Dense(6);
			for (int i = 0; i < ind.Length; i++)
			{
				// Global index
				int j = ind[i];

				// Set values
				us[i] = u[j];
			}

			// Set
			Displacements = us;
		}

		// Calculate local displacements
		public Vector<double> LocalDisplacements => TransMatrix * Displacements;

        // Global stringer forces
        public Vector<double> GlobalForces => TransMatrix.Transpose() * Forces;

        // Maximum stringer force
        public double MaxForce => Forces.AbsoluteMaximum();

        public class Linear : Stringer
        {
            // Private properties
            private double L  => Length;
            private double Ac => ConcreteArea;
            private double Ec => Concrete.Eci;

            // Constructor
            public Linear(ObjectId stringerObject, Material.Concrete concrete = null) : base(stringerObject, concrete)
            {
            }

            // Calculate local stiffness
            public override Matrix<double> LocalStiffness
            {
                get
                {
                    // Calculate the constant factor of stiffness
                    double EcAOverL = Ec * Ac / L;

                    // Calculate the local stiffness matrix
                    return
                        EcAOverL * Matrix<double>.Build.DenseOfArray(new double[,]
                        {
                            {  4, -6,  2 },
                            { -6, 12, -6 },
                            {  2, -6,  4 }
                        });
                }
            }

			// Calculate stringer forces
			public override Vector<double> Forces
            {
                get
                {
                    // Get the parameters
                    var Kl = LocalStiffness;
                    var ul = LocalDisplacements;

                    // Calculate the vector of normal forces
                    var fl = Kl * ul;

                    // Approximate small values to zero
                    fl.CoerceZero(0.001);

                    return fl;
                }
            }
		}

		public abstract class NonLinear : Stringer
		{
			// Public properties
			public (double N1, double N3) GenStresses { get; set; }
			public (double e1, double e3) GenStrains  { get; set; }

			public NonLinear(ObjectId stringerObject, Material.Concrete concrete) : base(stringerObject, concrete)
			{
			}

            // Concrete parameters
            public abstract double fc  { get; }
            public abstract double ec  { get; }
            public abstract double ecu { get; }
            public abstract double Ec  { get; }
            public abstract double fcr { get; }
            public abstract double ecr { get; }

				// Steel parameters
			private double fy  => Reinforcement.Steel.fy;
			private double ey  => Reinforcement.Steel.ey;
			private double Es  => Reinforcement.Steel.Es;
			private double esu => Reinforcement.Steel.esu;

			// Constants
			private double Ac    => ConcreteArea;
			private double As    => SteelArea;
			private double ps    => As / Ac;
			private double EcAc  => Ec * Ac;
			private double EsAs  => Es * As;
			private double xi    => EsAs / EcAc;
			private double t1    => EcAc + EsAs;
			private double ey_ec => ey / ec;

            // Maximum stringer forces
            private double Nc  => -fc * Ac;
			private double Nyr =>  fy * As;
			public abstract double Nyc { get; }
			public abstract double Nt  { get; }

			// Cracking load
			private double Ncr => fcr * Ac * (1 + xi);
			private double Nr  => Ncr / Math.Sqrt(1 + xi);

			// Number of strain steps
			private int StrainSteps = 5;

			// Get the B matrix
			private Matrix<double> BMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
			{
				{ -1,  1, 0},
				{  0, -1, 1}
			});

			// Get the initial F matrix
			public Matrix<double> InitialFMatrix
			{
				get
				{
					double de = 1 / t1;

					// Calculate the flexibility matrix elements
					double
						de11 = de * Length / 3,
						de12 = de11 / 2,
						de22 = de11;

					// Get the flexibility matrix
					return Matrix<double>.Build.DenseOfArray(new double[,]
					{
						{ de11, de12},
						{ de12, de22}
					});
				}
			}

            // Flexibility and Stiffness matrices
            public Matrix<double> FMatrix { get; set; }

			// Calculate local stiffness
            public override Matrix<double> LocalStiffness
            {
	            get
	            {
		            Matrix<double> F;

		            if (FMatrix != null)
			            F = FMatrix;
		            else
			            F = InitialFMatrix;

					return
						BMatrix.Transpose() * F.Inverse() * BMatrix;
	            }

            }

            // Forces from gen stresses
            public override Vector<double> Forces
			{
				get
				{
					var (N1, N3) = GenStresses;

					return
                        Vector<double>.Build.DenseOfArray(new []
					    {
						    -N1, N1 - N3, N3
					    });
				}
			}

            // Generalized strains and stresses for each iteration
            private (double e1, double e3) IterationGenStrains  { get; set; }
            private (double N1, double N3) IterationGenStresses { get; set; }

			// Forces from gen stresses for each iteration
			public Vector<double> IterationForces
			{
				get
				{
					var (N1, N3) = IterationGenStresses;

					return
                        Vector<double>.Build.DenseOfArray(new []
					    {
						    -N1, N1 - N3, N3
					    });
				}
			}

			// Global stringer forces for each iteration
			public Vector<double> IterationGlobalForces => TransMatrix.Transpose() * IterationForces;

            // Calculate the effective stringer force
            public void StringerForces()
            {
                // Get the initial forces (from previous load step)
                var (N1, N3) = GenStresses;

				// Get initial generalized strains (from previous load step)
				var (e1i, e3i) = GenStrains;

                // Get local displacements
                var ul = LocalDisplacements;

                // Calculate current generalized strains
                double
                    e1 = ul[1] - ul[0],
					e3 = ul[2] - ul[1];

				// Calculate strain increments
				double
					de1 = (e1 - e1i) / StrainSteps,
					de3 = (e3 - e3i) / StrainSteps;

				// Initiate flexibility matrix
				Matrix<double> F;

				// Calculate generalized strains and F matrix for N1 and N3
				((e1, e3), F) = StringerGenStrains((N1, N3));

                // Incremental process to find forces
                for (int i = 1; i <= StrainSteps ; i++ )
				{
					// Calculate F determinant
					double d = F.Determinant();

					// Calculate increments
					double
						dN1 = ( F[1, 1] * de1 - F[0, 1] * de3) / d,
						dN3 = (-F[0, 1] * de1 + F[0, 0] * de3) / d;

					// Increment forces
					N1 += dN1;
					N3 += dN3;

					// Recalculate generalized strains and F matrix for N1 and N3
					((e1, e3), F) = StringerGenStrains((N1, N3));
                }

                // Verify the values of N1 and N3
                N1 = PlasticForce(N1);
				N3 = PlasticForce(N3);
				double PlasticForce(double N)
                {
	                double Ni;

	                // Check the value of N
	                if (N < Nt)
		                Ni = Nt;

	                else if (N > Nyr)
		                Ni = Nyr;

	                else
		                Ni = N;

	                return Ni;
                }

                // Set values
                FMatrix = F;
				IterationGenStresses = (N1, N3);
				IterationGenStrains  = (e1, e3);
            }

            // Calculate the stringer flexibility and generalized strains
            public ((double e1, double e3) genStrains, Matrix<double> F) StringerGenStrains((double N1, double N3) genStresses)
            {
	            var (N1, N3) = genStresses;

	            // Calculate the approximated strains
				var (eps1, de1) = StringerStrain(N1);
				var (eps2, de2) = StringerStrain((2 * N1 + N3) / 3);
				var (eps3, de3) = StringerStrain((N1  + 2 * N3) / 3);
				var (eps4, de4) = StringerStrain(N3);

				// Calculate approximated generalized strains
				double
					e1 = Length * (3 * eps1 + 6 * eps2 + 3 * eps3) / 24,
					e3 = Length * (3 * eps2 + 6 * eps3 + 3 * eps4) / 24;

                // Calculate the flexibility matrix elements
                double
                    de11 = Length * (3 * de1 + 4 * de2 + de3) / 24,
					de12 = Length * (de2 + de3) / 12,
					de22 = Length * (de2 + 4 * de3 + 3 * de4) / 24;

				// Get the flexibility matrix
				var F = Matrix<double>.Build.DenseOfArray(new [,]
				{
					{ de11, de12},
					{ de12, de22}
				});

				return ((e1, e3), F);
            }

            // Abstract method to calculate strain
            public abstract (double e, double de) StringerStrain(double N);

			// Set stringer results (after reached convergence)
			public void Results()
			{
				// Get the values
				var genStresses = IterationGenStresses;
				var genStrains  = IterationGenStrains;
				
				// Set the final values
				GenStresses = genStresses;
				GenStrains  = genStrains;
			}

            // Calculate the total plastic generalized strain in a stringer
            public (double ep1, double ep3) PlasticGenStrains
            {
	            get
	            {
		            // Get generalized strains
		            var (e1, e3) = GenStrains;

		            // Calculate plastic strains
		            double PlasticStrain(double e)
		            {
			            // Initialize the plastic strain
			            double ep = 0;

			            // Case of tension
			            if (e > ey)
				            ep = Length / 8 * (e - ey);

			            // Case of compression
			            if (e < ec)
				            ep = Length / 8 * (e - ec);

			            return ep;
		            }

		            double
			            ep1 = PlasticStrain(e1),
			            ep3 = PlasticStrain(e3);

		            return  (ep1, ep3);
	            }
            }

            // Calculate the maximum plastic strain in a stringer for tension and compression
            public (double eput, double epuc) MaxPlasticStrain
            {
	            get
	            {
		            // Calculate the maximum plastic strain for tension
		            double eput = 0.3 * esu * Length;

		            // Calculate the maximum plastic strain for compression
		            double et   = Math.Max(ec, -ey);
		            double a    = Math.Min(Width, Height);
		            double epuc = (ecu - et) * a;

		            // Return a tuple in order Tension || Compression
		            return (eput, epuc);
	            }
            }

			// Classic SPM model
			public class Classic : NonLinear
			{
				public Classic(ObjectId stringerObject, Material.Concrete concrete) : base(stringerObject, concrete)
				{
				}

				// Calculate concrete parameters
				public override double fc  => Concrete.fcm;
				public override double ec  => -0.002;
				public override double ecu => -0.0035;
				public override double Ec  => -2 * fc / ec;
				public override double fcr => 0.33 * Math.Sqrt(fc);
				public override double ecr => fcr / Ec;

                // Maximum stringer forces
                public override double Nyc => -Nyr + Nc * (-2 * ey_ec - (-ey_ec) * (-ey_ec));
                public override double Nt
                {
	                get
	                {
		                double
			                Nt1 = Nc * (1 + xi) * (1 + xi),
			                Nt2 = Nc - Nyr;

		                return
			                Math.Max(Nt1, Nt2);
	                }
                }

                // Calculate the strain and derivative on a stringer given a force N and the concrete parameters
                public override (double e, double de) StringerStrain(double N)
                {
	                // Verify the value of N
	                // Tensioned stringer
	                if (N >= 0)
	                {
		                if (N <= Ncr)
			                return
				                Uncracked(N);

		                if (N <= Nyr)
			                return
				                Cracked(N);

		                return
			                YieldingSteel(N);
	                }

	                // Compressed stringer
	                if (N > Nt)
		                return
			                ConcreteNotCrushed(N);

	                return
		                ConcreteCrushing(N);
                }

                // Tension Cases
                // Case T.1: Uncracked
                private (double e, double de) Uncracked(double N)
                {
                    double
                        e  = N / t1,
                        de = 1 / t1;

                    return
                        (e, de);
                }

                // Case T.2: Cracked with not yielding steel
                private (double e, double de) Cracked(double N)
                {
                    double
                        e  = (N * N - Nr * Nr) / (EsAs * N),
                        de = (N * N + Nr * Nr) / (EsAs * N * N);

                    return
                        (e, de);
                }

                // Case T.3: Cracked with yielding steel
                private (double e, double de) YieldingSteel(double N)
                {

                    double
                        e = (Nyr * Nyr - Nr * Nr) / (EsAs * Nyr) + (N - Nyr) / t1,
                        de = 1 / t1;

                    return
                        (e, de);
                }


                // Compression Cases
                // Case C.1: concrete not crushed
                private (double e, double de) ConcreteNotCrushed(double N)
                {
                    // Calculate the strain for steel not yielding
                    double
                        t2 = Math.Sqrt((1 + xi) * (1 + xi) - N / Nc),
                        e = ec * (1 + xi - t2);

                    // Check the strain
                    if (e < -ey)
                    {
                        // Recalculate the strain for steel yielding
                        t2 = Math.Sqrt(1 - (N + Nyr) / Nc);
                        e = ec * (1 - t2);
                    }

                    // Calculate de
                    double de = 1 / (EcAc * t2);

                    return
                        (e, de);
                }

                // Case C.2: Concrete crushing
                private (double e, double de) ConcreteCrushing(double N)
                {
                    // Calculate the strain for steel not yielding
                    double
                        t2 = Math.Sqrt((1 + xi) * (1 + xi) - Nt / Nc),
                        e = ec * ((1 + xi) - t2) + (N - Nt) / t1;

                    // Check the strain
                    if (e < -ey)
                    {
                        // Recalculate the strain for steel yielding
                        e = ec * (1 - Math.Sqrt(1 - (Nyr + Nt) / Nc)) + (N - Nt) / t1;
                    }

                    // Calculate de
                    double de = 1 / t1;

                    return
                        (e, de);
                }
            }

            // MC2010 model for concrete
            public class MC2010 : NonLinear
            {
	            public MC2010(ObjectId stringerObject, Material.Concrete concrete) : base(stringerObject, concrete)
	            {
	            }

                // Get concrete parameters
                public override double fc     => Concrete.fcm;
                public override double ec     => Concrete.ec1;
                public override double ecu    => Concrete.eclim;
                public override double Ec     => Concrete.Eci;
                public override double fcr    => Concrete.fctm;
                public override double ecr    => Concrete.ecr;
                public double          k      => Concrete.k;
                private double         beta   =  0.6;
                private double         sigSr  => fcr * (1 + xi) / ps;

                // Calculate the yield force on compression
                public override double Nyc => -Nyr + Nc * (-k * ey_ec - ey_ec * ey_ec) / (1 - (k - 2) * ey_ec);

                // Calculate limit force on compression
                private double NlimC => EsAs * ec + Nc;
				private double NlimS => -Nyr + Nc;
				public override double Nt
				{
					get
					{
						if (-ey < ec)
							return
								NlimC;

						return
							NlimS;
					}
                }

                // Calculate the strain and derivative on a stringer given a force N and the concrete parameters
                public override (double e, double de) StringerStrain(double N)
                {
	                // Verify the value of N
	                if (N >= 0) // tensioned stringer
	                {
		                if (N < Ncr)
			                return
				                Uncracked(N);

		                if (N <= Nyr)
			                return
				                Cracked(N);

		                return
			                YieldingSteel(N);
	                }

	                // Verify if steel yields before concrete crushing
	                if (-ey <= ec)
	                {
		                // Steel doesn't yield
		                if (N > NlimC)
			                return
				                SteelNotYielding(N);

		                // Else, concrete crushes
		                return
			                ConcreteCrushing(N);
	                }

	                // Else, steel yields first
	                if (N >= Nyc)
		                return
			                SteelNotYielding(N);

	                if (N >= NlimS)
		                return
			                SteelYielding(N);

	                return
		                SteelYieldingConcreteCrushed(N);
                }

                // Tension Cases
                // Case T.1: Uncracked
                private (double e, double de) Uncracked(double N)
                {
                    double
                        e  = N / t1,
                        de = 1 / t1;

                    return
                        (e, de);
                }

                // Case T.2: Cracked with not yielding steel
                private (double e, double de) Cracked(double N)
                {
                    double
                        e  = (N / As - beta * sigSr) / Es,
                        de = 1 / EsAs;

                    return
                        (e, de);
                }

                // Case T.3: Cracked with yielding steel
                private (double e, double de) YieldingSteel(double N)
                {

                    double
                        e = (Nyr / As - beta * sigSr) / Es + (N - Nyr) / t1,
                        de = 1 / t1;

                    return
                        (e, de);
                }


                // Compression Cases
                // Case C.1: steel not yielding
                private (double e, double de) SteelNotYielding(double N)
                {
                    double
                        k1     = (Nc / ec - EsAs * (k - 2)) / ec,
                        k2     = (N * (k - 2) - Nc * k) / ec - EsAs,
                        dk2    = (k - 2) / ec,
                        rdelta = Math.Sqrt(k2 * k2 - 4 * k1 * N),

                        // Calculate e and de
                        e  = 0.5 * (-k2 + rdelta) / k1,
                        de = 0.5 * (dk2 * (-k2 + rdelta) + 2 * k1) / (k1 * rdelta);

                    return
                        (e, de);
                }

                // Case C.2: Concrete crushing and steel is not yielding
                private (double e, double de) ConcreteCrushing(double N)
                {
                    double
                        k1     = (Nc / ec - EsAs * (k - 2)) / ec,
                        k2     = (NlimC * (k - 2) - Nc * k) / ec - EsAs,
                        rdelta = Math.Sqrt(k2 * k2 - 4 * k1 * NlimC),

                        // Calculate e and de
                        e  = 0.5 * (-k2 + rdelta) / k1 + (N - NlimC) / t1,
                        de = 1 / t1;

                    return
                        (e, de);
                }

                // Case C.3: steel is yielding and concrete is not crushed
                private (double e, double de) SteelYielding(double N)
                {
                    double
                        k3     = Nc / (ec * ec),
                        k4     = ((Nyr + N) * (k - 2) - Nc * k) / ec,
                        k5     = Nyr + N,
                        dk4    = (k - 2) / ec,
                        rdelta = Math.Sqrt(k4 * k4 - 4 * k3 * k5),

                        // Calculate e and de
                        e  = 0.5 * (-k4 + rdelta) / k3,
                        de = 0.5 * (dk4 * (-k4 + rdelta) + 2 * k3) / (k3 * rdelta);

                    return
                        (e, de);
                }

                // Case C.4: steel is yielding and concrete is crushed
                private (double e, double de) SteelYieldingConcreteCrushed(double N)
                {
                    double
                        k3    = Nc / (ec * ec),
                        k4    = ((Nyr + NlimS) * (k - 2) - Nc * k) / ec,
                        k5    = Nyr + NlimS,
                        delta = Math.Sqrt(k4 * k4 - 4 * k3 * k5),

                        // Calculate e and de
                        e = 0.5 * (-k4 + delta) / k3 + (N - NlimS) / t1,
                        de = 1 / t1;

                    return
                        (e, de);
                }

            }
        }
	}
}

