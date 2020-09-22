using System;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using OnPlaneComponents;
using SPMTool.AutoCAD;
using SPM.Elements;
using ForceData = SPMTool.XData.Force;
using SupportData = SPMTool.XData.Support;
using static SPMTool.AutoCAD.Auxiliary;
using static SPMTool.AutoCAD.DataBase;

namespace SPMTool.Input
{
    /// <summary>
    /// Input conditions class.
    /// </summary>
    public static class Conditions
    {
        /// <summary>
        /// Set forces to nodes.
        /// </summary>
        /// <param name="forceObjectIds">The <see cref="ObjectIdCollection"/> of force objects in the drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
	    public static void SetForces(ObjectIdCollection forceObjectIds, Node[] nodes)
	    {
		    foreach (ObjectId obj in forceObjectIds)
			    SetForces(obj, nodes);
	    }

        /// <summary>
        /// Set forces to nodes.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of force object in the drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
	    private static void SetForces(ObjectId objectId, Node[] nodes)
	    {
            // Read object
            var fBlock = (BlockReference) objectId.ToDBObject();

			// Set to node
			foreach (var node in nodes)
			{
				if (node.Position.Approx(fBlock.Position))
				{
					node.Force += ReadForce(fBlock);
					break;
				}
			}
	    }

        /// <summary>
        /// Read a <see cref="Force"/> from an object in the drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of force object in the drawing.</param>
        public static Force ReadForce(ObjectId objectId) => ReadForce((BlockReference) objectId.ToDBObject());

        /// <summary>
        /// Read a <see cref="Force"/> from an object in the drawing.
        /// </summary>
        /// <param name="forceBlock">The <see cref="BlockReference"/> of force object in the drawing.</param>
        public static Force ReadForce(BlockReference forceBlock)
        {
	        // Read the XData and get the necessary data
	        var data = forceBlock.ReadXData(AppName);

	        // Get value and direction
	        var value     = data[(int)ForceData.Value].ToDouble();
	        var direction = (Directions)data[(int)ForceData.Direction].ToInt();

	        // Get force
	        return
		        direction is Directions.X ? Force.InX(value) : Force.InY(value);
        }

        /// <summary>
        /// Set constraints to nodes.
        /// </summary>
        /// <param name="supportObjectIds">The <see cref="ObjectIdCollection"/> of support objects in the drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
        public static void SetConstraints(ObjectIdCollection supportObjectIds, Node[] nodes)
        {
	        foreach (ObjectId obj in supportObjectIds)
		        SetConstraints(obj, nodes);
        }

        /// <summary>
        /// Set constraint to nodes.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of support object in the drawing.</param>
        /// <param name="nodes">The <see cref="Array"/> containing all nodes of SPM model.</param>
	    private static void SetConstraints(ObjectId objectId, Node[] nodes)
	    {
            // Read object
            var sBlock = (BlockReference) objectId.ToDBObject();

			// Set to node
			foreach (var node in nodes)
			{
				if (node.Position.Approx(sBlock.Position))
				{
					node.Constraint = ReadConstraint(sBlock);
					break;
				}
			}
	    }

        /// <summary>
        /// Read a <see cref="Constraint"/> from an object in the drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of support object in the drawing.</param>
        public static Constraint ReadConstraint(ObjectId objectId) => ReadConstraint((BlockReference) objectId.ToDBObject());

        /// <summary>
        /// Read a <see cref="Constraint"/> from an object in the drawing.
        /// </summary>
        /// <param name="supportBlock">The <see cref="BlockReference"/> of support object in the drawing.</param>
        public static Constraint ReadConstraint(BlockReference supportBlock)
        {
	        // Read the XData and get the necessary data
	        var data = supportBlock.ReadXData(AppName);

	        // Get the direction
	        return (Constraint)data[(int)SupportData.Direction].ToInt();
        }

    }
}
