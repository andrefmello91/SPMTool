using System.Linq;
using Autodesk.AutoCAD.Runtime;
using andrefmello91.SPMElements;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Editor.Commands;
using SPMTool.Extensions;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.DataBase;
using static SPMTool.Core.Model;


[assembly: CommandClass(typeof(ElementInput))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Element input command class
    /// </summary>
    public static class ElementInput
    {
		/// <summary>
        ///		Add a stringer to to stringer list and drawing.
        /// </summary>
	    [CommandMethod(CommandName.AddStringer)]
	    public static void AddStringer()
	    {
			// Get current OSMODE
			var osmode = GetSystemVariable("OSMODE");

			// Set OSMODE only to end point and node
			SetSystemVariable("OSMODE", 9);

		    // Prompt for the start point of Stringer
		    var stPtn = UserInput.GetPoint("Enter the start point:");

		    if (stPtn is null)
			    return;

		    var stPt = stPtn.Value;

		    // Loop for creating infinite stringers (until user exits the command)
		    for ( ; ; )
		    {
			    // Prompt for the start point of Stringer
			    var endPtn = UserInput.GetPoint("Enter the end point:", stPt);

			    if (endPtn is null)
				    // Finish command
				    break;

			    var endPt = endPtn.Value;

			    var pts = new[] {stPt, endPt}.OrderBy(p => p).ToArray();

			    // Create the Stringer and add to drawing
			    Stringers.Add(pts[0], pts[1]);

			    // Set the start point of the new Stringer
			    stPt = endPt;
		    }

			// Set old OSMODE
			SetSystemVariable("OSMODE", osmode);

			// Update nodes
			Nodes.Update();
	    }

		/// <summary>
		///		Add a panel to panel list and drawing.
		/// </summary>
		[CommandMethod(CommandName.AddPanel)]
		public static void AddPanel()
		{
			var unit = DataBase.Settings.Units.Geometry;

			// Create a loop for creating infinite panels
			for ( ; ; )
			{
				// Prompt for user select 4 vertices of the panel
				var nds = UserInput.SelectNodes("Select four nodes to be the vertices of the panel", NodeType.External)?.ToArray();

				if (nds is null)
					return;

				// Check if there are four points
				if (nds.Length == 4)
				{
					Panels.Add(nds.Select(nd => nd.Position.ToPoint(unit)).ToArray());
					continue;
				}

				// Move panels to bottom
				Panels.Select(p => p.ObjectId).MoveToBottom();

				ShowAlertDialog("Please select four external nodes.");
				//foreach (var id in nds.GetObjectIds().ToArray())
				//	Model.Editor.WriteMessage($"\n{Nodes.GetByObjectId(id)}");
			}
		}
    }
}
