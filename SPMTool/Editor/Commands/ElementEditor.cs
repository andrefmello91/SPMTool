using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Editor.Commands;
using SPMTool.Extensions;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.Model;

[assembly: CommandClass(typeof(ElementEditor))]

namespace SPMTool.Editor.Commands
{
	/// <summary>
	///     Element editor command class.
	/// </summary>
	public static class ElementEditor
	{

		#region Methods

		[CommandMethod(CommandName.DividePanel)]
		public static void DividePanel()
		{
			// Prompt for select panels
			var pnls = UserInput.SelectPanels("Select panels to divide")?.ToArray();

			if (pnls.IsNullOrEmpty())
				return;

			// Prompt for the number of rows
			var rown = UserInput.GetInteger("Enter the number of rows for division:", 2);

			if (!rown.HasValue)
				return;

			// Prompt for the number of columns
			var clnn = UserInput.GetInteger("Enter the number of columns for division:", 2);

			if (!clnn.HasValue)
				return;

			// Get values
			int
				row = rown.Value,
				cln = clnn.Value;

			// Get the panels and stringers to divide
			var pnlsToDivide = Panels.GetByObjectIds(pnls.GetObjectIds())?.ToArray();

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
				var strEdges = Stringers.GetFromPanelGeometry(pnl.Geometry).ToArray();

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
			Results.ResultLayers.EraseObjects();

			// Add other stringers
			newStrs.AddRange(newPanels.SelectMany(p => p.Geometry.Edges.Select(e => new StringerObject(e.InitialVertex, e.FinalVertex))).ToArray());

			// Remove mid nodes
			Nodes.RemoveRange(strsToDivide.Select(s => s.Geometry.CenterPoint).ToArray());

			// Erase the original elements
			var verts = pnlsToDivide.Select(p => p.Geometry.Vertices).ToList();
			var c     = Panels.RemoveAll(p => verts.Contains(p.Vertices));
			Stringers.RemoveRange(strsToDivide);

			// Add the elements
			Panels.AddRange(newPanels);
			Stringers.AddRange(newStrs);

			// Update nodes
			Nodes.Update();

			Panels.Select(p => p.ObjectId).MoveToBottom();

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
			// Prompt for select stringers
			var strs = UserInput.SelectStringers("Select stringers to divide")?.ToArray();

			if (strs.IsNullOrEmpty())
				return;

			// Prompt for the number of segments
			var numn = UserInput.GetInteger("Enter the number of new stringers:", 2);

			if (!numn.HasValue)
				return;

			var num = numn.Value;

			// Get stringers from list
			var toDivide = Stringers.GetByObjectIds(strs.GetObjectIds().ToArray())?.ToArray();

			if (toDivide.IsNullOrEmpty())
				return;

			// Erase result objects
			Results.ResultLayers.EraseObjects();

			// Remove mid nodes
			Nodes.RemoveRange(toDivide.Select(s => s.Geometry.CenterPoint).ToArray());

			// Divide the stringers
			var newStrs = toDivide.SelectMany(s => s.Divide(num)).ToArray();

			// Erase the original stringers
			Stringers.RemoveRange(toDivide);

			// Add the stringers
			Stringers.AddRange(newStrs);

			// Update nodes
			Nodes.Update();
		}

		/// <summary>
		///     Set geometry to a selection of panels.
		/// </summary>
		[CommandMethod(CommandName.EditPanel)]
		public static void EditPanel()
		{
			// Request objects to be selected in the drawing area
			var pnls = UserInput.SelectPanels("Select the panels to assign properties (you can select other elements, the properties will be only applied to panels)")?.ToArray();

			if (pnls.IsNullOrEmpty())
				return;

			// Start the config window
			var geoWindow = new PanelWindow(Panels.GetByObjectIds(pnls.GetObjectIds())!);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
		///     Set geometry to a selection of stringers.
		/// </summary>
		[CommandMethod(CommandName.EditStringer)]
		public static void EditStringer()
		{
			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)")?.ToArray();

			if (strs.IsNullOrEmpty())
				return;

			// Start the config window
			var geoWindow = new StringerWindow(Stringers.GetByObjectIds(strs.GetObjectIds())!);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
		///     Update all the elements in the drawing.
		/// </summary>
		[CommandMethod(CommandName.UpdateElements)]
		public static void UpdateElements()
		{
			Model.UpdateElements();

			// Display the number of updated elements
			Model.Editor.WriteMessage($"\n{Nodes.Count} nodes, {Stringers.Count} stringers and {Panels.Count} panels updated.");
		}

		#endregion

	}
}