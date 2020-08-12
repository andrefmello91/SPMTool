using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.AutoCAD;

namespace SPMTool.Elements
{
	public enum ElementTypes
	{
		Default,
		Node,
		Stringer,
		Panel,
		Support,
		Force
	}

    public abstract class SPMElement
    {
		// Common properties
	    public ObjectId       ObjectId { get; set; }
	    public int            Number   { get; set; }
	    public abstract int[] DoFIndex { get; }

        /// <summary>
        /// Collection of SPM element layers
        /// </summary>
        public static Layers[] layers =
        {
	        Layers.ExtNode,
	        Layers.IntNode,
	        Layers.Stringer,
	        Layers.Panel,
	        Layers.Force,
	        Layers.Support
        };

		/// <summary>
        /// Read the SPM Element.
        /// </summary>
        /// <param name="objectId">The Object Id of the drawing element.</param>
        /// <param name="units">Current units.</param>
        /// <returns></returns>
        public static SPMElement ReadElement(ObjectId objectId, Units units = null)
        {
	        units = (units ?? Config.ReadUnits()) ?? new Units();

			// Read the layer
			var layer = Auxiliary.ReadObjectLayer(objectId);

	        if (layer == Layers.ExtNode || layer == Layers.IntNode)
		        return
			        new Node(objectId, units);

	        if (layer == Layers.Stringer)
		        return
			        new Stringer(objectId, units);

	        if (layer == Layers.Panel)
		        return
			        new Panel(objectId, units);

	        if (layer == Layers.Force)
		        return
			        new Force(objectId);

	        if (layer == Layers.Support)
		        return
			        new Constraint(objectId);

	        return null;
        }

        /// <summary>
        /// Read the SPM Element.
        /// </summary>
        /// <param name="entity">The entity of the drawing element.</param>
        /// <param name="units">Current units.</param>
        /// <returns></returns>
        public static SPMElement ReadElement(Entity entity, Units units = null)
        {
	        units = (units ?? Config.ReadUnits()) ?? new Units();

			// Read the layer
			var layer = Auxiliary.ReadObjectLayer(entity);

	        if (layer == Layers.ExtNode || layer == Layers.IntNode)
		        return
			        new Node(entity.ObjectId, units);

	        if (layer == Layers.Stringer)
		        return
			        new Stringer(entity.ObjectId, units);

	        if (layer == Layers.Panel)
		        return
			        new Panel(entity.ObjectId, units);

	        if (layer == Layers.Force)
		        return
			        new Force(entity.ObjectId);

	        if (layer == Layers.Support)
		        return
			        new Constraint(entity.ObjectId);

	        return null;
        }
    }
}
