using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
    public class InputData
    {
		// Properties
		public Material.Concrete Concrete    { get; set; }
        public Node[]            Nodes       { get; set; }
	    public Stringer[]        Stringers   { get; set; }
	    public Panel[]           Panels      { get; set; }
	    public Vector<double>    ForceVector { get; set; }
	    public List<int>         Constraints { get; set; }
	    public int               numDoFs     => 2 * Nodes.Length;

		// Private properties
		private ObjectIdCollection NodeObjects     { get; }
		private ObjectIdCollection StringerObjects { get; }
		private ObjectIdCollection PanelObjects    { get; }

		public InputData()
		{
			// Get the collection of elements in the model
			NodeObjects = Geometry.Node.UpdateNodes();
			StringerObjects = Geometry.Stringer.UpdateStringers();
			PanelObjects = Geometry.Panel.UpdatePanels();

			// Get concrete data
			Concrete = new Material.Concrete();

			// Read nodes, forces and constraints
			Nodes       = ReadNodes();
			ForceVector = ReadForces();
			Constraints = ConstraintList();
		}

        // Read the parameters of nodes
        private Node[] ReadNodes()
	    {
		    Node[] nodes = new Node[NodeObjects.Count];

		    foreach (ObjectId ndObj in NodeObjects)
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
			        f[i]     = nd.Force.X * 1000;
			        f[i + 1] = nd.Force.Y * 1000;
		        }
	        }

	        return f;
        }

		// Get constraint list
		private List<int> ConstraintList()
		{
			var constraintList = new List<int>();

			foreach (var nd in Nodes)
			{
				// Get the index of the row
				int i = 2 * nd.Number - 2;

				// Simplify the matrices removing the rows that have constraints (external nodes)
				if (nd.Type == (int) Node.NodeType.External)
				{
					if (nd.Support.X)
						// There is a support in X direction
						constraintList.Add(i);

					if (nd.Support.Y)
						// There is a support in Y direction
						constraintList.Add(i + 1);
				}

			}

			return constraintList.OrderBy(i => i).ToList();
		}

		public class Linear : InputData
	    {
			public Linear()
			{
                // Get linear elements
                Stringers = ReadStringers();
				Panels    = ReadPanels();
			}

            // Read linear parameters stringers
            private Stringer[] ReadStringers()
			{
				Stringer[] stringers = new Stringer[StringerObjects.Count];

				foreach (ObjectId strObj in StringerObjects)
				{
					Stringer stringer = new Stringer.Linear(strObj, Concrete);

					// Set to the array
					int i = stringer.Number - 1;
					stringers[i] = stringer;
				}

				// Return the stringers
				return stringers;
			}

			// Read linear parameters of panels
			private Panel[] ReadPanels()
			{
				Panel[] panels = new Panel[PanelObjects.Count];

				foreach (ObjectId pnlObj in PanelObjects)
				{
					Panel panel = new Panel.Linear(pnlObj, Concrete);

					// Set to the array
					int i = panel.Number - 1;
					panels[i] = panel;
				}

				return panels;
			}
        }

        public class NonLinear : InputData
        {
	        public NonLinear()
	        {
		        // Get nonlinear elements
		        Stringers = ReadStringers();
		        Panels    = ReadPanels();
	        }

            // Read nonlinear parameters stringers
            private Stringer[] ReadStringers()
	        {
		        Stringer[] stringers = new Stringer[StringerObjects.Count];

		        foreach (ObjectId strObj in StringerObjects)
		        {
			        Stringer stringer = new Stringer.NonLinear(strObj, Concrete);

			        // Set to the array
			        int i = stringer.Number - 1;
			        stringers[i] = stringer;
		        }

		        // Return the stringers
		        return stringers;
	        }

	        // Read nonlinear parameters of panels
	        private Panel[] ReadPanels()
	        {
		        Panel[] panels = new Panel[PanelObjects.Count];

		        foreach (ObjectId pnlObj in PanelObjects)
		        {
			        Panel panel = new Panel.NonLinear(pnlObj, Concrete, Stringers);

			        // Set to the array
			        int i = panel.Number - 1;
			        panels[i] = panel;
		        }

		        return panels;
	        }
        }
    }
}
