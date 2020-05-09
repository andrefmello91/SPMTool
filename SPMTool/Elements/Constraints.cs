using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.AutoCAD;
using SupportData = SPMTool.XData.Support;

namespace SPMTool.Elements
{
	// Constraints related commands
	public class Constraint
	{
		// SupportDirection conditions
		public enum SupportDirection
		{
			X,
			Y,
			XY
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
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				// Read the object as a blockreference
				var sBlck = trans.GetObject(supportObject, OpenMode.ForRead) as BlockReference;

				// Get the position
				Position = sBlck.Position;

				// Read the XData and get the necessary data
				ResultBuffer rb = sBlck.GetXDataForApplication(Current.appName);
				TypedValue[] data = rb.AsArray();

				// Get the direction
				SupportDirection dir = (SupportDirection)Convert.ToInt32(data[(int) SupportData.Direction].Value);

				var (x, y) = (false, false);

				if (dir == SupportDirection.X || dir == SupportDirection.XY)
					x = true;

				if (dir == SupportDirection.Y || dir == SupportDirection.XY)
					y = true;

				Direction = (x, y);
			}

		}

		// Get support list
		public static Constraint[] ListOfConstraints()
		{
			var constraints = new List<Constraint>();

			// Get force objects
			var sObjs = AutoCAD.Auxiliary.GetEntitiesOnLayer(Layers.Support);

			foreach (ObjectId sObj in sObjs)
				constraints.Add(new Constraint(sObj));

			return
				constraints.ToArray();
		}
	}

}
