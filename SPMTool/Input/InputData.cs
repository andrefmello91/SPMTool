using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPM.Analysis;
using SPMTool.AutoCAD;
using static SPMTool.AutoCAD.Material;

namespace SPMTool.Input
{
    /// <summary>
    /// Input data class.
    /// </summary>
    public static class Data
    {
        /// <summary>
        /// Get the <see cref="InputData"/> from objects in drawing.
        /// </summary>
        /// <param name="dataOk">Returns true if data is consistent to start analysis.</param>
        /// <param name="message">Message to show if data is inconsistent.</param>
        public static InputData ReadInput(out bool dataOk, out string message)
        {
	        // Get concrete
	        var concrete = ReadConcreteData();

			// Get units
			var units = Config.ReadUnits();

	        if (!concrete.HasValue)
	        {
		        dataOk  = false;
		        message = "Please set concrete parameters.";
				return null;
	        }

			// Read elements
			var nodes = Geometry.Node.UpdateNodes()

        }
    }
}
