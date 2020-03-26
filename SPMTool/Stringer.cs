using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RootFinding;

namespace SPMTool
{
	public class Stringer
	{
		// Stringer properties
		public ObjectId               ObjectId         { get; }
		public int                    Number           { get; }
		public int[]                  Grips            { get; }
		public Point3d[]              PointsConnected  { get; }
		public double                 Length           { get; }
		public double                 Angle            { get; }
		public double                 Width            { get; }
		public double                 Height           { get; }
		public Reinforcement.Stringer Reinforcement    { get; }
		public virtual Matrix<double> InitialStiffness { get; }
        public virtual Matrix<double> LocalStiffness   { get; }
		public virtual Vector<double> Forces           { get; }
		public virtual Vector<double> GlobalForces     { get; }
		public Vector<double>         Displacements    { get; set; }

		// Constructor
		public Stringer(ObjectId stringerObject)
		{
			ObjectId = stringerObject;

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
		}

		// Enum for setting stringer behavior
		public enum Behavior
		{
			Linear    = 1,
			NonLinear = 2
		}

		// Set global indexes from grips
		public int[] Index
		{
			get
			{
				// Initialize the array
				int[] ind = new int[Grips.Length];

				// Get the indexes
				for (int i = 0; i < Grips.Length; i++)
					ind[i] = 2 * Grips[i] - 2;

				return ind;
			}
		}

		// Calculate direction cosines
		public (double cos, double sin) DirectionCosines => Auxiliary.DirectionCosines(Angle);

		// Calculate steel area
		public double SteelArea => Reinforcement.Area;

		// Calculate concrete area
		public double ConcreteArea => Width * Height - SteelArea;

		// Calculate the transformation matrix
		public  Matrix<double>       TransMatrix => transMatrix.Value;
		private Lazy<Matrix<double>> transMatrix => new Lazy<Matrix<double>>(TransformationMatrix);
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
		public Matrix<double>        GlobalStiffness => globalStiffness.Value;
		private Lazy<Matrix<double>> globalStiffness => new Lazy<Matrix<double>>(GloballStiffness);
		public Matrix<double> GloballStiffness()
		{
			var T = TransMatrix;

			return T.Transpose() * LocalStiffness * T;
		}

		// Get stringer displacements from global displacement vector
		public void Displacement(Vector<double> globalDisplacementVector)
		{
			var u = globalDisplacementVector;
			int[] ind = Index;

			// Get the displacements
			var us = Vector<double>.Build.Dense(6);
			for (int i = 0; i < 3; i++)
			{
				// Indexers
				int
					j = ind[i],
					k = 2 * i;

				// Set values
				us[k]     = u[j];
				us[k + 1] = u[j + 1];
			}

			// Set
			Displacements = us;
		}

		// Calculate local displacements
		public Vector<double> LocalDisplacements => TransMatrix * Displacements;

		// Maximum stringer force
		public double MaxForce => Forces.AbsoluteMaximum();

		public class Linear : Stringer
		{
			// Private properties
			private double L     => Length;
			private double Ac    => ConcreteArea;
			private double Ec    { get; }

			// Constructor
			public Linear(ObjectId stringerObject, Material.Concrete concrete) : base(stringerObject)
			{
				Ec = concrete.Eci;
			}

			// Calculate local stiffness
			public override Matrix<double> LocalStiffness => localStiffness.Value;
			private Lazy<Matrix<double>>   localStiffness => new Lazy<Matrix<double>>(Stiffness);
			private Matrix<double> Stiffness()
			{
				// Calculate the constant factor of stiffness
				double EcAOverL = Ec * Ac / L;

				// Calculate the local stiffness matrix
				return EcAOverL * Matrix<double>.Build.DenseOfArray(new double[,]
				{
					{  4, -6,  2 },
					{ -6, 12, -6 },
					{  2, -6,  4 }
				});
			}

			// Calculate stringer forces
			public override Vector<double> Forces => forces.Value;
			private Lazy<Vector<double>>   forces => new Lazy<Vector<double>>(StringerForces);
			private Vector<double> StringerForces()
			{
				// Get the parameters
				var Kl = LocalStiffness;
				var ul = LocalDisplacements;

				// Calculate the vector of normal forces (in kN)
				var fl = 0.001 * Kl * ul;

				// Approximate small values to zero
				fl.CoerceZero(0.000001);

				return fl;
			}
		}

		public class NonLinear : Stringer
		{
			// Public properties
			public (double N1, double N3) GenStresses { get; set; }
			public (double e1, double e3) GenStrains { get; set; }

            // Private parameters
            private Material.Concrete Concrete { get; }

			public NonLinear(ObjectId stringerObject, Material.Concrete concrete) : base(stringerObject)
			{
				Concrete = concrete;
			}

            // Calculate concrete parameters
            private double fc  => Concrete.fcm;
			private double ec  = 0.002;
			private double ecu = 0.0035;
			private double Ec  => 2 * fc / ec;
			private double fcr => 0.33 * Math.Sqrt(fc);
			private double ecr => fcr / Ec;

			// Steel parameters
			private double fy  => Reinforcement.Steel.fy;
			private double ey  => Reinforcement.Steel.ey;
			private double Es  => Reinforcement.Steel.Es;
			private double esu => Reinforcement.Steel.esu;

			// Constants
			private double Ac   => ConcreteArea;
			private double As   => SteelArea;
			private double ps   => As / Ac;
			private double EcAc => Ec * Ac;
			private double EsAs => Es * As;
			private double xi   => EsAs / EcAc;
			private double t1   => EcAc * (1 + xi);

			// Maximum stringer forces
			private double Nc  => -fc * Ac;
			private double Nyr =>  fy * As;
			private double Nt
			{
				get
				{
					double
						Nt1 = Nc * (1 + xi) * (1 + xi),
						Nt2 = Nc - Nyr;

					return Math.Max(Nt1, Nt2);
				}
            }

			// Cracking load
			private double Ncr => fcr * Ac * (1 + xi);
			private double Nr  => Ncr / Math.Sqrt(1 + xi);

			// Number of strain steps
			private int StrainSteps = 10;

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

			// Initial global stiffness
			public override Matrix<double> InitialStiffness
			{
				get
				{
					var T = TransMatrix;
					var B = BMatrix;

					return T.Transpose() * B.Transpose() * InitialFMatrix.Inverse() * B * T;
				}
			}

            // Flexibility and Stiffness matrices
            private Matrix<double> FMatrix { get; set; }

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

					return Vector<double>.Build.DenseOfArray(new []
					{
						-N1, N1 - N3, N3
					});
				}
			}

			// Global stringer forces
			public override Vector<double> GlobalForces => TransMatrix.Transpose() * Forces;

            // Generalized strains and stresses for each iteration
            private (double N1, double N3) IterationGenStrains  { get; set; }
            private (double N1, double N3) IterationGenStresses { get; set; }

			// Forces from gen stresses for each iteration
			public Vector<double> IterationForces
			{
				get
				{
					var (N1, N3) = IterationGenStresses;

					return Vector<double>.Build.DenseOfArray(new []
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
                var genStresses = GenStresses;
                double
	                N1 = genStresses.N1,
	                N3 = genStresses.N3;

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
				Matrix<double> F = Matrix<double>.Build.Dense(2,2);

				// Incremental process to find forces
                for (int i = 1; i <= StrainSteps ; i++ )
				{
					// Calculate generalized strains and F matrix for N1 and N3
					F = StringerGenStrains((N1, N3)).F;

					// Calculate F determinant
					double d = F.Determinant();

					// Calculate increments
					double
						dN1 = ( F[1, 1] * de1 - F[0, 1] * de3) / d,
						dN3 = (-F[0, 1] * de1 + F[0, 0] * de3) / d;

					// Increment forces
					N1 += dN1;
					N3 += dN3;
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

			// Iteration process to find forces
			public void IterateForces()
			{
				// Get the initial forces (from previous load step)
				var genStresses = GenStresses;
				double
					N1 = genStresses.N1,
					N3 = genStresses.N3;

				// Get initial generalized strains (from previous load step)
				//var (e1i, e3i) = GenStrains;

				// Get local displacements
				var ul = LocalDisplacements;

				// Calculate current generalized strains
				double
					e1 = ul[1] - ul[0],
					e3 = ul[2] - ul[1];

				// Get the vectors
				Vector<double>
					N = Vector<double>.Build.DenseOfArray(new [] { N1, N3 }),
					e = Vector<double>.Build.DenseOfArray(new [] { e1, e3 });

				// Initiate values
				double e1i, e3i;
				Matrix<double> F;

				// Iterate the forces
				for ( ; ; )
				{
					// Get generalized strains and flexibility matrix
					((e1i, e3i),F) = StringerGenStrains((N[0], N[1]));
					var ei = Vector<double>.Build.DenseOfArray(new[] { e1i, e3i });

					// Calculate residual
					var er = e - ei;

					// Verify tolerance
					double tol = er.AbsoluteMaximum();

					if (tol < 0.001)
						break;

					// Calculate increment
					var Ninc = F.Solve(er);
					N += Ninc;
				}

				// Verify the values of N1 and N3
				N1 = PlasticForce(N1);
				N3 = PlasticForce(N3);
				double PlasticForce(double Ni)
				{
					double Np;

					// Check the value of N
					if (Ni < Nt)
						Np = Nt;

					else if (Ni > Nyr)
						Np = Nyr;

					else
						Np = Ni;

					return Np;
				}

				// Set values
				FMatrix = F;
				IterationGenStresses = (N1, N3);
				IterationGenStrains  = (e1i, e3i);
            }

            // Calculate the stringer flexibility and generalized strains
            public ((double e1, double e3) genStrains, Matrix<double> F) StringerGenStrains((double N1, double N3) genStresses)
            {
	            double
		            N1 = genStresses.N1,
		            N3 = genStresses.N3;

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

            // Calculate the strain and derivative on a stringer given a force N and the concrete parameters
            public (double e, double de) StringerStrain(double N)
            {
                double e, de;

                // Verify the value of N
                if (N >= 0) // tensioned stringer
                {
                    if (N <= Ncr)
                    {
                        // uncracked
                        e  = N / t1;
                        de = 1 / t1;
                    }

                    else if (N <= Nyr)
                    {
                        // cracked with not yielding steel
                        e  = (N * N - Nr * Nr) / (EsAs * N);
                        de = (N * N + Nr * Nr) / (EsAs * N * N);
                    }

                    else
                    {
                        // yielding steel
                        //double n = Nyr / Nr;
                        e = (Nyr * Nyr - Nr * Nr) / (EsAs * Nyr) + (N - Nyr) / t1;
                        de = 1 / t1;
                    }
                }

                else // compressed stringer (N < 0)
                {
                    // Calculate the yield force
                    //double Nyc = -Nyr + Nc * (2 * -ey / ec - ey / ec * ey / ec);
                    //Console.WriteLine(Nyc);

                    // Verify the value of N
                    if (N > Nt)
                    {
                        // Calculate the strain for steel not yielding
                        double t2 = Math.Sqrt((1 + xi) * (1 + xi) - N / Nc);
                        e = ec * (1 + xi - t2);

                        // Check the strain
                        if (e < -ey)
                        {
                            // Recalculate the strain for steel yielding
                            t2 = Math.Sqrt(1 - (N + Nyr) / Nc);
                            e = ec * (1 - t2);
                        }

                        // Calculate de
                        de = 1 / (EcAc * t2);
                    }

                    else
                    {
                        // Calculate the strain for steel not yielding
                        double t2 = Math.Sqrt((1 + xi) * (1 + xi) - Nt / Nc);
                        e = ec * ((1 + xi) - t2) + (N - Nt) / t1;

                        // Check the strain
                        if (e < -ey)
                        {
                            // Recalculate the strain for steel yielding
                            e = ec * (1 - Math.Sqrt(1 - (Nyr + Nt) / Nc)) + (N - Nt) / t1;
                        }

                        // Calculate de
                        de = 1 / t1;
                    }
                }

                return (e, de);
            }

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
            public  (double ep1, double ep3) PlasticGenStrains
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
            public  (double eput, double epuc) MaxPlasticStrain
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
		}
	}
}

