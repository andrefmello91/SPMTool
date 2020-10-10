using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Database;
using SPMTool.Database.Conditions;
using SPMTool.Editor.Commands;

[assembly: CommandClass(typeof(ConditionsInput))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Conditions input class.
    /// </summary>
    public static class ConditionsInput
    {
		/// <summary>
        /// Add a force to drawing.
        /// </summary>
	    [CommandMethod("AddForce")]
	    public static void AddForce()
	    {
		    // Read units
		    var units = DataBase.Units;

		    // Request objects to be selected in the drawing area
		    var nds = UserInput.SelectNodes("Select nodes to add load:");

		    if (nds is null)
			    return;

		    // Get force from user
		    var force = UserInput.GetForceValue(units.AppliedForces);

		    if (!force.HasValue)
			    return;

		    // Get node positions
		    var positions = nds.Select(nd => nd.Position).ToArray();
			
		    // Erase blocks
		    Forces.EraseBlocks(positions);

		    // Add force blocks
		    Forces.AddBlocks(positions, force.Value, units.Geometry);
	    }
    }
}
