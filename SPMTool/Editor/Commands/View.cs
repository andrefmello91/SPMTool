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
        [CommandMethod("ToogleForces")]
	    public static void ToogleForces()
	    {
		    Layer.Force.Toogle();
		    Layer.ForceText.Toogle();
	    }

	    /// <summary>
	    /// Toggle view for supports.
	    /// </summary>
	    [CommandMethod("ToogleSupports")]
	    public static void ToogleSupports() => Layer.Support.Toogle();

	    /// <summary>
	    /// Toggle view for nodes.
	    /// </summary>
	    [CommandMethod("ToogleNodes")]
	    public static void ToogleNodes()
	    {
		    Layer.ExtNode.Toogle();
		    Layer.IntNode.Toogle();
	    }

	    /// <summary>
	    /// Toggle view for stringers.
	    /// </summary>
	    [CommandMethod("ToogleStringers")]
	    public static void ToogleStringers() => Layer.Stringer.Toogle();

	    /// <summary>
	    /// Toggle view for panels.
	    /// </summary>
	    [CommandMethod("TooglePanels")]
	    public static void TooglePanels() => Layer.Panel.Toogle();

	    /// <summary>
	    /// Toggle view for stringer forces.
	    /// </summary>
	    [CommandMethod("ToogleStringerForces")]
	    public static void ToogleStringerForces() => Layer.StringerForce.Toogle();

	    /// <summary>
	    /// Toggle view for panel forces.
	    /// </summary>
	    [CommandMethod("TooglePanelForces")]
	    public static void TooglePanelForces() => Layer.PanelForce.Toogle();

	    /// <summary>
	    /// Toggle view for panel stresses.
	    /// </summary>
	    [CommandMethod("TooglePanelStresses")]
	    public static void TooglePanelStresses()
	    {
		    Layer.CompressivePanelStress.Toogle();
		    Layer.TensilePanelStress.Toogle();
	    }

	    /// <summary>
	    /// Toggle view for displacements.
	    /// </summary>
	    [CommandMethod("ToogleDisplacements")]
	    public static void ToogleDisplacements() => Layer.Displacements.Toogle();
    }
}
