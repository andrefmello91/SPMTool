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
        public Matrix<double>     GlobalStiffness    { get; set; }
		public Vector<double>     DisplacementVector { get; set; }
		public Node[]             Nodes              { get; set; }
        public Stringer[]         Stringers          { get; set; }
        public Panel[]            Panels             { get; set; }

		// Constructor
		public Analysis(ObjectIdCollection nodeObjects, ObjectIdCollection stringerObjects, ObjectIdCollection panelObjects, Material.Concrete concrete, Material.Steel steel)
		{
			// Get nodes
			Nodes = ReadNodes(nodeObjects);
		}

		// Get the number of DoFs
		private int numDoFs => 2 * Nodes.Length;

        // Read the parameters of nodes
        private Node[] ReadNodes(ObjectIdCollection nodeObjects)
        {
            Node[] nodes = new Node[nodeObjects.Count];

            foreach (ObjectId ndObj in nodeObjects)
            {
	            Node node = new Node(ndObj);

	            // Set to nodes
	            int i = node.Number - 1;
	            nodes[i] = node;
            }

            // Return the nodes
            return nodes;
        }

        // Get the force vector
        public Vector<double> ForceVector => ReadForces();
        private Vector<double> ReadForces()
		{
			// Initialize the force vector with size 2x number of DoFs (forces in x and y)
			var f = Vector<double>.Build.Dense(numDoFs);

			// Read the nodes data
			foreach (var nd in Nodes)
			{
				// Check if it's a external node
				if (nd.Type == (int)Node.NodeType.External && (nd.Force.X != 0 || nd.Force.Y != 0))
				{
					// Get the position in the vector
					int i = 2 * nd.Number - 2;

					// Read the forces in x and y (transform in N) and assign the values in the force vector at position (i) and (i + 1)
					f[i] = nd.Force.X * 1000;
					f[i + 1] = nd.Force.Y * 1000;
				}
			}

			return f;
		}

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
        public class Linear:Analysis
        {
	        [CommandMethod("DoLinearAnalysis")]
	        public static void DoLinearAnalysis()
	        {
		        // Get the collection of elements in the model
		        ObjectIdCollection
			        nodeObjects     = Geometry.Node.UpdateNodes(),
			        stringerObjects = Geometry.Stringer.UpdateStringers(),
			        panelObjects    = Geometry.Panel.UpdatePanels();

		        // Get concrete data
		        Material.Concrete concrete = new Material.Concrete();

		        if (concrete.IsSet)
		        {
			        // Do a linear analysis
			        Linear analysis = new Linear(nodeObjects, stringerObjects, panelObjects, concrete);

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

	        public Linear(ObjectIdCollection nodeObjects, ObjectIdCollection stringerObjects, ObjectIdCollection panelObjects, Material.Concrete concrete, Material.Steel steel = null) : base(nodeObjects, stringerObjects, panelObjects, concrete, steel)
            {
                // Get linear elements
                Stringers = ReadStringers(stringerObjects, concrete);
                Panels = ReadPanels(panelObjects, concrete);

	            // Get force Vector
	            var forceVector = ForceVector;

	            // Calculate and simplify global stiffness and force vector
	            GlobalStiffness = GlobalSStiffness(forceVector);

	            // Solve
	            DisplacementVector = GlobalStiffness.Solve(forceVector);
            }

            // Read the linear parameters of a stringer
            private Stringer[] ReadStringers(ObjectIdCollection stringerObjects, Material.Concrete concrete)
            {
	            Stringer[] stringers = new Stringer[stringerObjects.Count];

	            foreach (ObjectId strObj in stringerObjects)
	            {
		            Stringer stringer = new Stringer.Linear(strObj, concrete);

		            // Set to the array
		            int i = stringer.Number - 1;
		            stringers[i] = stringer;
	            }

	            // Return the stringers
	            return stringers;
            }

            // Read the parameters of a collection of panel objects
            private Panel[] ReadPanels(ObjectIdCollection panelObjects, Material.Concrete concrete)
            {
	            Panel[] panels = new Panel[panelObjects.Count];

	            foreach (ObjectId pnlObj in panelObjects)
	            {
		            Panel panel = new Panel.Linear(pnlObj, concrete);

		            // Set to the array
		            int i = panel.Number - 1;
		            panels[i] = panel;
	            }

	            return panels;
            }

            //// Calculate the stiffness matrix stringers, save to XData and add to global stiffness matrix, set the matrices to each element
            //private void StringersStiffness()
            //{
	           // // Calculate linear properties
	           // foreach (var str in Stringers)
		          //  str.LinearStringer = new Stringer.Linear(str, Material.Concrete);
            //}

            //// Calculate the stiffness matrix of a panel, get the dofs and save to XData, returns the all the matrices in an ordered list
            //public void PanelsStiffness()
            //{
	           // // Calculate linear properties
	           // foreach (var pnl in Panels)
		          //  pnl.LinearPanel = new Panel.Linear(pnl, Material.Concrete);
            //}

            //// Do a linear analysis and return the vector of displacements
            //public static Vector<double> LinearAnalysis(Node[] nodes, Stringer[] stringers, Panel[] panels, Vector<double> forceVector)
            //{
            //    // Get the elastic modulus
            //    double Ec = Concrete.Eci;

            //    // Calculate the approximated shear modulus (elastic material)
            //    double Gc = Ec / 2.4;

            //    // Get the number of DoFs
            //    int numDofs = 2 * nodes.Length;

            //    // Initialize the global stiffness matrix
            //    var Kg = Matrix<double>.Build.Dense(numDofs, numDofs);

            //    // Calculate the stiffness of each stringer and panel, add to the global stiffness and get the matrices of the stiffness of elements
            //    Stringer.Linear.StringersStiffness(stringers, Concrete, Kg);
            //    Panel.Linear.PanelsStiffness(panels, Concrete, Kg);

            //    // Simplify the stiffness matrix
            //    SimplifyStiffnessMatrix(Kg, forceVector, nodes);

            //    // Solve the system
            //    return Kg.Solve(forceVector);
            //}
        }
    }
}
