using System;
using System.Windows;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
	public partial class MCFT
	{
		//public class Membrane
		//{
		//	// Public Properties
		//	public bool                     Stop                          { get; set; }
		//	public string                   StopMessage                   { get; set; }
		//	public int                      LSCrack                       { get; set; }
		//	public int                      LSYieldX                      { get; set; }
		//	public int                      LSYieldY                      { get; set; }
		//	public int                      LSPeak                        { get; set; }
		//	public Vector<double>           Strains                       { get; set; }
		//	public double                   StrainSlope                   { get; set; }
		//	public Vector<double>           AppliedStresses               { get; set; }
		//	public (double ec1, double ec2) ConcreteStrains               { get; set; }
		//	public (double X, double Y)     ReinforcementSlopes           { get; set; }
		//	public (double X, double Y)     BarDiameter                   { get; set; }
		//	public (double X, double Y)     BarSpacing                    { get; set; }
		//	public (double X, double Y)     CrackSpacing                  { get; set; }
		//	public (double X, double Y)     ReinforcementRatio            { get; set; }
		//	public (double fc1, double fc2) ConcreteStresses              { get; set; }
		//	public (double fsx, double fsy) ReinforcementStresses         { get; set; }
		//	public Matrix<double>           ConcreteMatrix                { get; set; }
		//	public Matrix<double>           SteelMatrix                   { get; set; }
		//	public Vector<double>           Stresses                      { get; set; }
		//	public Matrix<double>           Stiffness => ConcreteMatrix + SteelMatrix;

		//	// Private properties
		//	private Material.Concrete Concrete { get; }
		//	private Material.Steel    Steel { get; }
		//	private double Esx => Steel.Es;
		//	private double Esy => Steel.Es;
		//	private double fyx => Steel.fy;
		//	private double fyy => Steel.fy;

		//	// Constructor
		//	public Membrane(Panel panel, (double X, double Y) effectiveRatio, Vector<double> sigma,
		//		Material.Concrete concrete)
		//	{
		//		BarDiameter        = panel.BarDiameter;
		//		BarSpacing         = panel.BarSpacing;
		//		ReinforcementRatio = effectiveRatio;
		//		AppliedStresses = sigma;
		//		Concrete = concrete;
		//	}

		//	// Calculate the initial material matrix for initiating the iterations
		//	public Membrane InitialStiffness(Panel panel, (double X, double Y) effectiveRatio, Vector<double> sigma)
		//	{
		//		Membrane membrane = new Membrane(panel, effectiveRatio, sigma)
		//		{
		//			ConcreteStrains  = (0, 0),
		//			ConcreteStresses = (0, 0),
		//			StrainSlope      = Constants.PiOver4
		//		};

		//		// Get the initial material stiffness
		//		ConcreteStiffness(membrane);
		//		SteelStiffness(membrane);

		//		return  membrane;
		//	}

		//	// Calculate concrete principal strains
		//	private (double ec1, double ec2) ConcretePrincipalStrains()
		//	{
		//		// Get the apparent strains and concrete net strains
		//		var e = Strains;

		//		double
		//			ecx  = e[0],
		//			ecy  = e[1],
		//			ycxy = e[2];

		//		// Calculate radius and center of Mohr's Circle
		//		double
		//			cen = 0.5 * (ecx + ecy),
		//			rad = 0.5 * Math.Sqrt((ecx - ecy) * (ecx - ecy) + ycxy * ycxy);

		//		// Calculate principal strains in concrete
		//		double
		//			ec1 = cen + rad,
		//			ec2 = cen - rad;

		//		return (ec1, ec2);
		//	}

		//	// Calculate slopes related to reinforcement
		//	private static void ReinforcementSlope(Membrane membrane, double theta)
		//	{
		//		// Calculate angles
		//		double
		//			thetaNx = theta,
		//			thetaNy = theta - Constants.PiOver2;

		//		membrane.ReinforcementSlopes = (thetaNx, thetaNy);
		//	}

		//	// Calculate reinforcement stresses
		//	static void ReinforcementsStresses(Membrane membrane)
		//	{
		//		// Get the strains
		//		double
		//			ex = membrane.Strains[0],
		//			ey = membrane.Strains[1];

		//		// Calculate stresses and secant moduli
		//		double fsx, fsy;
		//		if (ex >= 0)
		//			fsx = Math.Min(Steel.Es * ex, Steel.fy);

		//		else
		//			fsx = Math.Max(Steel.Es * ex, -Steel.fy);

		//		if (ey >= 0)
		//			fsy = Math.Min(Steel.Es * ey, Steel.fy);

		//		else
		//			fsy = Math.Max(Steel.Es * ey, -Steel.fy);

		//		membrane.ReinforcementStresses = (fsx, fsy);
		//	}

		//	// Calculate steel stiffness matrix
		//	private static void SteelStiffness(Membrane membrane)
		//	{
		//		// Get the strains
		//		double
		//			ex = membrane.Strains[0],
		//			ey = membrane.Strains[1];

		//		// Get the stresses
		//		var (fsx, fsy) = membrane.ReinforcementStresses;

		//		// Calculate secant moduli
		//		double Esx, Esy;
		//		SteelSecantModuli();

		//		// Steel matrix
		//		var Ds = Matrix<double>.Build.Dense(3, 3);
		//		Ds[0, 0] = membrane.ReinforcementRatio.X * Esx;
		//		Ds[1, 1] = membrane.ReinforcementRatio.Y * Esy;

		//		membrane.SteelMatrix = Ds;

		//		// Calculate secant moduli of steel
		//		void SteelSecantModuli()
		//		{
		//			// Steel
		//			if (ex == 0 || fsx == 0)
		//				Esx = Steel.Es;

		//			else
		//				Esx = Math.Min(fsx / ex, Steel.Es);

		//			if (ey == 0 || fsy == 0)
		//				Esy = Steel.Es;

		//			else
		//				Esy = Math.Min(fsy / ey, Steel.Es);
		//		}
		//	}

		//	// Calculate concrete stiffness matrix
		//	private void ConcreteStiffness()
		//	{
		//		// Get the strains and stresses
		//		double
		//			ec1 = ConcreteStrains.ec1,
		//			ec2 = ConcreteStrains.ec2,
		//			fc1 = ConcreteStresses.fc1,
		//			fc2 = ConcreteStresses.fc2;

		//		// Get secant moduli
		//		double Ec1, Ec2, Gc;
		//		ConcreteSecantModuli();

		//		// Calculate concrete transformation matrix
		//		Matrix<double> T;
		//		TransMatrix();

		//		// Concrete matrix
		//		var Dc1 = Matrix<double>.Build.Dense(3, 3);
		//		Dc1[0, 0] = Ec1;
		//		Dc1[1, 1] = Ec2;
		//		Dc1[2, 2] = Gc;

		//		// Calculate Dc
		//		ConcreteMatrix = T.Transpose() * Dc1 * T;

		//		// Calculate secant moduli of concrete
		//		void ConcreteSecantModuli()
		//		{
		//			if (ec1 == 0 || fc1 == 0)
		//				Ec1 = Concrete.Eci;

		//			else
		//				Ec1 = fc1 / ec1;

		//			if (ec2 == 0 || fc2 == 0)
		//				Ec2 = Concrete.Eci;

		//			else
		//				Ec2 = fc2 / ec2;

		//			Gc = Ec1 * Ec2 / (Ec1 + Ec2);
		//		}

		//		// Calculate concrete transformation matrix
		//		void TransMatrix()
		//		{
		//			// Get psi angle
		//			// Calculate Psi angle
		//			double psi = Constants.Pi - membrane.StrainSlope;
		//			double[] dirCos = Auxiliary.DirectionCosines(psi);

		//			double
		//				cos    = dirCos[0],
		//				sin    = dirCos[1],
		//				cos2   = cos * cos,
		//				sin2   = sin * sin,
		//				cosSin = cos * sin;

		//			T = Matrix<double>.Build.DenseOfArray(new [,]
		//			{
		//				{         cos2,       sin2,      cosSin },
		//				{         sin2,       cos2,    - cosSin },
		//				{ - 2 * cosSin, 2 * cosSin, cos2 - sin2 }
		//			});
		//		}
		//	}
		//}
	}
}