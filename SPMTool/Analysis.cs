using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.Statistics;

[assembly: CommandClass(typeof(SPMTool.Analysis))]
[assembly: CommandClass(typeof(SPMTool.Analysis.Linear))]
[assembly: CommandClass(typeof(SPMTool.Analysis.NonLinear))]

namespace SPMTool
{
    public abstract class Analysis
    {
        // Public Properties
		public Vector<double> DisplacementVector { get; set; }
		public Node[]         Nodes              { get; }
        public Stringer[]     Stringers          { get; }
        public Panel[]        Panels             { get; }
        public Vector<double> ForceVector        { get; }
        public List<int>      Constraints        { get; }

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
				// Get DoF indexes
				var index = stringer.DoFIndex;

				// Get the stiffness
				var K = stringer.GlobalStiffness;

                // Add the local matrix to the global at the DoFs positions
                AddStiffness(K, index);
            }

            // Add panel stiffness to global stiffness
            foreach (var panel in Panels)
			{
				// Get DoF indexes
				var index = panel.DoFIndex;

                // Get the stiffness
                var K = panel.GlobalStiffness;

                // Add the local matrix to the global at the DoFs positions
                AddStiffness(K, index);
			}

			// Add local stiffness to global matrix
			void AddStiffness(Matrix<double> elementStiffness, int[] dofIndex)
			{
				for (int i = 0; i < dofIndex.Length; i++)
				{
					// Global index
					int k = dofIndex[i];

					for (int j = 0; j < dofIndex.Length; j++)
					{
						// Global index
						int l = dofIndex[j];

						Kg[k, l] += elementStiffness[i, j];
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
	        foreach (var i in Constraints)
	        {
		        // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
		        Kg.ClearRow(i);
		        Kg.ClearColumn(i);

		        // Set the diagonal element to 1
		        Kg[i, i] = 1;

		        // Clear the row in the force vector
				if (forceVector != null)
					forceVector[i] = 0;

		        // So ui = 0
	        }

            foreach (var node in Nodes)
            {
                // Get DoF indexes
                var index = node.DoFIndex;

                // Simplification for internal nodes (There is only a displacement at the stringer direction, the perpendicular one will be zero)
                if (node.Type == (int)Node.NodeType.Internal)
                {
                    // Verify rows
                    foreach (int i in index)
                    {
                        // Verify what line of the matrix is composed of zeroes
                        if (!Kg.Row(i).Exists(Auxiliary.NotZero))
                        {
                            // The row is composed of only zeroes, so the displacement must be zero
                            // Set the diagonal element to 1
                            Kg[i, i] = 1;

                            // Clear the row in the force vector
                            if (forceVector != null)
                                forceVector[i] = 0;
                        }
                    }
                }
            }

			// Approximate small numbers to zero
			Kg.CoerceZero(1E-9);
        }

        // Set stringer displacements
        private void StringerDisplacements(Vector<double> globalDisplacements)
        {
	        foreach (var str in Stringers)
		        str.Displacement(globalDisplacements);
        }

        // Set panel displacements
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

        // Calculate maximum stringer force
        public double MaxStringerForce
        {
	        get
	        {
		        // Create a list to store the maximum stringer forces
		        var strForces = new List<double>();

                foreach (var str in Stringers)
			        // Add to the list of forces
			        strForces.Add(str.MaxForce);

		        // Verify the maximum stringer force
		        return
			        strForces.MaximumAbsolute();
            }
        }

        // Calculate maximum panel force
        public double MaxPanelForce
        {
	        get
	        {
		        // Create a list to store the maximum panel forces
		        var pnlForces = new List<double>();

                foreach (var pnl in Panels)
			        // Add to the list of forces
			        pnlForces.Add(pnl.MaxForce);

		        // Verify the maximum panel force
		        return
			        pnlForces.MaximumAbsolute();
            }
        }

        // Get maximum element force
        private double MaxElementForce => Math.Max(MaxStringerForce, MaxPanelForce);

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
                                var (l1, m1) = str1.DirectionCosines;
                                var (l2, m2) = str2.DirectionCosines;

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
                                var (l1, m1) = str1.DirectionCosines;
                                var (l2, m2) = str2.DirectionCosines;

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

        // Get the list of panel's DoFs that have continuity
        private List<int> PanelContinuousDoFs()
        {
	        var contDofs = new List<int>();

	        foreach (var pnl1 in Panels)
	        {
		        // Get Dofs normal to edges
		        var DoFs1 = pnl1.DoFIndex;
		        var nDofs1 = new[]
		        {
			        DoFs1[1], DoFs1[2], DoFs1[5], DoFs1[6]
		        };

		        foreach (var pnl2 in Panels)
		        {
			        if (pnl1.Number != pnl2.Number)
			        {
				        // Get Dofs normal to edges
				        var DoFs2 = pnl2.DoFIndex;
				        var nDofs2 = new[]
				        {
					        DoFs2[1], DoFs2[2], DoFs2[5], DoFs2[6]
				        }.ToList();

				        // Verify if there is a common DoF
				        foreach (var dof in nDofs1)
					        if (nDofs2.Contains(dof) && !contDofs.Contains(dof))
						        contDofs.Add(dof);
			        }
		        }
	        }

	        return contDofs;
        }

        // Get DoFs that are not continuous
        private int[] NotContinuousPanelDoFs => notContPnlDoFs.Value;
        private Lazy<int[]> notContPnlDoFs => new Lazy<int[]>(NotContinuousPanelDofs);
        private int[] NotContinuousPanelDofs()
        {
	        // Create an array containing all DoFs
	        var AllDoFs = new int[numDoFs];
	        for (int i = 0; i < numDoFs; i++)
		        AllDoFs[i] = i;

	        // Get the continuous DoFs
	        var contDofs = PanelContinuousDoFs();

	        // Verify if exist continuous DoFs
	        if (contDofs.Count == 0)
		        return
			        AllDoFs;

	        // Create a list containing only not continuous DoFs
	        var notContDoFs = new List<int>();
	        foreach (int dof in AllDoFs)
	        {
		        if (!contDofs.Contains(dof))
			        notContDoFs.Add(dof);
	        }

	        return
		        notContDoFs.ToArray();
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
			// Max iterations and load steps
			private int maxIterations = 1000;
			private int loadSteps     = 100;

			[CommandMethod("DoNonLinearAnalysis")]
	        public static void DoNonLinearAnalysis()
	        {
		        // Get input data
		        InputData input = new InputData((int)Stringer.Behavior.NonLinearClassic, (int)Panel.Behavior.NonLinear);

		        if (input.Concrete.IsSet)
		        {
			        // Do a linear analysis
			        NonLinear analysis = new NonLinear(input);

                    // Draw results of analysis
                    Results.Draw(analysis);
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

		        DelimitedWriter.Write("D:/Ki.csv", Kg, ";");

                // Solve the initial displacements
                var u = Kg.Solve(0.01 * f);

                var uMatrix = Matrix<double>.Build.Dense(100, numDoFs);
		        var fiMatrix = Matrix<double>.Build.Dense(100, numDoFs);
                //var fstr = Matrix<double>.Build.Dense(4, 3);
                //var estr = Matrix<double>.Build.Dense(4, 2);
				var fPnl = Matrix<double>.Build.Dense(100, 8);
				var sigCPnl = Matrix<double>.Build.Dense(100, 12);
				var sigSPnl = Matrix<double>.Build.Dense(100, 12);
                var uPnl = Matrix<double>.Build.Dense(100, 8);
                var genStPnl = Matrix<double>.Build.Dense(100, 5);
                var epsPnl = Matrix<double>.Build.Dense(100, 12);
                var DcPnl = Matrix<double>.Build.Dense(1200, 12);
                var DsPnl = Matrix<double>.Build.Dense(1200, 12);

                // Initialize a loop for load steps
                for (int loadStep = 1; loadStep <= loadSteps; loadStep++)
				{
					// Calculate the current load factor
					double lf = Convert.ToDouble(loadStep) / loadSteps;

					// Get the force vector
					var fs = lf * f;

					Vector<double> fi = Vector<double>.Build.Dense(numDoFs);

					// Initiate iterations
					for (int it = 0; it < maxIterations; it++)
					{
						// Calculate element displacements and forces
						StringerAnalysis(u);
						PanelAnalysis(u);

						// Get the internal force vector
						fi = InternalForces();

						// Calculate residual forces
						var fr = fs - fi;

						// Check convergence
						if (EquilibriumConvergence(fr, it))
						{
							AutoCAD.edtr.WriteMessage("\nLS = " + loadStep + ": Iterations = " + it);
							break;
						}

						// Calculate displacement increment
						var du = Kg.Solve(fr);

						// Increment displacements
						u += du;
                    }

					var pnl = Panels[0] as Panel.NonLinear;

					fiMatrix.SetRow(loadStep - 1, fi);
					uMatrix.SetRow(loadStep - 1, u);
					fPnl.SetRow(loadStep - 1, pnl.Forces);
					sigCPnl.SetRow(loadStep - 1, pnl.StressVector.sigmaC);
					sigSPnl.SetRow(loadStep - 1, pnl.StressVector.sigmaS);
					epsPnl.SetRow(loadStep - 1, pnl.StrainVector);
					uPnl.SetRow(loadStep - 1, pnl.Displacements);
					genStPnl.SetRow(loadStep - 1, pnl.GenStrains);
					DcPnl.SetSubMatrix(12 * (loadStep - 1), 0, pnl.MaterialStiffness.Dc);
					DsPnl.SetSubMatrix(12 * (loadStep - 1), 0, pnl.MaterialStiffness.Ds);

                    // Set the results to stringers
                    StringerResults();

                    // Update stiffness
                    Kg = GlobalStiffness();

                    //if (loadStep < 56)
                    //{
                    //    foreach (Stringer.NonLinear stringer in Stringers)
                    //    {
                    //        fstr.SetRow(stringer.Number - 1, stringer.Forces);
                    //        estr.SetRow(stringer.Number - 1, new[] { stringer.GenStrains.e1, stringer.GenStrains.e3 });
                    //    }
                    //}
                }

                // Set nodal displacements
                NodalDisplacements(u);

                DelimitedWriter.Write("D:/K.csv", Kg, ";");
                DelimitedWriter.Write("D:/f.csv", f.ToColumnMatrix(), ";");
                DelimitedWriter.Write("D:/fi.csv", fiMatrix, ";");
                //DelimitedWriter.Write("D:/fstr.csv", fstr, ";");
                //DelimitedWriter.Write("D:/estr.csv", estr, ";");
                DelimitedWriter.Write("D:/u.csv", uMatrix, ";");
                DelimitedWriter.Write("D:/genStPnl.csv", genStPnl, ";");
                DelimitedWriter.Write("D:/sigCPnl.csv", sigCPnl, ";");
                DelimitedWriter.Write("D:/sigSPnl.csv", sigSPnl, ";");
                DelimitedWriter.Write("D:/epsPnl.csv", epsPnl, ";");

                //for (int i = 0; i < 4; i++)
                //{
                //    int n = i + 1;
                //    DelimitedWriter.Write("D:/fStr" + n + ".csv", fStrs[i], ";");
                //}

                DelimitedWriter.Write("D:/fPnl1.csv", fPnl, ";");
                DelimitedWriter.Write("D:/uPnl1.csv", uPnl, ";");
                DelimitedWriter.Write("D:/DcPnl1.csv", DcPnl, ";");
                DelimitedWriter.Write("D:/DsPnl1.csv", DsPnl, ";");
                //DelimitedWriter.Write("D:/sigPnl1.csv", sigPnl, ";");
                //DelimitedWriter.Write("D:/f1Pnl1.csv", f1Pnl, ";");
                //DelimitedWriter.Write("D:/e1Pnl1.csv", e1Pnl, ";");
                //DelimitedWriter.Write("D:/thetaPnl1.csv", thetaPnl, ";");
                //DelimitedWriter.Write("D:/DPnl1.csv", DPnl, ";");

                //DelimitedWriter.Write("D:/KPnl1.csv", KPnl, ";");
            }

			// Verify convergence
			private bool EquilibriumConvergence(Vector<double> residualForces, int iteration)
			{
				double
					maxForce  = residualForces.AbsoluteMaximum(),
					tolerance = MaxElementForce / 100;

                // Check convergence
                if (maxForce <= tolerance && iteration > 0)
					return true;

				// Else
				return false;
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
					int[] index = stringer.DoFIndex;
					var fs = stringer.IterationGlobalForces;

					// Add values
					AddForce(fs, index);
                }

                foreach (Panel.NonLinear panel in Panels)
				{
					// Get index and forces
					int[] index = panel.DoFIndex;
					var fp = panel.Forces;

					// Add values
					AddForce(fp, index);
				}

                // Add element forces to global vector
                void AddForce(Vector<double> elementForces, int[] dofIndex)
				{
					for (int i = 0; i < dofIndex.Length; i++)
					{
						// DoF index
						int j = dofIndex[i];

						// Add values
						fi[j] += elementForces[i];
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
