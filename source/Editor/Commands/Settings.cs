using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Editor.Commands;
using SPMTool.Application.UserInterface;

[assembly: CommandClass(typeof(Settings))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Settings command class.
    /// </summary>
    public static class Settings
    {
		/// <summary>
        /// Set units.
        /// </summary>
	    [CommandMethod("SetUnits")]
	    public static void SetUnits()
	    {
		    // Start the window of units configuration
		    var unitConfig = new UnitsConfig();
		    Application.ShowModalWindow(Application.MainWindow.Handle, unitConfig, false);
	    }

		/// <summary>
        /// Set analysis settings.
        /// </summary>
	    [CommandMethod("SetAnalysisSettings")]
	    public static void SetAnalysisSettings()
	    {
		    // Start the window of units configuration
		    var analysisConfig = new AnalysisConfig();
		    Application.ShowModalWindow(Application.MainWindow.Handle, analysisConfig, false);
	    }
    }
}
