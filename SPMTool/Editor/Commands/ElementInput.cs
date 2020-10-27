using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions.AutoCAD;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Editor.Commands;

[assembly: CommandClass(typeof(ElementInput))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Element input command class
    /// </summary>
    public static class ElementInput
    {
		/// <summary>
        /// Add a stringer to drawing.
        /// </summary>
	    [CommandMethod("AddStringer")]
	    public static void AddStringer()
	    {
		    // Get units
		    var units = UnitsData.SavedUnits;

		    // Get the list of start and endpoints
		    var strList = Stringers.StringerGeometries();

		    // Prompt for the start point of Stringer
		    var stPtn = UserInput.GetPoint("Enter the start point:");

		    if (stPtn is null)
			    return;

		    var stPt = stPtn.Value;

		    // Loop for creating infinite stringers (until user exits the command)
		    for ( ; ; )
		    {
			    // Create a point3d collection and add the Stringer start point
			    var nds = new List<Point3d> {stPt};

			    // Prompt for the start point of Stringer
			    var endPtn = UserInput.GetPoint("Enter the end point:", stPt);

			    if (endPtn is null)
				    // Finish command
				    break;

			    nds.Add(endPtn.Value);

			    // Get the points ordered in ascending Y and ascending X:
			    var extNds = nds.Order().ToArray();

			    // Create the Stringer and add to drawing
			    Stringers.Add(extNds[0], extNds[1]);

			    // Set the start point of the new Stringer
			    stPt = endPtn.Value;
		    }

		    // Update the nodes and stringers
		    Nodes.Update();
		    Stringers.Update(false);
	    }

		[CommandMethod("AddPanel")]
		public static void AddPanel()
		{
			// Read units
			var units = UnitsData.SavedUnits;

			// Create a loop for creating infinite panels
			for ( ; ; )
			{
				// Prompt for user select 4 vertices of the panel
				var nds = UserInput.SelectNodes("Select four nodes to be the vertices of the panel")?.ToArray();

				if (nds is null)
					break;

				// Check if there are four points
				if (nds.Length == 4)
					// Create the panel if it doesn't exist
					Panels.Add(nds.Select(nd => nd.Position).ToArray(), units.Geometry);

				else
					Application.ShowAlertDialog("Please select four external nodes.");
			}

			// Update panels
			Panels.Update(false);
		}
    }
}
