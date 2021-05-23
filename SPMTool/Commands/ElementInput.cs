using System.Linq;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;

using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.SPMModel;

namespace SPMTool.Commands
{
	/// <summary>
	///     Element input command class
	/// </summary>
	public static partial class AcadCommands
	{

		#region Methods

		/// <summary>
		///     Add a panel to panel list and drawing.
		/// </summary>
		[CommandMethod(CommandName.AddPanel)]
		public static void AddPanel()
		{
			var model  = ActiveModel;
			var panels = model.Panels;
			var unit = model.Settings.Units.Geometry;

			// Erase result objects
			model.AcadDocument.EraseObjects(SPMResults.ResultLayers);

			// Create a loop for creating infinite panels
			while (true)
			{
				// Prompt for user select 4 vertices of the panel
				var nds =  model.AcadDatabase.GetNodes("Select four nodes to be the vertices of the panel", NodeType.External)?.ToArray();

				if (nds is null)
					goto Finish;

				// Check if there are four points
				if (nds.Length == 4)
				{
					panels.Add(nds.Select(nd => nd.Position.ToPoint(unit)).ToArray());
					continue;
				}

				ShowAlertDialog("Please select four external nodes.");
			}

			Finish:

			// Move panels to bottom
			model.AcadDocument.MoveToBottom(panels.Select(p => p.ObjectId).ToList());
		}

		/// <summary>
		///     Add a stringer to to stringer list and drawing.
		/// </summary>
		[CommandMethod(CommandName.AddStringer)]
		public static void AddStringer()
		{
			// Get current OSMODE
			var osmode = GetSystemVariable("OSMODE");

			// Set OSMODE only to end point and node
			SetSystemVariable("OSMODE", 9);

			// Get elements
			var model     = ActiveModel;
			var stringers = model.Stringers;
			var nodes     = model.Nodes;
			var unit      = model.Settings.Units.Geometry;
			
			// Prompt for the start point of Stringer
			var stPtn = model.Editor.GetPoint("Enter the start point:", unit: unit);

			if (stPtn is null)
				return;
			
			var stPt = stPtn.Value;

			// Erase result objects
			model.AcadDocument.EraseObjects(SPMResults.ResultLayers);

			// Loop for creating infinite stringers (until user exits the command)
			while (true)
			{
				// Prompt for the start point of Stringer
				var endPtn = model.Editor.GetPoint("Enter the end point:", stPt, unit);

				if (endPtn is null)
					// Finish command
					goto Finish;

				var endPt = endPtn.Value;

				var pts = new[] { stPt, endPt }.OrderBy(p => p).ToArray();

				// Create the Stringer and add to drawing
				stringers.Add(pts[0], pts[1]);

				// Set the start point of the new Stringer
				stPt = endPt;
			}

			Finish:
			{
				// Set old OSMODE
				SetSystemVariable("OSMODE", osmode);

				// Update nodes
				nodes.Update();
			}
		}

		#endregion

	}
}