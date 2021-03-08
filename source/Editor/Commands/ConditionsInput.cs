using System;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using OnPlaneComponents;
using SPMTool.Core;
using SPMTool.Core.Conditions;
using SPMTool.Editor.Commands;
using SPMTool.Extensions;
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
	    [CommandMethod(CommandName.AddForce)]
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
		    var positions = nds.Select(nd => nd.Position.ToPoint(units.Geometry)).ToArray();
			
		    // Erase blocks
		    Forces.ChangeConditions(positions, force.Value);
	    }

		/// <summary>
        /// Add constraints to model.
        /// </summary>
		[CommandMethod(CommandName.AddConstraint)]
		public static void AddConstraint()
		{
			// Request objects to be selected in the drawing area
			var nds = UserInput.SelectNodes("Select nodes to add support conditions:")?.ToArray();

			if (nds is null)
				return;

			// Ask the user set the support conditions:
			var options = Enum.GetNames(typeof(ComponentDirection));

			var keyword = UserInput.SelectKeyword("Add support in which direction?", options, "None");

			if (keyword is null)
				return;

			// Set the support
			var direction  = (ComponentDirection) Enum.Parse(typeof(ComponentDirection), keyword);
			var constraint = Constraint.FromDirection(direction);

			// Get positions
			var unit = DataBase.Settings.Units.Geometry;
			var positions = nds.Select(nd => nd.Position.ToPoint(unit)).ToArray();

			// Erase blocks
			Constraints.ChangeConditions(positions, constraint);
		}
    }
}
