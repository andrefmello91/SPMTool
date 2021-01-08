using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using SPM.Elements;
using SPMTool.Database;
using SPMTool.Database.Conditions;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using SPMTool.UserInterface;

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
	    [CommandMethod("ViewElementData")]
	    public static void ViewElementData()
	    {
		    // Start a loop for viewing continuous elements
		    for ( ; ; )
		    {
			    // Get the entity for read
			    var ent = UserInput.SelectEntity("Select an element to view data:", Model.ElementLayers);

			    if (ent is null)
				    return;

			    string message;

			    if (ent.Layer == $"{Layer.Force}")
				    message = Forces.ReadForce((BlockReference) ent).ToString();

			    else if (ent.Layer == $"{Layer.Support}")
			    {
				    message = $"Constraint in {Supports.ReadConstraint((BlockReference) ent)}";
			    }

			    else
			    {
				    // Read the element
				    var element = Model.GetElement(ent);

				    message = element is null ? "Not a SPM element." : element.ToString();
			    }

				Model.Editor.WriteMessage($"\n{message}");
		    }
	    }

        /// <summary>
        /// Toggle view for forces.
        /// </summary>
        [CommandMethod("ToggleForces")]
	    public static void ToggleForces()
	    {
		    Layer.Force.Toggle();
		    Layer.ForceText.Toggle();
	    }

	    /// <summary>
	    /// Toggle view for supports.
	    /// </summary>
	    [CommandMethod("ToggleSupports")]
	    public static void ToggleSupports() => Layer.Support.Toggle();

	    /// <summary>
	    /// Toggle view for nodes.
	    /// </summary>
	    [CommandMethod("ToggleNodes")]
	    public static void ToggleNodes()
	    {
		    Layer.ExtNode.Toggle();
		    Layer.IntNode.Toggle();
	    }

	    /// <summary>
	    /// Toggle view for stringers.
	    /// </summary>
	    [CommandMethod("ToggleStringers")]
	    public static void ToggleStringers() => Layer.Stringer.Toggle();

	    /// <summary>
	    /// Toggle view for panels.
	    /// </summary>
	    [CommandMethod("TogglePanels")]
	    public static void TogglePanels() => Layer.Panel.Toggle();

	    /// <summary>
	    /// Toggle view for stringer forces.
	    /// </summary>
	    [CommandMethod("ToggleStringerForces")]
	    public static void ToggleStringerForces() => Layer.StringerForce.Toggle();

	    /// <summary>
	    /// Toggle view for panel forces.
	    /// </summary>
	    [CommandMethod("TogglePanelForces")]
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
	    [CommandMethod("TogglePanelStresses")]
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
	    [CommandMethod("ToggleConcreteStresses")]
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
	    [CommandMethod("ToggleDisplacements")]
	    public static void ToggleDisplacements() => Layer.Displacements.Toggle();

	    /// <summary>
	    /// Toggle view for cracks.
	    /// </summary>
	    [CommandMethod("ToggleCracks")]
	    public static void ToggleCracks() => Layer.Cracks.Toggle();
    }
}
