using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;
using NodeData       = SPMTool.XData.Node;
using ForceDirection = SPMTool.Core.Force.ForceDirection;

namespace SPMTool.Core
{
    public class Node
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
        public ObjectId             ObjectId     { get; }
	    public int                  Number       { get; }
	    public NodeType             Type         { get; }
	    public Point3d              Position     { get; }
	    public (bool X, bool Y)     Support      { get; }
	    public (double X, double Y) Force        { get; }
	    public (double X, double Y) Displacement { get; set; }

		// Constructor
		public Node(ObjectId nodeObject, Force[] forces = null, Constraint[] constraints = null)
		{
			ObjectId = nodeObject;

			if (forces == null)
				forces = Core.Force.ListOfForces();

			if (constraints == null)
				constraints = Constraint.ListOfConstraints();

			// Read the object as a point
			DBPoint ndPt =  Geometry.Node.ReadNode(nodeObject);

			// Read the XData and get the necessary data
			TypedValue[] data = Auxiliary.ReadXData(ndPt);

			// Get the position
			Position = ndPt.Position;

			// Get the node number
			Number = Convert.ToInt32(data[(int) NodeData.Number].Value);

			// Get type
			Type = GetNodeType(ndPt);

			// Get support conditions
			Support = GetSupportConditions(constraints);

			// Get forces
			Force = GetNodalForces(forces);

			// Get displacements
			double
				ux = Convert.ToDouble(data[(int) NodeData.Ux].Value),
				uy = Convert.ToDouble(data[(int) NodeData.Uy].Value);

			Displacement = (ux, uy);
		}

		// Get index of DoFs
		public int[] DoFIndex => GlobalAuxiliary.GlobalIndexes(Number);

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
        public void Displacements(Vector<double> displacementVector)
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
        private (double X, double Y) GetNodalForces(Force[] forces)
        {
            (double Fx, double Fy) = (0, 0);

            foreach (var force in forces)
            {
                if (force.Position == Position)
                {
                    // Read force
                    if (force.Direction == Core.Force.ForceDirection.X)
                        Fx = force.Value;

                    if (force.Direction == Core.Force.ForceDirection.Y)
                        Fy = force.Value;
                }
            }

            return
                (Fx, Fy);
        }

        // Get support conditions
        private (bool X, bool Y) GetSupportConditions(Constraint[] constraints)
        {
            var support = (false, false);

            foreach (var constraint in constraints)
            {
                if (constraint.Position == Position)
                {
                    support = constraint.Direction;
                    break;
                }
            }

            return
                support;
        }
    }
}
