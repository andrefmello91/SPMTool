using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Interpolation;

[assembly: CommandClass(typeof(SPMTool.Material.Concrete))]
[assembly: CommandClass(typeof(SPMTool.Material.Steel))]

namespace SPMTool
{
	// Material related commands:
	namespace Material
	{
		// Concrete
		public class Concrete
		{
			// Properties
			public  double                  AggregateDiameter { get; }
			public  double                  fc                { get; }
			private double                  alphaE            { get; }
			public (double ec1, double ec2) PrincipalStrains  { get; set; }
			public (double fc1, double fc2) PrincipalStresses { get; set; }
			public double                   ReferenceLength   { get; set; }


            // Read the concrete parameters
            public Concrete(double fc, double aggregateDiameter, double alphaE = 0)
			{
				this.fc = fc;
				AggregateDiameter = aggregateDiameter;
				this.alphaE = alphaE;
			}

			// Verify if concrete was set
			public bool IsSet
			{
				get
				{
					if (fc > 0)
						return true;

					// Else
					return false;
				}
			}

			// Verify if concrete is cracked
			public bool Cracked
			{
				get
				{
					if (PrincipalStrains.ec1 > ecr)
						return true;

					return false;
				}
			}

            // Calculate parameters according to FIB MC2010
            public virtual double fcr
			{
				get
				{
					if (fc <= 50)
						return 0.3 * Math.Pow(fc, 0.66666667);
					//else
					return 2.12 * Math.Log(1 + 0.1 * fc);
				}
			}
			public virtual double Ec  => 21500 * alphaE * Math.Pow(fc / 10, 0.33333333);
			public virtual double ec  => -1.6 / 1000 * Math.Pow(fc / 10, 0.25);
			public double Ec1 => fc / ec;
			public double k   => Ec / Ec1;
			public double ecr => fcr / Ec;
			public virtual double ecu
			{
				get
				{
					// Verify fcm
					if (fc < 50)
						return
							-0.0035;

					if (fc >= 90)
						return
							-0.003;

					// Get classes and ultimate strains
					if (classes.Contains(fc))
					{
						int i = Array.IndexOf(classes, fc);

						return
							ultimateStrain[i];
					}

					// Interpolate values
					var spline = ultStrainSpline.Value;

					return
						spline.Interpolate(fc);
				}
			}

			// Array of high strength concrete classes, C50 to C90 (MC2010)
			private double[] classes =
			{
				50, 55, 60, 70, 80, 90
			};

			// Array of ultimate strains for each concrete class, C50 to C90 (MC2010)
			private double[] ultimateStrain =
			{
				-0.0034, -0.0034, -0.0033, -0.0032, -0.0031, -0.003
			};

			// Interpolation for ultimate strains
			private Lazy<CubicSpline> ultStrainSpline => new Lazy<CubicSpline>(UltimateStrainSpline);
			private CubicSpline UltimateStrainSpline()
			{
				return
					CubicSpline.InterpolateAkimaSorted(classes, ultimateStrain);
			}

			// Calculate concrete stresses
			public virtual double TensileStress(double ec1, Reinforcement.Panel reinforcement = null, (double x, double y) reinforcementAngles = default)
			{
				throw new NotImplementedException();
			}

			public virtual double CompressiveStress((double ec1, double ec2) principalStrains)
			{
				throw new NotImplementedException();
			}

            // Calculate secant module of concrete
            public (double Ec1, double Ec2) SecantModule
			{
				get

				{
					double Ec1, Ec2;

					// Verify strains
					// Get values
					var (ec1, ec2) = PrincipalStrains;
					var (fc1, fc2) = PrincipalStresses;

					if (ec1 == 0 || fc1 == 0)
						Ec1 = Ec;

					else
						Ec1 = fc1 / ec1;

					if (ec2 == 0 || fc2 == 0)
						Ec2 = Ec;

					else
						Ec2 = fc2 / ec2;

					return
						(Ec1, Ec2);
				}
			}

			// Set concrete principal strains
			public void SetStrains((double ec1, double ec2) principalStrains)
			{
				PrincipalStrains = principalStrains;
			}

			// Set concrete stresses given strains
			public void SetStresses((double ec1, double ec2) principalStrains, Reinforcement.Panel reinforcement = null, (double x, double y) reinforcementAngles = default)
			{
				PrincipalStresses = (TensileStress(principalStrains.ec1, reinforcement, reinforcementAngles), CompressiveStress(principalStrains));
			}

			// Set concrete strains and stresses
			public void SetStrainsAndStresses((double ec1, double ec2) principalStrains, Reinforcement.Panel reinforcement = null, (double x, double y) reinforcementAngles = default)
			{
				SetStrains(principalStrains);
				SetStresses(principalStrains, reinforcement, reinforcementAngles);
			}

			// Set tensile stress limited by crack check
			public void SetTensileStress(double fc1)
			{
				// Get compressive stress
				double fc2 = PrincipalStresses.fc2;

				// Set
				PrincipalStresses = (fc1, fc2);
			}

            public class MCFT : Concrete
            {
                public MCFT(double fc, double aggregateDiameter) : base(fc, aggregateDiameter)
                {
                }

                // MCFT parameters
                public override double fcr => 0.33 * Math.Sqrt(fc);
                public override double ec => -0.002;
                public override double ecu => -0.0035;
                public override double Ec => -2 * fc / ec;

                // Principal stresses by classic formulation
                public override double CompressiveStress((double ec1, double ec2) principalStrains)
                {
                    // Get the strains
                    var (ec1, ec2) = principalStrains;

                    // Calculate the maximum concrete compressive stress
                    double
                        f2maxA = -fc / (0.8 - 0.34 * ec1 / ec),
                        f2max = Math.Max(f2maxA, -fc);

                    // Calculate the principal compressive stress in concrete
                    double n = ec2 / ec;

                    return
                        f2max * (2 * n - n * n);
                }

                // Calculate tensile stress in concrete
                public override double TensileStress(double ec1, Reinforcement.Panel reinforcement = null, (double x, double y) reinforcementAngles = default)
                {
                    // Constitutive relation
                    if (ec1 <= ecr) // Not cracked
                        return
                            ec1 * Ec;

                    // Else, cracked
                    // Constitutive relation
                    return
                        fcr / (1 + Math.Sqrt(500 * ec1));
                }
            }

            public class DSFM : Concrete
            {
                public DSFM(double fc, double aggregateDiameter) : base(fc, aggregateDiameter)
                {

                }

                // DSFM parameters
                public override double fcr => 0.65 * Math.Pow(fc, 0.33);
                public override double ec => -0.002;
                public override double ecu => -0.0035;
                public override double Ec => -2 * fc / ec;
                private double Gf = 0.075;
                private double ets => 2 * Gf / (fcr * ReferenceLength);

                public override double TensileStress(double ec1, Reinforcement.Panel reinforcement, (double x, double y) reinforcementAngles)
                {
                    // Initiate fc1
                    double fc1;

                    // Check if concrete is cracked
                    if (ec1 <= ecr) // Not cracked
                        fc1 = Ec * ec1;

                    else // Cracked
                    {
                        // Calculate concrete postcracking stress associated with tension softening
                        double fc1a = fcr * (1 - (ec1 - ecr) / (ets - ecr));

                        // Get reinforcement angles and stresses
                        var (thetaNx, thetaNy) = reinforcementAngles;
                        var (fsx, fsy) = reinforcement.Stresses;
                        var (psx, psy) = reinforcement.Ratio;
                        var (phiX, phiY) = reinforcement.BarDiameter;
                        double fyx = reinforcement.Steel.X.YieldStress;
                        double fyy = reinforcement.Steel.Y.YieldStress;

                        // Calculate coefficient for tension stiffening effect
                        double
                            cosNx = Auxiliary.DirectionCosines(thetaNx).cos,
                            cosNy = Auxiliary.DirectionCosines(thetaNy).cos,
                            m = 0.25 / (psx / phiX * Math.Abs(cosNx) + psy / phiY * Math.Abs(cosNy));

                        // Calculate concrete postcracking stress associated with tension stiffening
                        double fc1b = fcr / (1 + Math.Sqrt(2.2 * m * ec1));

                        // Calculate concrete tensile stress
                        fc1 = Math.Max(fc1a, fc1b);

                        //// Check the maximum value of fc1 that can be transmitted across cracks
                        //double
                        // cos2x = cosNx * cosNx,
                        // cos2y = cosNy * cosNy,
                        // fc1s = psx * (fyx - fsx) * cos2x + psy * (fyy - fsy) * cos2y;

                        //// Choose the minimum value of fc1
                        //fc1 = Math.Min(fc1c, fc1s);
                    }

                    return fc1;
                }

                public override double CompressiveStress((double ec1, double ec2) principalStrains)
                {
                    // Get strains
                    var (ec1, ec2) = principalStrains;

                    // Calculate the coefficients
                    double Cd, betaD;
                    if (ec1 == 0 || ec2 == 0 || -ec1 / ec2 <= 0.28)
                        Cd = 1;

                    else
                        Cd = Math.Max(0.35 * Math.Pow(-ec1 / ec2 - 0.28, 0.8), 1);

                    betaD = Math.Min(1 / (1 + 0.55 * Cd), 1);

                    // Calculate fp and ep
                    double
                        fp = -betaD * fc,
                        ep = betaD * ec;

                    // Calculate parameters of concrete
                    double k;
                    if (ep <= ec2)
                        k = 1;
                    else
                        k = 0.67 - fp / 62;

                    double
                        n = 0.8 - fp / 17,
                        ec2ep = ec2 / ep;

                    // Calculate the principal compressive stress in concrete
                    return
                        fp * n * ec2ep / (n - 1 + Math.Pow(ec2ep, n * k));
                }
            }


            [CommandMethod("SetConcreteParameters")]
			public static void SetConcreteParameters()
			{
				// Definition for the Extended Data
				string xdataStr = "Concrete data";

				// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
				Auxiliary.RegisterApp();

				// Ask the user to input the concrete compressive strength
				PromptDoubleOptions fcOp =
					new PromptDoubleOptions("\nInput the concrete mean compressive strength (fcm) in MPa:")
					{
						AllowZero = false,
						AllowNegative = false
					};

				// Get the result
				PromptDoubleResult fcRes = ACAD.Current.edtr.GetDouble(fcOp);
				if (fcRes.Status == PromptStatus.OK)
				{
					double fc = fcRes.Value;

					// Ask the user choose the type of the agregate
					PromptKeywordOptions agOp = new PromptKeywordOptions("\nChoose the type of the aggregate");
					agOp.Keywords.Add("Basalt");
					agOp.Keywords.Add("Quartzite");
					agOp.Keywords.Add("Limestone");
					agOp.Keywords.Add("Sandstone");
					agOp.Keywords.Default = "Quartzite";
					agOp.AllowNone = false;

					// Get the result
					PromptResult agRes = ACAD.Current.edtr.GetKeywords(agOp);

					if (agRes.Status == PromptStatus.OK)
					{
						string agrgt = agRes.StringResult;

						// Ask the user to input the maximum aggregate diameter
						PromptDoubleOptions phiAgOp =
							new PromptDoubleOptions("\nInput the maximum diameter for concrete aggregate:")
							{
								AllowZero = false,
								AllowNegative = false
							};

						// Get the result
						PromptDoubleResult phiAgRes = ACAD.Current.edtr.GetDouble(phiAgOp);

						if (phiAgRes.Status == PromptStatus.OK)
						{
							double phiAg = phiAgRes.Value;

							// Get the value of aE
							double aE = 1;
							switch (agrgt)
							{
								case "Basalt":
									aE = 1.2;
									break;

								case "Quartzite":
									// aE = 1 already
									break;

								case "Limestone":
									aE = 0.9;
									break;

								case "Sandstone":
									aE = 0.9;
									break;
							}

							// Start a transaction
							using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
							{

								// Get the NOD in the database
								DBDictionary nod =
									(DBDictionary) trans.GetObject(ACAD.Current.db.NamedObjectsDictionaryId,
										OpenMode.ForWrite);

								// Save the variables on the Xrecord
								using (ResultBuffer rb = new ResultBuffer())
								{
									rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName,
										ACAD.Current.appName));     // 0
									rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString,
										xdataStr));           // 1
									rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,
										fc));                        // 2
									rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,
										aE));                        // 3
									rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,
										phiAg));                     // 4

									// Create and add data to an Xrecord
									Xrecord xRec = new Xrecord();
									xRec.Data = rb;

									// Create the entry in the NOD and add to the transaction
									nod.SetAt("ConcreteParams", xRec);
									trans.AddNewlyCreatedDBObject(xRec, true);
								}

								// Save the new object to the database
								trans.Commit();
							}
						}
					}
				}
			}

			[CommandMethod("ViewConcreteParameters")]
			public static void ViewConcreteParameters()
			{
				// Get the values
				var concrete = ReadData();

				// Write the concrete parameters
				if (concrete.IsSet)
				{
					// Get the parameters
					string concmsg =
						"\nConcrete Parameters:\n"                                     +
						"\nfcm = "    + concrete.fc                         + " MPa"  +
						"\nfctm = "   + Math.Round(concrete.fcr, 2)         + " MPa"  +
						"\nEci = "    + Math.Round(concrete.Ec, 2)          + " MPa"  +
						"\nεc1 = "    + Math.Round(1000 * concrete.ec, 2)   + " E-03" +
						"\nεc,lim = " + Math.Round(1000 * concrete.ecu, 2) + " E-03" +
						"\nφ,ag = "   + concrete.AggregateDiameter           + " mm";

					// Display the values returned
					Application.ShowAlertDialog(ACAD.Current.appName + "\n\n" + concmsg);
				}
			}

			// Read the concrete parameters
			public static Concrete ReadData()
			{
				// Start a transaction
				using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
				{
					// Get the NOD in the database
					DBDictionary nod =
						(DBDictionary)trans.GetObject(ACAD.Current.db.NamedObjectsDictionaryId, OpenMode.ForRead);

					// Check if it exists
					if (nod.Contains("ConcreteParams"))
					{
						// Read the concrete Xrecord
						ObjectId concPar = nod.GetAt("ConcreteParams");
						Xrecord concXrec = (Xrecord)trans.GetObject(concPar, OpenMode.ForRead);
						ResultBuffer concRb = concXrec.Data;
						TypedValue[] concData = concRb.AsArray();

						// Get the parameters from XData
						double
							fc = Convert.ToDouble(concData[2].Value),
							alphaE = Convert.ToDouble(concData[3].Value),
							phiAg = Convert.ToDouble(concData[4].Value);

						return
							new Concrete(fc, phiAg, alphaE);
					}
					else
					{
						Application.ShowAlertDialog("Please set concrete parameters.");

						return null;
					}
				}
			}

        }
	}
}