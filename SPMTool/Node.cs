using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		    External = 1,
		    Internal = 2
	    }

	    // Properties
	    public ObjectId             ObjectId => 
		    PointObject.ObjectId;
        public DBPoint              PointObject  { get; }
        private TypedValue[]        Data         { get; }

		// Constructor
		public Node(ObjectId nodeObject)
		{
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
	            // Read the object as a point
	            PointObject = trans.GetObject(nodeObject, OpenMode.ForRead) as DBPoint;

	            // Read the XData and get the necessary data
	            ResultBuffer rb = PointObject.GetXDataForApplication(AutoCAD.appName);
	            Data = rb.AsArray();
            }
        }

		// Get the position
		public Point3d Position => PointObject.Position;

        // Get the node number
        public int Number => Convert.ToInt32(Data[(int)XData.Node.Number].Value);

	    // Get type of node
	    public int Type
	    {
		    get
		    {
			    if (PointObject.Layer == Layers.extNode)
				    return
					    (int)NodeType.External;
			    
			    return
				    (int)NodeType.Internal;
            }
        }

	    // Get support conditions
	    public (bool X, bool Y) Support
	    {
		    get
		    {
			    string support = Data[(int)XData.Node.Support].Value.ToString();
			    bool
				    supX = false,
				    supY = false;

			    if (support.Contains("X"))
				    supX = true;

			    if (support.Contains("Y"))
				    supY = true;

			    return
				    (supX, supY);
            }
        }

	    // Get applied forces
	    public (double X, double Y) Force
	    {
		    get
		    {
			    double
				    Fx = Convert.ToDouble(Data[(int)XData.Node.Fx].Value),
				    Fy = Convert.ToDouble(Data[(int)XData.Node.Fy].Value);

			    return
				    (Fx, Fy);
            }
        }

        // Get index of DoFs
        public int[] DoFIndex => Auxiliary.GlobalIndexes(Number);

		// Get displacements
		private bool _displacementCalculated;
		private (double X, double Y) _displacement;
		public (double X, double Y) Displacement 
		{
			get
			{
				if (_displacementCalculated)
					return
						_displacement;

				// Get displacements from XData
				double
					ux = Convert.ToDouble(Data[(int)XData.Node.Ux].Value),
					uy = Convert.ToDouble(Data[(int)XData.Node.Uy].Value);

				return
					(ux, uy);
			}
			set
			{
				_displacementCalculated = true;
                _displacement           = value;
			}
		}

        // Get nodal displacements from displacement vector
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
    }
}
