using System;
using System.Linq;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.Statistics;
using SPMTool.Elements;

namespace SPMTool.Analysis
{
	/// <summary>
    /// Types of analysis
    /// </summary>
	public enum AnalysisType
	{
		Linear,
		Nonlinear,
		Simulation
	}

    public abstract class Analysis
    {
		// Public Properties
		public Vector<double> DisplacementVector { get; set; }
		public Matrix<double> GlobalStiffness    { get; set; }
		public Node[]         Nodes              { get; }
        public Stringer[]     Stringers          { get; }
        public Panel[]        Panels             { get; }
        public Vector<double> ForceVector        { get; }
        public int[]          Constraints        { get; }

        // Constructor
        public Analysis(InputData inputData)
		{
            // Get elements
            Nodes       = inputData.Nodes;
			Stringers   = inputData.Stringers;
			Panels      = inputData.Panels;
			ForceVector = inputData.ForceVector;
			Constraints = inputData.ConstraintIndex;
		}

        // Get the number of DoFs
		protected int numDoFs => 2 * Nodes.Length;

		// Calculate maximum Stringer force
		public double MaxStringerForce
		{
			get
			{
				// Create a list to store the maximum Stringer forces
				var strForces = new List<double>();

				foreach (var str in Stringers)
					// Add to the list of forces
					strForces.Add(str.MaxForce);

				// Verify the maximum Stringer force
				return
					strForces.MaximumAbsolute();
			}
		}

		// Calculate maximum panel force
		public double MaxPanelForce
		{
			get
			{
				if (Panels.Length > 0)
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

				return 0;
			}
		}

		// Get maximum element force
		private double MaxElementForce => Math.Max(MaxStringerForce, MaxPanelForce);

        // Calculate Global Stiffness
        protected Matrix<double> Global_Stiffness(Vector<double> forceVector = null, bool simplify = true)
		{
			// Initialize the global stiffness matrix
			var Kg = Matrix<double>.Build.Dense(numDoFs, numDoFs);

			// Add Stringer stiffness to global stiffness
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
			if (simplify)
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

                // Simplification for internal nodes (There is only a displacement at the Stringer direction, the perpendicular one will be zero)
                if (node.Type == Node.NodeType.Internal)
                {
                    // Verify rows
                    foreach (int i in index)
                    {
                        // Verify what line of the matrix is composed of zeroes
                        if (!Kg.Row(i).Exists(GlobalAuxiliary.NotZero))
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

		// Calculate force vector including reactions
		public Vector<double> ForcesAndReactions(Vector<double> globalDisplacements = null, Matrix<double> globalStiffness = null)
		{
			globalDisplacements = globalDisplacements ?? DisplacementVector;

			// Get global stiffness not simplified
			globalStiffness = globalStiffness ?? Global_Stiffness(null, false);

			// Calculate forces and reactions
			var f = globalStiffness * globalDisplacements;
			f.CoerceZero(1E-9);

			return f;
		}

        // Set element displacements
        protected void ElementAnalysis(Vector<double> globalDisplacements)
        {
	        foreach (var stringer in Stringers)
				stringer.Analysis(globalDisplacements);

	        foreach (var panel in Panels)
				panel.Analysis(globalDisplacements);
        }

		// Set nodal displacements
		protected void NodalDisplacements(Vector<double> globalDisplacements)
		{
			foreach (var node in Nodes)
				node.SetDisplacements(globalDisplacements);
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

                    // Verify if it's other Stringer
                    if (num1 != num2)
                    {
                        // Create a tuple with the minimum Stringer number first
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
                                if (cont < -par) // continued Stringer
                                {
                                    // Add to the list
                                    contStrs.Add(contStr);
                                }
                            }

                            // Case 2: a Stringer initiate and the other end at the same node
                            if (str1.Grips[0] == str2.Grips[2] || str1.Grips[2] == str2.Grips[0])
                            {
                                // Get the direction cosines
                                var (l1, m1) = str1.DirectionCosines;
                                var (l2, m2) = str2.DirectionCosines;

                                // Calculate the condition of continuity
                                double cont = l1 * l2 + m1 * m2;

                                // Verify the condition
                                if (cont > par) // continued Stringer
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

	        return
		        contDofs;
        }

        private void PrintStiffness()
        {
	        foreach (var stringer in Stringers)
	        {
		        DelimitedWriter.Write("D:/Ks" + stringer.Number + ".csv", stringer.GlobalStiffness, ";");
	        }
	        foreach (var panel in Panels)
	        {
		        DelimitedWriter.Write("D:/Kp" + panel.Number + ".csv", panel.GlobalStiffness, ";");
	        }
        }

        // Get DoFs that are not continuous
        private int[] NotContinuousPanelDoFs
        {
	        get
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
        //      ACAD.Current.edtr.WriteMessage(msg);
        //     }

        // Linear analysis methods
    }
}
