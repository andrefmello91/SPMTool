using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;
using Concrete           = Material.Concrete.Uniaxial;
using ConcreteParameters = Material.Concrete.Parameters;
using Behavior           = Material.Concrete.ModelBehavior;

namespace SPMTool.Core
{
	public partial class Stringer
	{
        public class Linear : Stringer
        {
            // Constructor
            public Linear(ObjectId stringerObject, ConcreteParameters concreteParameters, Behavior concreteBehavior = Behavior.Linear) : base(stringerObject, concreteParameters, concreteBehavior)
            {
            }

            // Calculate local stiffness
            public override Matrix<double> LocalStiffness
            {
                get
                {
                    // Calculate the constant factor of stiffness
                    double EcA_L = Concrete.Ec * Area / Length;

                    // Calculate the local stiffness matrix
                    return
                        EcA_L * Matrix<double>.Build.DenseOfArray(new double[,]
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
			public override void Analysis(Vector<double> globalDisplacements = null, int numStrainSteps = 5)
			{
				// Set displacements
				if (globalDisplacements != null)
					SetDisplacements(globalDisplacements);

                Forces = CalculateForces();
			}
        }
	}
}

