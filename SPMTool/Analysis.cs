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
using MathNet.Numerics.Statistics;

[assembly: CommandClass(typeof(SPMTool.Analysis))]
[assembly: CommandClass(typeof(SPMTool.Analysis.Linear))]
[assembly: CommandClass(typeof(SPMTool.Analysis.NonLinear))]

namespace SPMTool
{
    public class Analysis
    {
        // Public Properties
        public Matrix<double> GlobalStiffness    { get; set; }
		public Vector<double> DisplacementVector { get; set; }
		public Node[]         Nodes              { get; }
        public Stringer[]     Stringers          { get; }
        public Panel[]        Panels             { get; }
        public Vector<double> ForceVector        { get; }
        public double         MaxStringerForce   { get; set; }

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

			// Approximate small numbers to zero
			Kg.CoerceZero(1E-9);
        }

        // Get stringer displacements
        private void StringerDisplacements()
        {
	        // Create a matrix to store the stringer forces
	        var strForces = Matrix<double>.Build.Dense(Stringers.Length, 3);

	        foreach (var str in Stringers)
	        {
		        // Calculate forces
		        str.Displacement(DisplacementVector);

		        // Set to the matrix of forces
		        int i = str.Number - 1;
		        strForces.SetRow(i, str.Forces);

		        //Global.ed.WriteMessage("\nStringer " + strNum.ToString() + ":\n" + fl.ToString());
	        }

	        // Verify the maximum stringer force in the model to draw in an uniform scale
	        MaxStringerForce = strForces.Enumerate().MaximumAbsolute();
        }

        // Get panel displacements
        private void PanelDisplacements()
        {
	        foreach (var pnl in Panels)
		        pnl.Displacement(DisplacementVector);
        }

        // Get the nodal displacements and save to XData
        private void NodalDisplacements()
        {
	        foreach (var nd in Nodes)
		        nd.Displacements(DisplacementVector);
        }

        // Get the list of continued stringers
        public List<(int str1, int str2)> ContinuedStringers(Stringer[] stringers)
        {
            // Initialize a Tuple to store the continued stringers
            var contStrs = new List<(int str1, int str2)>();

            // Calculate the parameter of continuity
            double par = 0.5 * Math.Sqrt(2);

            // Verify in the list what stringers are continuous
            foreach (var str1 in stringers)
            {
                // Access the number
                int num1 = str1.Number;

                foreach (var str2 in stringers)
                {
                    // Access the number
                    int num2 = str2.Number;

                    // Verify if it's other stringer
                    if (num1 != num2)
                    {
                        // Create a tuple with the minimum stringer number first
                        var contStr = (Math.Min(num1, num2), Math.Max(num1, num2));

                        // Verify if it's already on the list
                        if (!contStrs.Contains(contStr))
                        {
                            // Verify the cases
                            // Case 1: stringers initiate or end at the same node
                            if (str1.Grips[0] == str2.Grips[0] || str1.Grips[2] == str2.Grips[2])
                            {
                                // Get the direction cosines
                                double[]
                                    dir1 = Auxiliary.DirectionCosines(str1.Angle),
                                    dir2 = Auxiliary.DirectionCosines(str2.Angle);
                                double
                                    l1 = dir1[0],
                                    m1 = dir1[1],
                                    l2 = dir2[0],
                                    m2 = dir2[1];

                                // Calculate the condition of continuity
                                double cont = l1 * l2 + m1 * m2;

                                // Verify the condition
                                if (cont < -par) // continued stringer
                                {
                                    // Add to the list
                                    contStrs.Add(contStr);
                                }
                            }

                            // Case 2: a stringer initiate and the other end at the same node
                            if (str1.Grips[0] == str2.Grips[2] || str1.Grips[2] == str2.Grips[0])
                            {
                                // Get the direction cosines
                                double[]
                                    dir1 = Auxiliary.DirectionCosines(str1.Angle),
                                    dir2 = Auxiliary.DirectionCosines(str2.Angle);
                                double
                                    l1 = dir1[0],
                                    m1 = dir1[1],
                                    l2 = dir2[0],
                                    m2 = dir2[1];

                                // Calculate the condition of continuity
                                double cont = l1 * l2 + m1 * m2;

                                // Verify the condition
                                if (cont > par) // continued stringer
                                {
                                    // Add to the list
                                    contStrs.Add(contStr);
                                }
                            }
                        }
                    }
                }
            }

            // Order the list
            contStrs = contStrs.OrderBy(str => str.Item2).ThenBy(str => str.Item1).ToList();

            // Return the list
            return contStrs;
        }

        //View the continued stringers
   //     [CommandMethod("ViewContinuedStringers")]
   //     public void ViewContinuedStringers()
   //     {
			//// Get input data
			//var inputData = new InputData();

	  //      // Get the list of continued stringers
	  //      var contStrs = ContinuedStringers(inputData.Stringers);

	  //      // Initialize a message to show
	  //      string msg = "Continued stringers: ";

	  //      // If there is none
	  //      if (contStrs.Count == 0)
		 //       msg += "None.";

	  //      // Write all the continued stringers
	  //      else
	  //      {
		 //       foreach (var contStr in contStrs)
		 //       {
			//        msg += contStr.str1 + " - " + contStr.str2 + ", ";
		 //       }
	  //      }

	  //      // Write the message in the editor
	  //      AutoCAD.edtr.WriteMessage(msg);
   //     }

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

                    // Draw results of analysis
                    Results.Draw(analysis);
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

	            // Calculate displacements
	            StringerDisplacements();
	            PanelDisplacements();
	            NodalDisplacements();
            }
        }

        public class NonLinear : Analysis
        {
	        [CommandMethod("DoNonLinearAnalysis")]
	        public static void DoNonLinearAnalysis()
	        {
		        // Get input data
		        InputData input = new InputData.NonLinear();

		        if (input.Concrete.IsSet)
		        {
			        // Do a linear analysis
			        NonLinear analysis = new NonLinear(input);

			        // Calculate results of analysis
			        //Results results = new Results(analysis);

			        // Draw results
			        //Results.DrawResults.Draw(results);
		        }

		        else
		        {
			        Application.ShowAlertDialog("Please set concrete parameters");
		        }
	        }

            public NonLinear(InputData inputData) : base(inputData)
	        {
		        // Get the initial stiffness and force vector simplified
		        var (Ki, f) = InitialParameters();

				// Get the initial force vector
				var fi = 0.01 * f;

				// Solve the initial displacements
				var u = Ki.Solve(fi);

				// Initiate iterations
				for (int it = 1; it <= 1000; it++)
				{

				}

		        DelimitedWriter.Write("D:/Ki.csv", Ki, ";");
		        DelimitedWriter.Write("D:/f.csv", f.ToColumnMatrix(), ";");
	        }

			// Get initial global stiffness
			private (Matrix<double> GlobalStiffness, Vector<double> ForceVector) InitialParameters()
			{
				// Get force vector
				var forceVector = ForceVector;

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
					var K = stringer.InitialStiffness;

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
					var K = panel.InitialStiffness;

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
								Kg[n, i]     += K[o, 0];
								Kg[n, i + 1] += K[o, 1];
								Kg[n, j]     += K[o, 2];
								Kg[n, j + 1] += K[o, 3];
								Kg[n, k]     += K[o, 4];
								Kg[n, k + 1] += K[o, 5];
								Kg[n, l]     += K[o, 6];
								Kg[n, l + 1] += K[o, 7];
							}

							// Increment the line index
							o++;
						}
					}

				}

				// Simplify stiffness matrix
				SimplifyStiffnessMatrix(Kg, forceVector);

				return (Kg, forceVector);
			}
        }
    }
}
