using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SPMTool.Enums;
using SPMTool.Model.Conditions;

namespace SPMTool.Database
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
	    public static ObjectIdCollection ForceCollection => GetObjectsOnLayer(Layer.Force);

	    /// <summary>
	    /// Get the collection of supports in the model.
	    /// </summary>
	    public static ObjectIdCollection SupportCollection => GetObjectsOnLayer(Layer.Support);

        /// <summary>
        /// Get the collection of force texts in the model.
        /// </summary>
        public static ObjectIdCollection ForceTextCollection => GetObjectsOnLayer(Layer.ForceText);

        /// <summary>
        /// Get a <see cref="ObjectIdCollection"/> containing all the objects in this <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static ObjectIdCollection GetObjectsOnLayer(Layer layer)
        {
	        // Get layer name
	        var layerName = layer.ToString();

	        // Build a filter list so that only entities on the specified layer are selected
	        TypedValue[] tvs =
	        {
		        new TypedValue((int) DxfCode.LayerName, layerName)
	        };

	        var selFt = new SelectionFilter(tvs);

	        // Get the entities on the layername
	        var selRes = DataBase.Editor.SelectAll(selFt);

	        return
		        selRes.Status == PromptStatus.OK && selRes.Value.Count > 0 ? new ObjectIdCollection(selRes.Value.GetObjectIds()) : null;
        }

        /// <summary>
        /// Erase all the objects in this <see cref="ObjectIdCollection"/>.
        /// </summary>
        /// <param name="objects">The <see cref="ObjectIdCollection"/> containing the objects to erase.</param>
        public static void EraseObjects(ObjectIdCollection objects)
        {
	        if (objects is null || objects.Count == 0)
		        return;

	        // Start a transaction
	        using (var trans = DataBase.StartTransaction())
	        {
		        foreach (ObjectId obj in objects)
		        {
                    // Read as entity
                    using (var ent = (Entity)trans.GetObject(obj, OpenMode.ForWrite))
                        ent.Erase();
		        }

		        // Commit changes
		        trans.Commit();
	        }
        }

        /// <summary>
        /// Erase all the objects in this <paramref name="layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/>.</param>
        public static void EraseObjects(Layer layer)
        {
	        // Get objects
	        using (var objs = GetObjectsOnLayer(layer))
		        EraseObjects(objs);
        }

        /// <summary>
        /// Read a <see cref="DBObject"/> in the drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of the <see cref="DBObject"/>.</param>
        public static DBObject ReadDBObject(ObjectId objectId)
        {
	        // Start a transaction
	        using (var trans = DataBase.StartTransaction())
		        return
			        trans.GetObject(objectId, OpenMode.ForRead);
        }
    }
}
