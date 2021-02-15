using Autodesk.AutoCAD.Runtime;
using SPMTool.Editor.Commands;
using SPMTool.Application.UserInterface;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly:CommandClass(typeof(MaterialInput))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Material input class.
    /// </summary>
    public static class MaterialInput
    {
		/// <summary>
        /// Set concrete parameters to model.
        /// </summary>
	    [CommandMethod("SetConcreteParameters")]
	    public static void SetConcreteParameters()
	    {
		    // Start the config window
		    var concreteConfig = new ConcreteConfig();
		    ShowModalWindow(MainWindow.Handle, concreteConfig, false);
	    }
    }
}
