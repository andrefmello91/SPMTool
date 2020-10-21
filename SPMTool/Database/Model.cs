using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.Database.Conditions;
using SPMTool.Database.Elements;
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
        public static void UpdateElements(bool addNodes = true)
        {
	        Nodes.Update(addNodes);
	        Stringers.Update(false);
	        Panels.Update(false);
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
	        var units = DataBase.Units;

	        // Get concrete
	        var concrete = DataBase.Concrete;

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

	        if (!ElementLayers.Contains(layer))
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

		/// <summary>
        /// Draw results of <paramref name="analysis"/>.
        /// </summary>
        /// <param name="analysis">The <see cref="Analysis"/> done.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
		public static void DrawResults(Analysis analysis, Units units)
		{
			Nodes.SetDisplacements(analysis.Nodes);
			DrawDisplacements(analysis.Stringers, analysis.Nodes, units);
			Stringers.DrawForces(analysis.Stringers, analysis.MaxStringerForce, units);
			Panels.DrawStresses(analysis.Panels, units);
		}

        /// <summary>
        /// Draw displacements.
        /// </summary>
        /// <param name="stringers">The collection of <see cref="Stringer"/>'s.</param>
        /// <param name="nodes">The collection of <see cref="Node"/>'s</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
		private static void DrawDisplacements(IEnumerable<Stringer> stringers, IEnumerable<Node> nodes, Units units)
		{
			// Turn the layer off
			Layer.Displacements.Off();

			// Erase all the displaced objects in the drawing
			Layer.Displacements.EraseObjects();

			// Set a scale factor for displacements
			double scFctr = 100 * units.Geometry.ScaleFactor();

			// Create lists of points for adding the nodes later
			var dispNds = new List<Point3d>();

			foreach (var str in stringers)
			{
				// Initialize the displacements of the initial and end nodes
				var (ux1, uy1) = nodes.Where(nd => nd.Type is NodeType.External && str.Grip1 == nd).Select(nd => (nd.Displacement.ComponentX * scFctr, nd.Displacement.ComponentY * scFctr)).First();
				var (ux3, uy3) = nodes.Where(nd => nd.Type is NodeType.External && str.Grip3 == nd).Select(nd => (nd.Displacement.ComponentX * scFctr, nd.Displacement.ComponentY * scFctr)).First();

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
					newStr.Add();
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
        /// Set size to points in the drawing.
        /// </summary>
        public static void SetPointSize()
        {
	        // Set the style for all point objects in the drawing
	        DataBase.Database.Pdmode = 32;
	        DataBase.Database.Pdsize = 40 * DataBase.Units.Geometry.ScaleFactor();
        }

        /// <summary>
        /// Set to OFF the <see cref="DataBase.Database.Fillmode"/> setting.
        /// </summary>
        public static void SetFillMode() => DataBase.Database.Fillmode = false;

		/// <summary>
        /// Command names for undo and redo.
        /// </summary>
		private static readonly string[] CmdNames = { "UNDO", "REDO", "_U", "_R", "_.U", "_.R" };

        /// <summary>
        /// Event to run after undo or redo commands.
        /// </summary>
        public static void On_UndoOrRedo(object sender, CommandEventArgs e)
		{
			if (CmdNames.Any(cmd => cmd.Contains(e.GlobalCommandName.ToUpper())))
		        UpdateElements(false);
        }
    }
}
