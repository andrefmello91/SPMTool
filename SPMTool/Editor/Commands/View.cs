using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Editor.Commands;
using SPMTool.Enums;

[assembly:CommandClass(typeof(View))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// View commands class.
    /// </summary>
    public static class View
    {
	    /// <summary>
	    /// Toggle view for forces.
	    /// </summary>
	    [CommandMethod("ToogleForces")]
	    public static void ToogleForces()
	    {
		    Layer.Force.Toogle();
		    Layer.ForceText.Toogle();
	    }
    }
}
