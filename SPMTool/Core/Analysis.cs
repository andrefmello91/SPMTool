using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.Statistics;
using SPMTool.UserInterface;

namespace SPMTool.Core
{
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
        public Analysis(InputData inputData = null)
		{
			// Get input data
			if (inputData == null)
				inputData = new InputData(Stringer.Behavior.Linear, Panel.Behavior.Linear);

            // Get elements
            Nodes       = inputData.Nodes;
			Stringers   = inputData.Stringers;
			Panels      = inputData.Panels;
			ForceVector = inputData.ForceVector;
			Constraints = inputData.ConstraintIndex;
		}

		// Get the number of DoFs
		private int numDoFs => 2 * Nodes.Length;

        // Calculate Global Stiffness
        private Matrix<double> Global_Stiffness(Vector<double> forceVector = null, bool simplify = true)
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
			if (globalDisplacements == null)
				globalDisplacements = DisplacementVector;

			// Get global stiffness not simplified
			if (globalStiffness == null)
				globalStiffness = Global_Stiffness(null, false);

			// Calculate forces and reactions
			var f = globalStiffness * globalDisplacements;
			f.CoerceZero(1E-9);

			return f;
		}

        // Set element displacements
        private void ElementAnalysis(Vector<double> globalDisplacements)
        {
	        foreach (var stringer in Stringers)
	        {
		        stringer.Displacement(globalDisplacements);
				stringer.Analysis();
	        }

	        foreach (var panel in Panels)
	        {
		        panel.Displacement(globalDisplacements);
				panel.Analysis();
	        }
        }

		// Set nodal displacements
		private void NodalDisplacements(Vector<double> globalDisplacements)
		{
			foreach (var node in Nodes)
				node.Displacements(globalDisplacements);
		}

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

        public sealed class Linear : Analysis
        {
	        public Linear(InputData inputData = null, double loadFactor = 1) : base(inputData)
            {
                // Get force Vector
                var f = loadFactor * ForceVector;

	            // Calculate and simplify global stiffness and force vector
	            GlobalStiffness = Global_Stiffness(f);

	            // Solve
	            DisplacementVector = GlobalStiffness.Solve(f);

	            // Calculate element displacements and forces
	            ElementAnalysis(DisplacementVector);

				// Set nodal displacements
				NodalDisplacements(DisplacementVector);
            }
        }

        public sealed class NonLinear : Analysis
        {
            // Properties
            public List<double> MonitoredDisplacements { get; set; }
            public List<double> MonitoredLoadFactor    { get; set; }

            // Max iterations and load steps
            private int maxIterations = 1000;
			private int loadSteps     = 50;

            public NonLinear(InputData inputData) : base(inputData)
	        {
				// Initiate lists
				MonitoredDisplacements = new List<double>();
				MonitoredLoadFactor    = new List<double>();

                // Get force vector
                var f = ForceVector;

                // Get the initial stiffness and force vector simplified
                var Kg = Global_Stiffness(f);

                //DelimitedWriter.Write("D:/Ki.csv", Kg, ";");

                // Solve the initial displacements
                double lf0 = (double) 1 / loadSteps;
                var u0 = Kg.Solve(lf0 * f);
                var ui = u0;

                DelimitedWriter.Write("D:/u0.csv", u0.ToColumnMatrix(), ";");

				var uMatrix = Matrix<double>.Build.Dense(100, numDoFs);
		        var fiMatrix = Matrix<double>.Build.Dense(100, numDoFs);
                var fstr = Matrix<double>.Build.Dense(4, 3);
                var estr = Matrix<double>.Build.Dense(4, 2);
                var fPnl = Matrix<double>.Build.Dense(100, 8);
                var sigCPnl = Matrix<double>.Build.Dense(100, 12);
                var sigSPnl = Matrix<double>.Build.Dense(100, 12);
                var uPnl = Matrix<double>.Build.Dense(100, 8);
                var genStPnl = Matrix<double>.Build.Dense(100, 5);
                var epsPnl = Matrix<double>.Build.Dense(100, 12);
                var DcPnl = Matrix<double>.Build.Dense(1200, 12);
                var DsPnl = Matrix<double>.Build.Dense(1200, 12);
                var thetaPnl1 = Matrix<double>.Build.Dense(100, 4);

                // Initialize a loop for load steps
                for (int loadStep = 1; loadStep <= loadSteps; loadStep++)
				{
                    // Calculate the current load factor
                    double lf = (double)loadStep / loadSteps;

                    // Get the force vector
                    var fs = lf * f;

					Vector<double> fi = Vector<double>.Build.Dense(numDoFs);

					// Initiate iterations
					for (int it = 0; it < maxIterations; it++)
					{
						// Calculate element displacements and forces
						ElementAnalysis(ui);

                        // Get the internal force vector
                        fi = InternalForces();

						// Calculate residual forces
						var fr = fs - fi;

						// Check convergence
						if (EquilibriumConvergence(fr, ui - u0, it))
						{
							AutoCAD.Current.edtr.WriteMessage("\nLS = " + loadStep + ": Iterations = " + it);
                            MonitoredDisplacements.Add(ui[14]);
                            MonitoredLoadFactor.Add(lf);
                            break;
						}

						// Set initial displacements
						u0 = ui;

						// Calculate displacement increment
						var du = Kg.Solve(fr);

						// Increment displacements
						ui += du;
                    }

                    var pnl = (Panel.NonLinear) Panels[0];

                    fiMatrix.SetRow(loadStep - 1, fi);
                    uMatrix.SetRow(loadStep - 1, ui);
                    fPnl.SetRow(loadStep - 1, pnl.Forces);
                    sigCPnl.SetRow(loadStep - 1, pnl.StressVector.sigmaC);
                    sigSPnl.SetRow(loadStep - 1, pnl.StressVector.sigmaS);
                    epsPnl.SetRow(loadStep - 1, pnl.StrainVector);
                    uPnl.SetRow(loadStep - 1, pnl.Displacements);
                    DcPnl.SetSubMatrix(12 * (loadStep - 1), 0, pnl.MaterialStiffness.Dc);
                    DsPnl.SetSubMatrix(12 * (loadStep - 1), 0, pnl.MaterialStiffness.Ds);

                    var thetaPnl = new double[4];

                    for (int i = 0; i < 4; i++)
                        thetaPnl[i] = pnl.IntegrationPoints[i].PrincipalAngles.theta2;

                    thetaPnl1.SetRow(loadStep - 1, thetaPnl);

                    // Set the results to elements
                    Results();

                    // Update stiffness
                    Kg = Global_Stiffness();


                    //if (loadStep < 56)
                    //{
                    //    foreach (Stringer.NonLinear Stringer in Stringers)
                    //    {
                    //        fstr.SetRow(Stringer.Number - 1, Stringer.Forces);
                    //        estr.SetRow(Stringer.Number - 1, new[] { Stringer.GenStrains.e1, Stringer.GenStrains.e3 });
                    //    }
                    //}
                }

                //uWindow.AddRange(uList.ToArray(), lfList.ToArray());

                // Set nodal displacements
                NodalDisplacements(ui);

                DelimitedWriter.Write("D:/K.csv", Kg, ";");
                DelimitedWriter.Write("D:/f.csv", f.ToColumnMatrix(), ";");
                DelimitedWriter.Write("D:/fi.csv", fiMatrix, ";");
                //DelimitedWriter.Write("D:/fstr.csv", fstr, ";");
                //DelimitedWriter.Write("D:/estr.csv", estr, ";");
                DelimitedWriter.Write("D:/u.csv", uMatrix, ";");
                //DelimitedWriter.Write("D:/genStPnl.csv", genStPnl, ";");
                DelimitedWriter.Write("D:/sigCPnl.csv", sigCPnl, ";");
                DelimitedWriter.Write("D:/sigSPnl.csv", sigSPnl, ";");
                DelimitedWriter.Write("D:/epsPnl.csv", epsPnl, ";");

                //for (int i = 0; i < 4; i++)
                //{
                //    int n = i + 1;
                //    DelimitedWriter.Write("D:/fStr" + n + ".csv", fStrs[i], ";");
                //}

                //DelimitedWriter.Write("D:/fPnl1.csv", fPnl, ";");
                //DelimitedWriter.Write("D:/uPnl1.csv", uPnl, ";");
                //DelimitedWriter.Write("D:/DcPnl1.csv", DcPnl, ";");
                //DelimitedWriter.Write("D:/DsPnl1.csv", DsPnl, ";");
                //DelimitedWriter.Write("D:/sigPnl1.csv", sigPnl, ";");
                //DelimitedWriter.Write("D:/f1Pnl1.csv", f1Pnl, ";");
                //DelimitedWriter.Write("D:/e1Pnl1.csv", e1Pnl, ";");
                DelimitedWriter.Write("D:/thetaPnl1.csv", thetaPnl1, ";");
                //DelimitedWriter.Write("D:/DPnl1.csv", DPnl, ";");

                //DelimitedWriter.Write("D:/KPnl1.csv", KPnl, ";");
            }

            // Set element displacements
            //public override void ElementAnalysis(Vector<double> globalDisplacements)
            //{
	           // foreach (Stringer.NonLinear stringer in Stringers)
	           // {
		          //  stringer.Displacement(globalDisplacements);
		          //  stringer.Analysis();
	           // }

	           // foreach (Panel.NonLinear panel in Panels)
	           // {
		          //  panel.Displacement(globalDisplacements);
		          //  panel.Analysis();
	           // }
            //}

            // Verify convergence
            private bool EquilibriumConvergence(Vector<double> residualForces, Vector<double> residualDisplacements, int iteration)
			{
				double
					maxForce = residualForces.AbsoluteMaximum(),
					maxDisp  = residualDisplacements.AbsoluteMaximum(),
					fTol     = 0.01,
					uTol     = 0.01;

                // Check convergence
                if (maxForce <= fTol && maxDisp <= uTol && iteration > 1)
					return true;

				// Else
				return false;
			}

			// Get the internal force vector
			private Vector<double> InternalForces()
			{
				var fi = Vector<double>.Build.Dense(numDoFs);

				foreach (Stringer.NonLinear stringer in Stringers)
				{
					// Get index and forces
					int[] index = stringer.DoFIndex;
					var fs = stringer.GlobalForces;

					// Add values
					AddForce(fs, index);
                }

				if (Panels.Length > 0)
				{
					foreach (Panel.NonLinear panel in Panels)
					{
						// Get index and forces
						int[] index = panel.DoFIndex;
						var fp = panel.Forces;

						// Add values
						AddForce(fp, index);
					}
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

			// Set the results for each Stringer
			private void Results()
			{
                //foreach (Stringer.NonLinear stringer in Stringers)
                //    stringer.Results();

                foreach (Panel.NonLinear panel in Panels)
					panel.Results();
			}
        }
    }
}
