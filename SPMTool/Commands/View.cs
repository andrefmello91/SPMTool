using System.Linq;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Commands;
using SPMTool.Enums;
using static SPMTool.Core.SPMModel;

namespace SPMTool.Commands
{
	/// <summary>
	///     View commands class.
	/// </summary>
	public static partial class AcadCommands
	{

		#region Methods

		/// <summary>
		///     Toggle view for concrete principal stresses.
		/// </summary>
		[CommandMethod(Command.ConcreteStresses)]
		public static void ToggleConcreteStresses()
		{
			var dat = ActiveModel.AcadDatabase;
			
			if (!dat.Toggle(Layer.ConcreteStress))
				return;

			// Turn off layers
			dat.TurnOff(Layer.PanelForce, Layer.PanelStress);
		}

		/// <summary>
		///     Toggle view for cracks.
		/// </summary>
		[CommandMethod(Command.Cracks)]
		public static void ToggleCracks() => ActiveModel.AcadDatabase.Toggle(Layer.Cracks);

		/// <summary>
		///     Toggle view for displacements.
		/// </summary>
		[CommandMethod(Command.Displacements)]
		public static void ToggleDisplacements() => ActiveModel.AcadDatabase.Toggle(Layer.Displacements);

		/// <summary>
		///     Toggle view for forces.
		/// </summary>
		[CommandMethod(Command.Forces)]
		public static void ToggleForces() => ActiveModel.AcadDatabase.Toggle(Layer.Force);

		/// <summary>
		///     Toggle view for nodes.
		/// </summary>
		[CommandMethod(Command.Nodes)]
		public static void ToggleNodes()  => ActiveModel.AcadDatabase.Toggle(Layer.ExtNode, Layer.IntNode);

		/// <summary>
		///     Toggle view for panel forces.
		/// </summary>
		[CommandMethod(Command.PanelShear)]
		public static void TogglePanelForces()
		{
			var dat = ActiveModel.AcadDatabase;

			if (!dat.Toggle(Layer.PanelForce))
				return;

			// Turn off layers
			dat.TurnOff(Layer.PanelStress, Layer.ConcreteStress);
		}

		/// <summary>
		///     Toggle view for panels.
		/// </summary>
		[CommandMethod(Command.Panels)]
		public static void TogglePanels() => ActiveModel.AcadDatabase.Toggle(Layer.Panel);

		/// <summary>
		///     Toggle view for panel stresses.
		/// </summary>
		[CommandMethod(Command.PanelStresses)]
		public static void TogglePanelStresses()
		{
			var dat = ActiveModel.AcadDatabase;
			
			if (!dat.Toggle(Layer.PanelStress))
				return;

			// Turn off layers
			dat.TurnOff(Layer.PanelForce, Layer.ConcreteStress);
		}

		/// <summary>
		///     Toggle view for stringer forces.
		/// </summary>
		[CommandMethod(Command.StringerForces)]
		public static void ToggleStringerForces() => ActiveModel.AcadDatabase.Toggle(Layer.StringerForce);

		/// <summary>
		///     Toggle view for stringers.
		/// </summary>
		[CommandMethod(Command.Stringers)]
		public static void ToggleStringers() => ActiveModel.AcadDatabase.Toggle(Layer.Stringer);

		/// <summary>
		///     Toggle view for supports.
		/// </summary>
		[CommandMethod(Command.Supports)]
		public static void ToggleSupports() => ActiveModel.AcadDatabase.Toggle(Layer.Support);

		/// <summary>
		///     View data of a selected element.
		/// </summary>
		[CommandMethod(Command.ElementData)]
		public static void ViewElementData()
		{
			// Get model and database
			var model    = ActiveModel;
			var database = model.AcadDatabase;
			var unit     = model.Settings.Units.Geometry;
			
			// Create auxiliary points on panel centers
			var pts = model.Panels.Select(p => new DBPoint(p.Vertices.CenterPoint.ToPoint3d(unit)) { Layer = $"{Layer.PanelCenter}" }).ToList();
			model.AcadDocument.AddObjects(pts);

			// Start a loop for viewing continuous elements
			while (true)
			{
				// Get the entity for read
				var ent = database.GetEntity("Select an element to view data:", ElementLayers);

				if (ent is null)
					break;

				// Read the element
				var element = ent.GetSPMObject();
			
				model.Editor.WriteMessage($"\n{element?.ToString() ?? "Not a SPM element."}");
			}
			
			// Remove panel auxiliary points
			model.AcadDocument.EraseObjects(Layer.PanelCenter);
		}

		#endregion

	}
}