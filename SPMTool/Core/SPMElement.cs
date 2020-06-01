using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.AutoCAD;

namespace SPMTool.Core
{
    public abstract class SPMElement
    {
		// Common properties
	    public ObjectId       ObjectId { get; set; }
	    public int            Number   { get; set; }
	    public abstract int[] DoFIndex { get; }

        // Collection of SPM element layers
        public static Layers[] layers = { Layers.ExtNode, Layers.IntNode, Layers.Stringer, Layers.Panel, Layers.Force, Layers.Support };
    }
}
