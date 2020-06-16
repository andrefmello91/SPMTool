using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;
using NodeData       = SPMTool.XData.Node;
using ForceDirection = SPMTool.Core.Force.ForceDirection;

namespace SPMTool.Core
{
    public class Node : SPMElement
    {
        // Node types (All excludes displaced)
        public enum NodeType
	    {
		    All,
		    External,
		    Internal,
			Displaced
	    }

	    // Properties
	    public NodeType             Type         { get; }
	    public Point3d              Position     { get; }
		public Constraint	        Constraint   { get; }
		public (Force X, Force Y)	Forces       { get; }
	    public (double X, double Y) Displacement { get; set; }

		// Constructor
		public Node(ObjectId nodeObject, Force[] forces = null, Constraint[] constraints = null)
		{
			ObjectId = nodeObject;

			forces = forces ?? Core.Force.ListOfForces();

			constraints = constraints ?? Constraint.ListOfConstraints();

			// Read the object as a point
			DBPoint ndPt = Geometry.Node.ReadNode(nodeObject);

			// Read the XData and get the necessary data
			TypedValue[] data = Auxiliary.ReadXData(ndPt);

			// Get the position
			Position = ndPt.Position;

			// Get the node number
			Number = Convert.ToInt32(data[(int) NodeData.Number].Value);

			// Get type
			Type = GetNodeType(ndPt);

            // Get support conditions
            Constraint = GetSupportConditions(constraints);

			// Get forces
			Forces = GetNodalForces(forces);

			// Get displacements
			double
				ux = Convert.ToDouble(data[(int) NodeData.Ux].Value),
				uy = Convert.ToDouble(data[(int) NodeData.Uy].Value);

			Displacement = (ux, uy);
		}

        // Get support condition
        public (bool X, bool Y) Support
        {
	        get
	        {
		        if (Constraint == null)
			        return
				        (false, false);

		        return
			        Constraint.Direction;
	        }
        }

		// Verify if node is free
		public bool IsFree => Support == (false, false);

		// Verify if displacement is set
		public bool DisplacementSet => Displacement != (0, 0);

		// Read Forces
		public (double X, double Y) Force
		{
			get
			{
				double
					Fx = 0,
					Fy = 0;

				if (Forces.X != null)
					Fx = Forces.X.Value;

				if (Forces.Y != null)
					Fy = Forces.Y.Value;

				return (Fx, Fy);
			}
		}

        // Verify if forces are not zero
        public bool ForcesSet => Force != (0, 0);

        // Get index of DoFs
        public override int[] DoFIndex => GlobalAuxiliary.GlobalIndexes(Number);

        // Get node type
        private NodeType GetNodeType(DBPoint nodePoint)
        {
            if (nodePoint.Layer == Geometry.Node.ExtNodeLayer)
                return
                    NodeType.External;

            return
                NodeType.Internal;
        }

        // Get nodal displacements
        public void SetDisplacements(Vector<double> displacementVector)
        {
	        var u = displacementVector;

	        // Get the index of the node on the list
	        var index = DoFIndex;
	        int 
		        i = index[0],
		        j = index[1];

	        // Get the displacements
	        double
		        ux = Math.Round(u[i], 6),
		        uy = Math.Round(u[j], 6);

	        // Save to the node
	        Displacement = (ux, uy);
        }

        // Get forces
        private (Force X, Force Y) GetNodalForces(Force[] forces)
        {
	        Force Fx = null;
	        Force Fy = null;

            foreach (var force in forces)
            {
                if (force.Position == Position)
                {
                    // Read force
                    if (force.Direction == ForceDirection.X)
                        Fx = force;

                    if (force.Direction == ForceDirection.Y)
                        Fy = force;
                }
            }

            return
                (Fx, Fy);
        }

        // Get support conditions
        Constraint GetSupportConditions(Constraint[] constraints)
        {
	        Constraint support = null;

            foreach (var constraint in constraints)
            {
                if (constraint.Position == Position)
                {
                    support = constraint;
                    break;
                }
            }

            return
                support;
        }

        public override string ToString()
        {
	        // Get the position
	        double
		        x = Math.Round(Position.X, 2),
		        y = Math.Round(Position.Y, 2);

	        string msgstr =
		        "Node " + Number + "\n\n" +
		        "Position: (" + x + ", " + y + ")";

	        // Read applied forces
	        if (ForcesSet)
	        {
		        msgstr +=
			        "\n\nApplied forces:";

		        if (Forces.X != null)
			        msgstr += "\n" + Forces.X;

		        if (Forces.Y != null)
			        msgstr += "\n" + Forces.Y;
	        }

	        // Get supports
	        if (!IsFree)
		        msgstr += "\n\n" + Constraint;

	        // Get displacements
	        if (DisplacementSet)
	        {
		        // Approximate displacements
		        double
			        ux = Math.Round(Displacement.X, 2),
			        uy = Math.Round(Displacement.Y, 2);

		        msgstr +=
			        "\n\nDisplacements:\n" +
			        "ux = " + ux + " mm" + "\n" +
			        "uy = " + uy + " mm";
	        }

	        return msgstr;
        }
    }
}
