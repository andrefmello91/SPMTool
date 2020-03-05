﻿using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.Statistics;

[assembly: CommandClass(typeof(SPMTool.Analysis))]

namespace SPMTool
{
    public partial class Analysis
    {
     //   public class Stringer:SPMTool.Stringer
     //   {
	    //    public Stringer(ObjectId stringerObject) : base(stringerObject)
	    //    {
	    //    }

     //       // Get the list of continued stringers
     //       public static List<Tuple<int, int>> ContinuedStringers(Stringer[] stringers)
     //       {
     //           // Initialize a Tuple to store the continued stringers
     //           var contStrs = new List<Tuple<int, int>>();
            
     //           // Calculate the parameter of continuity
     //           double par = Math.Sqrt(2) / 2;

     //           // Verify in the list what stringers are continuous
     //           foreach (var str1 in stringers)
     //           {
     //               // Access the number
     //               int num1 = str1.Number;

     //               foreach (var str2 in stringers)
     //               {
     //                   // Access the number
     //                   int num2 = str2.Number;

     //                   // Verify if it's other stringer
     //                   if (num1 != num2)
     //                   {
     //                       // Create a tuple with the minimum stringer number first
     //                       var contStr = Tuple.Create(Math.Min(num1, num2), Math.Max(num1, num2));

     //                       // Verify if it's already on the list
     //                       if (!contStrs.Contains(contStr))
     //                       {
     //                           // Verify the cases
     //                           // Case 1: stringers initiate or end at the same node
     //                           if (str1.Grips[0] == str2.Grips[0] || str1.Grips[2] == str2.Grips[2])
     //                           {
     //                               // Get the direction cosines
     //                               double[]
     //                                   dir1 = Auxiliary.DirectionCosines(str1.Angle),
     //                                   dir2 = Auxiliary.DirectionCosines(str2.Angle);
     //                               double 
     //                                   l1 = dir1[0], 
     //                                   m1 = dir1[1],
     //                                   l2 = dir2[0], 
     //                                   m2 = dir2[1];

     //                               // Calculate the condition of continuity
     //                               double cont = l1 * l2 + m1 * m2;

     //                               // Verify the condition
     //                               if (cont < -par) // continued stringer
     //                               {
     //                                   // Add to the list
     //                                   contStrs.Add(contStr);
     //                               }
     //                           }

     //                           // Case 2: a stringer initiate and the other end at the same node
     //                           if (str1.Grips[0] == str2.Grips[2] || str1.Grips[2] == str2.Grips[0])
     //                           {
     //                               // Get the direction cosines
     //                               double[]
     //                                   dir1 = Auxiliary.DirectionCosines(str1.Angle),
     //                                   dir2 = Auxiliary.DirectionCosines(str2.Angle);
     //                               double
     //                                   l1 = dir1[0],
     //                                   m1 = dir1[1],
     //                                   l2 = dir2[0],
     //                                   m2 = dir2[1];

     //                               // Calculate the condition of continuity
     //                               double cont = l1 * l2 + m1 * m2;

     //                               // Verify the condition
     //                               if (cont > par) // continued stringer
     //                               {
     //                                   // Add to the list
     //                                   contStrs.Add(contStr);
     //                               }
     //                           }
     //                       }
     //                   }
     //               }
     //           }

     //           // Order the list
     //           contStrs = contStrs.OrderBy(str => str.Item2).ThenBy(str => str.Item1).ToList();

     //           // Return the list
     //           return contStrs;
     //       }

     //       // View the continued stringers
     //       //[CommandMethod("ViewContinuedStringers")]
     //       //public static void ViewContinuedStringers()
     //       //{
     //       //    // Update and get the elements collection
     //       //    ObjectIdCollection 
     //       //        nds = Geometry.Node.UpdateNodes(),
     //       //        strs = Geometry.Stringer.UpdateStringers();

     //       //    // Get the parameters
     //       //    var stringers = Parameters(strs);

     //       //    // Get the list of continued stringers
     //       //    var contStrs = Stringer.ContinuedStringers(stringers);

     //       //    // Initialize a message to show
     //       //    string msg = "Continued stringers: ";

     //       //    // If there is none
     //       //    if (contStrs.Count == 0)
     //       //        msg += "None.";

     //       //    // Write all the continued stringers
     //       //    else
     //       //    {
     //       //        foreach (var contStr in contStrs)
     //       //        {
     //       //            msg += contStr.Item1 + " - " + contStr.Item2 + ", ";
     //       //        }
     //       //    }

     //       //    // Write the message in the editor
     //       //    AutoCAD.edtr.WriteMessage(msg);
     //       //}


     ////       public class NonLinear
     ////       {
     ////           // SPMTool default analysis methods
     ////           public class Default
     ////           {
					////// Static parameters of materials
					////private static double fc, fctm, Ec, ec, fy, Es, ey;

					////// Calculate the initial parameters of stringers
					////public static void InitialParameters(Stringer[] stringers, Material.Concrete concrete, Material.Steel steel)
					////{
					////	foreach (var str in stringers)
					////	{
					////		// Calculate transformation matrix
					////		TransformationMatrix(str);

					////		// Get material properties
					////		fc   = concrete.fcm;
					////		fctm = concrete.fctm;
					////		Ec   = concrete.Eci;
					////		ec   = concrete.ec1;
					////		fy   = steel.fy;
					////		Es   = steel.Es;
					////		ey   = steel.ey;

					////		// Calculate EcAc and EsAs
					////		str.EcAc = Ec * str.ConcreteArea;
					////		str.EsAs = Es * str.SteelArea;

					////		// Calculate constants
					////		double
					////			Ac = str.ConcreteArea,
					////			As = str.SteelArea,
					////			ps = str.ps,
					////			xi = str.xi,
					////			xiP1 = 1 + xi,
					////			EcAc = Ec * Ac,
					////			EsAs = Es * As,
					////			t1 = EcAc * xiP1;



     ////                   }
     ////               }

     ////               // Calculate the strain and derivative on a stringer given a force N and the concrete parameters
     ////               static (double e, double de) StringerStrain(Stringer stringer, double N)
     ////               {
     ////                   // Initialize the strain and derivative
     ////                   double
     ////                       e  = 0,
     ////                       de = 0;

					////	// Get material properties
					////	double
					////		fc   = concrete.fcm,
					////		fctm = concrete.fctm,
					////		Ec   = concrete.Eci,
					////		ec   = concrete.ec1,
					////		fy   = steel.fy,
					////		Es   = steel.Es,
					////		ey   = steel.ey;
						
     ////                   // Calculate constants
     ////                   double
					////		Ac   = stringer.ConcreteArea,
					////		As   = stringer.SteelArea,
					////		ps   = stringer.ps,
	    ////                    xi   = ps * Es / Ec,
	    ////                    xiP1 = 1 + xi,
	    ////                    EcAc = Ec * Ac,
	    ////                    EsAs = Es * As,
	    ////                    t1   = EcAc * xiP1;

     ////                   // Calculate maximum forces of concrete and steel
     ////                   double
	    ////                    Nc  = -fc * Ac,
	    ////                    Nyr = fy * As;

     ////                   // Calculate the maximum compressive force
     ////                   double
	    ////                    Nt1 = Nc * xiP1 * xiP1,
	    ////                    Nt2 = Nc - Nyr,
	    ////                    Nt  = Math.Max(Nt1, Nt2);

     ////                   // Verify the value of N
     ////                   if (N > 0) // tensioned stringer
     ////                   {
     ////                       // Calculate critical force for concrete remain uncracked
     ////                       double
     ////                           Ncr = fctm * Ac * xiP1,
     ////                           Nr  = Ncr / Math.Sqrt(xiP1);

     ////                       if (N <= Ncr)
     ////                       {
     ////                           // uncracked
     ////                           e = N / t1;
     ////                           de = 1 / t1;
     ////                       }

     ////                       else if (N <= Nyr)
     ////                       {
     ////                           // cracked with not yielding steel
     ////                           e = (N * N - Nr * Nr) / (EsAs * N);
     ////                           de = (N * N + Nr * Nr) / (EsAs * N * N);
     ////                       }

     ////                       else
     ////                       {
     ////                           // yielding steel
     ////                           //double n = Nyr / Nr;
     ////                           e = (Nyr * Nyr - Nr * Nr) / (EsAs * Nyr) + (N - Nyr) / t1;
     ////                           de = 1 / t1;
     ////                       }
     ////                   }

     ////                   else if (N < 0) // compressed stringer
     ////                   {
     ////                       // Calculate the yield force
     ////                       //double Nyc = -Nyr + Nc * (2 * -ey / ec - ey / ec * ey / ec);
     ////                       //Console.WriteLine(Nyc);

     ////                       // Verify the value of N
     ////                       if (N > Nt)
     ////                       {
     ////                           // Calculate the strain for steel not yielding
     ////                           double t2 = Math.Sqrt(xiP1 * xiP1 - N / Nc);
     ////                           e = ec * (xiP1 - t2);

     ////                           // Check the strain
     ////                           if (e < -ey)
     ////                           {
     ////                               // Recalculate the strain for steel yielding
     ////                               t2 = Math.Sqrt(1 - (N + Nyr) / Nc);
     ////                               e = ec * (1 - t2);
     ////                           }

     ////                           // Calculate de
     ////                           de = 1 / (EcAc * t2);
     ////                       }

     ////                       else
     ////                       {
     ////                           // Concrete crushed
     ////                           //double n = N / Nt;

     ////                           // Calculate the strain for steel not yielding
     ////                           double t2 = Math.Sqrt(xiP1 * xiP1 - Nt / Nc);
     ////                           e = ec * (xiP1 - t2) + (N - Nt) / t1;

     ////                           // Check the strain
     ////                           if (e < -ey)
     ////                           {
     ////                               // Recalculate the strain for steel yielding
     ////                               e = ec * (1 - Math.Sqrt(1 - (Nyr + Nt) / Nc)) + (N - Nt) / t1;
     ////                           }

     ////                           // Calculate de
     ////                           de = 1 / t1;
     ////                       }
     ////                   }

     ////                   return (e, de);
     ////               }

     ////               // Calculate the effective stringer force
     ////               double StringerForce(double N)
     ////               {
	    ////                double Ni;

	    ////                // Check the value of N
	    ////                if (N < Nt)
		   ////                 Ni = Nt;

	    ////                else if (N > Nyr)
		   ////                 Ni = Nyr;

	    ////                else
		   ////                 Ni = N;

	    ////                return Ni;
     ////               }

     ////               // Calculate the strain on a stringer given a force N and the concrete parameters
     ////               //public static double StringerStrain(double N, double Ac, double As, List<double> concParams, List<double> steelParams)
     ////               //{
     ////               //    // Get the parameters
     ////               //    concParams = Material.Concrete.ConcreteParams();
     ////               //    steelParams = Material.Steel.SteelParams();

     ////               //    // Initialize the strain
     ////               //    double e = 0;

     ////               //    if (concParams != null)
     ////               //    {
     ////               //        // Get the values for concrete
     ////               //        double fcm = concParams[0],
     ////               //            fcr = concParams[1],
     ////               //            Eci = concParams[2],
     ////               //            Ec1 = concParams[3],
     ////               //            ec1 = concParams[4],
     ////               //            k = concParams[5];

     ////               //        // Get the values for steel
     ////               //        double fy = steelParams[0],
     ////               //            Es = steelParams[1],
     ////               //            ey = steelParams[2];

     ////               //        // Calculate ps and xi
     ////               //        double ps = As / Ac,
     ////               //            xi = ps * Es / Eci;

     ////               //        // Calculate maximum forces of concrete and steel
     ////               //        double Ncm = -fcm * Ac,
     ////               //            Ny = fy * As;

     ////               //        // Verify the value of N
     ////               //        if (N > 0) // tensioned stringer
     ////               //        {
     ////               //            // Calculate critical force for concrete remain uncracked
     ////               //            double Ncr = fcr * Ac * (1 + xi);

     ////               //            if (N <= Ncr) // uncracked
     ////               //                e = N / (Eci * Ac * (1 + xi));

     ////               //            else // cracked
     ////               //            {
     ////               //                // Calculate ssr
     ////               //                double ssr = (fcr / ps) * (1 + xi);

     ////               //                e = (1 / Es) * (N / As - 0.6 * ssr);
     ////               //            }
     ////               //        }

     ////               //        if (N < 0) // compressed stringer
     ////               //        {
     ////               //            // Calculate K1 and K2
     ////               //            double K1 = 1 / ec1 * (-Ncm / ec1 + Es * As * (k - 2)),
     ////               //                K2 = 1 / ec1 * (Ncm * k - N * (k - 2)) + Es * As;

     ////               //            // Compare ey and ec1
     ////               //            if (ey < ec1) // steel yields before concrete crushing
     ////               //            {
     ////               //                // Calculate the yield force and the limit force on the stringer
     ////               //                double Nyc = -Ny + Ncm * (-k * ey / ec1 - Math.Pow(-ey / ec1, 2)) / (1 - (k - 2) * ey / ec1),
     ////               //                    Nlim = -Ny + Ncm;

     ////               //                // Verify the value of N
     ////               //                if (Nlim <= N && N <= Nyc)
     ////               //                {
     ////               //                    // Calculate the constants K3, K4 and K5
     ////               //                    double K3 = -Ncm / (ec1 * ec1),
     ////               //                        K4 = 1 / ec1 * (Ncm * k - (Ny + N) * (k - 2)),
     ////               //                        K5 = -Ny - N;

     ////               //                    // Calculate the strain
     ////               //                    e = (-K4 + Math.Sqrt(K4 * K4 - 4 * K3 * K5)) / (2 * K3);
     ////               //                }

     ////               //                else
     ////               //                    e = (-K2 + Math.Sqrt(K2 * K2 + 4 * K1 * N)) / (2 * K1);
     ////               //            }

     ////               //            else // steel yields together or after concrete crushing
     ////               //            {
     ////               //                e = (-K2 + Math.Sqrt(K2 * K2 + 4 * K1 * N)) / (2 * K1);
     ////               //            }
     ////               //        }
     ////               //    }

     ////               //    return e;
     ////               //}
     ////           }

     ////           // Classic SpanCAD Methods
     ////           public class Classic
     ////           {
     ////               //// Calculate the strain on a stringer given a force N and the concrete parameters
     ////               //public static double StringerStrain(double N, double Ac, double As)
     ////               //{
     ////               //    // Get the parameters of materials
     ////               //    var concParams = Material.Concrete.ConcreteParams();
     ////               //    var steelParams = Material.Steel.SteelParams();

     ////               //    // Initialize the strain
     ////               //    double e = 0;

     ////               //    if (concParams != null)
     ////               //    {
     ////               //        // Get the values for concrete
     ////               //        double fcm = concParams[0],
     ////               //            fcr = concParams[1],
     ////               //            Eci = concParams[2],
     ////               //            Ec1 = concParams[3],
     ////               //            ec1 = concParams[4],
     ////               //            k = concParams[5];

     ////               //        // Get the values for steel
     ////               //        double fy = steelParams[0],
     ////               //            Es = steelParams[1],
     ////               //            ey = steelParams[2];

     ////               //        // Calculate ps and xi
     ////               //        double ps = As / Ac,
     ////               //            xi = ps * Es / Eci;

     ////               //        // Calculate maximum forces of concrete and steel
     ////               //        double Nc = -fcm * Ac,
     ////               //            Nyr = fy * As;

     ////               //        // Verify the value of N
     ////               //        if (N > 0) // tensioned stringer
     ////               //        {
     ////               //            // Calculate critical force for concrete remain uncracked
     ////               //            double Ncr = fcr * Ac * (1 + xi);

     ////               //            if (N <= Ncr) // uncracked
     ////               //                e = N / (Eci * Ac * (1 + xi));

     ////               //            else // cracked
     ////               //            {
     ////               //                // Calculate ssr
     ////               //                double Nr = Ncr / (Math.Sqrt(1 + xi));

     ////               //                e = (N * N - Nr * Nr) / (Es * As * N);
     ////               //            }
     ////               //        }

     ////               //        if (N < 0) // compressed stringer
     ////               //        {
     ////               //            // Calculate ec
     ////               //            double ec = -2 * fcm / Eci;

     ////               //            // Calculate the yield force
     ////               //            double Nyc = -fy * As + fcm * Ac * (2 * ey / ec - ey / ec * ey / ec);

     ////               //            // Compare ey and ec1
     ////               //            if (ey < ec) // steel yields before concrete crushing
     ////               //            {
     ////               //                // Calculate the ultimate force on the stringer
     ////               //                double Nt = -Nyr + Nc;

     ////               //                // Verify the value of N
     ////               //                if (N >= Nyc) // steel not yielding
     ////               //                    e = ec * (1 + xi - Math.Sqrt((1 + xi) * (1 + xi) - N / Nc));

     ////               //                else // steel yielding
     ////               //                    e = ec * (1 - Math.Sqrt(1 - (Nyr + N) / Nc));
     ////               //            }

     ////               //            else // steel yields together or after concrete crushing
     ////               //            {
     ////               //                e = ec * (1 + xi - Math.Sqrt((1 + xi) * (1 + xi) - N / Nc));
     ////               //            }
     ////               //        }
     ////               //    }

     ////               //    return e;
     ////               //}

     ////               //// Calculate the stringer stiffness
     ////               //public static Matrix<double> StringerStiffness(double L, double N1, double N3, double Ac, double As, double ec, double ey)
     ////               //{
     ////               //    // Calculate the approximated strains
     ////               //    double eps1 = StringerStrain(N1, Ac, As),
     ////               //        eps2 = StringerStrain(2 / 3 * N1 + N3 / 3, Ac, As),
     ////               //        eps3 = StringerStrain(N1 / 3 + 2 / 3 * N3, Ac, As),
     ////               //        eps4 = StringerStrain(N3, Ac, As);

     ////               //    // Calculate the flexibility matrix elements
     ////               //    double de1N1 = L / 24 * (3 * eps1 + 4 * eps2 + eps3),
     ////               //        de1N2 = L / 12 * (eps2 + eps3),
     ////               //        de2N2 = L / 24 * (eps2 + 4 * eps3 + 3 * eps4);

     ////               //    // Get the flexibility matrix
     ////               //    var F = Matrix<double>.Build.DenseOfArray(new double[,]
     ////               //    {
     ////               //        { de1N1, de1N2},
     ////               //        { de1N2, de2N2}
     ////               //    });

     ////               //    // Get the B matrix
     ////               //    var B = Matrix<double>.Build.DenseOfArray(new double[,]
     ////               //    {
     ////               //        { -1,  1, 0},
     ////               //        {  0, -1, 1}
     ////               //    });

     ////               //    // Calculate local stiffness matrix and return the value
     ////               //    var Kl = B.Transpose() * F.Inverse() * B;

     ////               //    return Kl;
     ////               //}

     ////               //// Calculate the total plastic generalized strain in a stringer
     ////               //public static double StringerPlasticStrain(double eps, double ec, double ey, double L)
     ////               //{
     ////               //    // Initialize the plastic strain
     ////               //    double ep = 0;

     ////               //    // Case of tension
     ////               //    if (eps > ey)
     ////               //        ep = L / 8 * (eps - ey);

     ////               //    // Case of compression
     ////               //    if (eps < ec)
     ////               //        ep = L / 8 * (eps - ec);

     ////               //    return ep;
     ////               //}

     ////               //// Calculate the maximum plastic strain in a stringer for tension and compression
     ////               //public static Tuple<double, double> StringerMaxPlasticStrain(double L, double b, double h, double ey, double esu, double ec1, double ecu)
     ////               //{
     ////               //    // Calculate the maximum plastic strain for tension
     ////               //    double eput = 0.3 * esu * L;

     ////               //    // Calculate the maximum plastic strain for compression
     ////               //    double et = Math.Max(ec1, -ey);
     ////               //    double a = Math.Min(b, h);
     ////               //    double epuc = (ecu - et) * a;

     ////               //    // Return a tuple in order Tension || Compression
     ////               //    return Tuple.Create(eput, epuc);
     ////               //}
     ////           }
     ////       }
     //   }
    }
}