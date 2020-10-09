using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Extensions.AutoCAD;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.Database;
using SPMTool.Database.Conditions;
using SPMTool.Database.Elements;
using SPMTool.Editor;
using SPMTool.Enums;
using Nodes = SPMTool.Database.Elements.Nodes;

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
	    public static ObjectIdCollection NodeCollection => Nodes.Update(DataBase.Units.Geometry);

	    /// <summary>
	    /// Get the collection of stringers in the model.
	    /// </summary>
	    public static ObjectIdCollection StringerCollection => Elements.Stringers.UpdateStringers();

	    /// <summary>
	    /// Get the collection of panels in the model.
	    /// </summary>
	    public static ObjectIdCollection PanelCollection => SPM.Elements.Panel.UpdatePanels();

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
        /// Get the <see cref="InputData"/> from objects in drawing.
        /// </summary>
        /// <param name="dataOk">Returns true if data is consistent to start analysis.</param>
        /// <param name="message">Message to show if data is inconsistent.</param>
        /// <param name="analysisType">The type of analysis to perform.</param>
        public static InputData GenerateInput(AnalysisType analysisType, out bool dataOk, out string message)
        {
	        // Get units
	        var units = DataBase.Units;

	        // Get concrete
	        var concrete = DataBase.Concrete;

	        // Read elements
	        var ndObjs = NodeCollection;
	        var strObjs = StringerCollection;
	        var pnlObjs = PanelCollection;

	        // Verify if there is stringers and nodes at least
	        if (ndObjs.Count == 0 || strObjs.Count == 0)
	        {
		        dataOk = false;
		        message = "Please input model geometry";
		        return null;
	        }

	        // Get nodes
	        var nodes = Nodes.Read(ndObjs, units);

	        // Set supports and forces
	        Forces.Set(ForceCollection, nodes);
	        Supports.Set(SupportCollection, nodes);

	        // Get stringers and panels
	        var stringers = Stringers.Read(strObjs, units, concrete.Parameters, concrete.Constitutive, nodes, analysisType);
	        var panels = Panels.Read(pnlObjs, units, concrete.Parameters, concrete.Constitutive, nodes, analysisType);

	        // Generate input
	        dataOk = true;
	        message = null;
	        return new InputData(nodes, stringers, panels, analysisType);
        }

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
	        var selRes = UserInput.Editor.SelectAll(selFt);

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
                    using (var ent = (Entity)trans.GetObject(obj, OpenMode.ForWrite))
                        ent.Erase();

		        // Commit changes
		        trans.Commit();
	        }
        }

        /// <summary>
        /// Erase all the objects in this <see cref="DBObjectCollection"/>.
        /// </summary>
        /// <param name="objects">The <see cref="DBObjectCollection"/> containing the objects to erase.</param>
        public static void EraseObjects(DBObjectCollection objects) => EraseObjects(objects.ToObjectIdCollection());

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

        /// <summary>
        /// Return an <see cref="SPMElement"/> from <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> of SPM object.</param>
        public static SPMElement GetElement(Entity entity)
        {
	        // Get element layer
	        var layer = (Layer) Enum.Parse(typeof(Layer), entity.Layer);

	        if (!Geometry.ElementLayers.Contains(layer))
		        return null;

	        // Get concrete and units
	        var concrete = DataBase.Concrete;
	        var units    = DataBase.Units;

	        if (layer is Layer.IntNode || layer is Layer.ExtNode)
		        return Nodes.Read(entity.ObjectId, units);

	        // Read nodes
	        var nodes = Nodes.Read(NodeCollection, units);

	        if (layer is Layer.Stringer)
		        return Stringers.Read(entity.ObjectId, units, concrete.Parameters, concrete.Constitutive, nodes);

	        if (layer is Layer.Panel)
		        return Panels.Read(entity.ObjectId, units, concrete.Parameters, concrete.Constitutive, nodes);

	        return null;
        }

        /// <summary>
        /// Return an <see cref="SPMElement"/> from <paramref name="objectId"/>.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of SPM object.</param>
        public static SPMElement GetElement(ObjectId objectId) => GetElement(objectId.ToEntity());

		/// <summary>
        /// Create blocks for use in SPMTool.
        /// </summary>
        public static void CreateBlocks()
        {
	        Forces.CreateBlock();
			Supports.CreateBlocks();
			SPM.Elements.Panel.CreateBlocks();
        }
    }
}
