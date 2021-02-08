using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions.AutoCAD;
using Extensions.Number;
using MathNet.Numerics;
using SPM.Elements.PanelProperties;
using SPM.Elements.StringerProperties;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Editor.Commands;
using SPMTool.Enums;
using SPMTool.UserInterface;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(ElementEditor))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Element editor command class.
    /// </summary>
    public static class ElementEditor
    {
		/// <summary>
        /// Divide a stringer into new ones.
        /// </summary>
	    [CommandMethod("DivideStringer")]
	    public static void DivideStringer()
	    {
		    // Prompt for select stringers
		    var strs = UserInput.SelectStringers("Select stringers to divide")?.ToArray();

		    if (strs is null)
			    return;

		    // Prompt for the number of segments
		    var numn = UserInput.GetInteger("Enter the number of new stringers:", 2);

		    if (!numn.HasValue)
			    return;

		    int num = numn.Value;

			// Divide the stringers
			var newStrs = new List<Line>();

			foreach (var str in strs)
				newStrs.AddRange(str.Divide(num));

			// Erase the original stringers
			Stringers.Remove(strs);

			// Add the stringers
			Stringers.Add(newStrs);

			// Create the nodes and update stringers
			Stringers.Update();

			//foreach (var str in strs)
			//         {
			//          // Get the coordinates of the initial and end points
			//          Point3d
			//           strSt  = str.StartPoint,
			//           strEnd = str.EndPoint;

			//          // Calculate the distance of the points in X and Y
			//          double
			//           distX = strEnd.DistanceInX(strSt) / num,
			//           distY = strEnd.DistanceInY(strSt) / num;

			//          // Initialize the start point
			//          var stPt = strSt;

			//          // Get the midpoint
			//          var midPt = strSt.MidPoint(strEnd);

			//          // Read the internal nodes to erase
			//          ndsToErase.AddRange(intNds.Where(nd => nd.Position.Approx(midPt)));

			//          // Create the new stringers
			//          for (int i = 1; i <= num; i++)
			//          {
			//           // Get the coordinates of the other points
			//           double
			//            xCrd = str.StartPoint.X + i * distX,
			//            yCrd = str.StartPoint.Y + i * distY;

			//           var endPt = new Point3d(xCrd, yCrd, 0);

			//           // Create the Stringer
			//           Stringers.Add(stPt, endPt);

			//           // Set the start point of the next Stringer
			//           stPt = endPt;
			//          }

			//          // Remove from the list
			//          var strList = stringerCollection.ToList();
			//          strList.Remove(new StringerGeometry(strSt, strEnd, 0, 0));
			//          stringerCollection = strList;
			//         }
		}

		[CommandMethod("DividePanel")]
		public static void DividePanel()
		{
			// Get units
			var units = SettingsData.SavedUnits;

			// Prompt for select panels
			var pnls = UserInput.SelectPanels("Select panels to divide")?.ToArray();

			if (pnls is null || !pnls.Any())
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

			// Get the list of start and endpoints
			var geoList = Stringers.Geometries;

			// Get the list of panels
			var pnlList = Panels.VerticesList;

			// Create a list of lines for adding the stringers later
			var newStrList = new List<Line>();

			// Auxiliary rectangular panel error
			var error = false;

            // Create a collection of stringers and nodes to erase
            var toErase = new List<DBObject>();

			// Access the stringers in the model
			var strs = Model.StringerCollection;

            // Access the internal nodes in the model
            var intNds = Model.IntNodeCollection;

			// Get the selection set and analyze the elements
			foreach (var pnl in pnls)
			{
				// Get vertices
				var verts = pnl.GetVertices().ToArray();

				// Get panel geometry
				var geometry = new PanelGeometry(verts, 0, units.Geometry);

				// Verify if the panel is rectangular
				if (geometry.Rectangular) // panel is rectangular
				{
					// Get the surrounding stringers to erase
					foreach (var str in strs)
					{
						// Verify if the Stringer starts and ends in a panel vertex
						if (!verts.Contains(str.StartPoint) || !verts.Contains(str.EndPoint))
							continue;

                        // Add the internal nodes for erasing
                        toErase.AddRange(intNds.Where(nd => nd.Position.Approx(str.MidPoint())));

						// Erase and remove from the list
						toErase.Add(str);
						geoList.Remove(new StringerGeometry(str.StartPoint, str.EndPoint, 0, 0));
					}

					// Calculate the distance of the points in X and Y
					double
						distX = (geometry.Edge1.Length / cln).ConvertFromMillimeter(units.Geometry),
						distY = (geometry.Edge2.Length / row).ConvertFromMillimeter(units.Geometry);

					// Initialize the start point
					var stPt = verts[0];

					// Create the new panels
					for (int i = 0; i < row; i++)
					{
						for (int j = 0; j < cln; j++)
						{
							// Get the vertices of the panel and add to a list
							var newVerts = new[]
							{
								new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
								new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0),
								new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0),
								new Point3d(stPt.X + (j + 1) * distX, stPt.Y + (i + 1) * distY, 0)
							};

							// Create the panel with XData of the original panel
							Panels.Add(newVerts);

							// Create tuples to adding the stringers later
							var strsToAdd = new[]
							{
								new StringerGeometry(newVerts[0], newVerts[1], 0, 0), 
								new StringerGeometry(newVerts[0], newVerts[2], 0, 0),
								new StringerGeometry(newVerts[2], newVerts[3], 0, 0),
								new StringerGeometry(newVerts[1], newVerts[3], 0, 0)
							};

							// Add to the list of new stringers
							newStrList.AddRange(strsToAdd.Where(geo => !geoList.Contains(geo)).Select(geo => new Line(geo.InitialPoint, geo.EndPoint)));
						}
					}

					// Add to objects to erase
					toErase.Add(pnl);

					// Remove from the list
					var list = pnlList.ToList();
					list.Remove(geometry.Vertices);
					pnlList = list;
				}

				else // panel is not rectangular
				{
					error = true;
					break;
				}
			}

			if (error)
			{
				Application.ShowAlertDialog("\nAt least one selected panel is not rectangular.");
				return;
			}

			// Erase objects
			toErase.RemoveFromDrawing();

			// Create the stringers
			foreach (var str in newStrList)
				Stringers.Add(str);

			// Create the nodes and update elements
			Nodes.Update();
			Stringers.Update(false);
			Panels.Update(false);

			// Show an alert for editing stringers
			Application.ShowAlertDialog("Alert: stringers and panels' parameters must be set again.");
		}

		/// <summary>
		/// Set geometry to a selection of stringers.
		/// </summary>
		[CommandMethod("SetStringerGeometry")]
		public static void SetStringerGeometry()
		{
			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)")?.ToArray();

			if (strs is null || !strs.Any())
				return;

			// Start the config window
			var geoWindow = new StringerWindow(strs);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
        /// Set geometry to a selection of panels.
        /// </summary>
        [CommandMethod("SetPanelGeometry")]
		public static void SetPanelGeometry()
		{
			// Request objects to be selected in the drawing area
			var pnls = UserInput.SelectPanels("Select the panels to assign properties (you can select other elements, the properties will be only applied to panels)")?.ToArray();

			if (pnls is null || !pnls.Any())
				return;

			// Start the config window
			var geoWindow = new PanelWindow(pnls);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
		/// Set the reinforcement in a collection of stringers.
		/// </summary>
		[CommandMethod("SetStringerReinforcement")]
		public static void SetStringerReinforcement()
		{
			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers).")?.ToArray();

			if (strs is null || !strs.Any())
				return;

			// Start the config window
			var geoWindow = new StringerWindow(strs);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

        /// <summary>
        /// Set reinforcement to a collection of panels.
        /// </summary>
        [CommandMethod("SetPanelReinforcement")]
		public static void SetPanelReinforcement()
		{
			// Request objects to be selected in the drawing area
			var pnls = UserInput.SelectPanels("Select the panels to assign reinforcement (you can select other elements, the properties will be only applied to panels).")?.ToArray();

			if (pnls is null || !pnls.Any())
				return;

			// Start the config window
			var geoWindow = new PanelWindow(pnls);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

        /// <summary>
        /// Update all the elements in the drawing.
        /// </summary>
        [CommandMethod("UpdateElements")]
		public static void UpdateElements()
		{
			Model.UpdateElements();

			// Enumerate and get the number of nodes
			var nds = Model.NodeCollection;

			// Update and get the number of stringers
			var strs = Model.StringerCollection;

			// Update and get the number of panels
			var pnls = Model.PanelCollection;

			// Display the number of updated elements
			Model.Editor.WriteMessage($"\n{nds.Length} nodes, {strs.Length} stringers and {pnls.Length} panels updated.");
		}
    }
}
