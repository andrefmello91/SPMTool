using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.Data.Text;

[assembly: CommandClass(typeof(SPMTool.Analysis))]
[assembly: CommandClass(typeof(SPMTool.Analysis.Linear))]

namespace SPMTool
{
    public partial class Analysis
    {
        // Public Properties
        public Matrix<double> GlobalStiffness    { get; set; }
		public Vector<double> DisplacementVector { get; set; }
		public Node[]         Nodes              { get; }
        public Stringer[]     Stringers          { get; }
        public Panel[]        Panels             { get; }
        public Vector<double> ForceVector        { get; }

        // Constructor
        public Analysis(InputData inputData)
		{
			// Get elements
			Nodes       = inputData.Nodes;
			Stringers   = inputData.Stringers;
			Panels      = inputData.Panels;
			ForceVector = inputData.ForceVector;
		}

		// Get the number of DoFs
		private int numDoFs => 2 * Nodes.Length;

        // Calculate Global Stiffness
        private Matrix<double> GlobalSStiffness(Vector<double> forceVector)
		{
			// Initialize the global stiffness matrix
			var Kg = Matrix<double>.Build.Dense(numDoFs, numDoFs);

			// Add stringer stiffness to global stiffness
			foreach (var stringer in Stringers)
			{
				// Get the positions in the global matrix
				int 
					i = stringer.Index[0],
					j = stringer.Index[1],
					k = stringer.Index[2];

				// Get the stiffness
				var K = stringer.GlobalStiffness;

				// Initialize an index for lines of the local matrix
				int o = 0;

				// Add the local matrix to the global at the DoFs positions
				// n = index of the node in global matrix
				// o = index of the line in the local matrix
				foreach (int ind in stringer.Index)
				{
					for (int n = ind; n <= ind + 1; n++)
					{
						// Line o
						// Check if the row is composed of zeroes
						if (K.Row(o).Exists(Auxiliary.NotZero))
						{
							Kg[n, i]     += K[o, 0];     
							Kg[n, i + 1] += K[o, 1];
							Kg[n, j]     += K[o, 2];     
							Kg[n, j + 1] += K[o, 3];
							Kg[n, k]     += K[o, 4];    
							Kg[n, k + 1] += K[o, 5];
						}

						// Increment the line index
						o++;
					}
				}

            }

			// Add panel stiffness to global stiffness
			foreach (var panel in Panels)
			{
				// Get the positions in the global matrix
				int 
					i = panel.Index[0],
					j = panel.Index[1],
					k = panel.Index[2],
					l = panel.Index[3];

				// Get the stiffness
				var K = panel.GlobalStiffness;

                // Initialize an index for lines of the local matrix
                int o = 0;

				// Add the local matrix to the global at the DoFs positions
				// i = index of the node in global matrix
				// o = index of the line in the local matrix
				foreach (int ind in panel.Index)
				{
					for (int n = ind; n <= ind + 1; n++)
					{
						// Line o
						// Check if the row is composed of zeroes
						if (K.Row(o).Exists(Auxiliary.NotZero))
						{
							Kg[n, i] += K[o, 0]; Kg[n, i + 1] += K[o, 1];
							Kg[n, j] += K[o, 2]; Kg[n, j + 1] += K[o, 3];
							Kg[n, k] += K[o, 4]; Kg[n, k + 1] += K[o, 5];
							Kg[n, l] += K[o, 6]; Kg[n, l + 1] += K[o, 7];
						}

						// Increment the line index
						o++;
					}
				}

            }

			// Simplify stiffness matrix
			SimplifyStiffnessMatrix(Kg, forceVector);

			return Kg;
		}

        // Simplify the stiffness matrix
        private void SimplifyStiffnessMatrix(Matrix<double> Kg, Vector<double> forceVector)
        {
            foreach (var nd in Nodes)
            {
                // Get the index of the row
                int i = 2 * nd.Number - 2;

                // Simplify the matrices removing the rows that have constraints (external nodes)
                if (nd.Type == (int)Node.NodeType.External)
                {
                    if (nd.Support.X)
                        // There is a support in this direction
                    {
                        // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
                        Kg.ClearRow(i);
                        Kg.ClearColumn(i);

                        // Set the diagonal element to 1
                        Kg[i, i] = 1;

                        // Clear the row in the force vector
                        forceVector[i] = 0;

                        // So ui = 0
                    }

                    if (nd.Support.Y)
                        // There is a support in this direction
                    {
                        // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
                        Kg.ClearRow(i + 1);
                        Kg.ClearColumn(i + 1);

                        // Set the diagonal element to 1
                        Kg[i + 1, i + 1] = 1;

                        // Clear the row in the force vector
                        forceVector[i + 1] = 0;

                        // So ui = 0
                    }
                }
                
                // Simplification for internal nodes (There is only a displacement at the stringer direction, the perpendicular one will be zero)
                else
                {
                    // Verify rows i and i + 1
                    for (int j = i; j <= i + 1; j++)
                    {
                        // Verify what line of the matrix is composed of zeroes
                        if (!Kg.Row(j).Exists(Auxiliary.NotZero))
                        {
                            // The row is composed of only zeroes, so the displacement must be zero
                            // Set the diagonal element to 1
                            Kg[j, j] = 1;

                            // Clear the row in the force vector
                            forceVector[j] = 0;
                        }
                    }
                }
            }
        }

        // Linear analysis methods
        public class Linear : Analysis
        {
	        [CommandMethod("DoLinearAnalysis")]
	        public static void DoLinearAnalysis()
	        {
		        // Get input data
				InputData input = new InputData.Linear();

		        if (input.Concrete.IsSet)
		        {
			        // Do a linear analysis
			        Linear analysis = new Linear(input);

			        // Calculate results of analysis
			        Results results = new Results(analysis);

			        // Draw results
			        Results.DrawResults.Draw(results);
		        }

		        else
		        {
			        Application.ShowAlertDialog("Please set concrete parameters");
		        }
	        }

	        public Linear(InputData inputData) : base(inputData)
            {
	            // Get force Vector
	            var forceVector = ForceVector;

	            // Calculate and simplify global stiffness and force vector
	            GlobalStiffness = GlobalSStiffness(forceVector);

	            // Solve
	            DisplacementVector = GlobalStiffness.Solve(forceVector);
            }
        }
    }
}
