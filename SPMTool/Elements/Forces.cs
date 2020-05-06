using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.ACAD;

namespace SPMTool.Elements
{
	// Constraints related commands
	public class Force
	{
		// Force directions
		public enum ForceDirection
		{
			X = 0,
			Y = 1
		}

		// Properties
		public ObjectId ForceObject { get; }
		public double   Value       { get; }
		public Point3d  Position    { get; }
		public int      Direction   { get; }

		// Constructor
		public Force(ObjectId forceObject)
		{
			ForceObject = forceObject;

			// Start a transaction
			using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
			{
				// Read the object as a blockreference
				var fBlck = trans.GetObject(ForceObject, OpenMode.ForRead) as BlockReference;

				// Get the position
				Position = fBlck.Position;

				// Read the XData and get the necessary data
				ResultBuffer rb = fBlck.GetXDataForApplication(ACAD.Current.appName);
				TypedValue[] data = rb.AsArray();

				// Get value and direction
				Value     = Convert.ToDouble(data[(int) XData.Force.Value].Value);
				Direction = Convert.ToInt32(data[(int) XData.Force.Direction].Value);
			}
		}

		// Read applied forces
		public static Force[] ListOfForces()
		{
			var forces = new List<Force>();

			// Get force objects
			var fObjs = Auxiliary.GetEntitiesOnLayer(Layers.force);

			foreach (ObjectId fObj in fObjs)
				forces.Add(new Force(fObj));

			return
				forces.ToArray();
		}
	}

}
