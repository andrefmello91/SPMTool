﻿using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions.AutoCAD;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Editor.Commands;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;


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
			    StringerList.Add(stPt, endPt);

			    // Set the start point of the new Stringer
			    stPt = endPt;
		    }

		    // Update the nodes and stringers
		    StringerList.Update();

			// Set old OSMODE
			SetSystemVariable("OSMODE", osmode);
	    }

		[CommandMethod("AddPanel")]
		public static void AddPanel()
		{
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
					PanelList.Add(nds.Select(nd => nd.Position).ToArray());

				else
					ShowAlertDialog("Please select four external nodes.");
			}

			// Update panels
			PanelList.Update(false);
		}
    }
}
