using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPM.Analysis;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Core.Conditions;
using SPMTool.Core.Elements;
using SPMTool.Core.Materials;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;

#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Model class
	/// </summary>
	public static class Model
	{
		#region Fields

		/// <summary>
		///     Collection of element <see cref="Layer" />'s.
		/// </summary>
		public static readonly Layer[] ElementLayers = { Layer.ExtNode, Layer.IntNode, Layer.Stringer, Layer.Panel, Layer.Force, Layer.Support };

		/// <summary>
		///     Collection of result <see cref="Layer" />'s.
		/// </summary>
		public static readonly Layer[] ResultLayers = { Layer.StringerForce, Layer.PanelForce, Layer.CompressivePanelStress, Layer.TensilePanelStress, Layer.ConcreteCompressiveStress, Layer.ConcreteTensileStress, Layer.Displacements, Layer.Cracks};

		/// <summary>
		///     Command names for undo and redo.
		/// </summary>
		private static readonly string[] CmdNames = { "UNDO", "REDO", "_U", "_R", "_.U", "_.R" };

		/// <summary>
		///     The collection of <see cref="NodeObject" />'s in the model.
		/// </summary>
		public static readonly NodeList Nodes = NodeList.ReadFromDrawing();

		/// <summary>
		///     The collection of <see cref="StringerObject" />'s in the model.
		/// </summary>
		public static readonly StringerList Stringers = StringerList.ReadFromDrawing();

		/// <summary>
		///     The collection of <see cref="PanelObject" />'s in the model.
		/// </summary>
		public static readonly PanelList Panels = PanelList.ReadFromDrawing();

		/// <summary>
		///     The collection of <see cref="ForceObject" />'s in the model.
		/// </summary>
		public static readonly ForceList Forces = ForceList.ReadFromDrawing();

		/// <summary>
		///     The collection of <see cref="ConstraintObject" />'s in the model.
		/// </summary>
		public static readonly ConstraintList Constraints = ConstraintList.ReadFromDrawing();

		#endregion

		#region Properties

		/// <summary>
		///     Get application <see cref="Autodesk.AutoCAD.EditorInput.Editor" />.
		/// </summary>
		public static Autodesk.AutoCAD.EditorInput.Editor Editor => DataBase.Document.Editor;

		/// <summary>
		///     Get the list of distinct stringer's <see cref="CrossSection" />'s from objects in the model.
		/// </summary>
		public static List<CrossSection> StringerCrossSections => Stringers.GetCrossSections();

		/// <summary>
		///     Get the list of distinct widths from objects in the model..
		/// </summary>
		public static List<Length> ElementWidths => Stringers.GetWidths().Concat(Panels.GetWidths()).Distinct().ToList();

		#endregion

		#region  Methods

		/// <summary>
		///     Update all the elements in the drawing.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public static void UpdateElements(bool addNodes = true, bool removeNodes = true)
		{
			Stringers.Update();
			Panels.Update();
			Nodes.Update(addNodes, removeNodes);
		}

		/// <summary>
		///     Get the <see cref="InputData" /> from objects in drawing.
		/// </summary>
		/// <param name="dataOk">Returns true if data is consistent to start analysis.</param>
		/// <param name="message">Message to show if data is inconsistent.</param>
		/// <param name="analysisType">The type of analysis to perform.</param>
		public static InputData GenerateInput(AnalysisType analysisType, out bool dataOk, out string message)
		{
			// Get units
			var units = Settings.Units;

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
			var nodes = NodeList.ReadFromPoints(ndObjs).Select(n => n.AsNode()).ToArray();

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
		///     Return an <see cref="SPMElement" /> from <paramref name="entity" />.
		/// </summary>
		/// <param name="entity">The <see cref="Entity" /> of SPM object.</param>
		public static SPMElement GetElement(Entity entity)
		{
			// Get element layer
			var layer = (Layer) Enum.Parse(typeof(Layer), entity.Layer);

			if (!ElementLayers.Contains(layer))
				return null;

			// Get concrete and units
			var parameters   = ConcreteData.Parameters;
			var constitutive = ConcreteData.ConstitutiveModel;
			var units        = Settings.Units;

			if (layer is Layer.IntNode || layer is Layer.ExtNode)
				return Nodes.GetByObjectId(entity.ObjectId).AsNode();

			// Read nodes
			var nodes = NodeList.ReadFromPoints(NodeCollection).Select(n => n.AsNode()).ToArray();

			if (layer is Layer.Stringer)
				return Stringers.Read((Line) entity, units, parameters, constitutive, nodes);

			if (layer is Layer.Panel)
				return Panels.Read((Solid) entity, units, parameters, constitutive, nodes);

			return null;
		}

		/// <summary>
		///     Return an <see cref="SPMElement" /> from <paramref name="objectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of SPM object.</param>
		public static SPMElement GetElement(ObjectId objectId) => GetElement(objectId.GetEntity());

		/// <summary>
		///     Draw results of <paramref name="analysis" />.
		/// </summary>
		/// <param name="analysis">The <see cref="Analysis" /> done.</param>
		public static void DrawResults(Analysis analysis)
		{
			// Erase result objects
			ResultLayers.EraseObjects();

			//Nodes.SetDisplacements(analysis.Nodes);
			DrawDisplacements(analysis.Stringers);
			StringerList.DrawForces(analysis.Stringers, analysis.MaxStringerForce);
			PanelList.DrawStresses(analysis.Panels);

			if (!(analysis is SecantAnalysis))
				return;

			PanelList.DrawCracks(analysis.Panels);
			StringerList.DrawCracks(analysis.Stringers);
		}

		/// <summary>
		///     Draw displacements.
		/// </summary>
		/// <param name="stringers">The collection of <see cref="Stringer" />'s.</param>
		private static void DrawDisplacements(IEnumerable<Stringer> stringers)
		{
			// Get units
			var units = Settings.Units;

			// Turn the layer off
			Layer.Displacements.Off();

			// Set a scale factor for displacements
			var scFctr = units.DisplacementScaleFactor;

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
		///     Set application parameters for drawing.
		/// </summary>
		public static void SetAppParameters()
		{
			SetPointSize();
			SetLineWeightDisplay();
		}

		/// <summary>
		///     Set size to points in the drawing.
		/// </summary>
		public static void SetPointSize()
		{
			// Set the style for all point objects in the drawing
			DataBase.Database.Pdmode = 32;
			DataBase.Database.Pdsize = 40 * Settings.Units.ScaleFactor;
		}

		/// <summary>
		///     Turn off fillmode setting.
		/// </summary>
		public static void SetFillMode() => DataBase.Database.Fillmode = false;

		/// <summary>
		///     Turn on line weight display.
		/// </summary>
		public static void SetLineWeightDisplay() => DataBase.Database.LineWeightDisplay = true;


		/// <summary>
		///     Event to run after undo or redo commands.
		/// </summary>
		public static void On_UndoOrRedo(object sender, CommandEventArgs e)
		{
			if (CmdNames.Any(cmd => cmd.Contains(e.GlobalCommandName.ToUpper())))
				UpdateElements(false);
		}



		#endregion

		/// <summary>
		///     Event to execute when an object is erased.
		/// </summary>
		public static void On_ObjectErase(object sender, ObjectErasedEventArgs e)
		{
			var layer = ((Entity) e.DBObject).ReadLayer();

			var id = e.DBObject.ObjectId;

			switch (layer)
			{
				case Layer.ExtNode:
				case Layer.IntNode:
					Nodes.RemoveAll(n => n.ObjectId == id, false);
					return;

				case Layer.Stringer :
					Stringers.RemoveAll(s => s.ObjectId == id, false);
					return;

				case Layer.Panel:
					Panels.RemoveAll(p => p.ObjectId == id, false);
					return;

				case Layer.Force:
					// Get the force
					var force = Forces.Find(f => f.ObjectId == id);

					if (force is null)
						return;

					// Erase text
					force.Text.RemoveFromDrawing();

					Forces.Remove(force, false);
					return;

				case Layer.Support:
					Constraints.RemoveAll(c => c.ObjectId == id, false);
					return;

				default:
					return;
			}
		}
	}
}