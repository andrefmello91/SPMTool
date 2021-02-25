using System.Linq;
using Autodesk.AutoCAD.Runtime;
using SPM.Elements;
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
	    [CommandMethod("AddStringer")]
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

			    if (!endPtn.HasValue)
				    // Finish command
				    break;

			    var endPt = endPtn.Value;

			    // Create the Stringer and add to drawing
			    Stringers.Add(stPt, endPt);

			    // Set the start point of the new Stringer
			    stPt = endPt;
		    }

			// Set old OSMODE
			SetSystemVariable("OSMODE", osmode);
	    }

		/// <summary>
		///		Add a panel to panel list and drawing.
		/// </summary>
		[CommandMethod("AddPanel")]
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
					// Create the panel if it doesn't exist
					Panels.Add(nds.Select(nd => nd.Position.ToPoint(unit)).ToArray());

				else
					ShowAlertDialog("Please select four external nodes.");
			}
		}
    }
}
