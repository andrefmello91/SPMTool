using System;
using System.Linq;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using SPMTool.Editor.Commands;

using static SPMTool.Core.SPMModel;
using static SPMTool.Core.SPMDatabase;
using static SPMTool.Core.SPMModel;

[assembly: CommandClass(typeof(AcadCommands))]

namespace SPMTool.Editor.Commands
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
			// Request objects to be selected in the drawing area
			var nds = UserInput.SelectNodes("Select nodes to add support conditions:", NodeType.External)?.ToArray();

			if (nds is null)
				return;
			
			var unit = ActiveDatabase.Settings.Units.Geometry;

			var model = ActiveModel;
			
			// Erase result objects
			model.AcadDocument.EraseObjects(Results.ResultLayers);

			// Ask the user set the support conditions:
			var defDirection = nds.Length == 1
				? ActiveModel.Constraints.GetConstraintByPosition(nds[0].Position.ToPoint(unit)).Direction
				: ComponentDirection.None;

			var options = Enum.GetNames(typeof(ComponentDirection));

			var keyword = UserInput.SelectKeyword("Add support in which direction?", options, $"{defDirection}");

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
			var unit = ActiveDatabase.Settings.Units.Geometry;

			// Request objects to be selected in the drawing area
			var nds = UserInput.SelectNodes("Select nodes to add load:", NodeType.External)?.ToArray();

			if (nds is null)
				return;

			var model = ActiveModel;

			// Erase result objects
			model.AcadDocument.EraseObjects(Results.ResultLayers.Select(l => $"{l}").ToArray());

			// Get force from user
			var initialForce = nds.Length == 1
				? ActiveModel.Forces.GetForceByPosition(nds[0].Position.ToPoint(unit))
				: (PlaneForce?) null;

			var force = UserInput.GetForceValue(initialForce);

			if (!force.HasValue)
				return;

			// Get node positions
			var positions = nds.Select(nd => nd.Position.ToPoint(unit)).ToArray();

			// Erase blocks
			model.Forces.ChangeConditions(positions, force.Value);
		}

		#endregion

	}
}