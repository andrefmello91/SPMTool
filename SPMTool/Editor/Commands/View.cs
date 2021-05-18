using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using SPMTool.Editor.Commands;
using SPMTool.Enums;

using static SPMTool.Core.SPMModel;

namespace SPMTool.Editor.Commands
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
			var dat = ActiveModel.Database.AcadDatabase;
			
			if (!dat.Toggle(Layer.ConcreteStress))
				return;

			// Turn off layers
			dat.TurnOff(Layer.PanelForce, Layer.PanelStress);
		}

		/// <summary>
		///     Toggle view for cracks.
		/// </summary>
		[CommandMethod(CommandName.Cracks)]
		public static void ToggleCracks() => ActiveModel.Database.AcadDatabase.Toggle(Layer.Cracks);

		/// <summary>
		///     Toggle view for displacements.
		/// </summary>
		[CommandMethod(CommandName.Displacements)]
		public static void ToggleDisplacements() => ActiveModel.Database.AcadDatabase.Toggle(Layer.Displacements);

		/// <summary>
		///     Toggle view for forces.
		/// </summary>
		[CommandMethod(CommandName.Forces)]
		public static void ToggleForces() => ActiveModel.Database.AcadDatabase.Toggle(Layer.Force);

		/// <summary>
		///     Toggle view for nodes.
		/// </summary>
		[CommandMethod(CommandName.Nodes)]
		public static void ToggleNodes()  => ActiveModel.Database.AcadDatabase.Toggle(Layer.ExtNode, Layer.IntNode);

		/// <summary>
		///     Toggle view for panel forces.
		/// </summary>
		[CommandMethod(CommandName.PanelShear)]
		public static void TogglePanelForces()
		{
			var dat = ActiveModel.Database.AcadDatabase;

			if (!dat.Toggle(Layer.PanelForce))
				return;

			// Turn off layers
			dat.TurnOff(Layer.PanelStress, Layer.ConcreteStress);
		}

		/// <summary>
		///     Toggle view for panels.
		/// </summary>
		[CommandMethod(CommandName.Panels)]
		public static void TogglePanels() => ActiveModel.Database.AcadDatabase.Toggle(Layer.Panel);

		/// <summary>
		///     Toggle view for panel stresses.
		/// </summary>
		[CommandMethod(CommandName.PanelStresses)]
		public static void TogglePanelStresses()
		{
			var dat = ActiveModel.Database.AcadDatabase;
			
			if (!dat.Toggle(Layer.PanelStress))
				return;

			// Turn off layers
			dat.TurnOff(Layer.PanelForce, Layer.ConcreteStress);
		}

		/// <summary>
		///     Toggle view for stringer forces.
		/// </summary>
		[CommandMethod(CommandName.StringerForces)]
		public static void ToggleStringerForces() => ActiveModel.Database.AcadDatabase.Toggle(Layer.StringerForce);

		/// <summary>
		///     Toggle view for stringers.
		/// </summary>
		[CommandMethod(CommandName.Stringers)]
		public static void ToggleStringers() => ActiveModel.Database.AcadDatabase.Toggle(Layer.Stringer);

		/// <summary>
		///     Toggle view for supports.
		/// </summary>
		[CommandMethod(CommandName.Supports)]
		public static void ToggleSupports() => ActiveModel.Database.AcadDatabase.Toggle(Layer.Support);

		/// <summary>
		///     View data of a selected element.
		/// </summary>
		[CommandMethod(CommandName.ElementData)]
		public static void ViewElementData()
		{
			// Start a loop for viewing continuous elements
			for (;;)
			{
				// Get the entity for read
				var ent = UserInput.SelectEntity("Select an element to view data:", ElementLayers);

				if (ent is null)
					return;

				// Read the element
				var element = ent.GetSPMObject();

				var message = element is null
					? "Not a SPM element."
					: element.ToString();

				ActiveModel.Editor.WriteMessage($"\n{message}");
			}
		}

		#endregion

	}
}