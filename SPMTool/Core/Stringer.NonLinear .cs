using System;
using Autodesk.AutoCAD.DatabaseServices;
using Material;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RootFinding;
using Concrete           = Material.Concrete.Uniaxial;
using ConcreteParameters = Material.Concrete.Parameters;
using Reinforcement      = Material.Reinforcement.Uniaxial;
using Behavior           = Material.Concrete.ModelBehavior;


namespace SPMTool.Core
{
	public partial class Stringer
	{
		public class NonLinear : Stringer
		{
			// Public properties
			public (double N1, double N3)  GenStresses { get; set; }
			public (double e1, double e3)  GenStrains  { get; set; }
			public  Matrix<double>         FMatrix     { get; set; }
			private IntegrationPoint[]     IntPoints   { get; }
			private StressStrainRelations  Relations   { get; }

			// Generalized strains and stresses for each iteration
			private (double e1, double e3) IterationGenStrains  { get; set; }
			private (double N1, double N3) IterationGenStresses { get; set; }

            public NonLinear(ObjectId stringerObject, ConcreteParameters concreteParameters, Behavior concreteBehavior = Behavior.MCFT) : base(stringerObject, concreteParameters, concreteBehavior)
			{
				// Initiate F matrix
				FMatrix = InitialFMatrix();

				// Initiate integration points
				IntPoints = new []
				{
					new IntegrationPoint(Concrete.ecr, Steel.YieldStrain),
					new IntegrationPoint(Concrete.ecr, Steel.YieldStrain),
					new IntegrationPoint(Concrete.ecr, Steel.YieldStrain),
					new IntegrationPoint(Concrete.ecr, Steel.YieldStrain)
				};

				// Get the relations
				Relations = GetRelations(concreteBehavior);
			}

			// Get steel
            private Steel Steel => Reinforcement.Steel;

            // Implementation to verify cracked or yielded state on each integration point
            private struct IntegrationPoint
            {
	            public bool Cracked  { get; set; }
				public bool Yielding { get; set; }

				private double ecr { get; }
				private double ey  { get; }

				public bool Uncracked             => !Cracked && !Yielding;
				public bool CrackedAndYielding    =>  Cracked &&  Yielding;
				public bool CrackedAndNotYielding =>  Cracked && !Yielding;

				public IntegrationPoint(double ecr, double ey)
				{
					this.ecr = ecr;
					this.ey  = ey;
					Cracked  = false;
					Yielding = false;
				}

				// Verify if stringer is cracked
				public void VerifyCracked(double strain)
				{
					if (!Cracked && strain >= ecr)
						Cracked = true;
				}

				// Verify if steel is yielding
				public void VerifyYielding(double strain)
				{
					if (!Yielding && Math.Abs(strain) >= ey)
						Yielding = true;
				}
            }

			// Get the B matrix
			private readonly Matrix<double> BMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
			{
				{ -1,  1, 0},
				{  0, -1, 1}
			});

			// Calculate local stiffness
			public override Matrix<double> LocalStiffness => BMatrix.Transpose() * FMatrix.Inverse() * BMatrix;

			// Forces from gen stresses
			public override Vector<double> Forces
			{
				get
				{
					var (N1, N3) = GenStresses;

					return
						Vector<double>.Build.DenseOfArray(new[]
						{
							-N1, N1 - N3, N3
						});
				}
			}

			// Forces from gen stresses for each iteration
			public Vector<double> IterationForces
			{
				get
				{
					var (N1, N3) = IterationGenStresses;

					return
						Vector<double>.Build.DenseOfArray(new[]
						{
							-N1, N1 - N3, N3
						});
				}
			}

			// Global Stringer forces for each iteration
			public Vector<double> IterationGlobalForces => TransMatrix.Transpose() * IterationForces;

			// Calculate the total plastic generalized strain in a Stringer
			public (double ep1, double ep3) PlasticGenStrains
			{
				get
				{
					// Get generalized strains
					var (e1, e3) = GenStrains;

					double
						ep1 = PlasticStrain(e1),
						ep3 = PlasticStrain(e3);

					return (ep1, ep3);
				}
			}

			// Calculate the maximum plastic strain in a Stringer for tension and compression
			public (double eput, double epuc) MaxPlasticStrain
			{
				get
				{
					// Calculate the maximum plastic strain for tension
					double
						ey = Steel.YieldStrain,
						esu = Steel.UltimateStrain,
						ec = Concrete.ec,
						ecu = Concrete.ecu,
						eput = 0.3 * esu * Length;

					// Calculate the maximum plastic strain for compression
					double et = Math.Max(ec, -ey);
					double a = Math.Min(Width, Height);
					double epuc = (ecu - et) * a;

					// Return a tuple in order Tension || Compression
					return (eput, epuc);
				}
			}

			// Get the relations
			private StressStrainRelations GetRelations(Behavior concreteBehavior)
			{
				switch (concreteBehavior)
				{
					case Behavior.MCFT:
						return
							new StressStrainRelations.MCFT(Concrete, Reinforcement);

					case Behavior.DSFM:
						throw new NotImplementedException(); //Implement DSFM
				}

				return null;
			}

            // Get the initial F matrix
            public Matrix<double> InitialFMatrix()
			{
				double
					t1 = Concrete.Stiffness + Reinforcement.Stiffness, 
					de = 1 / t1;

				// Calculate the flexibility matrix elements
				double
					de11 = de * Length / 3,
					de12 = de11 / 2,
					de22 = de11;

				// Get the flexibility matrix
				return Matrix<double>.Build.DenseOfArray(new [,]
				{
					{ de11, de12},
					{ de12, de22}
				});
			}

            // Calculate the effective Stringer force
            public override void Analysis(Vector<double> globalDisplacements = null, int numStrainSteps = 5)
            {
				// Set displacements
				if (globalDisplacements != null)
					SetDisplacements(globalDisplacements);

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
					de1 = (e1 - e1i) / numStrainSteps,
					de3 = (e3 - e3i) / numStrainSteps;

				// Initiate flexibility matrix
				Matrix<double> F;

				// Calculate generalized strains and F matrix for N1 and N3
				((e1, e3), F) = StringerGenStrains((N1, N3));

                // Incremental process to find forces
                for (int i = 1; i <= numStrainSteps ; i++ )
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

                // Set values
                FMatrix              = F;
				IterationGenStresses = (N1, N3); 
				IterationGenStrains  = (e1, e3);
            }

			// Calculate plastic force
            private double PlasticForce(double N)
            {
	            double
		            Nt  = Relations.Nt,
		            Nyr = Reinforcement.YieldForce;

	            // Check the value of N
	            if (N < Nt)
		            return Nt;

	            if (N > Nyr)
		            return Nyr;

	            return N;
            }

            // Calculate the Stringer flexibility and generalized strains
            public ((double e1, double e3) genStrains, Matrix<double> F) StringerGenStrains((double N1, double N3) genStresses)
            {
	            var (N1, N3) = genStresses;
	            var N = new[]
	            {
		            N1, (2 * N1 + N3) / 3, (N1  + 2 * N3) / 3, N3
	            };

				var e  = new double[4];
				var de = new double[4];

				for (int i = 0; i < N.Length; i++)
				{
					(e[i], de[i]) = Relations.StringerStrain(N[i], IntPoints[i]);
                }

				// Calculate approximated generalized strains
				double
					e1 = Length * (3 * e[0] + 6 * e[1] + 3 * e[2]) / 24,
					e3 = Length * (3 * e[1] + 6 * e[2] + 3 * e[3]) / 24;

                // Calculate the flexibility matrix elements
                double
                    de11 = Length * (3 * de[0] + 4 * de[1] + de[2]) / 24,
					de12 = Length * (de[1] + de[2]) / 12,
					de22 = Length * (de[1] + 4 * de[2] + 3 * de[3]) / 24;

				// Get the flexibility matrix
				var F = Matrix<double>.Build.DenseOfArray(new [,]
				{
					{ de11, de12},
					{ de12, de22}
				});

				return ((e1, e3), F);
            }

            // Set Stringer results (after reached convergence)
            public void Results()
            {
                // Get the values
                var genStresses = IterationGenStresses;
                var genStrains = IterationGenStrains;

                // Set the final values
                GenStresses = genStresses;
                GenStrains = genStrains;
            }

            // Calculate plastic strains
            private double PlasticStrain(double strain)
            {
	            // Initialize the plastic strain
	            double
		            ep = 0,
		            ey = Steel.YieldStrain,
		            ec = Concrete.ec;

	            // Case of tension
	            if (strain > ey)
		            ep = Length / 8 * (strain - ey);

	            // Case of compression
	            if (strain < ec)
		            ep = Length / 8 * (strain - ec);

	            return ep;
            }

			// MCFT model
			private abstract class StressStrainRelations
			{
				private Concrete      Concrete      { get; }
				private Reinforcement Reinforcement { get; }
				private Steel         Steel         => Reinforcement.Steel;

				public StressStrainRelations(Concrete concrete, Reinforcement reinforcement)
				{
					Concrete      = concrete;
					Reinforcement = reinforcement;
				}

				// Constants
				private double EcAc  => Concrete.Stiffness;
				private double EsAs  => Reinforcement.Stiffness;
				private double xi    => EsAs / EcAc;
				private double t1    => EcAc + EsAs;
				private double ey_ec => Steel.YieldStrain / Concrete.ec;

				// Maximum Stringer forces
				private double Nc  => Concrete.MaxForce;
				private double Nyr => Reinforcement.YieldForce;
				public  double Nyc => -Nyr + Nc * (-2 * ey_ec - (-ey_ec) * (-ey_ec));
				public  double Nt
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

                // Cracking load
                private double Ncr => Concrete.fcr * Concrete.Area * (1 + xi);
				private double Nr  => Ncr / Math.Sqrt(1 + xi);

				public abstract (double e, double de) StringerStrain(double N, IntegrationPoint intPoint);

				public class MCFT : StressStrainRelations
				{
					public MCFT(Concrete concrete, Reinforcement reinforcement) : base(concrete, reinforcement)
					{
					}

					// Calculate the strain and derivative on a Stringer given a force N and the concrete parameters
					public override (double e, double de) StringerStrain(double N, IntegrationPoint intPoint)
					{
						(double e, double de) result = (0, 1 / t1);

						// Verify the value of N
						// Tensioned Stringer
						if (N > 0)
						{
							// Calculate uncracked
							if (intPoint.Uncracked)
							{
								result = Uncracked(N);

								// Verify if concrete is cracked
								intPoint.VerifyCracked(result.e);
							}

							if (intPoint.CrackedAndNotYielding)
							{
								// Calculate cracked
								var cracked = Cracked(N);

								if (cracked.HasValue)
								{
									result = cracked.Value;

									// Verify if reinforcement yielded
									intPoint.VerifyYielding(result.e);
								}
								else
								{
									// Steel yielded
									intPoint.Yielding = true;
								}
							}

							if (intPoint.CrackedAndYielding)
							{
								// Steel is yielding
								result = YieldingSteel(N);
							}
						}

						else if (N < 0)
						{
							// Compressed Stringer
							if (N > Nt)
								result = ConcreteNotCrushed(N);

							else
								result = ConcreteCrushing(N);
						}

						return result;
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

					// Case T.2: Cracked with yielding or not yielding steel
					private (double e, double de)? Cracked(double N) => Solver(N, Concrete.ecr, Steel.YieldStrain);

					// Case T.3: Cracked with yielding steel
					private (double e, double de) YieldingSteel(double N)
					{
						double
							ey = Steel.YieldStrain,
							e  = ey + (N - Nyr) / t1,
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
							ec = Concrete.ec,
							ey = Steel.YieldStrain,
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
							ec = Concrete.ec,
							ey = Steel.YieldStrain,
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

					// Compressed case
					private (double e, double de)? Compressed(double N) => Solver(N, Concrete.ecu, 0);

					// Solver to find strain given force
					private (double e, double de)? Solver(double N, double lowerBound, double upperBound)
					{
						// Iterate to find strain
						(double e, double de)? result = null;
						double? e = null;

						try
						{
							e = FindRoots.OfFunction(eps => N - Force(eps), lowerBound, upperBound);
						}
						catch
						{
						}
						finally
						{
							if (e.HasValue)
							{
								// Calculate derivative of function
								double
									dN = Differentiate.FirstDerivative(Force, e.Value),
									de = 1 / dN;

								result = (e.Value, de);
							}
						}

						return result;
					}

					// Calculate cracked force based on strain
					private double Force(double e) => Concrete.CalculateForce(e) + Reinforcement.CalculateForce(e);
				}
			}

            // MC2010 model for concrete
            //        public class MC2010 : NonLinear
            //        {
            //         public MC2010(ObjectId stringerObject, ConcreteParameters concreteParameters) : base(stringerObject, concreteParameters)
            //         {
            //         }

            //            // Get concrete parameters
            //            //public override double fc     => Concrete.fc;
            //            //public override double ec     => Concrete.ec;
            //            //public override double ecu    => Concrete.ecu;
            //            //public override double Ec     => Concrete.Ec;
            //            //public override double fcr    => Concrete.fcr;
            //            //public override double ecr    => Concrete.ecr;
            //            public double          k      => Concrete.Ec / Concrete.Ecs;
            //            private double         beta   =  0.6;
            //            private double         sigSr  => fcr * (1 + xi) / ps;

            //            // Calculate the yield force on compression
            //            public override double Nyc => -Nyr + Nc * (-k * ey_ec - ey_ec * ey_ec) / (1 - (k - 2) * ey_ec);

            //            // Calculate limit force on compression
            //            private double NlimC => EsAs * ec + Nc;
            //private double NlimS => -Nyr + Nc;
            //public override double Nt
            //{
            //	get
            //	{
            //		if (-ey < ec)
            //			return
            //				NlimC;

            //		return
            //			NlimS;
            //	}
            //            }

            //            // Calculate the strain and derivative on a Stringer given a force N and the concrete parameters
            //            public override (double e, double de) StringerStrain(double N)
            //            {
            //             // Verify the value of N
            //             if (N >= 0) // tensioned Stringer
            //             {
            //              if (N < Ncr)
            //               return
            //                Uncracked(N);

            //              if (N <= Nyr)
            //               return
            //                Cracked(N);

            //              return
            //               YieldingSteel(N);
            //             }

            //             // Verify if steel yields before concrete crushing
            //             if (-ey <= ec)
            //             {
            //              // Steel doesn't yield
            //              if (N > NlimC)
            //               return
            //                SteelNotYielding(N);

            //              // Else, concrete crushes
            //              return
            //               ConcreteCrushing(N);
            //             }

            //             // Else, steel yields first
            //             if (N >= Nyc)
            //              return
            //               SteelNotYielding(N);

            //             if (N >= NlimS)
            //              return
            //               SteelYielding(N);

            //             return
            //              SteelYieldingConcreteCrushed(N);
            //            }

            //            // Tension Cases
            //            // Case T.1: Uncracked
            //            private (double e, double de) Uncracked(double N)
            //            {
            //                double
            //                    e  = N / t1,
            //                    de = 1 / t1;

            //                return
            //                    (e, de);
            //            }

            //            // Case T.2: Cracked with not yielding steel
            //            private (double e, double de) Cracked(double N)
            //            {
            //                double
            //                    e  = (N / As - beta * sigSr) / Es,
            //                    de = 1 / EsAs;

            //                return
            //                    (e, de);
            //            }

            //            // Case T.3: Cracked with yielding steel
            //            private (double e, double de) YieldingSteel(double N)
            //            {

            //                double
            //                    e = (Nyr / As - beta * sigSr) / Es + (N - Nyr) / t1,
            //                    de = 1 / t1;

            //                return
            //                    (e, de);
            //            }


            //            // Compression Cases
            //            // Case C.1: steel not yielding
            //            private (double e, double de) SteelNotYielding(double N)
            //            {
            //                double
            //                    k1     = (Nc / ec - EsAs * (k - 2)) / ec,
            //                    k2     = (N * (k - 2) - Nc * k) / ec - EsAs,
            //                    dk2    = (k - 2) / ec,
            //                    rdelta = Math.Sqrt(k2 * k2 - 4 * k1 * N),

            //                    // Calculate e and de
            //                    e  = 0.5 * (-k2 + rdelta) / k1,
            //                    de = 0.5 * (dk2 * (-k2 + rdelta) + 2 * k1) / (k1 * rdelta);

            //                return
            //                    (e, de);
            //            }

            //            // Case C.2: Concrete crushing and steel is not yielding
            //            private (double e, double de) ConcreteCrushing(double N)
            //            {
            //                double
            //                    k1     = (Nc / ec - EsAs * (k - 2)) / ec,
            //                    k2     = (NlimC * (k - 2) - Nc * k) / ec - EsAs,
            //                    rdelta = Math.Sqrt(k2 * k2 - 4 * k1 * NlimC),

            //                    // Calculate e and de
            //                    e  = 0.5 * (-k2 + rdelta) / k1 + (N - NlimC) / t1,
            //                    de = 1 / t1;

            //                return
            //                    (e, de);
            //            }

            //            // Case C.3: steel is yielding and concrete is not crushed
            //            private (double e, double de) SteelYielding(double N)
            //            {
            //                double
            //                    k3     = Nc / (ec * ec),
            //                    k4     = ((Nyr + N) * (k - 2) - Nc * k) / ec,
            //                    k5     = Nyr + N,
            //                    dk4    = (k - 2) / ec,
            //                    rdelta = Math.Sqrt(k4 * k4 - 4 * k3 * k5),

            //                    // Calculate e and de
            //                    e  = 0.5 * (-k4 + rdelta) / k3,
            //                    de = 0.5 * (dk4 * (-k4 + rdelta) + 2 * k3) / (k3 * rdelta);

            //                return
            //                    (e, de);
            //            }

            //            // Case C.4: steel is yielding and concrete is crushed
            //            private (double e, double de) SteelYieldingConcreteCrushed(double N)
            //            {
            //                double
            //                    k3    = Nc / (ec * ec),
            //                    k4    = ((Nyr + NlimS) * (k - 2) - Nc * k) / ec,
            //                    k5    = Nyr + NlimS,
            //                    delta = Math.Sqrt(k4 * k4 - 4 * k3 * k5),

            //                    // Calculate e and de
            //                    e = 0.5 * (-k4 + delta) / k3 + (N - NlimS) / t1,
            //                    de = 1 / t1;

            //                return
            //                    (e, de);
            //            }
            //        }

            // Classic SPM model
            //public class Classic : NonLinear
            //{
            //    public Classic(ObjectId stringerObject, ConcreteParameters concreteParameters) : base(stringerObject, concreteParameters)
            //    {
            //    }

            //    // Calculate concrete parameters
            //    //public override double fc  => Concrete.fc;
            //    //public override double ec  => -0.002;
            //    //public override double ecu => -0.0035;
            //    //public override double Ec  => -2 * fc / ec;
            //    //public override double fcr => 0.33 * Math.Sqrt(fc);
            //    //public override double ecr => fcr / Ec;

            //    // Maximum Stringer forces
            //    public override double Nyc => -Nyr + Nc * (-2 * ey_ec - (-ey_ec) * (-ey_ec));
            //    public override double Nt
            //    {
            //        get
            //        {
            //            double
            //                Nt1 = Nc * (1 + xi) * (1 + xi),
            //                Nt2 = Nc - Nyr;

            //            return
            //                Math.Max(Nt1, Nt2);
            //        }
            //    }

            //    // Calculate the strain and derivative on a Stringer given a force N and the concrete parameters
            //    public override (double e, double de) StringerStrain(double N, IntegrationPoint intPoint)
            //    {
            //        // Verify the value of N
            //        if (N == 0)
            //            return (0, 1 / t1);

            //        // Tensioned Stringer
            //        if (N >= 0)
            //        {
            //            // Calculate uncracked
            //            var res = Uncracked(N);

            //            // Verify if concrete is cracked
            //            intPoint.VerifyCracked(res.e);


            //            if (intPoint.Uncracked)
            //                return res;

            //            if (intPoint.CrackedAndNotYielding)
            //            {
            //                // Calculate cracked
            //                var cracked = Cracked(N);

            //                // Verify if reinforcement yielded
            //                intPoint.VerifyYielding(cracked.e);

            //                if (intPoint.CrackedAndNotYielding)
            //                    return cracked;
            //            }

            //            // Steel is yielding
            //            return
            //                YieldingSteel(N);
            //        }

            //        // Compressed Stringer
            //        if (N > Nt)
            //            return
            //                ConcreteNotCrushed(N);

            //        return
            //            ConcreteCrushing(N);
            //    }

            //    // Tension Cases
            //    // Case T.1: Uncracked
            //    public (double e, double de) Uncracked(double N)
            //    {
            //        double
            //            e = N / t1,
            //            de = 1 / t1;

            //        return
            //            (e, de);
            //    }

            //    // Case T.2: Cracked with not yielding steel
            //    public (double e, double de) Cracked(double N)
            //    {
            //        double
            //            e = (N * N - Nr * Nr) / (EsAs * N),
            //            de = (N * N + Nr * Nr) / (EsAs * N * N);

            //        return
            //            (e, de);
            //    }

            //    // Case T.3: Cracked with yielding steel
            //    public (double e, double de) YieldingSteel(double N)
            //    {

            //        double
            //            e = (Nyr * Nyr - Nr * Nr) / (EsAs * Nyr) + (N - Nyr) / t1,
            //            de = 1 / t1;

            //        return
            //            (e, de);
            //    }

            //    // Compression Cases
            //    // Case C.1: concrete not crushed
            //    public (double e, double de) ConcreteNotCrushed(double N)
            //    {
            //        // Calculate the strain for steel not yielding
            //        double
            //            ec = Concrete.ec,
            //            ey = Steel.YieldStrain,
            //            t2 = Math.Sqrt((1 + xi) * (1 + xi) - N / Nc),
            //            e = ec * (1 + xi - t2);

            //        // Check the strain
            //        if (e < -ey)
            //        {
            //            // Recalculate the strain for steel yielding
            //            t2 = Math.Sqrt(1 - (N + Nyr) / Nc);
            //            e = ec * (1 - t2);
            //        }

            //        // Calculate de
            //        double de = 1 / (EcAc * t2);

            //        return
            //            (e, de);
            //    }

            //    // Case C.2: Concrete crushing
            //    public (double e, double de) ConcreteCrushing(double N)
            //    {
            //        // Calculate the strain for steel not yielding
            //        double
            //            ec = Concrete.ec,
            //            ey = Steel.YieldStrain,
            //            t2 = Math.Sqrt((1 + xi) * (1 + xi) - Nt / Nc),
            //            e = ec * ((1 + xi) - t2) + (N - Nt) / t1;

            //        // Check the strain
            //        if (e < -ey)
            //        {
            //            // Recalculate the strain for steel yielding
            //            e = ec * (1 - Math.Sqrt(1 - (Nyr + Nt) / Nc)) + (N - Nt) / t1;
            //        }

            //        // Calculate de
            //        double de = 1 / t1;

            //        return
            //            (e, de);
            //    }
            //}

        }
    }
}

