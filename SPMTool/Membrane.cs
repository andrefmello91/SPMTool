using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
	public class Membrane
	{
		// Public properties
		public (double X, double Y) ReinforcementRatio { get; }
		public double               StrainSlope        { get; }

        // Private properties
        private double Ec1, Ec2;
		private double Esx, Esy;
		private double psx => ReinforcementRatio.X;
		private double psy => ReinforcementRatio.Y;

		// Constructor
		public Membrane((double Ec1, double Ec2) concreteSecantModule, (double Esx, double Esy) steelSecantModule, (double X, double Y) reinforcementRatio, double strainSlope)
		{
			// Get parameters
			Ec1                = concreteSecantModule.Ec1;
			Ec2                = concreteSecantModule.Ec2;
			Esx                = steelSecantModule.Esx;
			Esy                = steelSecantModule.Esy;
			ReinforcementRatio = reinforcementRatio;
			StrainSlope        = strainSlope;
		}

		// Calculate stiffness
		public Matrix<double> Stiffness => ConcreteStiffness + SteelStiffness;

        // Calculate steel stiffness matrix
        public  Matrix<double> SteelStiffness => Steel();
        private Matrix<double> Steel()
		{
			// Steel matrix
			var Ds = Matrix<double>.Build.Dense(3, 3);
			Ds[0, 0] = psx * Esx;
			Ds[1, 1] = psy * Esy;

			return Ds;
		}

        // Calculate concrete stiffness matrix
        private double Gc => Ec1 * Ec2 / (Ec1 + Ec2);
        public  Matrix<double> ConcreteStiffness => Concrete();
		private Matrix<double> Concrete()
		{
			// Concrete matrix
			var Dc1 = Matrix<double>.Build.Dense(3, 3);
			Dc1[0, 0] = Ec1;
			Dc1[1, 1] = Ec2;
			Dc1[2, 2] = Gc;

			// Get transformation matrix
			var T = TransformationMatrix();

			// Calculate Dc
			return T.Transpose() * Dc1 * T;
		}

		// Calculate concrete transformation matrix
		private Matrix<double> TransformationMatrix()
		{
			// Get psi angle
			// Calculate Psi angle
			double psi = Constants.Pi - StrainSlope;
			double[] dirCos = Auxiliary.DirectionCosines(psi);

			double
				cos = dirCos[0],
				sin = dirCos[1],
				cos2 = cos * cos,
				sin2 = sin * sin,
				cosSin = cos * sin;

			return Matrix<double>.Build.DenseOfArray(new[,]
			{
				{         cos2,       sin2,      cosSin },
				{         sin2,       cos2,    - cosSin },
				{ - 2 * cosSin, 2 * cosSin, cos2 - sin2 }
			});
		}
    }

}