using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using andrefmello91.SPMElements;
using SPMTool.Core;
using SPMTool.Core.Conditions;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using SPMTool.Extensions;
using SPMTool.Application.UserInterface;

[assembly:CommandClass(typeof(View))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// View commands class.
    /// </summary>
    public static class View
    {
	    /// <summary>
	    /// View data of a selected element.
	    /// </summary>
	    [CommandMethod(CommandName.ElementData)]
	    public static void ViewElementData()
	    {
		    // Start a loop for viewing continuous elements
		    for ( ; ; )
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

		/// <summary>
		/// Toggle view for forces.
		/// </summary>
		[CommandMethod(CommandName.Forces)]
	    public static void ToggleForces()
	    {
		    Layer.Force.Toggle();
		    Layer.ForceText.Toggle();
	    }

		/// <summary>
		/// Toggle view for supports.
		/// </summary>
		[CommandMethod(CommandName.Supports)]
	    public static void ToggleSupports() => Layer.Support.Toggle();

		/// <summary>
		/// Toggle view for nodes.
		/// </summary>
		[CommandMethod(CommandName.Nodes)]
	    public static void ToggleNodes()
	    {
		    Layer.ExtNode.Toggle();
		    Layer.IntNode.Toggle();
	    }

		/// <summary>
		/// Toggle view for stringers.
		/// </summary>
		[CommandMethod(CommandName.Stringers)]
	    public static void ToggleStringers() => Layer.Stringer.Toggle();

		/// <summary>
		/// Toggle view for panels.
		/// </summary>
		[CommandMethod(CommandName.Panels)]
	    public static void TogglePanels() => Layer.Panel.Toggle();

		/// <summary>
		/// Toggle view for stringer forces.
		/// </summary>
		[CommandMethod(CommandName.StringerForces)]
	    public static void ToggleStringerForces() => Layer.StringerForce.Toggle();

		/// <summary>
		/// Toggle view for panel forces.
		/// </summary>
		[CommandMethod(CommandName.PanelForces)]
	    public static void TogglePanelForces()
	    {
			// Turn off layers
			Layer.CompressivePanelStress.Off();
			Layer.TensilePanelStress.Off();
		    Layer.ConcreteCompressiveStress.Off();
		    Layer.ConcreteTensileStress.Off();

			Layer.PanelForce.Toggle();
	    }

		/// <summary>
		/// Toggle view for panel stresses.
		/// </summary>
		[CommandMethod(CommandName.PanelStresses)]
	    public static void TogglePanelStresses()
	    {
			// Turn off layers
			Layer.PanelForce.Off();
		    Layer.ConcreteCompressiveStress.Off();
		    Layer.ConcreteTensileStress.Off();

			Layer.CompressivePanelStress.Toggle();
		    Layer.TensilePanelStress.Toggle();
	    }

		/// <summary>
		/// Toggle view for concrete principal stresses.
		/// </summary>
		[CommandMethod(CommandName.ConcreteStresses)]
	    public static void ToggleConcreteStresses()
	    {
		    // Turn off layers
		    Layer.PanelForce.Off();
		    Layer.CompressivePanelStress.Off();
		    Layer.TensilePanelStress.Off();

			Layer.ConcreteCompressiveStress.Toggle();
		    Layer.ConcreteTensileStress.Toggle();
	    }

		/// <summary>
		/// Toggle view for displacements.
		/// </summary>
		[CommandMethod(CommandName.Displacements)]
	    public static void ToggleDisplacements() => Layer.Displacements.Toggle();

		/// <summary>
		/// Toggle view for cracks.
		/// </summary>
		[CommandMethod(CommandName.Cracks)]
	    public static void ToggleCracks() => Layer.Cracks.Toggle();
    }
}
