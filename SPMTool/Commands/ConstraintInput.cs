using System;
using System.Linq;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using SPMTool.Commands;

using static SPMTool.Core.SPMModel;
using static SPMTool.Core.SPMModel;
using static SPMTool.Core.SPMModel;
using AcadCommands = SPMTool.Commands.AcadCommands;

[assembly: CommandClass(typeof(AcadCommands))]

namespace SPMTool.Commands
{
	/// <summary>
	///     Conditions input class.
	/// </summary>
	public static partial class AcadCommands
	{

		#region Methods

		/// <summary>
		///     Add constraints to model.
		/// </summary>
		[CommandMethod(CommandName.AddConstraint)]
		public static void AddConstraint()
		{
			var model = SPMModel.ActiveModel;
			var unit  = model.Database.Settings.Units.Geometry;
			
			// Request objects to be selected in the drawing area
			var nds =  model.Database.AcadDatabase.GetNodes("Select nodes to add support conditions:", NodeType.External)?.ToArray();

			if (nds is null)
				return;
			
			// Erase result objects
			model.AcadDocument.EraseObjects(SPMResults.ResultLayers);

			// Ask the user set the support conditions:
			var defDirection = nds.Length == 1
				? SPMModel.ActiveModel.Constraints.GetConstraintByPosition(nds[0].Position.ToPoint(unit)).Direction
				: ComponentDirection.None;

			var options = Enum.GetNames(typeof(ComponentDirection));

			var keyword = model.Editor.GetKeyword("Add support in which direction?", options, $"{defDirection}");

			if (keyword is null)
				return;

			// Set the support
			var direction  = (ComponentDirection) Enum.Parse(typeof(ComponentDirection), keyword);
			var constraint = Constraint.FromDirection(direction);

			// Get positions
			var positions = nds.Select(nd => nd.Position.ToPoint(unit)).ToArray();

			// Erase blocks
			model.Constraints.ChangeConditions(positions, constraint);
		}

		/// <summary>
		///     Add forces to model.
		/// </summary>
		[CommandMethod(CommandName.AddForce)]
		public static void AddForce()
		{
			// Read units
			var model = SPMModel.ActiveModel;
			var units = model.Database.Settings.Units;

			// Request objects to be selected in the drawing area
			var nds = model.Database.AcadDatabase.GetNodes("Select nodes to add load:", NodeType.External)?.ToArray();

			if (nds is null)
				return;

			// Erase result objects
			model.AcadDocument.EraseObjects(SPMResults.ResultLayers.Select(l => $"{l}").ToArray());

			// Get force from user
			var initialForce = nds.Length == 1
				? SPMModel.ActiveModel.Forces.GetForceByPosition(nds[0].Position.ToPoint(units.Geometry))
				: (PlaneForce?) null;

			var force = model.Editor.GetForce(initialForce, units.AppliedForces);

			if (!force.HasValue)
				return;

			// Get node positions
			var positions = nds.Select(nd => nd.Position.ToPoint(units.Geometry)).ToArray();

			// Erase blocks
			model.Forces.ChangeConditions(positions, force.Value);
		}

		#endregion

	}
}