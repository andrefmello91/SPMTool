using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Material.Concrete;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;
using SPMTool.Elements;
using AnalysisType       = SPMTool.Analysis.AnalysisType;

namespace SPMTool.Analysis
{
    public class InputData
    {
		// Properties
		public Units          Units                { get; }
		public Parameters     ConcreteParameters   { get; }
		public Constitutive   ConcreteConstitutive { get; }
        public Node[]         Nodes                { get; }
	    public Stringer[]     Stringers            { get; }
	    public Panel[]        Panels               { get; }
	    public Force[]        Forces               { get; }
	    public Constraint[]   Constraints          { get; }
	    public Vector<double> ForceVector          { get; }
	    public int[]          ConstraintIndex      { get; }
	    public int            numDoFs              => 2 * Nodes.Length;

		// Private properties
		private ObjectIdCollection NodeObjects      { get; }
		private ObjectIdCollection StringerObjects  { get; }
		private ObjectIdCollection PanelObjects     { get; }

		public InputData(AnalysisType analysisType)
		{
			// Get units
			Units = Config.ReadUnits() ?? new Units();

			// Get the collection of elements in the model
			NodeObjects     = Geometry.Node.UpdateNodes(Units);
			StringerObjects = Geometry.Stringer.UpdateStringers();
			PanelObjects    = Geometry.Panel.UpdatePanels();

            // Read forces and constraints
            Forces      = Force.ListOfForces(Units.AppliedForces);
            Constraints = Constraint.ListOfConstraints();

			// Get concrete data
			(ConcreteParameters, ConcreteConstitutive) = AutoCAD.Material.ReadConcreteData().Value;

			// Read nodes, forces and constraints indexes
			Nodes           = ReadNodes();
			ForceVector     = ReadForceVector();
			ConstraintIndex = ConstraintsIndex();

			// Read elements
			Stringers = ReadStringers(analysisType);
			Panels    = ReadPanels(analysisType);
		}

		// Read the parameters of nodes
        private Node[] ReadNodes()
	    {
		    Node[] nodes = new Node[NodeObjects.Count];

		    foreach (ObjectId ndObj in NodeObjects)
		    {
			    Node node = new Node(ndObj, Units, Forces, Constraints);

			    // Set to nodes
			    int i    = node.Number - 1;
			    nodes[i] = node;
		    }

		    // Return the nodes
		    return nodes;
	    }

        // Read parameters stringers
        private Stringer[] ReadStringers(AnalysisType analysisType)
        {
	        Stringer[] stringers = new Stringer[StringerObjects.Count];

	        foreach (ObjectId strObj in StringerObjects)
	        {
		        Stringer stringer = Stringer.ReadStringer(analysisType, strObj, Units, ConcreteParameters, ConcreteConstitutive);

				// Set to the array
                int i = stringer.Number - 1;
		        stringers[i] = stringer;
	        }

	        // Return the stringers
	        return stringers;
        }

        // Read parameters of panels
        private Panel[] ReadPanels(AnalysisType analysisType)
        {
	        Panel[] panels = new Panel[PanelObjects.Count];

	        foreach (ObjectId pnlObj in PanelObjects)
	        {
		        var panel = Panel.ReadPanel(analysisType, pnlObj, Units, ConcreteParameters, ConcreteConstitutive, Stringers);

		        // Set to the array
                int i     = panel.Number - 1;
		        panels[i] = panel;
	        }

	        return panels;
        }

        // Get the force vector
        private Vector<double> ReadForceVector()
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
			        f[i]     = nd.Force.X;
			        f[i + 1] = nd.Force.Y;
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
