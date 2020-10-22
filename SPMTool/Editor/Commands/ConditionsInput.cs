﻿using System;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using SPM.Elements;
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
        /// Add forces to model.
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

			// Update
			Forces.Update();
	    }

		/// <summary>
        /// Add constraints to model.
        /// </summary>
		[CommandMethod("AddConstraint")]
		public static void AddConstraint()
		{
			// Read units
			var units = DataBase.Units;

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
			Supports.EraseBlocks(positions);

			// If the node is not Free, add the support blocks
			if (support != Constraint.Free)
				Supports.AddBlocks(positions, support, units.Geometry);
		}
    }
}