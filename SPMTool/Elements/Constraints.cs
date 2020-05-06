using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.ACAD;

namespace SPMTool.Elements
{
	// Constraints related commands
	public class Constraint
	{
		// Support conditions
		public enum Support
		{
			X  = 0,
			Y  = 1,
			XY = 2
		}

		// Properties
		public ObjectId         SupportObject { get; }
		public Point3d          Position      { get; }
		public (bool X, bool Y) Direction     { get; }

		// Constructor
		public Constraint(ObjectId supportObject)
		{
			SupportObject = supportObject;

			// Start a transaction
			using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
			{
				// Read the object as a blockreference
				var sBlck = trans.GetObject(supportObject, OpenMode.ForRead) as BlockReference;

				// Get the position
				Position = sBlck.Position;

				// Read the XData and get the necessary data
				ResultBuffer rb = sBlck.GetXDataForApplication(ACAD.Current.appName);
				TypedValue[] data = rb.AsArray();

				// Get the direction
				int dir = Convert.ToInt32(data[(int) XData.Support.Direction].Value);

				var (x, y) = (false, false);

				if (dir == (int) Support.X || dir == (int) Support.XY)
					x = true;

				if (dir == (int) Support.Y || dir == (int) Support.XY)
					y = true;

				Direction = (x, y);
			}

		}

		// Get support list
		public static Constraint[] ListOfConstraints()
		{
			var constraints = new List<Constraint>();

			// Get force objects
			var sObjs = Auxiliary.GetEntitiesOnLayer(Layers.support);

			foreach (ObjectId sObj in sObjs)
				constraints.Add(new Constraint(sObj));

			return
				constraints.ToArray();
		}
	}

}
