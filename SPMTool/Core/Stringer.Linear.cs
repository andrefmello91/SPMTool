using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.Material;

namespace SPMTool.Core
{
	public partial class Stringer
	{
        public class Linear : Stringer
        {
            // Private properties
            private double L  => Length;
            private double Ac => ConcreteArea;
            private double Ec => Concrete.Ec;

            // Constructor
            public Linear(ObjectId stringerObject, Concrete concrete) : base(stringerObject, concrete)
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

			// Calculate Stringer forces
			public Vector<double> CalculateForces()
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

			// Calculate forces
			public override void Analysis()
			{
				Forces = CalculateForces();
			}
        }
	}
}

