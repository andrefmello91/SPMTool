using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Extensions.AutoCAD;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.Database.Conditions;
using SPMTool.Database.Elements;
using SPMTool.Database.Materials;
using SPMTool.Enums;
using Analysis = SPM.Analysis.Analysis;
using Nodes = SPMTool.Database.Elements.Nodes;

namespace SPMTool.Database
{
    /// <summary>
    /// Model class
    /// </summary>
    public static class Model
    {
	    /// <summary>
	    /// Get application <see cref="Autodesk.AutoCAD.EditorInput.Editor"/>.
	    /// </summary>
	    public static Autodesk.AutoCAD.EditorInput.Editor Editor => DataBase.Document.Editor;

        /// <summary>
        /// Collection of element <see cref="Layer"/>'s.
        /// </summary>
        public static readonly Layer[] ElementLayers = { Layer.ExtNode, Layer.IntNode, Layer.Stringer, Layer.Panel, Layer.Force, Layer.Support };

        /// <summary>
        /// Collection of result <see cref="Layer"/>'s.
        /// </summary>
        public static readonly Layer[] ResultLayers = { Layer.StringerForce, Layer.PanelForce, Layer.CompressivePanelStress, Layer.TensilePanelStress, Layer.ConcreteCompressiveStress, Layer.ConcreteTensileStress, Layer.Displacements, Layer.Cracks};

        /// <summary>
        /// Get the collection of all nodes in the model.
        /// </summary>
        public static DBPoint[] NodeCollection => Nodes.GetAllNodes()?.ToArray();

	    /// <summary>
	    /// Get the collection of external nodes in the model.
	    /// </summary>
	    public static DBPoint[] ExtNodeCollection => Nodes.GetExtNodes()?.ToArray();

	    /// <summary>
	    /// Get the collection of internal nodes in the model.
	    /// </summary>
	    public static DBPoint[] IntNodeCollection => Nodes.GetIntNodes()?.ToArray();

	    /// <summary>
	    /// Get the collection of stringers in the model.
	    /// </summary>
	    public static Line[] StringerCollection => Stringers.GetObjects()?.ToArray();

	    /// <summary>
	    /// Get the collection of panels in the model.
	    /// </summary>
	    public static Solid[] PanelCollection => Panels.GetObjects()?.ToArray();

	    /// <summary>
	    /// Get the collection of forces in the model.
	    /// </summary>
	    public static BlockReference[] ForceCollection => Forces.GetObjects()?.ToArray();

	    /// <summary>
	    /// Get the collection of supports in the model.
	    /// </summary>
	    public static BlockReference[] SupportCollection => Supports.GetObjects()?.ToArray();

        /// <summary>
        /// Get the collection of force texts in the model.
        /// </summary>
        public static DBText[] ForceTextCollection => Forces.GetTexts()?.ToArray();

		/// <summary>
		/// Update all the elements in the drawing.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
        public static void UpdateElements(bool addNodes = true, bool removeNodes = true)
        {
	        Stringers.Update(false);
	        Panels.Update(false);
	        Nodes.Update(addNodes, removeNodes);
        }

		/// <summary>
		/// Get the <see cref="InputData"/> from objects in drawing.
		/// </summary>
		/// <param name="dataOk">Returns true if data is consistent to start analysis.</param>
		/// <param name="message">Message to show if data is inconsistent.</param>
		/// <param name="analysisType">The type of analysis to perform.</param>
		public static InputData GenerateInput(AnalysisType analysisType, out bool dataOk, out string message)
        {
	        // Get units
	        var units = SettingsData.SavedUnits;

	        // Get concrete
	        var parameters   = ConcreteData.Parameters;
	        var constitutive = ConcreteData.ConstitutiveModel;

	        // Read elements
	        var ndObjs  = NodeCollection;
	        var strObjs = StringerCollection;
	        var pnlObjs = PanelCollection;

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
	        //Forces.Set(ForceCollection, nodes);
	        //Supports.Set(SupportCollection, nodes);

	        // Get stringers and panels
	        var stringers = Stringers.Read(strObjs, units, parameters, constitutive, nodes, analysisType).ToArray();
	        var panels    = Panels.Read(pnlObjs, units, parameters, constitutive, nodes, analysisType).ToArray();

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

	        if (!ElementLayers.Contains(layer))
		        return null;

            // Get concrete and units
            var parameters   = ConcreteData.Parameters;
            var constitutive = ConcreteData.ConstitutiveModel;
	        var units        = SettingsData.SavedUnits;

	        if (layer is Layer.IntNode || layer is Layer.ExtNode)
		        return Nodes.Read((DBPoint) entity, units);

	        // Read nodes
	        var nodes = Nodes.Read(NodeCollection, units).ToArray();

	        if (layer is Layer.Stringer)
		        return Stringers.Read((Line) entity, units, parameters, constitutive, nodes);

	        if (layer is Layer.Panel)
		        return Panels.Read((Solid) entity, units, parameters, constitutive, nodes);

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
			// Get the block enum as an array
			var blocks = Enum.GetValues(typeof(Block)).Cast<Block>().ToArray();

			// Create the blocks
	        blocks.Create();
        }

		/// <summary>
        /// Draw results of <paramref name="analysis"/>.
        /// </summary>
        /// <param name="analysis">The <see cref="Analysis"/> done.</param>
		public static void DrawResults(Analysis analysis)
		{
			// Erase result objects
			ResultLayers.EraseObjects();

			Nodes.SetDisplacements(analysis.Nodes);
            DrawDisplacements(analysis.Stringers);
            Stringers.DrawForces(analysis.Stringers, analysis.MaxStringerForce);
            Panels.DrawStresses(analysis.Panels);

            if (!(analysis is SecantAnalysis))
	            return;

            Panels.DrawCracks(analysis.Panels);
            Stringers.DrawCracks(analysis.Stringers);
		}

        /// <summary>
        /// Draw displacements.
        /// </summary>
        /// <param name="stringers">The collection of <see cref="Stringer"/>'s.</param>
		private static void DrawDisplacements(IEnumerable<Stringer> stringers)
        {
			// Get units
	        var units = SettingsData.SavedUnits;

			// Turn the layer off
			Layer.Displacements.Off();

			// Set a scale factor for displacements
			double scFctr = units.DisplacementScaleFactor;

			// Create lists of points for adding the nodes later
			var dispNds = new List<Point3d>();

			foreach (var str in stringers)
			{
				// Get displacements of the initial and end nodes
				var d1 = str.Grip1.Displacement.Copy();
				var d3 = str.Grip3.Displacement.Copy();
				d1.ChangeUnit(units.Displacements);
				d3.ChangeUnit(units.Displacements);

				double
					ux1 = d1.ComponentX * scFctr,
					uy1 = d1.ComponentY * scFctr,
					ux3 = d3.ComponentX * scFctr,
					uy3 = d3.ComponentY * scFctr;

				// Calculate the displaced nodes
				Point3d
					stPt = new Point3d(str.Geometry.InitialPoint.X + ux1, str.Geometry.InitialPoint.Y + uy1, 0),
					enPt = new Point3d(str.Geometry.EndPoint.X + ux3, str.Geometry.EndPoint.Y + uy3, 0),
					midPt = stPt.MidPoint(enPt);

				// Draw the displaced Stringer
				using (var newStr = new Line(stPt, enPt))
				{
					// Set the layer to Stringer
					newStr.Layer = $"{Layer.Displacements}";

					// Add the line to the drawing
					newStr.AddToDrawing();
				}

				// Add the position of the nodes to the list
				if (!dispNds.Contains(stPt))
					dispNds.Add(stPt);

				if (!dispNds.Contains(enPt))
					dispNds.Add(enPt);

				if (!dispNds.Contains(midPt))
					dispNds.Add(midPt);
			}

			// Add the nodes
			Nodes.Add(dispNds, NodeType.Displaced);
		}

		/// <summary>
		/// Set application parameters for drawing.
		/// </summary>
        public static void SetAppParameters()
        {
			SetPointSize();
			SetLineWeightDisplay();
        }

		/// <summary>
        /// Set size to points in the drawing.
        /// </summary>
        public static void SetPointSize()
        {
	        // Set the style for all point objects in the drawing
	        DataBase.Database.Pdmode = 32;
	        DataBase.Database.Pdsize = 40 * SettingsData.SavedUnits.ScaleFactor;
        }

        /// <summary>
        /// Turn off fillmode setting.
        /// </summary>
        public static void SetFillMode() => DataBase.Database.Fillmode = false;

		/// <summary>
		/// Turn on line weight display.
		/// </summary>
		public static void SetLineWeightDisplay() => DataBase.Database.LineWeightDisplay = true;

        /// <summary>
		/// Command names for undo and redo.
		/// </summary>
		private static readonly string[] CmdNames = { "UNDO", "REDO", "_U", "_R", "_.U", "_.R" };

		/// <summary>
		/// Remove a <see cref="SPMElement"/> from drawing.
		/// </summary>
		/// <param name="element">The <see cref="SPMElement"/> to remove.</param>
		public static void RemoveFromDrawing(SPMElement element) => element?.ObjectId.RemoveFromDrawing();

		/// <summary>
		/// Remove a collection of <see cref="SPMElement"/>'s from drawing.
		/// </summary>
		/// <param name="elements">The <see cref="SPMElement"/>'s to remove.</param>
		public static void RemoveFromDrawing(IEnumerable<SPMElement> elements) => elements?.Select(e => e.ObjectId).ToArray().RemoveFromDrawing();

		/// <summary>
		/// Event to run after undo or redo commands.
		/// </summary>
		public static void On_UndoOrRedo(object sender, CommandEventArgs e)
		{
			if (CmdNames.Any(cmd => cmd.Contains(e.GlobalCommandName.ToUpper())))
		        UpdateElements(false);
        }

		/// <summary>
		/// Event to execute when a <see cref="SPMElement"/> is removed.
		/// </summary>
		public static void On_ElementRemoved(object sender, ItemEventArgs<SPMElement> e) => RemoveFromDrawing(e.Item);

		/// <summary>
		/// Event to execute when a range of <see cref="SPMElement"/>'s is removed.
		/// </summary>
		public static void On_ElementsRemoved(object sender, RangeEventArgs<SPMElement> e) => RemoveFromDrawing(e.ItemCollection);
    }
}
