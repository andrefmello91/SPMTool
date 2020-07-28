using System;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;
using Concrete           = Material.Concrete.Uniaxial;
using ConcreteParameters = Material.Concrete.Parameters;
using Behavior           = Material.Concrete.Behavior;

namespace SPMTool.Core
{
	public partial class Stringer
	{
		/// <summary>
        /// Stringer linear class.
        /// </summary>
        public class Linear : Stringer
        {
            /// <summary>
            /// Stringer linear object.
            /// </summary>
            /// <param name="stringerObject">The object ID from AutoCAD drawing.</param>
            /// <param name="units">Units current in use.</param>
            /// <param name="concreteParameters">The concrete parameters.</param>
            /// <param name="concreteBehavior">The concrete behavior.</param>
            public Linear(ObjectId stringerObject, Units units, ConcreteParameters concreteParameters, Behavior concreteBehavior = null) : base(stringerObject, units, concreteParameters, concreteBehavior)
            {
            }

            /// <summary>
            /// Get local stiffness.
            /// </summary>
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

            /// <summary>
            /// Calculate Stringer forces
            /// </summary>
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

			/// <inheritdoc/>
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

