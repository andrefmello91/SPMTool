using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Database.Settings;
using SPMTool.UserInterface;

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
		    // Read data
		    var units = UnitsData.ReadUnits(false);

		    // Start the window of units configuration
		    var unitConfig = new UnitsConfig(units);
		    Application.ShowModalWindow(Application.MainWindow.Handle, unitConfig, false);
	    }
    }
}
