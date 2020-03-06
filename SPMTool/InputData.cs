using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
    public class InputData
    {
		// Properties
		public Material.Concrete Concrete    { get; set; }
		public Material.Concrete Steel       { get; set; }
        public Node[]            Nodes       { get; set; }
	    public Stringer[]        Stringers   { get; set; }
	    public Panel[]           Panels      { get; set; }
	    public Vector<double>    ForceVector { get; set; }
	    public int               numDoFs     => 2 * Nodes.Length;

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

        public class Linear : InputData
	    {
			public Linear()
			{
				// Get the collection of elements in the model
				ObjectIdCollection
					nodeObjects     = Geometry.Node.UpdateNodes(),
					stringerObjects = Geometry.Stringer.UpdateStringers(),
					panelObjects    = Geometry.Panel.UpdatePanels();

				// Get concrete data
				Concrete = new Material.Concrete();

                // Get linear elements
                Nodes       = ReadNodes(nodeObjects);
                Stringers   = ReadStringers(stringerObjects, Concrete);
				Panels      = ReadPanels(panelObjects, Concrete);
				ForceVector = ReadForces();
			}

            // Read linear parameters stringers
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

			// Read linear parameters of panels
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
        }
    }
}
