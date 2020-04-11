using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
    public class Node
    {
	    // Node types
	    public enum NodeType
	    {
		    All = 0,
		    External  = 1,
		    Internal  = 2,
			Displaced = 3
	    }

	    // Properties
        public ObjectId             ObjectId     { get; }
	    public int                  Number       { get; }
	    public int                  Type         { get; }
	    public Point3d              Position     { get; }
	    public (bool X, bool Y)     Support      { get; }
	    public (double X, double Y) Force        { get; }
	    public (double X, double Y) Displacement { get; set; }

		// Constructor
		public Node(ObjectId nodeObject, Force[] forces = null, Constraint[] constraints = null)
		{
			ObjectId = nodeObject;

            if (forces == null)
                forces = SPMTool.Force.ListOfForces();

            if (constraints == null)
                constraints = Constraint.ListOfConstraints();

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
	            // Read the object as a point
	            DBPoint ndPt = trans.GetObject(nodeObject, OpenMode.ForRead) as DBPoint;

	            // Read the XData and get the necessary data
	            ResultBuffer rb = ndPt.GetXDataForApplication(AutoCAD.appName);
	            TypedValue[] data = rb.AsArray();

	            // Get the position
	            Position = ndPt.Position;

	            // Get the node number
	            Number = Convert.ToInt32(data[(int) XData.Node.Number].Value);

                // Get type
                Type = GetNodeType(ndPt);

                // Get support conditions
                Support = GetSupportConditions(constraints);

	            // Get forces
	            Force = GetNodalForces(forces);

                // Get displacements
                double
	                ux = Convert.ToDouble(data[(int)XData.Node.Ux].Value),
	                uy = Convert.ToDouble(data[(int)XData.Node.Uy].Value);

                Displacement = (ux, uy);
            }
        }

		// Get index of DoFs
		public int[] DoFIndex => Auxiliary.GlobalIndexes(Number);

        // Get node type
        private int GetNodeType(DBPoint nodePoint)
        {
            if (nodePoint.Layer == Layers.extNode)
                return
                    (int)NodeType.External;

            return
                (int)NodeType.Internal;
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
                    if (force.Direction == (int)SPMTool.Force.ForceDirection.X)
                        Fx = force.Value;

                    if (force.Direction == (int)SPMTool.Force.ForceDirection.Y)
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
