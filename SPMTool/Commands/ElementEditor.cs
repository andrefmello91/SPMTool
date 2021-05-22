﻿using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Commands;

using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.SPMModel;

namespace SPMTool.Commands
{
	/// <summary>
	///     Element editor command class.
	/// </summary>
	public static partial class AcadCommands
	{

		#region Methods

		[CommandMethod(CommandName.DividePanel)]
		public static void DividePanel()
		{
			// Get elements
			var model     = ActiveModel;
			var stringers = model.Stringers;
			var nodes     = model.Nodes;
			var panels    = model.Panels;
			var database  = model.Database.AcadDatabase;
			
			// Prompt for select panels
			var pnls = database.GetPanels("Select panels to divide")?.ToArray();

			if (pnls.IsNullOrEmpty())
				return;

			// Prompt for the number of rows
			var rown = model.Editor.GetInteger("Enter the number of rows for division:", 2);

			if (!rown.HasValue)
				return;

			// Prompt for the number of columns
			var clnn = model.Editor.GetInteger("Enter the number of columns for division:", 2);

			if (!clnn.HasValue)
				return;
			
			// Get values
			int
				row = rown.Value,
				cln = clnn.Value;

			// Get the panels and stringers to divide
			var pnlsToDivide = panels.GetByObjectIds(pnls.GetObjectIds())?.ToArray();

			if (pnlsToDivide.IsNullOrEmpty())
				return;

			// Remove non-rectangular panels
			var nonRecSelected = pnlsToDivide.Any(p => !p.Vertices.IsRectangular);

			if (nonRecSelected)
				pnlsToDivide = pnlsToDivide.Where(p => p.Vertices.IsRectangular).ToArray();

			// Verify if there is at least one panel to divide
			if (!pnlsToDivide.Any())
			{
				ShowAlertDialog("Please select at least one rectangular panel.");
				return;
			}

			// Get the panels divided
			var newPanels = pnlsToDivide.SelectMany(p => p.Divide(row, cln)).ToArray();

			// Get the stringers to divide and divide them
			var strsToDivide = new EList<StringerObject>();
			var newStrs      = new EList<StringerObject>();

			foreach (var pnl in pnlsToDivide)
			{
				var strEdges = stringers.GetFromPanelGeometry(pnl.Geometry).ToArray();

				if (strEdges.IsNullOrEmpty())
					continue;

				strsToDivide.AddRange(strEdges.Where(s => s is not null)!);

				// Divide by correct number
				for (var i = 0; i < 2; i++)
				{
					var j = 2 * i;

					var hor = strEdges[j]?.Divide(cln)?.Where(s => s is not null)?.ToArray();
					var ver = strEdges[j + 1]?.Divide(row)?.Where(s => s is not null)?.ToArray();

					if (!hor.IsNullOrEmpty())
						newStrs.AddRange(hor);

					if (!ver.IsNullOrEmpty())
						newStrs.AddRange(ver);
				}
			}

			// Erase result objects
			model.AcadDocument.EraseObjects(SPMResults.ResultLayers);

			// Add other stringers
			newStrs.AddRange(newPanels.SelectMany(p => p.Geometry.Edges.Select(e => new StringerObject(e.InitialVertex, e.FinalVertex, model.Database.BlockTableId))).ToArray());

			// Remove mid nodes
			nodes.RemoveRange(strsToDivide.Select(s => s.Geometry.CenterPoint).ToArray());

			// Erase the original elements
			var verts = pnlsToDivide.Select(p => p.Geometry.Vertices).ToList();
			var c     = panels.RemoveAll(p => verts.Contains(p.Vertices));
			stringers.RemoveRange(strsToDivide);

			// Add the elements
			panels.AddRange(newPanels);
			stringers.AddRange(newStrs);

			// Update nodes
			nodes.Update();

			// Move panels to bottom
			model.AcadDocument.MoveToBottom(panels.Select(p => p.ObjectId).ToList());

			// Show alert if there was a non-rectangular panel
			var message = (nonRecSelected
				              ? "Only rectangular panels were divided.\n\n"
				              : $"{c} panels divided.\n\n") +
			              " Set geometry to new internal stringers!";

			ShowAlertDialog(message);
		}

		/// <summary>
		///     Divide a stringer into new ones.
		/// </summary>
		[CommandMethod(CommandName.DivideStringer)]
		public static void DivideStringer()
		{
			// Get elements
			var model     = ActiveModel;
			var stringers = model.Stringers;
			var nodes     = model.Nodes;
			var database  = model.Database.AcadDatabase;

			// Prompt for select stringers
			var strs = database.GetStringers("Select stringers to divide")?.ToArray();

			if (strs.IsNullOrEmpty())
				return;

			// Prompt for the number of segments
			var numn = model.Editor.GetInteger("Enter the number of new stringers:", 2);

			if (!numn.HasValue)
				return;
			
			var num = numn.Value;

			// Get stringers from list
			var toDivide = stringers.GetByObjectIds(strs.GetObjectIds()!.ToArray())?.ToArray();

			if (toDivide.IsNullOrEmpty())
				return;

			// Erase result objects
			model.AcadDocument.EraseObjects(SPMResults.ResultLayers);

			// Remove mid nodes
			nodes.RemoveRange(toDivide.Select(s => s.Geometry.CenterPoint).ToArray());

			// Divide the stringers
			var newStrs = toDivide.SelectMany(s => s.Divide(num)).ToArray();

			// Erase the original stringers
			stringers.RemoveRange(toDivide);

			// Add the stringers
			stringers.AddRange(newStrs);

			// Update nodes
			nodes.Update();
		}

		/// <summary>
		///     Set geometry to a selection of panels.
		/// </summary>
		[CommandMethod(CommandName.EditPanel)]
		public static void EditPanel()
		{
			// Get model and database
			var model    = ActiveModel;
			var database = model.Database.AcadDatabase;

			// Request objects to be selected in the drawing area
			var pnls = database.GetPanels("Select the panels to assign properties (you can select other elements, the properties will be only applied to panels)")?.ToArray();

			if (pnls.IsNullOrEmpty())
				return;
			
			// Get the elements
			var panels = model.Panels.GetByObjectIds(pnls.GetObjectIds())!.ToList();
			
			// Start the config window
			SPMToolInterface.ShowWindow(new PanelWindow(panels));
		}

		/// <summary>
		///     Set geometry to a selection of stringers.
		/// </summary>
		[CommandMethod(CommandName.EditStringer)]
		public static void EditStringer()
		{
			// Get model and database
			var model    = ActiveModel;
			var database = model.Database.AcadDatabase;

			// Request objects to be selected in the drawing area
			var strs = database.GetStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)")?.ToArray();

			if (strs.IsNullOrEmpty())
				return;

			// Get the elements
			var stringers = ActiveModel.Stringers.GetByObjectIds(strs.GetObjectIds())!.ToList();
			
			// Start the config window
			SPMToolInterface.ShowWindow(new StringerWindow(stringers));
		}

		/// <summary>
		///     Update all the elements in the drawing.
		/// </summary>
		[CommandMethod(CommandName.UpdateElements)]
		public static void UpdateElements()
		{
			var models = OpenedModels;
			var model  = ActiveModel;
			
			model.UpdateElements();

			// Display the number of updated elements
			model.Editor.WriteMessage($"\n{model.Nodes.Count} nodes, {model.Stringers.Count} stringers and {model.Panels.Count} panels updated.");
			model.Editor.WriteMessage($"\n{model.Nodes.Count} nodes, {model.Stringers.Count} stringers and {model.Panels.Count} panels updated.");
		}

		#endregion

	}
}