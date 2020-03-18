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
		public Vector<double> DisplacementVector { get; set; }
		public Node[]         Nodes              { get; }
        public Stringer[]     Stringers          { get; }
        public Panel[]        Panels             { get; }
        public Vector<double> ForceVector        { get; }
        public List<int>      Constraints        { get; }
        public double         MaxStringerForce   { get; set; }

        // Constructor
        public Analysis(InputData inputData)
		{
			// Get elements
			Nodes       = inputData.Nodes;
			Stringers   = inputData.Stringers;
			Panels      = inputData.Panels;
			ForceVector = inputData.ForceVector;
			Constraints = inputData.Constraints;
		}

		// Get the number of DoFs
		private int numDoFs => 2 * Nodes.Length;

        // Calculate Global Stiffness
        private Matrix<double> GlobalStiffness(Vector<double> forceVector = null)
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
        private void SimplifyStiffnessMatrix(Matrix<double> Kg, Vector<double> forceVector = null)
        {
	        foreach (var index in Constraints)
	        {
		        // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
		        Kg.ClearRow(index);
		        Kg.ClearColumn(index);

		        // Set the diagonal element to 1
		        Kg[index, index] = 1;

		        // Clear the row in the force vector
				if (forceVector != null)
					forceVector[index] = 0;

		        // So ui = 0
	        }

            foreach (var nd in Nodes)
            {
                // Get the index of the row
                int i = 2 * nd.Number - 2;

                // Simplification for internal nodes (There is only a displacement at the stringer direction, the perpendicular one will be zero)
                if (nd.Type == (int)Node.NodeType.Internal)
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
                            if (forceVector != null)
                                forceVector[j] = 0;
                        }
                    }
                }
            }

			// Approximate small numbers to zero
			Kg.CoerceZero(1E-9);
        }

        // Get stringer displacements
        private void StringerDisplacements(Vector<double> globalDisplacements)
        {
	        // Create a matrix to store the stringer forces
	        var strForces = Matrix<double>.Build.Dense(Stringers.Length, 3);

	        foreach (var str in Stringers)
	        {
		        // Calculate forces
		        str.Displacement(globalDisplacements);

		        // Set to the matrix of forces
		        int i = str.Number - 1;
		        strForces.SetRow(i, str.Forces);

		        //Global.ed.WriteMessage("\nStringer " + strNum.ToString() + ":\n" + fl.ToString());
	        }

	        // Verify the maximum stringer force in the model to draw in an uniform scale
	        MaxStringerForce = strForces.Enumerate().MaximumAbsolute();
        }

        // Get panel displacements
        private void PanelDisplacements(Vector<double> globalDisplacements)
        {
	        foreach (var pnl in Panels)
		        pnl.Displacement(globalDisplacements);
        }

        // Get the nodal displacements and save to XData
        private void NodalDisplacements(Vector<double> globalDisplacements)
        {
	        foreach (var nd in Nodes)
		        nd.Displacements(globalDisplacements);
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
				InputData input = new InputData((int)Stringer.Behavior.Linear, (int)Panel.Behavior.Linear);

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
	            var globalStiffness = GlobalStiffness(forceVector);

	            // Solve
	            DisplacementVector = globalStiffness.Solve(forceVector);

	            // Calculate element displacements
	            StringerDisplacements(DisplacementVector);
	            PanelDisplacements(DisplacementVector);
	            NodalDisplacements(DisplacementVector);
            }
        }

        public class NonLinear : Analysis
        {
	        [CommandMethod("DoNonLinearAnalysis")]
	        public static void DoNonLinearAnalysis()
	        {
		        // Get input data
		        InputData input = new InputData((int)Stringer.Behavior.NonLinear, (int)Panel.Behavior.NonLinear);

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
				// Get force vector
				var f = ForceVector;

		        // Get the initial stiffness and force vector simplified
		        var Kg = GlobalStiffness(f);

		        var uMatrix = Matrix<double>.Build.Dense(100, numDoFs);
		        var fiMatrix = Matrix<double>.Build.Dense(100, numDoFs);

				// Initialize a loop for load steps
				for (int loadStep = 1; loadStep <= 100; loadStep++)
				{
					// Get the force vector
					var fs = 0.01 * loadStep * f;

					// Solve the initial displacements
					var u = Kg.Solve(fs);

					Vector<double> fi = Vector<double>.Build.Dense(numDoFs);

					// Initiate iterations
					for (int it = 0; it <= 100; it++)
					{
						// Calculate element displacements and forces
						StringerAnalysis(u);
						PanelAnalysis(u);

						// Get the internal force vector
						var fit = InternalForces();

						// Calculate residual forces
						var fr = fs - fit;

						// Calculate tolerance
						double tol = fr.AbsoluteMaximum();

						// Check tolerance
						if (tol <= 0.001)
						{
							AutoCAD.edtr.WriteMessage("\nLS = " + loadStep + ": Iterations = " + it);
							uMatrix.SetRow(loadStep - 1, u);
							break;
						}

						// Calculate displacement increment
						var du = Kg.Solve(fr);

						// Increment displacements
						u += du;

						fi = fit;
					}

					fiMatrix.SetRow(loadStep - 1, fi);

					// Set the results to stringers
					StringerResults();

					// Update stiffness
					Kg = GlobalStiffness();
				}

                DelimitedWriter.Write("D:/K.csv", Kg, ";");
                //DelimitedWriter.Write("D:/f.csv", f.ToColumnMatrix(), ";");
                DelimitedWriter.Write("D:/fi.csv", fiMatrix, ";");
                //DelimitedWriter.Write("D:/frr.csv", frr, ";");
                DelimitedWriter.Write("D:/u.csv", uMatrix, ";");
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

			// Calculate stringer forces
			private void StringerAnalysis(Vector<double> globalDisplacements)
			{
				foreach (Stringer.NonLinear stringer in Stringers)
				{
					stringer.Displacement(globalDisplacements);
					stringer.StringerForces();
                    //DelimitedWriter.Write("D:/fs" + stringer.Number + ".csv", stringer.IterationForces.ToColumnMatrix(), ";");
                    //DelimitedWriter.Write("D:/us" + stringer.Number + ".csv", stringer.LocalDisplacements.ToColumnMatrix(), ";");
                }
            }

			// Calculate panel stresses nd forces
			private void PanelAnalysis(Vector<double> globalDisplacements)
			{
				foreach (Panel.NonLinear panel in Panels)
				{
					panel.Displacement(globalDisplacements);
					panel.MCFTAnalysis();
                    //DelimitedWriter.Write("D:/up" + panel.Number + ".csv", panel.Displacements.ToColumnMatrix(), ";");
                    //DelimitedWriter.Write("D:/fp" + panel.Number + ".csv", panel.Forces.ToColumnMatrix(), ";");
				}
			}

			// Get the internal force vector
			private Vector<double> InternalForces()
			{
				var fi = Vector<double>.Build.Dense(numDoFs);

				foreach (Stringer.NonLinear stringer in Stringers)
				{
					// Get index and forces
					int[] index = stringer.Index;
					var fs = stringer.IterationGlobalForces;

					for (int i = 0; i < 3; i++)
					{
						// Indexers
						int
							j = index[i],
							k = 2 * i;

						// Add values
						fi[j]     += fs[k];
						fi[j + 1] += fs[k + 1];
					}

				}

				foreach (Panel.NonLinear panel in Panels)
				{
					// Get index and forces
					int[] index = panel.Index;
					var fp = panel.Forces;

					for (int i = 0; i < 4; i++)
					{
						// Indexers
						int
							j = index[i],
							k = 2 * i;

						// Add values
						fi[j]     += fp[k];
						fi[j + 1] += fp[k + 1];
					}
				}

				// Simplify for constraints
				foreach (var i in Constraints)
					fi[i] = 0;

                return fi;
			}

			// Set the results for each stringer
			private void StringerResults()
			{
				foreach (Stringer.NonLinear stringer in Stringers)
					stringer.Results();
			}
        }
    }
}
