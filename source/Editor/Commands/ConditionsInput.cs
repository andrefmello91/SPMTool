using System;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using SPMTool.Core.Conditions;
using SPMTool.Editor.Commands;

using static SPMTool.Core.DataBase;
using static SPMTool.Core.Model;

[assembly: CommandClass(typeof(ConditionsInput))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Conditions input class.
    /// </summary>
    public static class ConditionsInput
    {
		/// <summary>
        /// Add forces to model.
        /// </summary>
	    [CommandMethod("AddForce")]
	    public static void AddForce()
	    {
		    // Read units
		    var units = DataBase.Settings.Units;

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
		    ForceList.EraseBlocks(positions);

		    // Add force blocks
		    ForceList.AddBlocks(positions, force.Value);

			// Update
			ForceList.Update();
	    }

		/// <summary>
        /// Add constraints to model.
        /// </summary>
		[CommandMethod("AddConstraint")]
		public static void AddConstraint()
		{
			// Request objects to be selected in the drawing area
			var nds = UserInput.SelectNodes("Select nodes to add support conditions:")?.ToArray();

			if (nds is null)
				return;

			// Ask the user set the support conditions:
			var options = Enum.GetNames(typeof(Constraint));

			var keyword = UserInput.SelectKeyword("Add support in which direction?", options, "Free");

			if (keyword is null)
				return;

			// Set the support
			var support = (Constraint) Enum.Parse(typeof(Constraint), keyword);

			// Get positions
			var positions = nds.Select(nd => nd.Position).ToArray();

			// Erase blocks
			ConstraintList.EraseBlocks(positions);

			// If the node is not Free, add the support blocks
			if (support != Constraint.Free)
				ConstraintList.AddBlocks(positions, support);

			// Update
			ConstraintList.Update();
		}
    }
}
