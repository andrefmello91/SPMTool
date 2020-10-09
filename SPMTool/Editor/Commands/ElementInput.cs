using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions.AutoCAD;
using SPM.Elements;
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
		    var units = DataBase.Units;

		    // Get the list of start and endpoints
		    var strList = Stringers.StringerGeometries();

		    // Create lists of points for adding the nodes later
		    List<Point3d> newIntNds = new List<Point3d>(),
			    newExtNds = new List<Point3d>();

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
			    var extNds = nds.Order().ToList();

			    // Create the Stringer and add to drawing
			    Stringers.Add(extNds[0], extNds[1], ref strList);

			    // Get the midpoint
			    var midPt = extNds[0].MidPoint(extNds[1]);

			    // Add the position of the nodes to the list
			    if (!newExtNds.Contains(extNds[0]))
				    newExtNds.Add(extNds[0]);

			    if (!newExtNds.Contains(extNds[1]))
				    newExtNds.Add(extNds[1]);

			    if (!newIntNds.Contains(midPt))
				    newIntNds.Add(midPt);

			    // Set the start point of the new Stringer
			    stPt = endPtn.Value;
		    }

		    // Create the nodes
		    Nodes.Add(newExtNds, NodeType.External);
		    Nodes.Add(newIntNds, NodeType.Internal);

		    // Update the nodes and stringers
		    Nodes.Update(units.Geometry);
		    Stringers.UpdateStringers();
	    }
    }
}
