using System;
using System.Collections.Generic;
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
	    /// Get the collection of all nodes in the model.
	    /// </summary>
	    public static IEnumerable<DBPoint> NodeCollection => Nodes.Update(DataBase.Units.Geometry);

	    /// <summary>
	    /// Get the collection of external nodes in the model.
	    /// </summary>
	    public static IEnumerable<DBPoint> ExtNodeCollection => Layer.ExtNode.GetDBObjects().ToPoints();

	    /// <summary>
	    /// Get the collection of internal nodes in the model.
	    /// </summary>
	    public static IEnumerable<DBPoint> IntNodeCollection => Layer.IntNode.GetDBObjects().ToPoints();

	    /// <summary>
	    /// Get the collection of stringers in the model.
	    /// </summary>
	    public static IEnumerable<Line> StringerCollection => Stringers.Update();

	    /// <summary>
	    /// Get the collection of panels in the model.
	    /// </summary>
	    public static IEnumerable<Solid> PanelCollection => Panels.Update();

	    /// <summary>
	    /// Get the collection of forces in the model.
	    /// </summary>
	    public static IEnumerable<BlockReference> ForceCollection => Layer.Force.GetDBObjects().ToBlocks();

	    /// <summary>
	    /// Get the collection of supports in the model.
	    /// </summary>
	    public static IEnumerable<BlockReference> SupportCollection => Layer.Support.GetDBObjects().ToBlocks();

        /// <summary>
        /// Get the collection of force texts in the model.
        /// </summary>
        public static IEnumerable<DBText> ForceTextCollection => Layer.ForceText.GetDBObjects().ToTexts();

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
	        var ndObjs  = NodeCollection.ToArray();
	        var strObjs = StringerCollection.ToArray();
	        var pnlObjs = PanelCollection.ToArray();

	        // Verify if there is stringers and nodes at least
	        if (ndObjs.Length == 0 || strObjs.Length == 0)
	        {
		        dataOk = false;
		        message = "Please input model geometry";
		        return null;
	        }

	        // Get nodes
	        var nodes = Nodes.Read(ndObjs, units).ToArray();

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
		        return Nodes.Read((DBPoint) entity, units);

	        // Read nodes
	        var nodes = Nodes.Read(NodeCollection, units);

	        if (layer is Layer.Stringer)
		        return Stringers.Read((Line) entity, units, concrete.Parameters, concrete.Constitutive, nodes);

	        if (layer is Layer.Panel)
		        return Panels.Read((Solid) entity, units, concrete.Parameters, concrete.Constitutive, nodes);

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
			Panels.CreateBlocks();
        }
    }
}
