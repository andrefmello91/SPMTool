using System;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics;
using OnPlaneComponents;

namespace SPMTool.Database.Conditions
{
    public class ConstraintObject : ConditionObject<ConstraintObject, Constraint, BlockReference>
    {
		/// <summary>
		///		Get the rotation angle of the block.
		/// </summary>
	    private double RotationAngle =>
		    Value.Direction switch
		    {
				ConstraintDirection.X => Constants.PiOver2,
				_                     => 0
		    };

	    public ConstraintObject(Point position, Constraint value) : base(position, value)
	    {
	    }

	    public override BlockReference? CreateEntity() => throw new NotImplementedException();
    }
}
