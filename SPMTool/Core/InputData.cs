using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.Core;
using SPMTool.Material;
using SPMTool.AutoCAD;

namespace SPMTool.Core
{
    public class InputData
    {
		// Properties
		public Concrete       Concrete        { get; }
        public Node[]         Nodes           { get; }
	    public Stringer[]     Stringers       { get; }
	    public Panel[]        Panels          { get; }
	    public Force[]        Forces          { get; }
	    public Constraint[]   Constraints     { get; }
	    public Vector<double> ForceVector     { get; }
	    public int[]          ConstraintIndex { get; }
	    public int            numDoFs         => 2 * Nodes.Length;

		// Private properties
		private ObjectIdCollection NodeObjects      { get; }
		private ObjectIdCollection StringerObjects  { get; }
		private ObjectIdCollection PanelObjects     { get; }
		private Stringer.Behavior  StringerBehavior { get; }
		private Panel.Behavior     PanelBehavior    { get; }

		public InputData(Stringer.Behavior stringerBehavior, Panel.Behavior panelBehavior)
		{
			// Get the collection of elements in the model
			NodeObjects     = Geometry.Node.UpdateNodes();
			StringerObjects = Geometry.Stringer.UpdateStringers();
			PanelObjects    = Geometry.Panel.UpdatePanels();

            // Read forces and constraints
            Forces      = Force.ListOfForces();
            Constraints = Constraint.ListOfConstraints();

			// Get concrete data
			Concrete = AutoCAD.Material.ReadConcreteData();

			// Set the Behavior of elements
			StringerBehavior = stringerBehavior;
			PanelBehavior    = panelBehavior;

			// Read nodes, forces and constraints indexes
			Nodes           = ReadNodes();
			ForceVector     = ReadForces();
			ConstraintIndex = ConstraintsIndex();

			// Read elements
			Stringers = ReadStringers();
			Panels    = ReadPanels();
		}

        // Read the parameters of nodes
        private Node[] ReadNodes()
	    {
		    Node[] nodes = new Node[NodeObjects.Count];

		    foreach (ObjectId ndObj in NodeObjects)
		    {
			    Node node = new Node(ndObj, Forces, Constraints);

			    // Set to nodes
			    int i    = node.Number - 1;
			    nodes[i] = node;
		    }

		    // Return the nodes
		    return nodes;
	    }

        // Read parameters stringers
        private Stringer[] ReadStringers()
        {
	        Stringer[] stringers = new Stringer[StringerObjects.Count];

	        foreach (ObjectId strObj in StringerObjects)
	        {
		        Stringer stringer;

                // Verify the stringer Behavior
                if (StringerBehavior == Stringer.Behavior.Linear)
					stringer = new Stringer.Linear(strObj, Concrete);

				else if (StringerBehavior == Stringer.Behavior.NonLinearClassic)
					stringer = new Stringer.NonLinear.Classic(strObj, Concrete);

                else
					stringer = new Stringer.NonLinear.MC2010(strObj, Concrete);

				// Set to the array
                int i = stringer.Number - 1;
		        stringers[i] = stringer;
	        }

	        // Return the stringers
	        return stringers;
        }

        // Read parameters of panels
        private Panel[] ReadPanels()
        {
	        Panel[] panels = new Panel[PanelObjects.Count];

	        foreach (ObjectId pnlObj in PanelObjects)
	        {
		        Panel panel;

				// Verify the panelBehavior
				if (PanelBehavior == Panel.Behavior.Linear)
					panel = new Panel.Linear(pnlObj, Concrete);

				else if (PanelBehavior == Panel.Behavior.NonLinearMCFT)
					panel = new Panel.NonLinear(pnlObj, Concrete, Stringers);

				else
					panel = new Panel.NonLinear(pnlObj, Concrete, Stringers, Panel.Behavior.NonLinearDSFM);

                // Set to the array
                int i     = panel.Number - 1;
		        panels[i] = panel;
	        }

	        return panels;
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
		        if (nd.Type == Node.NodeType.External && (nd.Force.X != 0 || nd.Force.Y != 0))
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
		private int[] ConstraintsIndex()
		{
			var constraintList = new List<int>();

			foreach (var node in Nodes)
			{
				// Get DoF indexes
				var index = node.DoFIndex;
				int
					i = index[0],
					j = index[1];

				// Simplify the matrices removing the rows that have constraints (external nodes)
				if (node.Type == Node.NodeType.External)
				{
					if (node.Support.X)
						// There is a support in X direction
						constraintList.Add(i);

					if (node.Support.Y)
						// There is a support in Y direction
						constraintList.Add(j);
				}
			}

			return
				constraintList.OrderBy(i => i).ToArray();
		}
    }
}
