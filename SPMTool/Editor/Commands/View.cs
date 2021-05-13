using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using SPMTool.Editor.Commands;
using SPMTool.Enums;

[assembly: CommandClass(typeof(View))]

namespace SPMTool.Editor.Commands
{
	/// <summary>
	///     View commands class.
	/// </summary>
	public static class View
	{

		#region Methods

		/// <summary>
		///     Toggle view for concrete principal stresses.
		/// </summary>
		[CommandMethod(CommandName.ConcreteStresses)]
		public static void ToggleConcreteStresses()
		{
			if (!Layer.ConcreteStress.Toggle())
				return;

			// Turn off layers
			Layer.PanelForce.Off();
			Layer.PanelStress.Off();
		}

		/// <summary>
		///     Toggle view for cracks.
		/// </summary>
		[CommandMethod(CommandName.Cracks)]
		public static void ToggleCracks() => Layer.Cracks.Toggle();

		/// <summary>
		///     Toggle view for displacements.
		/// </summary>
		[CommandMethod(CommandName.Displacements)]
		public static void ToggleDisplacements() => Layer.Displacements.Toggle();

		/// <summary>
		///     Toggle view for forces.
		/// </summary>
		[CommandMethod(CommandName.Forces)]
		public static void ToggleForces()
		{
			Layer.Force.Toggle();
			Layer.ForceText.Toggle();
		}

		/// <summary>
		///     Toggle view for nodes.
		/// </summary>
		[CommandMethod(CommandName.Nodes)]
		public static void ToggleNodes()
		{
			Layer.ExtNode.Toggle();
			Layer.IntNode.Toggle();
		}

		/// <summary>
		///     Toggle view for panel forces.
		/// </summary>
		[CommandMethod(CommandName.PanelShear)]
		public static void TogglePanelForces()
		{
			if (!Layer.PanelForce.Toggle())
				return;

			// Turn off layers
			Layer.PanelStress.Off();
			Layer.ConcreteStress.Off();
		}

		/// <summary>
		///     Toggle view for panels.
		/// </summary>
		[CommandMethod(CommandName.Panels)]
		public static void TogglePanels() => Layer.Panel.Toggle();

		/// <summary>
		///     Toggle view for panel stresses.
		/// </summary>
		[CommandMethod(CommandName.PanelStresses)]
		public static void TogglePanelStresses()
		{
			if (!Layer.PanelStress.Toggle())
				return;

			// Turn off layers
			Layer.PanelForce.Off();
			Layer.ConcreteStress.Off();
		}

		/// <summary>
		///     Toggle view for stringer forces.
		/// </summary>
		[CommandMethod(CommandName.StringerForces)]
		public static void ToggleStringerForces() => Layer.StringerForce.Toggle();

		/// <summary>
		///     Toggle view for stringers.
		/// </summary>
		[CommandMethod(CommandName.Stringers)]
		public static void ToggleStringers() => Layer.Stringer.Toggle();

		/// <summary>
		///     Toggle view for supports.
		/// </summary>
		[CommandMethod(CommandName.Supports)]
		public static void ToggleSupports() => Layer.Support.Toggle();

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
				var ent = UserInput.SelectEntity("Select an element to view data:", Model.ElementLayers);

				if (ent is null)
					return;

				// Read the element
				var element = ent.GetSPMObject();

				var message = element is null
					? "Not a SPM element."
					: element.ToString();

				Model.Editor.WriteMessage($"\n{message}");
			}
		}

		#endregion

	}
}