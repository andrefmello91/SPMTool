using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
	public class Stringer
	{
		// Stringer properties
		public ObjectId               ObjectId        { get; }
		public int                    Number          { get; }
		public int[]                  Grips           { get; }
		public Point3d[]              PointsConnected { get; }
		public double                 Length          { get; }
		public double                 Angle           { get; }
		public double                 Width           { get; }
		public double                 Height          { get; }
		public Reinforcement.Stringer Reinforcement   { get; }
		public virtual Matrix<double> LocalStiffness  { get; }
		public virtual Vector<double> Forces          { get; }
		public Vector<double>         Displacements   { get; set; }

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
			double[] dirCos = Auxiliary.DirectionCosines(Angle);
			double
				l = dirCos[0],
				m = dirCos[1];

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

		// Get panel displacements from global displacement vector
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
				us[k] = u[j];
				us[k + 1] = u[j + 1];
			}

			// Set
			Displacements = us;
		}

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
				var T = TransMatrix;
				var us = Displacements;

				// Get the displacements in the direction of the stringer
				var ul = T * us;

				// Calculate the vector of normal forces (in kN)
				var fl = 0.001 * Kl * ul;

				// Approximate small values to zero
				fl.CoerceZero(0.000001);

				return fl;
			}
		}

		public class NonLinear : Stringer
		{

			// Private parameters
			private Material.Concrete Concrete { get; }

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


            public NonLinear(ObjectId stringerObject, Material.Concrete concrete) : base(stringerObject)
			{
				Concrete = concrete;
			}

            // Calculate the strain and derivative on a stringer given a force N and the concrete parameters
            public (double e, double de) StringerStrain(double N)
            {
	            double 
		            e  = 0,
		            de = 0;

                // Verify the value of N
                if (N > 0) // tensioned stringer
                {
                    if (N <= Ncr)
                    {
                        // uncracked
                        e = N / t1;
                        de = 1 / t1;
                    }

                    else if (N <= Nyr)
                    {
                        // cracked with not yielding steel
                        e = (N * N - Nr * Nr) / (EsAs * N);
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

                else if (N < 0) // compressed stringer
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

            // Calculate the effective stringer force
            double StringerForce(double N)
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


            // Calculate the stringer stiffness
            public Matrix<double> StringerStiffness(double N1, double N3)
            {
				// Calculate the approximated strains
				double 
					de1 = StringerStrain(N1).de,
					de2 = StringerStrain(2 / 3 * N1 + N3 / 3).de,
					de3 = StringerStrain(N1 / 3 + 2 / 3 * N3).de,
					de4 = StringerStrain(N3).de;

				// Calculate the flexibility matrix elements
				double 
					de1N1 = Length / 24 * (3 * de1 + 4 * de2 + de3),
					de1N2 = Length / 12 * (de2 + de3),
					de2N2 = Length / 24 * (de2 + 4 * de3 + 3 * de4);

				// Get the flexibility matrix
				var F = Matrix<double>.Build.DenseOfArray(new double[,]
				{
					{ de1N1, de1N2},
					{ de1N2, de2N2}
				});

				// Get the B matrix
				var B = Matrix<double>.Build.DenseOfArray(new double[,]
				{
					{ -1,  1, 0},
					{  0, -1, 1}
				});

				// Calculate local stiffness matrix and return the value
				var Kl = B.Transpose() * F.Inverse() * B;

				return Kl;
			}

			// Calculate the total plastic generalized strain in a stringer
			public double StringerPlasticStrain(double eps)
			{
				// Initialize the plastic strain
				double ep = 0;

				// Case of tension
				if (eps > ey)
					ep = Length / 8 * (eps - ey);

				// Case of compression
				if (eps < ec)
					ep = Length / 8 * (eps - ec);

				return ep;
			}

			// Calculate the maximum plastic strain in a stringer for tension and compression
			private (double eput, double epuc) StringerMaxPlasticStrain()
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

