using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.AutoCAD;
using SPMTool.Database;

namespace SPMTool.Model
{
    /// <summary>
    /// Model class
    /// </summary>
    public static class Model
    {
	    /// <summary>
	    /// Get the collection of nodes in the model.
	    /// </summary>
	    public static ObjectIdCollection NodeCollection => Geometry.Node.UpdateNodes(DataBase.Units);

	    /// <summary>
	    /// Get the collection of stringers in the model.
	    /// </summary>
	    public static ObjectIdCollection StringerCollection => Geometry.Stringer.UpdateStringers();

	    /// <summary>
	    /// Get the collection of panels in the model.
	    /// </summary>
	    public static ObjectIdCollection PanelCollection => Geometry.Panel.UpdatePanels();

	    /// <summary>
	    /// Get the collection of forces in the model.
	    /// </summary>
	    public static ObjectIdCollection ForceCollection => Auxiliary.GetObjectsOnLayer(Layers.Force);

	    /// <summary>
	    /// Get the collection of supports in the model.
	    /// </summary>
	    public static ObjectIdCollection SupportCollection => Auxiliary.GetObjectsOnLayer(Layers.Support);

        /// <summary>
        /// Get the collection of force texts in the model.
        /// </summary>
        public static ObjectIdCollection ForceTextCollection => Auxiliary.GetObjectsOnLayer(Layers.ForceText);

    }
}
