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
		[CommandMethod(CommandName.ConcreteStresses)]
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
		[CommandMethod(CommandName.Cracks)]
		public static void ToggleCracks() => ActiveModel.AcadDatabase.Toggle(Layer.Cracks);

		/// <summary>
		///     Toggle view for displacements.
		/// </summary>
		[CommandMethod(CommandName.Displacements)]
		public static void ToggleDisplacements() => ActiveModel.AcadDatabase.Toggle(Layer.Displacements);

		/// <summary>
		///     Toggle view for forces.
		/// </summary>
		[CommandMethod(CommandName.Forces)]
		public static void ToggleForces() => ActiveModel.AcadDatabase.Toggle(Layer.Force);

		/// <summary>
		///     Toggle view for nodes.
		/// </summary>
		[CommandMethod(CommandName.Nodes)]
		public static void ToggleNodes()  => ActiveModel.AcadDatabase.Toggle(Layer.ExtNode, Layer.IntNode);

		/// <summary>
		///     Toggle view for panel forces.
		/// </summary>
		[CommandMethod(CommandName.PanelShear)]
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
		[CommandMethod(CommandName.Panels)]
		public static void TogglePanels() => ActiveModel.AcadDatabase.Toggle(Layer.Panel);

		/// <summary>
		///     Toggle view for panel stresses.
		/// </summary>
		[CommandMethod(CommandName.PanelStresses)]
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
		[CommandMethod(CommandName.StringerForces)]
		public static void ToggleStringerForces() => ActiveModel.AcadDatabase.Toggle(Layer.StringerForce);

		/// <summary>
		///     Toggle view for stringers.
		/// </summary>
		[CommandMethod(CommandName.Stringers)]
		public static void ToggleStringers() => ActiveModel.AcadDatabase.Toggle(Layer.Stringer);

		/// <summary>
		///     Toggle view for supports.
		/// </summary>
		[CommandMethod(CommandName.Supports)]
		public static void ToggleSupports() => ActiveModel.AcadDatabase.Toggle(Layer.Support);

		/// <summary>
		///     View data of a selected element.
		/// </summary>
		[CommandMethod(CommandName.ElementData)]
		public static void ViewElementData()
		{
			// Get model and database
			var model    = ActiveModel;
			var database = model.AcadDatabase;

			// Start a loop for viewing continuous elements
			while (true)
			{
				// Get the entity for read
				var ent = database.GetEntity("Select an element to view data:", ElementLayers);

				if (ent is null)
					return;

				// Read the element
				var element = ent.GetSPMObject();

				var message = element is null
					? "Not a SPM element."
					: $"{element}";

				model.Editor.WriteMessage($"\n{message}");
			}
		}

		#endregion

	}
}