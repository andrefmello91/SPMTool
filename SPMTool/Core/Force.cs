using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.AutoCAD;
using UnitsNet.Units;
using ForceData = SPMTool.XData.Force;

namespace SPMTool.Core
{
	// Constraints related commands
	public class Force : SPMElement
	{
		// Force directions
		public enum ForceDirection
		{
			X,
			Y
		}

		// Properties
		public  ObjectId       ForceObject { get; }
		private ForceUnit      ForceUnit   { get; }
		public  double         Value       { get; }
		public  Point3d        Position    { get; }
		public  ForceDirection Direction   { get; }
		public  override int[] DoFIndex    { get; }

		// Convert force
		private UnitsNet.Force ConvertedForce => UnitsNet.Force.FromNewtons(Value).ToUnit(ForceUnit);

        // Constructor
        public Force(ObjectId forceObject, ForceUnit forceUnit = ForceUnit.Newton)
		{
			ForceObject = forceObject;
			ForceUnit   = forceUnit;

			// Start a transaction
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				// Read the object as a blockreference
				var fBlck = trans.GetObject(ForceObject, OpenMode.ForRead) as BlockReference;

				// Get the position
				Position = fBlck.Position;

				// Read the XData and get the necessary data
				ResultBuffer rb   = fBlck.GetXDataForApplication(AutoCAD.Current.appName);
				TypedValue[] data = rb.AsArray();

				// Get value and direction
				Value     = Convert.ToDouble(data[(int) ForceData.Value].Value);
				Direction = (ForceDirection)Convert.ToInt32(data[(int) ForceData.Direction].Value);
			}
		}

		// Read applied forces
		public static Force[] ListOfForces(ForceUnit forceUnit = ForceUnit.Newton)
		{
			var forces = new List<Force>();

			// Get force objects
			var fObjs = Auxiliary.GetEntitiesOnLayer(Layers.Force);

			foreach (ObjectId fObj in fObjs)
				forces.Add(new Force(fObj, forceUnit));

			return
				forces.ToArray();
		}

		public override string ToString()
		{
			if (Direction == ForceDirection.X)
				return
					"Fx = " + ConvertedForce;

			return
				"Fy = " + ConvertedForce;
		}

	}

}
